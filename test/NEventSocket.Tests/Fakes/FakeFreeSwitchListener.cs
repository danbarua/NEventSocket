// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FakeFreeSwitchListener.cs" company="Business Systems (UK) Ltd">
//   Copyright © Business Systems (UK) Ltd and contributors. All rights reserved.
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