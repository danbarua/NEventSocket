// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogProvider.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   The log provider.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Logging
{
    using System;
    using System.Diagnostics;

    using NEventSocket.Logging.LogProviders;

    /// <summary>The log provider.</summary>
    public static class LogProvider
    {
        private static ILogProvider currentLogProvider;

        /// <summary>The get current class logger.</summary>
        /// <returns>The <see cref="ILog"/>.</returns>
        public static ILog GetCurrentClassLogger()
        {
#if SILVERLIGHT
            var stackFrame = new StackTrace().GetFrame(1);
#else
            var stackFrame = new StackFrame(1, false);
#endif
            return GetLogger(stackFrame.GetMethod().DeclaringType);
        }

        /// <summary>The get logger.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The <see cref="ILog"/>.</returns>
        public static ILog GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }

        /// <summary>The get logger.</summary>
        /// <param name="name">The name.</param>
        /// <returns>The <see cref="ILog"/>.</returns>
        public static ILog GetLogger(string name)
        {
            var temp = currentLogProvider ?? ResolveLogProvider();
            return temp == null ? new NoOpLogger() : (ILog)new LoggerExecutionWrapper(temp.GetLogger(name));
        }

        /// <summary>The set current log provider.</summary>
        /// <param name="logProvider">The log provider.</param>
        public static void SetCurrentLogProvider(ILogProvider logProvider)
        {
            currentLogProvider = logProvider;
        }

        private static ILogProvider ResolveLogProvider()
        {
            if (NLogLogProvider.IsLoggerAvailable()) return new NLogLogProvider();
            if (Log4NetLogProvider.IsLoggerAvailable()) return new Log4NetLogProvider();
            return null;
        }

        /// <summary>The no op logger.</summary>
        public class NoOpLogger : ILog
        {
            /// <summary>The log.</summary>
            /// <param name="logLevel">The log level.</param>
            /// <param name="messageFunc">The message func.</param>
            public void Log(LogLevel logLevel, Func<string> messageFunc)
            {
            }

            /// <summary>The log.</summary>
            /// <param name="logLevel">The log level.</param>
            /// <param name="messageFunc">The message func.</param>
            /// <param name="exception">The exception.</param>
            /// <typeparam name="TException"></typeparam>
            public void Log<TException>(LogLevel logLevel, Func<string> messageFunc, TException exception)
                where TException : Exception
            {
            }
        }
    }
}