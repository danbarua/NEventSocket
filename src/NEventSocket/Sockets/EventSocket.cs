// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventSocket.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Sockets
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Sockets;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
    using NEventSocket.Util;
    using NEventSocket.Util.ObjectPooling;

    using AsyncLock = Util.AsyncLock;

    /// <summary>
    /// Base class providing common functionality shared between an <seealso cref="InboundSocket"/> and an <seealso cref="OutboundSocket"/>.
    /// </summary>
    public abstract class EventSocket : ObservableSocket
    {
        private readonly ILog Log;

        // minimum events required for this class to do its job
        private readonly HashSet<EventName> subscribedEvents = new HashSet<EventName>();

        private readonly HashSet<string> customEvents = new HashSet<string>();

        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        private readonly AsyncLock gate = new AsyncLock();

        private readonly IObservable<BasicMessage> messages;

        private readonly InterlockedBoolean disposed = new InterlockedBoolean();

        /// <summary>
        /// Instantiates an <see cref="EventSocket"/> instance wrapping the provided <seealso cref="TcpClient"/>
        /// </summary>
        /// <param name="tcpClient">A TcpClient.</param>
        /// <param name="responseTimeOut">(Optional) The response timeout.</param>
        protected EventSocket(TcpClient tcpClient, TimeSpan? responseTimeOut = null) : base(tcpClient)
        {
            Log = LogProvider.GetLogger("{0} ({1})".Fmt(GetType(), Id));

            ResponseTimeOut = responseTimeOut ?? TimeSpan.FromSeconds(5);

            messages =
                Receiver.SelectMany(x => Encoding.UTF8.GetString(x))
                    .AggregateUntil(() => new Parser(), (builder, ch) => builder.Append(ch), builder => builder.Completed)
                    .Select(builder => builder.ExtractMessage())
                    .Do(
                        x => Log.Trace("Messages Received [{0}].".Fmt(x.ContentType)),
                        ex => { },
                        () =>
                        {
                            Log.Debug(() => "Messages Observable completed.");
                            Dispose();
                        })
                    .ObserveOn(Scheduler.Immediate)
                    .Publish()
                    .RefCount();
                        

            Log.Trace(() => "EventSocket initialized");
        }

        public long Id
        {
            get { return id; }
        }

        /// <summary>
        /// Gets or sets the TimeOut after which the socket will throw a <seealso cref="TimeoutException"/>.
        /// </summary>
        [Obsolete("This is due to be removed.")]
        public TimeSpan ResponseTimeOut { get; set; }

        /// <summary>
        /// Gets an observable sequence of <seealso cref="BasicMessage"/>.
        /// </summary>
        public IObservable<BasicMessage> Messages
        {
            get
            {
                return messages.AsObservable();
            }
        }

        /// <summary>
        /// Gets an observable sequence of <seealso cref="EventMessage"/>.
        /// </summary>
        public IObservable<EventMessage> Events
        {
            get
            {
                return Messages
                                .Where(x => x.ContentType == ContentTypes.EventPlain)
                                .Select(x => new EventMessage(x));
            }
        }

        /// <summary>
        /// Gets an observable sequence of <seealso cref="ChannelEvent"/>.
        /// </summary>
        public IObservable<ChannelEvent> ChannelEvents
        {
            get
            {
                return Events.Where(x => x.Headers.ContainsKey(HeaderNames.UniqueId))
                             .Select(x => new ChannelEvent(x));
            }
        }

        /// <summary>
        /// Gets an observable sequence of <seealso cref="ConferenceEvent"/>.
        /// </summary>
        public IObservable<ConferenceEvent> ConferenceEvents
        {
            get
            {
                return
                    Events.Where(x => x.EventName == EventName.Custom && x.Headers[HeaderNames.EventSubclass] == CustomEvents.Conference.Maintainence)
                          .Select(x => new ConferenceEvent(x));
            }
        }

        /// <summary>
        /// Send an api command (blocking mode)
        /// </summary>
        /// <remarks>
        /// See https://freeswitch.org/confluence/display/FREESWITCH/mod_event_socket#mod_event_socket-api
        /// </remarks>
        /// <param name="command">The API command to send (see https://wiki.freeswitch.org/wiki/Mod_commands) </param>
        /// <returns>A Task of <seealso cref="ApiResponse"/>.</returns>
        public async Task<ApiResponse> SendApi(string command)
        {

            using (var asyncLock = await gate.LockAsync().ConfigureAwait(false))
            {
                Log.Trace(() => "Sending [api {0}]".Fmt(command));
                var tcs = new TaskCompletionSource<ApiResponse>();
                var subscriptions = new CompositeDisposable { cts.Token.Register(() => tcs.TrySetCanceled()) };

                subscriptions.Add(
                    Messages.Where(x => x.ContentType == ContentTypes.ApiResponse)
                            .Take(1)
                            .Select(x => new ApiResponse(x))
                            .Do(
                                m =>
                                {
                                    var logLevel = m.Success ? LogLevel.Debug : LogLevel.Error;

                                    if (m.Success && command.StartsWith("uuid_dump"))
                                    {
                                        //we don't need to dump the entire response to the logs
                                        Log.Log(logLevel, () => "ApiResponse received CHANNEL_DATA for [{0}]".Fmt(command));
                                    }
                                    else
                                    {
                                        Log.Log(
                                            logLevel,
                                            () => "ApiResponse received [{0}] for [{1}]".Fmt(m.BodyText.Replace("\n", string.Empty), command));
                                    }
                                },
                                ex => Log.ErrorException("Error waiting for Api Response to [{0}].".Fmt(command), ex))
                                .Subscribe(x => tcs.TrySetResult(x), ex => tcs.TrySetException(ex), subscriptions.Dispose));

                await
                    SendAsync(Encoding.ASCII.GetBytes("api " + command + "\n\n"), cts.Token)
                        .ContinueOnFaultedOrCancelled(tcs, subscriptions.Dispose)
                        .ConfigureAwait(false);

                return await tcs.Task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Send an event socket command
        /// </summary>
        /// <remarks>
        /// See https://freeswitch.org/confluence/display/FREESWITCH/mod_event_socket#mod_event_socket-api
        /// </remarks>
        /// <param name="command">The command to send.</param>
        /// <returns>A Task of <seealso cref="CommandReply"/>.</returns>
        public async Task<CommandReply> SendCommand(string command)
        {
            using (var asyncLock = await gate.LockAsync().ConfigureAwait(false))
            {
                Log.Trace(() => "Sending [{0}]".Fmt(command));
                var tcs = new TaskCompletionSource<CommandReply>();
                var subscriptions = new CompositeDisposable { cts.Token.Register(() => tcs.TrySetCanceled()) };

                subscriptions.Add(
                    Messages.Where(x => x.ContentType == ContentTypes.CommandReply)
                            .Take(1)
                            .Select(x => new CommandReply(x))
                            .Do(
                                result =>
                                    {
                                        var logLevel = result.Success ? LogLevel.Debug : LogLevel.Error;
                                        Log.Log(logLevel,
                                            () =>
                                            "CommandReply received [{0}] for [{1}]".Fmt(
                                                result.ReplyText.Replace("\n", string.Empty),
                                                command));
                                    },
                                ex => Log.ErrorException("Error waiting for Command Reply to [{0}].".Fmt(command), ex))
                            .Subscribe(x => tcs.TrySetResult(x), ex => tcs.TrySetException(ex), subscriptions.Dispose));

                await
                    SendAsync(Encoding.ASCII.GetBytes(command + "\n\n"), cts.Token)
                        .ContinueOnFaultedOrCancelled(tcs, subscriptions.Dispose)
                        .ConfigureAwait(false);

                return await tcs.Task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously executes a dialplan application on the given channel.
        /// </summary>
        /// <remarks>
        /// See https://freeswitch.org/confluence/display/FREESWITCH/mod_dptools
        /// </remarks>
        /// <param name="uuid">The channel UUID.</param>
        /// <param name="application">The dialplan application to execute.</param>
        /// <param name="applicationArguments">(Optional) arguments to pass to the application.</param>
        /// <param name="eventLock">(Default: false) Whether to block the socket until the application completes before processing further.
        ///  (see https://wiki.freeswitch.org/wiki/Event_Socket_Outbound#Q:_Ordering_and_async_keyword )</param>
        /// <param name="async">(Default: false) Whether to return control from the application immediately. 
        /// (see https://wiki.freeswitch.org/wiki/Event_Socket_Outbound#Q:_Should_I_use_sync_mode_or_async_mode.3F)
        /// </param>
        /// <param name="loops">(Optional) How many times to repeat the application.</param>
        /// <returns>
        /// A Task of <seealso cref="EventMessage"/> that wraps the ChannelExecuteComplete event if the application completes successfully.
        /// The Task result will be null if the application did not execute, for example, the socket disconnected or the channel was hung up.
        /// </returns>
        public Task<ChannelEvent> ExecuteApplication(
            string uuid, string application, string applicationArguments = null, bool eventLock = false, bool async = false, int loops = 1)
        {
            if (uuid == null)
            {
                throw new ArgumentNullException("uuid");
            }

            if (application == null)
            {
                throw new ArgumentNullException("application");
            }

            //lists.freeswitch.org/pipermail/freeswitch-users/2013-May/095329.html
            var applicationUUID = Guid.NewGuid().ToString();

            var sb = StringBuilderPool.Allocate();
            sb.AppendFormat("sendmsg {0}\nEvent-UUID: {1}\ncall-command: execute\nexecute-app-name: {2}\n", uuid, applicationUUID, application);

            if (eventLock)
            {
                sb.Append("event-lock: true\n");
            }

            if (loops > 1)
            {
                sb.Append("loops: " + loops + "\n");
            }

            if (async)
            {
                sb.Append("async: true\n");
            }

            if (applicationArguments != null)
            {
                sb.AppendFormat("content-type: text/plain\ncontent-length: {0}\n\n{1}\n", applicationArguments.Length, applicationArguments);
            }

            var tcs = new TaskCompletionSource<ChannelEvent>();
            var subscriptions = new CompositeDisposable();

            if (cts.Token.CanBeCanceled)
            {
                subscriptions.Add(cts.Token.Register(() => tcs.TrySetCanceled()));
            }

            subscriptions.Add(
                ChannelEvents.Where(
                    x => x.EventName == EventName.ChannelExecuteComplete && x.Headers["Application-UUID"] == applicationUUID)
                    .Take(1)
                    .Subscribe(
                        executeCompleteEvent =>
                            {
                                if (executeCompleteEvent != null)
                                {
                                    Log.Debug(
                                        () =>
                                        "{0} ChannelExecuteComplete [{1} {2} {3}]".Fmt(
                                            executeCompleteEvent.UUID,
                                            executeCompleteEvent.AnswerState,
                                            executeCompleteEvent.Headers[HeaderNames.Application],
                                            executeCompleteEvent.Headers[HeaderNames.ApplicationResponse]));
                                }
                                else
                                {
                                    Log.Trace(() => "No ChannelExecuteComplete event received for {0}".Fmt(application));
                                }

                                tcs.TrySetResult(executeCompleteEvent);
                            },
                            ex => tcs.TrySetException(ex),
                            subscriptions.Dispose));

            SubscribeEvents(EventName.ChannelExecuteComplete).ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    SendCommand(StringBuilderPool.ReturnAndFree(sb))
                        .Then(reply =>
                        {
                            if (!reply.Success)
                            {
                                tcs.TrySetResult(null);
                            }
                        })
                        .ContinueOnFaultedOrCancelled(tcs, subscriptions.Dispose);
                }
                else
                {
                    tcs.TrySetException(t.Exception);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Send an api command (non-blocking mode) this will let you execute a job in the background and the result will be sent as an event with an indicated uuid to match the reply to the command)
        /// </summary>
        /// <remarks>
        /// See https://freeswitch.org/confluence/display/FREESWITCH/mod_event_socket#mod_event_socket-bgapi
        /// </remarks>
        /// <param name="command">The command to execute.</param>
        /// <param name="arg">(Optional) command argument.</param>
        /// <param name="jobUUID">(Optional) job unique identifier.</param>
        /// <returns>A Task of <seealso cref="BackgroundJobResult"/>.</returns>
        public Task<BackgroundJobResult> BackgroundJob(string command, string arg = null, Guid? jobUUID = null)
        {
            if (jobUUID == null)
            {
                jobUUID = Guid.NewGuid();
            }

            var backgroundApiCommand = arg != null
                                           ? "bgapi {0} {1}\nJob-UUID: {2}".Fmt(command, arg, jobUUID)
                                           : "bgapi {0}\nJob-UUID: {1}".Fmt(command, jobUUID);

            var tcs = new TaskCompletionSource<BackgroundJobResult>();
            var subscriptions = new CompositeDisposable();

            if (cts.Token.CanBeCanceled)
            {
                subscriptions.Add(
                    cts.Token.Register(() => tcs.TrySetCanceled()));
            }

            subscriptions.Add(
                Events.Where(x => x.EventName == EventName.BackgroundJob && x.Headers[HeaderNames.JobUUID] == jobUUID.ToString())
                        .Take(1)
                        .Select(x => new BackgroundJobResult(x))
                        .Do(result => Log.Debug(() => "BackgroundJobResult received [{0}] for [{1}]".Fmt(result.BodyText.Replace("\n", string.Empty), command)),
                            ex => Log.ErrorException("Error waiting for BackgroundJobResult Reply to [{0}].".Fmt(command), ex))
                        .Subscribe(x => tcs.TrySetResult(x), ex => tcs.TrySetException(ex), subscriptions.Dispose));

            SubscribeEvents(EventName.BackgroundJob).ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    SendCommand(backgroundApiCommand).ContinueOnFaultedOrCancelled(tcs, subscriptions.Dispose);
                }
                else
                {
                    tcs.TrySetException(t.Exception);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Bridge a new channel to the existing one. Generally used to route an incoming call to one or more endpoints.
        /// </summary>
        /// <remarks>
        /// See https://freeswitch.org/confluence/display/FREESWITCH/bridge
        /// </remarks>
        /// <param name="uuid">The UUID of the channel to bridge (the A-Leg).</param>
        /// <param name="endpoint">The destination to dial.</param>
        /// <param name="options">(Optional) Any <seealso cref="BridgeOptions"/> to configure the bridge.</param>
        /// <returns>A Task of <seealso cref="BridgeResult"/>.</returns>
        public async Task<BridgeResult> Bridge(string uuid, string endpoint, BridgeOptions options = null)
        {
            if (options == null)
            {
                options = new BridgeOptions();
            }

            if (string.IsNullOrEmpty(options.UUID))
            {
                options.UUID = Guid.NewGuid().ToString();
            }

            var bridgeString = string.Format("{0}{1}", options, endpoint);

            // some bridge options need to be set in channel vars
            if (options.ChannelVariables.Any())
            {
                await
                    this.SetMultipleChannelVariables(
                        uuid, options.ChannelVariables.Select(kvp => kvp.Key + "='" + kvp.Value + "'").ToArray()).ConfigureAwait(false);
            }

            /* If the bridge fails to connect we'll get a CHANNEL_EXECUTE_COMPLETE event with a failure message and the Execute task will complete.
             * If the bridge succeeds, that event won't arrive until after the bridged leg hangs up and completes the call.
             * In this case, we want to return a result as soon as the b-leg picks up and connects so we'll merge with the CHANNEL_BRIDGE event
             * observable.Amb(otherObservable) will propogate the first sequence to produce a result. */

            await SubscribeEvents(EventName.ChannelBridge, EventName.ChannelHangup).ConfigureAwait(false);

            var bridgedOrHungupEvent =
                ChannelEvents.FirstOrDefaultAsync(x => x.UUID == uuid && (x.EventName == EventName.ChannelBridge || x.EventName == EventName.ChannelHangup))
                    .Do(
                        e =>
                            {
                                if (e != null)
                                {
                                    switch (e.EventName)
                                    {
                                        case EventName.ChannelBridge:
                                            Log.Debug(() => "Bridge [{0} - {1}] complete - {2}".Fmt(uuid, options.UUID, e.Headers[HeaderNames.OtherLegUniqueId]));
                                            break;
                                        case EventName.ChannelHangup:
                                            Log.Debug(() => "Bridge [{0} - {1}]  aborted, channel hangup [{2}]".Fmt(uuid, options.UUID, e.Headers[HeaderNames.HangupCause]));
                                            break;
                                    }
                                }
                            });

            return
                await
                ExecuteApplication(uuid, "bridge", bridgeString)
                    .ToObservable()
                    .Amb(bridgedOrHungupEvent)
                    .Select(x => new BridgeResult(x))
                    .ToTask()
                    .ConfigureAwait(false);
        }

        /// <summary>
        /// Requests FreeSwitch shuts down the socket
        /// </summary>
        public Task Exit()
        {
            // we're not using the CancellationToken here because we want to wait until the reply comes back
            var command = "exit";

            Log.Trace(() => "Sending [{0}]".Fmt(command));

            lock (gate)
            {
                var tcs = new TaskCompletionSource<BasicMessage>();
                var subscriptions = new CompositeDisposable();

                subscriptions.Add(
                    Messages.Where(x => x.ContentType == ContentTypes.CommandReply)
                            .Take(1)
                            .Select(x => new CommandReply(x))
                            .Subscribe(
                                result =>
                                Log.Debug(
                                    () => "CommandReply received [{0}] for [{1}]".Fmt(result.ReplyText.Replace("\n", string.Empty), command)),
                                ex =>
                                    {
                                        Log.ErrorException("Error waiting for Command Reply to [{0}].".Fmt(command), ex);
                                        tcs.TrySetException(ex);
                                    }));

                subscriptions.Add(
                    Messages.Where(x => x.ContentType == ContentTypes.DisconnectNotice)
                            .Take(1)
                            .Timeout(
                                TimeSpan.FromSeconds(2),
                                Observable.Throw<BasicMessage>(new TimeoutException("No Disconnect Notice received.")))
                            .Subscribe(
                                x =>
                                    {
                                        Log.Debug(() => "Disconnect Notice received [{0}]".Fmt(x.BodyText));
                                        tcs.TrySetResult(x);
                                    },
                                ex =>
                                    {
                                        Log.ErrorException("Error waiting for Disconnect Notice", ex);
                                        if (ex is TimeoutException)
                                        {
                                            tcs.TrySetResult(null);
                                        }
                                        else
                                        {
                                            tcs.TrySetException(ex);
                                        }
                                    },
                                () =>
                                    {
                                        subscriptions.Dispose();
                                        tcs.TrySetResult(null);
                                    }));

                SendAsync(Encoding.ASCII.GetBytes(command + "\n\n"), CancellationToken.None)
                    .ContinueOnFaultedOrCancelled(tcs, subscriptions.Dispose);

                return tcs.Task;
            }
        }

        /// <summary>
        /// Subscribes this EventSocket to one or more events.
        /// </summary>
        /// <param name="events">The <seealso cref="EventName"/>s to subscribe to.</param>
        /// <returns>A Task.</returns>
        /// <remarks>This is additive - additional calls to .SubscribeEvents will add more event subscriptions.</remarks>
        public Task SubscribeEvents(params EventName[] events)
        {
            if (!events.All(@event => subscribedEvents.Contains(@event)))
            {
                subscribedEvents.UnionWith(events);
                return EnsureEventsSubscribed();
            }

            return TaskHelper.Completed;
        }

        /// <summary>
        /// Subscribes this EventSocket to one or more custom events.
        /// </summary>
        /// <param name="events">The custom event names to subscribe to.</param>
        /// <returns>A Task.</returns>
        public Task SubscribeCustomEvents(params string[] events)
        {
            if (!events.All(@event => customEvents.Contains(@event)))
            {
                customEvents.UnionWith(events);
                return EnsureEventsSubscribed();
            }

            return TaskHelper.Completed;
        }

        /// <summary>
        /// Register a callback to be invoked when the given Channel UUID hangs up.
        /// </summary>
        /// <param name="uuid">The Channel UUID.</param>
        /// <param name="action">A Callback to be invoked on hangup.</param>
        public void OnHangup(string uuid, Action<EventMessage> action)
        {
            ChannelEvents.Where(x => x.UUID == uuid && x.EventName == EventName.ChannelHangup).Take(1).Subscribe(action);
        }

        protected Task EnsureEventsSubscribed()
        {
            var sb = StringBuilderPool.Allocate();
            sb.Append("event plain");

            foreach (var @event in subscribedEvents)
            {
                sb.Append(" ");
                sb.Append(@event.ToString().ToUpperWithUnderscores());
            }

            if (customEvents.Any())
            {
                sb.Append(" CUSTOM ");

                foreach (var @event in customEvents)
                {
                    sb.Append(" ");
                    sb.Append(@event);
                }
            }

            return SendCommand(StringBuilderPool.ReturnAndFree(sb));
        }

        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "cts",
            Justification =
                "Need to keep hold of the CancellationTokenSource in case callers try to use the socket after it has been disposed.")]
        protected override void Dispose(bool disposing)
        {
            if (disposed != null && !disposed.EnsureCalledOnce())
            {
                if (disposing)
                {
                    // cancel any outgoing network sends
                    if (cts != null)
                    {
                        cts.Cancel();
                    }
                }
            }

            base.Dispose(disposing);
        }
    }
}