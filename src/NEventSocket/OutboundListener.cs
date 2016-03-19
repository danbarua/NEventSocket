// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutboundListener.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket
{
    using System;
    using System.Reactive.Linq;

    using NEventSocket.Channels;
    using NEventSocket.Logging;
    using NEventSocket.Sockets;

    /// <summary>
    /// Listens for Outbound connections from FreeSwitch, providing notifications via the Connections observable.
    /// </summary>
    public class OutboundListener : ObservableListener<OutboundSocket>
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        private readonly IObservable<Channel> channels;

        /// <summary>
        /// Initializes a new OutboundListener on the given port.
        /// Pass 0 as the port to auto-assign a dynamic port. Usually used for testing.
        /// </summary>
        /// <param name="port">The Tcp port to listen on.</param>
        public OutboundListener(int port) : base(port, tcpClient => new OutboundSocket(tcpClient))
        {
            channels = Connections.SelectMany(
                    async socket =>
                    {
                        await socket.Connect().ConfigureAwait(false);
                        return await Channel.Create(socket).ConfigureAwait(false);
                    });
        }

        /// <summary>
        /// Gets an observable sequence of incoming calls wrapped as <seealso cref="Channel"/> abstractions.
        /// </summary>
        public IObservable<Channel> Channels
        {
            get
            {
                //if there is an error connecting the channel, eg. FS hangs up and goes away
                //before we can do the connect/channel_data handshake
                //then carry on allowing new connections
                return channels
                    .Do(_ => { }, ex => Log.ErrorException("Unable to connect Channel", ex))
                    .Retry();
            }
        }
    }
}