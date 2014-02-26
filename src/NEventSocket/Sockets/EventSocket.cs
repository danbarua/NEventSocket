namespace NEventSocket.Sockets
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Common.Logging;

    using NEventSocket.FreeSwitch;
    using NEventSocket.FreeSwitch.Api;
    using NEventSocket.FreeSwitch.Applications;
    using NEventSocket.Util;

    public abstract class EventSocket : ObservableSocket, IEventSocket, IEventSocketCommands
    {
        protected readonly CompositeDisposable disposables = new CompositeDisposable();

        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly ISubject<BasicMessage> incomingMessages = new Subject<BasicMessage>();

        private readonly Queue<TaskCompletionSource<CommandReply>> commandCallbacks = new Queue<TaskCompletionSource<CommandReply>>();
 
        private readonly Queue<TaskCompletionSource<ApiResponse>> apiCallbacks = new Queue<TaskCompletionSource<ApiResponse>>();
        
        // minimum events required for this class to do its job
        private readonly HashSet<EventType> events = new HashSet<EventType>()
                                                         {
                                                             EventType.CHANNEL_EXECUTE_COMPLETE,
                                                             EventType.BACKGROUND_JOB,
                                                             EventType.CHANNEL_HANGUP,
                                                             EventType.CHANNEL_ANSWER,
                                                             EventType.CHANNEL_PROGRESS,
                                                             EventType.CHANNEL_PROGRESS_MEDIA,
                                                             EventType.RECORD_STOP,
                                                             EventType.CHANNEL_BRIDGE,
                                                             EventType.CHANNEL_UNBRIDGE
                                                         };

        private readonly HashSet<string> customEvents = new HashSet<string>() { "conference::maintenance" }; 

        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        protected EventSocket(TcpClient tcpClient) : base(tcpClient)
        {
            Receiver
                .SelectMany(x => Encoding.ASCII.GetString(x))
                .AggregateUntil(() => new Parser(), (builder, ch) => builder.Append(ch), builder => builder.Completed)
                .Select(builder => builder.ParseMessage()).Subscribe(msg => incomingMessages.OnNext(msg));

            // some messages will be received in reply to a command that we sent earlier through the socket
            // we'll parse those into the appropriate message and complete the outstanding task associated with that command

            disposables.Add(Messages
                                    .Where(x => x.ContentType == ContentTypes.CommandReply)
                                    .Subscribe(response =>
                                        {
                                                     var result = new CommandReply(response);
                                                     Log.TraceFormat("CommandReply received [{0}]", result.ReplyText);
                                                     lock (commandCallbacks)
                                                     {
                                                         commandCallbacks.Dequeue().SetResult(result);
                                                     }
                                                 },
                                                 ex =>
                                                     {
                                                         lock (commandCallbacks)
                                                         {
                                                             commandCallbacks.Dequeue().SetException(ex);
                                                         }
                                                     }));

            disposables.Add(Messages
                                    .Where(x => x.ContentType == ContentTypes.ApiResponse)
                                    .Subscribe(response =>
                                                 {
                                                     Log.TraceFormat("ApiResponse received [{0}]", response.BodyText);
                                                     lock (apiCallbacks)
                                                     {
                                                         apiCallbacks.Dequeue().SetResult(new ApiResponse(response));
                                                     }
                                                 },
                                                 ex =>
                                                     {
                                                         lock (commandCallbacks)
                                                         {
                                                             apiCallbacks.Dequeue().SetException(ex);
                                                         }
                                                     }));

            Log.Trace("EventSocket initialized");
        }

        /// <summary> Gets an observable stream of BasicMessages </summary>
        public IObservable<BasicMessage> Messages { get { return incomingMessages; } }

        /// <summary>Observable of all Events received on this connection</summary>
        public IObservable<EventMessage> Events
        {
            get
            {
                return Messages
                            .Where(x => x.ContentType == ContentTypes.EventPlain)
                            .Select(x => new EventMessage(x));
            }
        }

        public Task<EventMessage> ExecuteAppAsync(string uuid, string appName, string appArg = null, bool eventLock = false)
        {
            if (uuid == null) throw new ArgumentNullException("uuid");
            if (appName == null) throw new ArgumentNullException("appName");           

            var tcs = new TaskCompletionSource<EventMessage>();

            var subscription = Events.Where(
                x => x.UUID == uuid && x.EventType == EventType.CHANNEL_EXECUTE_COMPLETE && x.Headers[HeaderNames.Application] == appName)
                .Take(1)
                .Subscribe(
                    x =>
                        {
                            Log.TraceFormat("CHANNEL_EXECUTE_COMPLETE [{0} {1} {2}]",
                                x.Headers[HeaderNames.AnswerState],
                                x.Headers[HeaderNames.Application],
                                x.Headers[HeaderNames.ApplicationResponse]);
                            tcs.SetResult(x);
                        });

            var appCmd = "sendmsg {0}\ncall-command: execute\nexecute-app-name: {1}\nexecute-app-arg: {2}".Fmt(uuid, appName, appArg);
            //if (eventLock) appCmd += "\nevent-lock: true";

            SendCommandAsync(appCmd).ContinueOnFaultedOrCancelled(tcs, subscription.Dispose);

            return tcs.Task;
        }

        public Task<BridgeResult> Bridge(string uuid, IEndpoint endpoint, BridgeOptions options = null)
        {
            if (options == null) options = new BridgeOptions();
            var bridgeString = string.Format("{0}{1}", options, endpoint);

            /* https://wiki.freeswitch.org/wiki/Variable_effective_caller_id_name
            /*  sets the effective callerid name. This is automatically exported to the B-leg; however, it is not valid in an origination string.
             * In other words, set this before calling bridge, otherwise use origination_caller_id_name */

            if (!string.IsNullOrEmpty(options.CallerIdName)) this.SetChannelVariable(uuid, "effective_caller_id_name", "'{0}'".Fmt(options.CallerIdName));
            if (!string.IsNullOrEmpty(options.CallerIdNumber)) this.SetChannelVariable(uuid, "effective_caller_id_number", options.CallerIdNumber);

            //for some reason bridge is ignoring options passed in the dial string.. setting channel vars for now

            this.SetChannelVariable(uuid, "hangup_after_bridge", options.HangupAfterBridge.ToString().ToLowerInvariant());
            this.SetChannelVariable(uuid, "continue_on_fail", options.ContinueOnFail.ToString().ToLowerInvariant());
            this.SetChannelVariable(uuid, "ignore_early_media", options.IgnoreEarlyMedia.ToString().ToLowerInvariant());
            this.SetChannelVariable(uuid, "ringback", options.RingBack);

            if (options.Timeout > 0) this.SetChannelVariable(uuid, "call_timeout", options.Timeout);


            var tcs = new TaskCompletionSource<BridgeResult>();

            var subscription = this.Events.Where(x => x.UUID == uuid && x.EventType == EventType.CHANNEL_BRIDGE)
                .Take(1)
                .Subscribe(x =>
                {
                    Log.TraceFormat("Bridge {0} complete - {1}", bridgeString, x.Headers[HeaderNames.OtherLegUniqueId]);
                    tcs.SetResult(new BridgeResult(x));
                });

            this.ExecuteAppAsync(uuid, "bridge", bridgeString, eventLock: true)
                .ContinueWith(t =>
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            tcs.SetResult(new BridgeResult(t.Result));
                        }
                    },
                    TaskContinuationOptions.OnlyOnRanToCompletion)
                .ContinueOnFaultedOrCancelled(tcs, subscription.Dispose);

            return tcs.Task;
        }

        public Task<ApiResponse> SendApiAsync(string command)
        {
            var tcs = new TaskCompletionSource<ApiResponse>();

            try
            {
                Monitor.Enter(apiCallbacks);
                Log.TraceFormat("Sending [api {0}]", command);
                SendAsync(Encoding.ASCII.GetBytes("api " + command + "\n\n")).Wait(cts.Token);
                apiCallbacks.Enqueue(tcs);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                Monitor.Exit(apiCallbacks);
            }

            return tcs.Task;
        }

        public Task<BackgroundJobResult> BgApi(string command, string arg = null, Guid? jobUUID = null)
        {
            if (jobUUID == null)
                jobUUID = Guid.NewGuid();

            var tcs = new TaskCompletionSource<BackgroundJobResult>();

            //we'll get an event in the future for this JobUUID and we'll use that to complete the task
            var subscription = Events.Where(
                x => x.EventType == EventType.BACKGROUND_JOB && x.Headers[HeaderNames.JobUUID] == jobUUID.ToString())
                                             .Take(1) //will auto terminate the subscription when received
                                             .Subscribe(x =>
                                                 {
                                                     var result = new BackgroundJobResult(x);
                                                     Log.TraceFormat("bgapi Job Complete [{0} {1} {2}]", result.JobUUID, result.Success, result.ErrorMessage);
                                                     tcs.SetResult(result);
                                                 });

            SendCommandAsync(arg != null
                                 ? "bgapi {0} {1}\nJob-UUID: {2}".Fmt(command, arg, jobUUID)
                                 : "bgapi {0}\nJob-UUID: {1}".Fmt(command, jobUUID))
                            .ContinueOnFaultedOrCancelled(tcs, subscription.Dispose);

            return tcs.Task;
        }

        public Task<CommandReply> SendCommandAsync(string command)
        {
            var tcs = new TaskCompletionSource<CommandReply>();
            try
            {
                Monitor.Enter(commandCallbacks);
                Log.TraceFormat("Sending [{0}]", command);
                SendAsync(Encoding.ASCII.GetBytes(command + "\n\n")).Wait(cts.Token);
                commandCallbacks.Enqueue(tcs);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                Monitor.Exit(commandCallbacks);
            }
            
            return tcs.Task;
        }

        public Task<CommandReply> SubscribeEvents(params EventType[] events)
        {
            this.events.UnionWith(events); //ensures we are always at least using the default minimum events
            return SendCommandAsync("event plain {0} CUSTOM {1}".Fmt(string.Join(" ", this.events), string.Join(" ", customEvents)));
        }

        public Task<CommandReply> SubscribeCustomEvents(params string[] events)
        {
            this.customEvents.UnionWith(events); //ensures we are always at least using the default minimum events
            return this.SubscribeEvents();
        }

        public void OnHangup(string uuid, Action<EventMessage> action)
        {
            Events.Where(x => x.UUID == uuid && x.EventType == EventType.CHANNEL_HANGUP)
                  .Take(1)
                  .Subscribe(action);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "cts",
        Justification = "Need to keep hold of the CancellationTokenSource in case callers try to use the socket after it has been disposed.")]
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

                    incomingMessages.OnCompleted();

                    disposables.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}