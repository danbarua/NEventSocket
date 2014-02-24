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
    using NEventSocket.Sockets.Protocol;
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
                                                             EventType.CHANNEL_PROGRESS
                                                         };

        private CancellationTokenSource cts = new CancellationTokenSource();

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

        public Task<EventMessage> ExecuteAppAsync(string uuid, string appName, string appArg = null)
        {
            if (uuid == null) throw new ArgumentNullException("uuid");
            if (appName == null) throw new ArgumentNullException("appName");           

            var tcs = new TaskCompletionSource<EventMessage>();

            var subscription = Events.Where(
                x =>
                x.EventType == EventType.CHANNEL_EXECUTE_COMPLETE
                && x.Headers[HeaderNames.Application] == appName)
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

            SendCommandAsync("sendmsg {0}\ncall-command: execute\nexecute-app-name: {1}\nexecute-app-arg: {2}".Fmt(uuid, appName, appArg))
                .ContinueWithNotComplete(tcs, subscription.Dispose);

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
                x => x.EventType == EventType.BACKGROUND_JOB && x.Headers["Job-UUID"] == jobUUID.ToString())
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
                            .ContinueWithNotComplete(tcs, subscription.Dispose);

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
            return this.SendCommandAsync("event plain {0}".Fmt(string.Join(" ", this.events)));
        }

        public IDisposable SubscribeEvent(EventType eventType, Action<EventMessage> handler)
        {
            if (!events.Contains(eventType)) SubscribeEvents(eventType).Wait();

            return Events.Where(x => x.EventType == eventType).Subscribe(handler);
        }

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
                        cts.Dispose();
                        cts = null;
                    }

                    incomingMessages.OnCompleted();

                    disposables.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}