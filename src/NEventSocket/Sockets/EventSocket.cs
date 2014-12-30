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
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
    using NEventSocket.Util;
    using NEventSocket.Util.ObjectPooling;

    /// <summary>
    /// Base class providing common functionality shared between an <seealso cref="InboundSocket"/> and an <seealso cref="OutboundSocket"/>.
    /// </summary>
    public abstract class EventSocket : ObservableSocket
    {
        private readonly ILog Log;

        // minimum events required for this class to do its job
        private readonly HashSet<EventName> events = new HashSet<EventName>()
                                                         {
                                                             EventName.ChannelExecuteComplete, 
                                                             EventName.BackgroundJob, 
                                                             EventName.ChannelHangup, 
                                                             EventName.ChannelAnswer, 
                                                             EventName.ChannelProgress, 
                                                             EventName.ChannelProgressMedia, 
                                                             EventName.ChannelBridge, 
                                                             EventName.ChannelUnbridge
                                                         };

        private readonly HashSet<string> customEvents = new HashSet<string>() { "conference::maintenance" };

        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        private readonly object gate = new object();

        /// <summary>
        /// Instantiates an <see cref="EventSocket"/> instance wrapping the provided <seealso cref="TcpClient"/>
        /// </summary>
        /// <param name="tcpClient">A TcpClient.</param>
        /// <param name="responseTimeOut">(Optional) The response timeout.</param>
        protected EventSocket(TcpClient tcpClient, TimeSpan? responseTimeOut = null) : base(tcpClient)
        {
            Log = LogProvider.GetLogger(this.GetType());

            ResponseTimeOut = responseTimeOut ?? TimeSpan.FromSeconds(5);

            Messages =
                Receiver.SelectMany(x => Encoding.ASCII.GetString(x))
                        .AggregateUntil(() => new Parser(), (builder, ch) => builder.Append(ch), builder => builder.Completed)
                        .Select(builder => builder.ExtractMessage())
                        .Do(x => Log.Trace("Received [{0}].".Fmt(x.ContentType)))
                        .Publish()
                        .RefCount();

            Log.Trace(() => "EventSocket initialized");
        }

        /// <summary>
        /// Gets or sets the TimeOut after which the socket will throw a <seealso cref="TimeoutException"/>.
        /// </summary>
        [Obsolete("This is due to be removed.")]
        public TimeSpan ResponseTimeOut { get; set; }

        /// <summary>
        /// Gets an observable sequence of <seealso cref="EventMessage"/>.
        /// </summary>
        public IObservable<EventMessage> Events
        {
            get
            {
                return Messages.Where(x => x.ContentType == ContentTypes.EventPlain).Select(x => new EventMessage(x));
            }
        }

        /// <summary>
        /// Gets an observable sequence of <seealso cref="BasicMessage"/>.
        /// </summary>
        public IObservable<BasicMessage> Messages { get; private set; }

        /// <summary>
        /// Send an api command (blocking mode)
        /// </summary>
        /// <remarks>
        /// See https://freeswitch.org/confluence/display/FREESWITCH/mod_event_socket#mod_event_socket-api
        /// </remarks>
        /// <param name="command">The API command to send (see https://wiki.freeswitch.org/wiki/Mod_commands) </param>
        /// <returns>A Task of <seealso cref="ApiResponse"/>.</returns>
        public Task<ApiResponse> SendApi(string command)
        {
            Log.Trace(() => "Sending [api {0}]".Fmt(command));

            lock (gate)
            {
                var tcs = new TaskCompletionSource<ApiResponse>();

                var subscription =
                    Messages.Where(x => x.ContentType == ContentTypes.ApiResponse)
                            .Take(1)
                            .Select(x => new ApiResponse(x))
                            .Do(
                                result =>
                                Log.Debug(
                                    () => "ApiResponse received [{0}] for [{1}]".Fmt(result.BodyText.Replace("\n", string.Empty), command)), 
                                ex => Log.ErrorException("Error waiting for Api Response to [{0}].".Fmt(command), ex))
                            .Subscribe(x => tcs.TrySetResult(x), ex => tcs.TrySetException(ex));

                SendAsync(Encoding.ASCII.GetBytes("api " + command + "\n\n"), cts.Token)
                    .ContinueOnFaultedOrCancelled(tcs, subscription.Dispose);

                return tcs.Task;
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
        public Task<CommandReply> SendCommand(string command)
        {
            Log.Trace(() => "Sending [{0}]".Fmt(command));

            lock (gate)
            {
                var tcs = new TaskCompletionSource<CommandReply>();

                var subscription =
                    Messages.Where(x => x.ContentType == ContentTypes.CommandReply)
                            .Take(1)
                            .Select(x => new CommandReply(x))
                            .Do(
                                result =>
                                Log.Debug(
                                    () => "CommandReply received [{0}] for [{1}]".Fmt(result.ReplyText.Replace("\n", string.Empty), command)), 
                                ex => Log.ErrorException("Error waiting for Command Reply to [{0}].".Fmt(command), ex))
                            .Subscribe(x => tcs.TrySetResult(x), ex => tcs.TrySetException(ex));

                SendAsync(Encoding.ASCII.GetBytes(command + "\n\n"), cts.Token).ContinueOnFaultedOrCancelled(tcs, subscription.Dispose);

                return tcs.Task;
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
        /// <returns>A Task of <seealso cref="EventMessage"/> that wraps the ChannelExecuteComplete event if the application completes successfully.</returns>
        public Task<EventMessage> ExecuteApplication(
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

            var sb = StringBuilderPool.Allocate();
            sb.AppendFormat("sendmsg {0}\ncall-command: execute\nexecute-app-name: {1}\n", uuid, application);

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

            var query = from send in this.SendCommand(StringBuilderPool.ReturnAndFree(sb)).ToObservable()
                        from e in
                            Events.FirstOrDefaultAsync(
                                x =>
                                x.UUID == uuid && x.EventName == EventName.ChannelExecuteComplete
                                && x.Headers[HeaderNames.Application] == application)
                        select e;

            return query.Do(
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
                    }).ToTask();
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

            /* We'll get a CommandReply message immediately acknowledging the job request,
             * then followed up with a BackgroundJob event matching our JobUUID when the job is complete. */
            var query = from send in SendCommand(backgroundApiCommand).ToObservable()
                        from e in
                            Events.FirstOrDefaultAsync(
                                x => x.EventName == EventName.BackgroundJob && x.Headers[HeaderNames.JobUUID] == jobUUID.ToString())
                        select new BackgroundJobResult(e);

            return query.ToTask(cts.Token);
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
                        uuid, options.ChannelVariables.Select(kvp => kvp.Key + "='" + kvp.Value + "'").ToArray());
            }

            /* If the bridge fails to connect we'll get a CHANNEL_EXECUTE_COMPLETE event with a failure message and the Execute task will complete.
             * If the bridge succeeds, that event won't arrive until after the bridged leg hangs up and completes the call.
             * In this case, we want to return a result as soon as the b-leg picks up and connects so we'll merge with the CHANNEL_BRIDGE event
             * observable.Amb(otherObservable) will propogate the first sequence to produce a result. */
            return
                await
                ExecuteApplication(uuid, "bridge", bridgeString)
                    .ToObservable()
                    .Amb(
                        Events.FirstOrDefaultAsync(x => x.UUID == uuid && x.EventName == EventName.ChannelBridge).Do(
                            bridgeEvent =>
                                {
                                    if (bridgeEvent != null)
                                    {
                                        Log.Debug(
                                            () =>
                                            "Bridge {0} complete - {1}".Fmt(bridgeString, bridgeEvent.Headers[HeaderNames.OtherLegUniqueId]));
                                    }
                                    else
                                    {
                                        Log.Trace(() => "No ChannelBridge event received for {0}".Fmt(bridgeString));
                                    }
                                }))
                    .Select(x => new BridgeResult(x)) // todo: what if we get neither, eg hangup or disconnect before either event arrives
                    .ToTask();
        }

        /// <summary>
        /// Subscribes this EventSocket to one or more events.
        /// </summary>
        /// <param name="events">The <seealso cref="EventName"/>s to subscribe to.</param>
        /// <returns>A Task.</returns>
        public async Task SubscribeEvents(params EventName[] events)
        {
            if (!this.events.SequenceEqual(events))
            {
                this.events.UnionWith(events); // ensures we are always at least using the default minimum events
                await
                    SendCommand(
                        "event plain {0} CUSTOM {1}".Fmt(
                            string.Join(" ", this.events.Select(x => x.ToString().ToUpperWithUnderscores())), string.Join(" ", customEvents)));
            }
        }

        /// <summary>
        /// Subscribes this EventSocket to one or more custom events.
        /// </summary>
        /// <param name="events">The custom event names to subscribe to.</param>
        /// <returns>A Task.</returns>
        public async Task SubscribeCustomEvents(params string[] events)
        {
            if (!this.customEvents.SequenceEqual(events))
            {
                this.customEvents.UnionWith(events); // ensures we are always at least using the default minimum events
                await this.SubscribeEvents();
            }
        }

        /// <summary>
        /// Register a callback to be invoked when the given Channel UUID hangs up.
        /// </summary>
        /// <param name="uuid">The Channel UUID.</param>
        /// <param name="action">A Callback to be invoked on hangup.</param>
        public void OnHangup(string uuid, Action<EventMessage> action)
        {
            Events.Where(x => x.UUID == uuid && x.EventName == EventName.ChannelHangup).Take(1).Subscribe(action);
        }

        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "cts", 
            Justification =
                "Need to keep hold of the CancellationTokenSource in case callers try to use the socket after it has been disposed.")]
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
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