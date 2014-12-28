// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutboundListener.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket
{
    using NEventSocket.Sockets;

    /// <summary>
    ///     Listens for Outbound connections from FreeSwitch
    /// </summary>
    public class OutboundListener : ObservableListener<OutboundSocket>
    {
        public OutboundListener(int port) : base(port, tcpClient => new OutboundSocket(tcpClient))
        {
        }
    }
}