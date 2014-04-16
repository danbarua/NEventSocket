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
    using System.Reactive.Threading.Tasks;
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

        public async Task<BridgeResult> Bridge(string uuid, string endpoint, BridgeOptions options = null)
        {
            if (options == null) options = new BridgeOptions();
            if (string.IsNullOrEmpty(options.UUID)) options.UUID = Guid.NewGuid().ToString();

            var bridgeString = string.Format("{0}{1}", options, endpoint);

            //some bridge options need to be set in channel vars
            if (options.ChannelVariables.Any())
            {
                await this.SetMultipleChannelVariables(
                    uuid, options.ChannelVariables.Select(kvp => kvp.Key + "='" + kvp.Value + "'").ToArray());
            }

            /* If the bridge fails to connect we'll get a CHANNEL_EXECUTE_COMPLETE event with a failure message and the Execute task will complete.
             * If the bridge succeeds, that event won't arrive until after the bridged leg hangs up and completes the call.
             * In this case, we want to return a result as soon as the b-leg picks up and connects so we'll merge with the CHANNEL_BRIDGE event
             * observable.Amb(otherObservable) will propogate the first sequence to produce a result. */

            return await Execute(uuid, "bridge", bridgeString)
                        .ToObservable()
                        .Amb(Events.FirstAsync(x => x.UUID == uuid && x.EventName == EventName.ChannelBridge)
                                  .Do(x => Log.TraceFormat("Bridge {0} complete - {1}", bridgeString, x.Headers[HeaderNames.OtherLegUniqueId])))
                        .Select(x => new BridgeResult(x))
                        .ToTask();
        }

        public async Task<ApiResponse> Api(string command)
        {
            Log.TraceFormat("Sending [api {0}]", command);
            await SendAsync(Encoding.ASCII.GetBytes("api " + command + "\n\n"), cts.Token);

            return await Messages
                .FirstAsync(x => x.ContentType == ContentTypes.ApiResponse)
                .Select(x => new ApiResponse(x))
                .Do(result => Log.TraceFormat("ApiResponse received [{0}] for [{1}]", result.BodyText, command))
                .ToTask(cts.Token);
        }

        public Task<EventMessage> Execute(string uuid, string application, string applicationArguments = null, int loops = 1, bool eventLock = false, bool async = false)
        {
            if (uuid == null) throw new ArgumentNullException("uuid");
            if (application == null) throw new ArgumentNullException("application");

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

            return this.SendCommand(command)
                        .ToObservable()
                        .Concat(Events.FirstAsync(x => x.UUID == uuid && x.EventName == EventName.ChannelExecuteComplete && x.Headers[HeaderNames.Application] == application).Cast<BasicMessage>())
                        .LastAsync()
                        .Cast<EventMessage>()
                        .Do(
                            x =>
                            Log.TraceFormat(
                                "{0} ChannelExecuteComplete [{1} {2} {3}]",
                                x.UUID,
                                x.AnswerState,
                                x.Headers[HeaderNames.Application],
                                x.Headers[HeaderNames.ApplicationResponse]))
                        .ToTask();
        }

        public Task<BackgroundJobResult> BackgroundJob(string command, string arg = null, Guid? jobUUID = null)
        {
            if (jobUUID == null)
                jobUUID = Guid.NewGuid();

            var backgroundApiCommand = arg != null
                                   ? "bgapi {0} {1}\nJob-UUID: {2}".Fmt(command, arg, jobUUID)
                                   : "bgapi {0}\nJob-UUID: {1}".Fmt(command, jobUUID);

            /* We'll get a CommandReply message immediately acknowledging the job request,
             * then followed up with a BackgroundJob event matching our JobUUID when the job is complete. */

            return this.SendCommand(backgroundApiCommand)
                        .ToObservable()
                        .Concat(Events.FirstAsync(x => x.EventName == EventName.BackgroundJob && x.Headers[HeaderNames.JobUUID] == jobUUID.ToString())
                                       .Cast<BasicMessage>())
                        .LastAsync()
                        .Cast<EventMessage>()
                        .Select(x => new BackgroundJobResult(x))
                        .ToTask(cts.Token);
        }

        public async Task<CommandReply> SendCommand(string command)
        {
            Log.TraceFormat("Sending [{0}]", command);
            await SendAsync(Encoding.ASCII.GetBytes(command + "\n\n"), cts.Token);

            return await Messages
                .FirstAsync(x => x.ContentType == ContentTypes.CommandReply)
                .Select(x => new CommandReply(x))
                .Do(result =>
                    { 
                        Log.TraceFormat("CommandReply received [{0}] for [{1}]", result.ReplyText, command);
                        if (!result.Success) throw new ApplicationException("Command {0} failed - {1}".Fmt(command, result.ReplyText));
                    })
                .ToTask(cts.Token);
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