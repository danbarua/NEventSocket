// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEventSocket.cs" company="Business Systems (UK) Ltd">
//   Copyright © Business Systems (UK) Ltd and contributors. All rights reserved.
// </copyright>
// <summary>
//   The EventSocket interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket
{
    using System;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;

    /// <summary>The EventSocket interface.</summary>
    public interface IEventSocket : IDisposable
    {
        /// <summary>Gets a value indicating whether the socket is connected.</summary>
        bool IsConnected { get; }

        /// <summary>Gets the stream of incoming messages.</summary>
        IObservable<BasicMessage> Messages { get; }

        Task<ApiResponse> Api(string command);

        Task<BackgroundJobResult> BackgroundJob(string command, string arguments = null, Guid? jobUUID = null);

        Task<EventMessage> Execute(string uuid, string application, string applicationArguments = null, int loops = 1, bool eventLock = false, bool async = false);

        Task<CommandReply> SendCommand(string command);
    }
}