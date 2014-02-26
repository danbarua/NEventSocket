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
    using NEventSocket.FreeSwitch.Applications;
    using NEventSocket.Sockets;
    using NEventSocket.Util;

    public class InboundSocket : EventSocket
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        protected InboundSocket(string host = "localhost", int port = 8021)
            : base(new TcpClient(host, port))
        {
        }

        public static Task<InboundSocket> Connect(string host = "localhost", int port = 8021, string password = "ClueCon")
        {
            var tcs = new TaskCompletionSource<InboundSocket>();
            var socket = new InboundSocket(host, port);

            socket.Messages
                    .Where(x => x.ContentType == ContentTypes.AuthRequest)
                    .Take(1)
                    .Subscribe(async x =>
                        {
                            var result = await socket.Auth(password);
                            if (result.Success) tcs.SetResult(socket);
                            else tcs.SetException(new SecurityException("Invalid password"));
                        },
                        ex => tcs.SetException(ex));

            return tcs.Task;
        }

        public Task<OriginateResult> Originate(IEndpoint endpoint, OriginateOptions options = null, string application = "park")
        {
            if (options == null) options = new OriginateOptions();

            // if no UUID provided, we'll set one now and use that to filter for the correct channel events
            // this way, one inbound socket can originate many calls
            if (string.IsNullOrEmpty(options.UUID)) options.UUID = Guid.NewGuid().ToString();

            var originateString = string.Format("{0}{1} &{2}", options, endpoint, application);

            var tcs = new TaskCompletionSource<OriginateResult>();

            var subscription = this.Events.Where(x => x.UUID == options.UUID
                                    && (x.EventType == EventType.CHANNEL_ANSWER || x.EventType == EventType.CHANNEL_HANGUP
                                        || (options.ReturnRingReady && x.EventType == EventType.CHANNEL_PROGRESS)))
                                             .Take(1)
                                             .Subscribe(x =>
                                             {
                                                 Log.TraceFormat("Originate {0} complete - {1}", originateString, x.Headers[HeaderNames.AnswerState]);
                                                 tcs.SetResult(new OriginateResult(x));
                                             });

            this.BgApi("originate", originateString)
                .ContinueWith(
                t =>
                {
                    if (t.Result != null && !t.Result.Success)
                    {
                        //the bgapi originate call failed
                        Log.TraceFormat("Originate {0} failed - {1}", originateString, t.Result.ErrorMessage);
                        subscription.Dispose();
                        tcs.SetResult(new OriginateResult(t.Result));
                    }
                },
                TaskContinuationOptions.OnlyOnRanToCompletion)
                .ContinueOnFaultedOrCancelled(tcs, subscription.Dispose);

            return tcs.Task;
        }
    }
}