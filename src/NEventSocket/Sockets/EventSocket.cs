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

    public class EventSocket : ObservableSocket, IEventSocket, IEventSocketCommands
    {
        protected readonly CompositeDisposable disposables = new CompositeDisposable();

        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly ISubject<BasicMessage> incomingMessages = new Subject<BasicMessage>();

        private readonly Queue<TaskCompletionSource<CommandReply>> commandCallbacks = new Queue<TaskCompletionSource<CommandReply>>();
 
        private readonly Queue<TaskCompletionSource<ApiResponse>> apiCallbacks = new Queue<TaskCompletionSource<ApiResponse>>(); 

        private readonly CancellationTokenSource cts = new CancellationTokenSource();


        protected EventSocket(TcpClient tcpClient) : base(tcpClient)
        {
            this.Receiver.ExtractBasicMessages().Subscribe(msg => this.incomingMessages.OnNext(msg));

            // some messages will be received in reply to a command that we sent earlier through the socket
            // we'll parse those into the appropriate message and complete the outstanding task associated with that command

            this.disposables.Add(this.MessagesReceived
                                    .Where(x => x.ContentType == ContentTypes.CommandReply)
                                    .Subscribe(response =>
                                        {
                                                     var result = new CommandReply(response);
                                                     Log.TraceFormat("CommandReply received [{0}]", result.ReplyText);
                                                     lock (this.commandCallbacks)
                                                     {
                                                         this.commandCallbacks.Dequeue().SetResult(result);
                                                     }
                                                 },
                                                 ex =>
                                                     {
                                                         lock (this.commandCallbacks)
                                                         {
                                                             this.commandCallbacks.Dequeue().SetException(ex);
                                                         }
                                                     }));

            this.disposables.Add(this.MessagesReceived
                                    .Where(x => x.ContentType == ContentTypes.ApiResponse)
                                    .Subscribe(response =>
                                                 {
                                                     Log.TraceFormat("ApiResponse received [{0}]", response.BodyText);
                                                     lock (this.apiCallbacks)
                                                     {
                                                         this.apiCallbacks.Dequeue().SetResult(new ApiResponse(response));
                                                     }
                                                 },
                                                 ex =>
                                                     {
                                                         lock (this.commandCallbacks)
                                                         {
                                                             this.apiCallbacks.Dequeue().SetException(ex);
                                                         }
                                                     }));

            Log.Trace("EventSocket initialized");

            this.Connected(this, EventArgs.Empty);
        }

        public event EventHandler Connected = (sender, args) => { };

        /// <summary> Gets an observable stream of BasicMessages </summary>
        public IObservable<BasicMessage> MessagesReceived { get { return this.incomingMessages; } }

        /// <summary>Observable of all Events received on this connection</summary>
        public IObservable<EventMessage> EventsReceived
        {
            get
            {
                return this.MessagesReceived
                            .Where(x => x.ContentType == ContentTypes.EventPlain)
                            .Select(x => new EventMessage(x));
            }
        }

        public Task<EventMessage> ExecuteAppAsync(string uuid, string appName, string appArg)
        {
            if (uuid == null) throw new ArgumentNullException("uuid");
            if (appName == null) throw new ArgumentNullException("appName");
            if (appArg == null) throw new ArgumentNullException("appArg");

            var tcs = new TaskCompletionSource<EventMessage>();

            var subscription = this.EventsReceived.Where(
                x =>
                x.EventType == EventType.CHANNEL_EXECUTE_COMPLETE
                && x.EventHeaders[HeaderNames.Application] == appName)
                .Take(1)
                .Subscribe(
                    x =>
                        {
                            Log.TraceFormat("CHANNEL_EXECUTE_COMPLETE [{0} {1} {2} {3}]",
                                x.EventHeaders[HeaderNames.AnswerState],
                                x.EventHeaders[HeaderNames.Application], 
                                x.EventHeaders[HeaderNames.ApplicationData], 
                                x.EventHeaders[HeaderNames.ApplicationResponse]);
                            tcs.SetResult(x);
                        });

            this.SendCommandAsync("sendmsg {0}\ncall-command: execute\nexecute-app-name: {1}\nexecute-app-arg: {2}".Fmt(uuid, appName, appArg))
                .ContinueWith(t =>
                {
                    if (!t.IsCompleted)
                    {
                        //we're never going to get a CHANNEL_EXECUTE_COMPLETE event for this call because we didn't send the command successfully
                        subscription.Dispose();

                        if (t.Exception != null)
                        {
                            //fail the parent task
                            tcs.SetException(t.Exception);
                        }
                    }
                });

            return tcs.Task;
        }

        public Task<ApiResponse> SendApiAsync(string command)
        {
            var tcs = new TaskCompletionSource<ApiResponse>();

            try
            {
                Monitor.Enter(this.apiCallbacks);
                Log.TraceFormat("Sending [api {0}]", command);
                this.SendAsync(Encoding.ASCII.GetBytes("api " + command + "\n\n")).Wait(this.cts.Token);
                this.apiCallbacks.Enqueue(tcs);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                Monitor.Exit(this.apiCallbacks);
            }

            return tcs.Task;
        }

        public Task<BackgroundJobResult> BgApi(string command, string arg = null, Guid? jobUUID = null)
        {
            if (jobUUID == null)
                jobUUID = Guid.NewGuid();

            var tcs = new TaskCompletionSource<BackgroundJobResult>();

            //we'll get an event in the future for this JobUUID and we'll use that to complete the task
            var subscription = this.EventsReceived.Where(
                x => x.EventType == EventType.BACKGROUND_JOB && x.EventHeaders["Job-UUID"] == jobUUID.ToString())
                                             .Take(1) //will auto terminate the subscription when received
                                             .Subscribe(x =>
                                                 {
                                                     var result = new BackgroundJobResult(x);
                                                     Log.TraceFormat("bgapi Job Complete [{0} {1} {2}]", result.JobUUID, result.Success, result.ErrorMessage);
                                                     tcs.SetResult(result);
                                                 });

            this.SendCommandAsync(arg != null
                                 ? "bgapi {0} {1}\nJob-UUID: {2}".Fmt(command, arg, jobUUID)
                                 : "bgapi {0}\nJob-UUID: {1}".Fmt(command, jobUUID))
                .ContinueWith(t =>
                {
                    if (!t.IsCompleted)
                    {
                        //we're never going to get a BACKGROUND_JOB event because we didn't send the command successfully
                        subscription.Dispose();

                        if (t.Exception != null)
                        {
                            //fail the parent task
                            tcs.SetException(t.Exception);
                        }
                    }
                });

            return tcs.Task;
        }

        public Task<CommandReply> SendCommandAsync(string command)
        {
            var tcs = new TaskCompletionSource<CommandReply>();

            try
            {
                Monitor.Enter(this.commandCallbacks);
                Log.TraceFormat("Sending [{0}]", command);
                this.SendAsync(Encoding.ASCII.GetBytes(command + "\n\n")).Wait(this.cts.Token);
                this.commandCallbacks.Enqueue(tcs);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                Monitor.Exit(this.commandCallbacks);
            }
            
            return tcs.Task;
        }

        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                // cancel any outgoing network sends
                this.cts.Cancel();

                this.incomingMessages.OnCompleted();

                this.disposables.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}