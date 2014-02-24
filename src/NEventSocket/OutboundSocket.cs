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
            ChannelData = new EventMessage(this.Connect().Result);
        }

        private async Task<CommandReply> Connect()
        {
            var result = await this.SendCommandAsync("connect");

            disposables.Add(
                this.MessagesReceived
                    .Where(x => x.ContentType == ContentTypes.DisconnectNotice)
                    .Take(1)
                    .Subscribe(_ => Log.Trace("Disconnect Notice received.")));

            return result;
        }

        /// <summary>
        /// When FS connects to an "Event Socket Outbound" handler, it sends
        /// a "CHANNEL_DATA" event in the headers of the Command-Reply received in response to Connect();
        /// </summary>
        public EventMessage ChannelData { get; private set; }
    }
}