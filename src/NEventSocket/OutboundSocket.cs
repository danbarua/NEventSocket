// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutboundSocket.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket
{
    using System;
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Security;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
    using NEventSocket.Sockets;
    using NEventSocket.Util;

    /// <summary>
    /// Represents a connection made outbound from FreeSwitch to the controlling application.
    /// </summary>
    /// <remarks>
    /// See https://wiki.freeswitch.org/wiki/Event_Socket_Outbound
    /// </remarks>
    public class OutboundSocket : EventSocket
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="OutboundSocket"/> class.
        /// </summary>
        /// <param name="tcpClient">The TCP client to wrap.</param>
        protected internal OutboundSocket(TcpClient tcpClient) : base(tcpClient)
        {
        }

        /// <summary>
        /// When FS connects to an "Event Socket Outbound" handler, it sends
        /// a "CHANNEL_DATA" event in the headers of the Command-Reply received in response to Connect();
        /// </summary>
        public EventMessage ChannelData { get; private set; }

        /// <summary>
        /// Sends the connect command to FreeSwitch, populating the <see cref="ChannelData"/> property on reply.
        /// </summary>
        public async Task<EventMessage> Connect()
        {
            var response = await SendCommand("connect").ConfigureAwait(false);
            ChannelData = new EventMessage(response);

            Messages.FirstAsync(m => m.ContentType == ContentTypes.DisconnectNotice)
                            .Subscribe(dn => Log.Trace(() => "Channel {0} Disconnect Notice {1} received.".Fmt(ChannelData.UUID, dn.BodyText)));

            return ChannelData;
        }
    }
}