namespace NEventSocket.Sockets.Implementation
{
    using System;
    using System.Net.Sockets;
    using System.Reactive.Linq;

    using Common.Logging;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Sockets.Protocol;

    public class OutboundSocket : EventSocket
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        protected internal OutboundSocket(TcpClient tcpClient)
            : base(tcpClient)
        {
            this.SendCommandAsync("connect")
                .ContinueWith(t =>
                    {
                        if (t.IsCompleted)
                        {
                            this.disposables.Add(
                                this.MessagesReceived.Where(x => x.ContentType == ContentTypes.DisconnectNotice)
                                    .Take(1)
                                    .Subscribe(
                                        _ =>
                                            {
                                                Log.Debug("Disconnect Notice received.");
                                                this.Disconnect();
                                            }));
                        }
                    });
        }
    }
}