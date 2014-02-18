// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IInboundSocket.cs" company="Business Systems (UK) Ltd">
//   Copyright © Business Systems (UK) Ltd and contributors. All rights reserved.
// </copyright>
// <summary>
//   The InboundSocket interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Sockets.Interfaces
{
    using System;

    /// <summary>The InboundSocket interface.</summary>
    public interface IInboundSocket
    {
        /// <summary>Raised when the client is authenticated.</summary>
        event EventHandler Authenticated;
    }
}