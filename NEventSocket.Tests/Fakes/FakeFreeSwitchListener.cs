// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FakeFreeSwitchListener.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   Defines the FakeFreeSwitchListener type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Tests.Fakes
{
    using NEventSocket.Sockets;

    public class FakeFreeSwitchListener : ObservableListener<FakeFreeSwitchSocket>
    {
        public FakeFreeSwitchListener(int port)
            : base(port, client => new FakeFreeSwitchSocket(client))
        {
        }
    }
}