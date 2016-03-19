namespace NEventSocket.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    using NEventSocket.Util.ObjectPooling;

    public class ColouredConsoleLogProvider : ILogProvider
    {
        private readonly LogLevel minLogLevel;

        public ColouredConsoleLogProvider(LogLevel minLogLevel = LogLevel.Trace)
        {
            this.minLogLevel = minLogLevel;
        }

        static ColouredConsoleLogProvider()
        {
            MessageFormatter = DefaultMessageFormatter;
            Colors = new Dictionary<LogLevel, ConsoleColor>
                     {
                         { LogLevel.Fatal, ConsoleColor.Red },
                         { LogLevel.Error, ConsoleColor.Yellow },
                         { LogLevel.Warn, ConsoleColor.Magenta },
                         { LogLevel.Info, ConsoleColor.White },
                         { LogLevel.Debug, ConsoleColor.Gray },
                         { LogLevel.Trace, ConsoleColor.DarkGray },
                     };
        }

        Logger ILogProvider.GetLogger(string name)
        {
            return new ColouredConsoleLogger(name, minLogLevel).Log;
        }

        public IDisposable OpenNestedContext(string message)
        {
            return new NoOpDisposable();
        }

        public IDisposable OpenMappedContext(string key, string value)
        {
            return new NoOpDisposable();
        }

        /// <summary>
        /// A delegate returning a formatted log message
        /// </summary>
        /// <param name="loggerName">The name of the Logger</param>
        /// <param name="level">The Log Level</param>
        /// <param name="message">The Log Message</param>
        /// <param name="e">The Exception, if there is one</param>
        /// <returns>A formatted Log Message string.</returns>
        public delegate string MessageFormatterDelegate(string loggerName, LogLevel level, object message, Exception e);

        public static Dictionary<LogLevel, ConsoleColor> Colors { get; set; }

        public static MessageFormatterDelegate MessageFormatter { get; set; }

        protected static string DefaultMessageFormatter(string loggerName, LogLevel level, object message, Exception e)
        {
            var stringBuilder = StringBuilderPool.Allocate();

            stringBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture));

            stringBuilder.Append(" ");

            // Append a readable representation of the log level
            stringBuilder.Append(("[" + level.ToString().ToUpper() + "]").PadRight(8));

            stringBuilder.Append("(" + loggerName + ") ");

            // Append the message
            stringBuilder.Append(message);

            // Append stack trace if there is an exception
            if (e != null)
            {
                stringBuilder.Append(Environment.NewLine).Append(e.GetType());
                stringBuilder.Append(Environment.NewLine).Append(e.Message);
                stringBuilder.Append(Environment.NewLine).Append(e.StackTrace);
            }

            return StringBuilderPool.ReturnAndFree(stringBuilder);
        }

        public class ColouredConsoleLogger : ILog
        {
            private readonly string name;

            private readonly LogLevel minLogLevel;

            public ColouredConsoleLogger(string name, LogLevel minLogLevel)
            {
                this.name = name;
                this.minLogLevel = minLogLevel;
            }

            public bool Log(LogLevel logLevel, Func<string> messageFunc)
            {
                if (logLevel < minLogLevel)
                {
                    return false;
                }

                if (messageFunc == null)
                {
                    return true;
                }

                Write(logLevel, messageFunc());
                return true;
            }

            public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters)
            {
                if (logLevel < minLogLevel)
                {
                    return false;
                }

                if (messageFunc == null)
                {
                    return true;
                }

                Write(logLevel, messageFunc(), exception);
                return true;
            }

            public void Log<TException>(LogLevel logLevel, Func<string> messageFunc, TException exception) where TException : Exception
            {
                Write(logLevel, messageFunc(), exception);
            }

            protected void Write(LogLevel logLevel, string message, Exception e = null)
            {
                var formattedMessage = MessageFormatter(name, logLevel, message, e);
                ConsoleColor color;

                if (Colors.TryGetValue(logLevel, out color))
                {
                    var originalColor = Console.ForegroundColor;
                    try
                    {
                        Console.ForegroundColor = color;
                        Console.Out.WriteLine(formattedMessage);
                    }
                    finally
                    {
                        Console.ForegroundColor = originalColor;
                    }
                }
                else
                {
                    Console.Out.WriteLine(formattedMessage);
                }
            }
        }

        public class NoOpDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}