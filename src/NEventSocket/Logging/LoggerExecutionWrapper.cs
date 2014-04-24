// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggerExecutionWrapper.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   The logger execution wrapper.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Logging
{
    using System;

    /// <summary>The logger execution wrapper.</summary>
    public class LoggerExecutionWrapper : ILog
    {
        /// <summary>The failed to generate log message.</summary>
        public static string FailedToGenerateLogMessage = "Failed to generate log message";

        private readonly ILog _logger;

        /// <summary>Initialises a new instance of the <see cref="LoggerExecutionWrapper"/> class.</summary>
        /// <param name="logger">The logger.</param>
        public LoggerExecutionWrapper(ILog logger)
        {
            this._logger = logger;
        }

        /// <summary>Gets the wrapped logger.</summary>
        public ILog WrappedLogger
        {
            get
            {
                return this._logger;
            }
        }

        /// <summary>The log.</summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="messageFunc">The message func.</param>
        public void Log(LogLevel logLevel, Func<string> messageFunc)
        {
            Func<string> wrappedMessageFunc = () =>
                {
                    try
                    {
                        return messageFunc();
                    }
                    catch (Exception ex)
                    {
                        this.Log(LogLevel.Error, () => FailedToGenerateLogMessage, ex);
                    }

                    return null;
                };
            this._logger.Log(logLevel, wrappedMessageFunc);
        }

        /// <summary>The log.</summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="messageFunc">The message func.</param>
        /// <param name="exception">The exception.</param>
        /// <typeparam name="TException"></typeparam>
        public void Log<TException>(LogLevel logLevel, Func<string> messageFunc, TException exception)
            where TException : Exception
        {
            Func<string> wrappedMessageFunc = () =>
                {
                    try
                    {
                        return messageFunc();
                    }
                    catch (Exception ex)
                    {
                        this.Log(LogLevel.Error, () => FailedToGenerateLogMessage, ex);
                    }

                    return null;
                };
            this._logger.Log(logLevel, wrappedMessageFunc, exception);
        }
    }
}