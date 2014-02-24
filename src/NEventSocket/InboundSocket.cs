namespace NEventSocket
{
    using System;
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Security;
    using System.Threading.Tasks;

    using Common.Logging;

    using NEventSocket.FreeSwitch;
    using NEventSocket.FreeSwitch.Api;
    using NEventSocket.Sockets;
    using NEventSocket.Util;

    public class InboundSocket : EventSocket
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly string password;

        public InboundSocket(string host = "localhost", int port = 8021, string password = "ClueCon")
            : base(new TcpClient(host, port))
        {
            this.password = password;

            this.MessagesReceived
                    .Where(x => x.ContentType == ContentTypes.AuthRequest)
                    .Take(1)
                    .Subscribe(x => this.Authenticate());
        }

        public event EventHandler Authenticated = (sender, args) => { };

        public Task<OriginateResult> Originate(IEndpoint endpoint, string application = "park")
        {
            return Originate(endpoint, new OriginateOptions(), application);
        }

        public Task<OriginateResult> Originate(IEndpoint endpoint, OriginateOptions options, string application = "park")
        {
            var args = string.Format("{0}{1} &{2}", options, endpoint, application);

            var tcs = new TaskCompletionSource<OriginateResult>();

            //we'll get an event in the future for this channel and we'll use that to complete the task
            var subscription = this.EventsReceived.Where(
                x => (x.EventType == EventType.CHANNEL_ANSWER || x.EventType == EventType.CHANNEL_HANGUP)
                                             || (options.ReturnRingReady && x.EventType == EventType.CHANNEL_PROGRESS))
                                             .Take(1) //will auto terminate the subscription
                                             .Subscribe(x =>
                                             {
                                                 Log.TraceFormat("Originate {0} complete - {1}", args, x.Headers[HeaderNames.AnswerState]);
                                                 tcs.SetResult(new OriginateResult(x));
                                             });

            this.BgApi("originate", args).ContinueWith(
                t =>
                {
                    if (t.Result != null && !t.Result.Success)
                    {
                        subscription.Dispose();
                        tcs.SetResult(new OriginateResult(t.Result));
                    }
                },
                TaskContinuationOptions.OnlyOnRanToCompletion)
                .ContinueWithNotComplete(tcs, subscription.Dispose);

            return tcs.Task;
        }

        private async Task Authenticate()
        {
            var result = await this.Auth(this.password);
            if (result.Success)
            {
                Log.Trace("Authenticated.");
                this.Authenticated(this, EventArgs.Empty);
            }
            else
            {
                Log.ErrorFormat("Invalid Password [{0}]", result.BodyText);
                throw new SecurityException("Invalid Password");
            }
        }
    }
}