// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutboundListener.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   Listens for Outbound connections from FreeSwitch
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket
{
    using NEventSocket.Sockets;

    /// <summary>
    ///     Listens for Outbound connections from FreeSwitch
    /// </summary>
    public class OutboundListener : ObservableListener<OutboundSocket>
    {
        public OutboundListener(int port)
            : base(port, tcpClient => new OutboundSocket(tcpClient))
        {
        }
    }
}