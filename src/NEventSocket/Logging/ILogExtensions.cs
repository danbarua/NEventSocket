// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILogExtensions.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   The i log extensions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Logging
{
    using System;
    using System.Globalization;

    /// <summary>The i log extensions.</summary>
    public static class ILogExtensions
    {
        /// <summary>The debug.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="messageFunc">The message func.</param>
        public static void Trace(this ILog logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Trace, messageFunc);
        }

        /// <summary>The debug.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void Trace(this ILog logger, string message)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Trace, () => message);
        }

        /// <summary>The debug format.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        public static void TraceFormat(this ILog logger, string message, params object[] args)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Trace, () => string.Format(CultureInfo.InvariantCulture, message, args));
        }

        /// <summary>The debug.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="messageFunc">The message func.</param>
        public static void Debug(this ILog logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Debug, messageFunc);
        }

        /// <summary>The debug.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void Debug(this ILog logger, string message)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Debug, () => message);
        }

        /// <summary>The debug format.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        public static void DebugFormat(this ILog logger, string message, params object[] args)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Debug, () => string.Format(CultureInfo.InvariantCulture, message, args));
        }

        /// <summary>The error.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void Error(this ILog logger, string message)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Error, () => message);
        }

        /// <summary>The error format.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        public static void ErrorFormat(this ILog logger, string message, params object[] args)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Error, () => string.Format(CultureInfo.InvariantCulture, message, args));
        }

        /// <summary>The error exception.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public static void ErrorException(this ILog logger, string message, Exception exception)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Error, () => message, exception);
        }

        /// <summary>The info.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="messageFunc">The message func.</param>
        public static void Info(this ILog logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Info, messageFunc);
        }

        /// <summary>The info.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void Info(this ILog logger, string message)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Info, () => message);
        }

        /// <summary>The info format.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        public static void InfoFormat(this ILog logger, string message, params object[] args)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Info, () => string.Format(CultureInfo.InvariantCulture, message, args));
        }

        /// <summary>The warn.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="messageFunc">The message func.</param>
        public static void Warn(this ILog logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Warn, messageFunc);
        }

        /// <summary>The warn.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        public static void Warn(this ILog logger, string message)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Warn, () => message);
        }

        /// <summary>The warn format.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        public static void WarnFormat(this ILog logger, string message, params object[] args)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Warn, () => string.Format(CultureInfo.InvariantCulture, message, args));
        }

        /// <summary>The warn exception.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <param name="ex">The ex.</param>
        public static void WarnException(this ILog logger, string message, Exception ex)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Warn, () => string.Format(CultureInfo.InvariantCulture, message), ex);
        }

        private static void GuardAgainstNullLogger(ILog logger)
        {
            if (logger == null) throw new ArgumentException("logger is null", "logger");
        }
    }
}