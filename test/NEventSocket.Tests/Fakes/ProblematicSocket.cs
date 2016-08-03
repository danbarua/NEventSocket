// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProblematicSocket.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Tests.Fakes
{
    using System.Net.Sockets;

    public class ProblematicSocket : FakeFreeSwitchSocket
    {
        public ProblematicSocket(TcpClient tcpClient) : base(tcpClient)
        {
        }
    }
}