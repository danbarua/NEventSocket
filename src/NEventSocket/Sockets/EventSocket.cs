namespace NEventSocket.Sockets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        private readonly ISubject<BasicMessage> incomingMessages = new ReplaySubject<BasicMessage>(1);

        private readonly Queue<TaskCompletionSource<CommandReply>> commandCallbacks = new Queue<TaskCompletionSource<CommandReply>>();
 
        private readonly Queue<TaskCompletionSource<ApiResponse>> apiCallbacks = new Queue<TaskCompletionSource<ApiResponse>>();
        
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
                                                     lock (commandCallbacks)
                                                     {
                                                         var callBack = commandCallbacks.Dequeue();
                                                         Log.TraceFormat("CommandReply received [{0}] for [{1}]", result.ReplyText, callBack.Task.AsyncState);
                                                         callBack.SetResult(result);
                                                     }
                                                 },
                                                 ex =>
                                                     {
                                                         lock (commandCallbacks)
                                                         {
                                                            var callBack = commandCallbacks.Dequeue();
                                                            Log.Error("Exception when receving reply for [{0}]".Fmt(callBack.Task.AsyncState),ex);
                                                            callBack.SetException(ex);
                                                         }
                                                     }));

            disposables.Add(Messages
                                    .Where(x => x.ContentType == ContentTypes.ApiResponse)
                                    .Subscribe(response =>
                                                 {
                                                     lock (apiCallbacks)
                                                     {
                                                         var callBack = apiCallbacks.Dequeue();
                                                         Log.TraceFormat("ApiResponse received [{0}] for [{1}]", response.BodyText, callBack.Task.AsyncState);
                                                         callBack.SetResult(new ApiResponse(response));
                                                     }
                                                 },
                                                 ex =>
                                                     {
                                                         lock (apiCallbacks)
                                                         {
                                                             var callBack = apiCallbacks.Dequeue();
                                                             Log.Error("Exception when receving reply for [{0}]".Fmt(callBack.Task.AsyncState), ex);
                                                             callBack.SetException(ex);
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

        public Task<EventMessage> Execute(string uuid, string application, string applicationArguments = null, int loops = 1, bool eventLock = false, bool async = false)
        {
            if (uuid == null) throw new ArgumentNullException("uuid");
            if (application == null) throw new ArgumentNullException("application");           

            var tcs = new TaskCompletionSource<EventMessage>();

            var subscription = Events.Where(
                x => x.UUID == uuid && x.EventName == EventName.ChannelExecuteComplete && x.Headers[HeaderNames.Application] == application)
                .Take(1)
                .Subscribe(
                    x =>
                        {
                            Log.TraceFormat("ChannelExecuteComplete [{0} {1} {2}]",
                                x.Headers[HeaderNames.AnswerState],
                                x.Headers[HeaderNames.Application],
                                x.Headers[HeaderNames.ApplicationResponse]);
                            tcs.SetResult(x);
                        });

            var command = "sendmsg {0}\ncall-command: execute\nexecute-app-name: {1}\n".Fmt(uuid, application);

            if (eventLock)
            {
                command += "event-lock: true\n";
            }

            if (loops > 1)
            {
                command += "loops: " + loops + "\n";
            }

            if (async)
            {
                command += "async: true\n";
            }

            if (applicationArguments != null)
            {
                command += "content-type: text/plain\ncontent-length: {0}\n\n{1}\n".Fmt(
                    applicationArguments.Length, applicationArguments);
            }

            SendCommand(command).ContinueOnFaultedOrCancelled(tcs, subscription.Dispose);

            return tcs.Task;
        }

        public async Task<BridgeResult> Bridge(string uuid, string endpoint, BridgeOptions options = null)
        {
            if (options == null) options = new BridgeOptions();
            if (string.IsNullOrEmpty(options.UUID)) options.UUID = Guid.NewGuid().ToString();

            var bridgeString = string.Format("{0}{1}", options, endpoint);

            //some bridge options need to be set in channel vars
            if (options.ChannelVariables.Any())
            {
                await
                    this.SetMultipleChannelVariables(
                        uuid, options.ChannelVariables.Select(kvp => kvp.Key + "='" + kvp.Value + "'").ToArray());
            }

            var tcs = new TaskCompletionSource<BridgeResult>(TaskCreationOptions.AttachedToParent);

            var subscription = this.Events.Where(x => x.UUID == uuid && x.EventName == EventName.ChannelBridge)
                .Take(1)
                .Subscribe(x =>
                {
                    Log.TraceFormat("Bridge {0} complete - {1}", bridgeString, x.Headers[HeaderNames.OtherLegUniqueId]);
                    tcs.SetResult(new BridgeResult(x));
                });

            this.Execute(uuid, "bridge", applicationArguments: bridgeString)
                .ContinueWith(t =>
                    {
                        /* If the bridge fails, we'll get a CHANNEL_EXECUTE_COMPLETE event immediately.
                         * If the bridge succeeds, we won't get the CHANNEL_EXECUTE_COMPLETE event until
                         * the bridged call completes, at which point we'll already have completed the outstanding
                         * task with the CHANNEL_BRIDGE event.*/

                        if (!tcs.Task.IsCompleted)
                        {
                            tcs.SetResult(new BridgeResult(t.Result));
                            subscription.Dispose();
                        }
                    },
                    TaskContinuationOptions.OnlyOnRanToCompletion)
                .ContinueOnFaultedOrCancelled(tcs, subscription.Dispose);

            return await tcs.Task;
        }

        public Task<ApiResponse> Api(string command)
        {
            var tcs = new TaskCompletionSource<ApiResponse>(command, TaskCreationOptions.AttachedToParent);

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

        public Task<BackgroundJobResult> BackgroundJob(string command, string arg = null, Guid? jobUUID = null)
        {
            if (jobUUID == null)
                jobUUID = Guid.NewGuid();

            var tcs = new TaskCompletionSource<BackgroundJobResult>(TaskCreationOptions.AttachedToParent);

            //we'll get an event in the future for this JobUUID and we'll use that to complete the task
            var subscription = Events.Where(
                x => x.EventName == EventName.BackgroundJob && x.Headers[HeaderNames.JobUUID] == jobUUID.ToString())
                                             .Take(1) //will auto terminate the subscription when received
                                             .Subscribe(x =>
                                                 {
                                                     var result = new BackgroundJobResult(x);
                                                     Log.TraceFormat("bgapi Job Complete [{0} {1} {2}]", result.JobUUID, result.Success, result.ErrorMessage);
                                                     tcs.SetResult(result);
                                                 });

            SendCommand(arg != null
                                 ? "bgapi {0} {1}\nJob-UUID: {2}".Fmt(command, arg, jobUUID)
                                 : "bgapi {0}\nJob-UUID: {1}".Fmt(command, jobUUID))
                            .ContinueOnFaultedOrCancelled(tcs, subscription.Dispose);

            return tcs.Task;
        }

        public Task<CommandReply> SendCommand(string command)
        {
            var tcs = new TaskCompletionSource<CommandReply>(command, TaskCreationOptions.AttachedToParent);

            try
            {
                Monitor.Enter(commandCallbacks);
                Log.TraceFormat("Sending [{0}]", command);
                SendAsync(Encoding.ASCII.GetBytes(command + "\n\n")).Wait(cts.Token);
                commandCallbacks.Enqueue(tcs);
            }
            catch (OperationCanceledException ex)
            {
                tcs.SetResult(null);
            }
            catch (AggregateException ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                Monitor.Exit(commandCallbacks);
            }
            
            return tcs.Task;
        }

        public async Task SubscribeEvents(params EventName[] events)
        {
            if (!this.events.SequenceEqual(events))
            {
                this.events.UnionWith(events); //ensures we are always at least using the default minimum events
                await SendCommand("event plain {0} CUSTOM {1}"
                    .Fmt(string.Join(" ", this.events.Select(x => x.ToString().ToUpperWithUnderscores())), string.Join(" ", customEvents)));
            }
        }

        public async Task SubscribeCustomEvents(params string[] events)
        {
            if (!this.customEvents.SequenceEqual(events))
            {
                this.customEvents.UnionWith(events); //ensures we are always at least using the default minimum events
                await this.SubscribeEvents();
            }
        }

        public void OnHangup(string uuid, Action<EventMessage> action)
        {
            Events.Where(x => x.UUID == uuid && x.EventName == EventName.ChannelHangup)
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