// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutboundSocket.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket
{
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.FreeSwitch.Channel;
    using NEventSocket.Logging;
    using NEventSocket.Sockets;
    using NEventSocket.Util;

    public class OutboundSocket : EventSocket
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        protected internal OutboundSocket(TcpClient tcpClient)
            : base(tcpClient)
        {
        }
        
        /// <summary>
        /// When FS connects to an "Event Socket Outbound" handler, it sends
        /// a "CHANNEL_DATA" event in the headers of the Command-Reply received in response to Connect();
        /// </summary>
        public EventMessage ChannelData { get; private set; }

        public Task Connect()
        {
            return
                this.SendCommand("connect")
                    .ToObservable()
                    .Do(x =>
                            {
                                this.ChannelData = new EventMessage(x);
                                this.Messages.FirstAsync(m => m.ContentType == ContentTypes.DisconnectNotice)
                                    .Do(_ => Log.Trace(() => "Channel {0} Disconnect Notice received.".Fmt(ChannelData.UUID)));
                            })
                    .ToTask();
        }

        public async Task<IChannel> GetChannel()
        {
            await this.Connect();
            return new Channel(this.ChannelData, this);
        }
    }
}