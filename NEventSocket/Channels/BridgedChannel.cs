// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BridgedChannel.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   Defines the BridgedChannel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Channels
{
    using NEventSocket.FreeSwitch;
    using NEventSocket.Sockets;

    public class BridgedChannel : BasicChannel
    {
        protected internal BridgedChannel(ChannelEvent eventMessage, EventSocket eventSocket) : base(eventMessage, eventSocket)
        {
        }
    }
}