// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILog.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   The Log interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Logging
{
    using System;

    /// <summary>The Log interface.</summary>
    public interface ILog
    {
        /// <summary>The log.</summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="messageFunc">The message func.</param>
        void Log(LogLevel logLevel, Func<string> messageFunc);

        /// <summary>The log.</summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="messageFunc">The message func.</param>
        /// <param name="exception">The exception.</param>
        /// <typeparam name="TException"></typeparam>
        void Log<TException>(LogLevel logLevel, Func<string> messageFunc, TException exception)
            where TException : Exception;
    }
}