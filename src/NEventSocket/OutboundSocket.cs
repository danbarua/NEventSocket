// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutboundSocket.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket
{
    using System;
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Common.Logging;

    using NEventSocket.FreeSwitch;
    using NEventSocket.FreeSwitch.Channel;
    using NEventSocket.Sockets;
    using NEventSocket.Util;

    public class OutboundSocket : EventSocket
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        protected internal OutboundSocket(TcpClient tcpClient)
            : base(tcpClient)
        {
        }

        public async Task Connect()
        {
            var result = await this.SendCommand("connect")
                    .Then(
                        () =>
                        this.disposables.Add(
                            this.Messages.Where(x => x.ContentType == ContentTypes.DisconnectNotice)
                                .Take(1)
                                .Subscribe(_ => Log.Trace("Disconnect Notice received."))));

            this.ChannelData = new EventMessage(result);
        }

        public async Task<IChannel> GetChannel()
        {
            await this.Connect();
            return new Channel(this.ChannelData, this);
        }

        /// <summary>
        /// When FS connects to an "Event Socket Outbound" handler, it sends
        /// a "CHANNEL_DATA" event in the headers of the Command-Reply received in response to Connect();
        /// </summary>
        public EventMessage ChannelData { get; private set; }
    }
}