// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutboundSocket.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket
{
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
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

        public Task<EventMessage> Connect()
        {
            return
                this.SendCommand("connect")
                    .ToObservable()
                    .Select(reply => new EventMessage(reply))
                    .Do(x =>
                        {
                            this.ChannelData = x;
                            this.Messages.FirstAsync(m => m.ContentType == ContentTypes.DisconnectNotice)
                                .Do(_ => Log.Trace(() => "Channel {0} Disconnect Notice received.".Fmt(ChannelData.UUID)));
                        })
                    .ToTask();
        }
    }
}