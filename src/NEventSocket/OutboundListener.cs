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
    using NEventSocket.Sockets;

    /// <summary>
    /// Listens for Outbound connections from FreeSwitch, providing notifications via the Connections observable.
    /// </summary>
    public class OutboundListener : ObservableListener<OutboundSocket>
    {
        /// <summary>
        /// Initializes a new OutboundListener on the given port.
        /// Pass 0 as the port to auto-assign a dynamic port. Usually used for testing.
        /// </summary>
        /// <param name="port">The Tcp port to listen on.</param>
        public OutboundListener(int port) : base(port, tcpClient => new OutboundSocket(tcpClient))
        {
        }

        /// <summary>
        /// Gets an observable sequence of incoming calls wrapped as <seealso cref="IChannel"/> abstractions.
        /// </summary>
        public IObservable<IChannel> Channels
        {
            get
            {
                return Connections.Select(c => c.GetChannel().Result);
            }
        }
    }
}