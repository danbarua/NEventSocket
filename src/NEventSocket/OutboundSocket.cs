namespace NEventSocket
{
    using System;
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Common.Logging;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Sockets;

    public class OutboundSocket : EventSocket
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        protected internal OutboundSocket(TcpClient tcpClient)
            : base(tcpClient)
        {
            }

        public async Task<CommandReply> Connect()
        {
            var result = await this.SendCommandAsync("connect");

            disposables.Add(
                            this.MessagesReceived.Where(x => x.ContentType == ContentTypes.DisconnectNotice)
                                .Take(1)
                                .Subscribe(
                                    _ =>
                                    {
                                        Log.Trace("Disconnect Notice received.");
                                        Dispose();
                                    }));
            return result;
        }
    }
}