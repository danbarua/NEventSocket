// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConsoleLogProvider.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   Defines the ConsoleLogProvider type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Logging.LogProviders
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    public class ConsoleLogProvider : ILogProvider
    {
        public ILog GetLogger(string name)
        {
            return new ColouredConsoleLogger(name);
        }

        public class ColouredConsoleLogger : ILog
        {
            private readonly string name;

            public ColouredConsoleLogger(string name)
            {
                this.name = name;
            }

            private static readonly Dictionary<LogLevel, ConsoleColor> Colors = new Dictionary<LogLevel, ConsoleColor>
            {
                { LogLevel.Fatal, ConsoleColor.Red },
                { LogLevel.Error, ConsoleColor.Yellow },
                { LogLevel.Warn, ConsoleColor.Magenta },
                { LogLevel.Info, ConsoleColor.White },
                { LogLevel.Debug, ConsoleColor.Gray },
                { LogLevel.Trace, ConsoleColor.DarkGray },
            };

            public void Log(LogLevel logLevel, Func<string> messageFunc)
            {
                this.Write(logLevel, messageFunc());
            }

            public void Log<TException>(LogLevel logLevel, Func<string> messageFunc, TException exception) where TException : Exception
            {
                this.Write(logLevel, messageFunc(), exception);
            }

            protected void Write(LogLevel logLevel, string message, Exception e = null)
            {
                var sb = new StringBuilder();
                this.FormatOutput(sb, logLevel, message, e);
                ConsoleColor color;

                if (Colors.TryGetValue(logLevel, out color))
                {
                    var originalColor = Console.ForegroundColor;
                    try
                    {
                        Console.ForegroundColor = color;
                        Console.Out.WriteLine(sb.ToString());
                        return;
                    }
                    finally
                    {
                        Console.ForegroundColor = originalColor;
                    }
                }

                Console.Out.WriteLine(sb.ToString());
            }

            protected virtual void FormatOutput(StringBuilder stringBuilder, LogLevel level, object message, Exception e)
            {
                if (stringBuilder == null) throw new ArgumentNullException("stringBuilder");

                stringBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture));

                stringBuilder.Append(" ");

                // Append a readable representation of the log level
                stringBuilder.Append(("[" + level.ToString().ToUpper() + "]").PadRight(8));

                stringBuilder.Append("(" + this.name + ") ");

                // Append the message
                stringBuilder.Append(message);

                // Append stack trace if not null
                if (e != null)
                {
                    stringBuilder.Append(Environment.NewLine).Append(e.GetType());
                    stringBuilder.Append(Environment.NewLine).Append(e.Message);
                    stringBuilder.Append(Environment.NewLine).Append(e.StackTrace);
                }
            }
        }
    }
}