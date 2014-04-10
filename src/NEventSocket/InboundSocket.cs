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

        protected InboundSocket(string host, int port)
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

        public Task<OriginateResult> Originate(string endpoint, OriginateOptions options = null, string application = "park")
        {
            if (options == null) options = new OriginateOptions();

            // if no UUID provided, we'll set one now and use that to filter for the correct channel events
            // this way, one inbound socket can originate many calls and we can complete the correct
            // TaskCompletionSource for each originated call.
            if (string.IsNullOrEmpty(options.UUID)) options.UUID = Guid.NewGuid().ToString();

            var originateString = string.Format("{0}{1} &{2}", options, endpoint, application);

            var tcs = new TaskCompletionSource<OriginateResult>();

            EventMessage channelData = null;

            var subscription = this.Events.Where(x => x.UUID == options.UUID
                                    && (x.EventName == EventName.ChannelAnswer || x.EventName == EventName.ChannelHangup
                                        || (options.ReturnRingReady && x.EventName == EventName.ChannelProgress)))
                                             .Take(1)
                                             .Subscribe(x =>
                                             {
                                                 channelData = x;

                                                 if (options.ReturnRingReady && x.EventName == EventName.ChannelProgress)
                                                 {
                                                     tcs.SetResult(new OriginateResult(x));
                                                 }
                                             });

            this.BackgroundJob("originate", originateString)
                .ContinueWith(
                    t =>
                        {
                            if (!tcs.Task.IsCompleted && t.Result != null)
                            {
                                //we got a BgApiResult before getting a ChannelAnswer or ChannelHangup event.
                                Log.TraceFormat("Originate {0} success: {1} - message: {2}", originateString, t.Result.Success, t.Result.ErrorMessage);

                                //clean up the event handler, we don't need it now
                                subscription.Dispose();

                                //complete the task
                                if (!t.Result.Success || channelData == null)
                                {
                                    tcs.SetResult(new OriginateResult(t.Result));
                                }
                                else if (channelData != null)
                                {
                                    tcs.SetResult(new OriginateResult(channelData));
                                }
                            }
                        },
                        TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueOnFaultedOrCancelled(tcs, subscription.Dispose);

            return tcs.Task;
        }

        public IDisposable On(string uuid, EventName eventName, Action<EventMessage> handler)
        {
            return this.Events.Where(x => x.UUID == uuid && x.EventName == eventName).Subscribe(handler);
        }
    }
}