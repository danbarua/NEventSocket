// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Log4NetLogProvider.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   The log 4 net log provider.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Logging.LogProviders
{
    using System;
    using System.Linq.Expressions;

    /// <summary>The log 4 net log provider.</summary>
    public class Log4NetLogProvider : ILogProvider
    {
        private readonly Func<string, object> getLoggerByNameDelegate;

        private static bool _providerIsAvailableOverride = true;

        /// <summary>Initialises a new instance of the <see cref="Log4NetLogProvider"/> class.</summary>
        /// <exception cref="InvalidOperationException"></exception>
        public Log4NetLogProvider()
        {
            if (!IsLoggerAvailable()) throw new InvalidOperationException("log4net.LogManager not found");
            this.getLoggerByNameDelegate = GetGetLoggerMethodCall();
        }

        /// <summary>Gets or sets a value indicating whether provider is available override.</summary>
        public static bool ProviderIsAvailableOverride
        {
            get
            {
                return _providerIsAvailableOverride;
            }

            set
            {
                _providerIsAvailableOverride = value;
            }
        }

        /// <summary>The get logger.</summary>
        /// <param name="name">The name.</param>
        /// <returns>The <see cref="ILog"/>.</returns>
        public ILog GetLogger(string name)
        {
            return new Log4NetLogger(this.getLoggerByNameDelegate(name));
        }

        /// <summary>The is logger available.</summary>
        /// <returns>The <see cref="bool"/>.</returns>
        public static bool IsLoggerAvailable()
        {
            return ProviderIsAvailableOverride && GetLogManagerType() != null;
        }

        private static Type GetLogManagerType()
        {
            return Type.GetType("log4net.LogManager, log4net");
        }

        private static Func<string, object> GetGetLoggerMethodCall()
        {
            var logManagerType = GetLogManagerType();
            var method = logManagerType.GetMethod("GetLogger", new[] { typeof(string) });
            ParameterExpression resultValue;
            var keyParam = Expression.Parameter(typeof(string), "key");
            var methodCall = Expression.Call(null, method, new Expression[] { resultValue = keyParam });
            return Expression.Lambda<Func<string, object>>(methodCall, new[] { resultValue }).Compile();
        }

#if !NET_3_5

        /// <summary>The log 4 net logger.</summary>
        public class Log4NetLogger : ILog
        {
            private readonly dynamic logger;

            /// <summary>Initialises a new instance of the <see cref="Log4NetLogger"/> class.</summary>
            /// <param name="logger">The logger.</param>
            internal Log4NetLogger(object logger)
            {
                this.logger = logger;
            }

            /// <summary>The log.</summary>
            /// <param name="logLevel">The log level.</param>
            /// <param name="messageFunc">The message func.</param>
            public void Log(LogLevel logLevel, Func<string> messageFunc)
            {
                switch (logLevel)
                {
                    case LogLevel.Info:
                        if (this.logger.IsInfoEnabled) this.logger.Info(messageFunc());
                        break;
                    case LogLevel.Warn:
                        if (this.logger.IsWarnEnabled) this.logger.Warn(messageFunc());
                        break;
                    case LogLevel.Error:
                        if (this.logger.IsErrorEnabled) this.logger.Error(messageFunc());
                        break;
                    case LogLevel.Fatal:
                        if (this.logger.IsFatalEnabled) this.logger.Fatal(messageFunc());
                        break;
                    default:
                        if (this.logger.IsDebugEnabled)
                        {
                            this.logger.Debug(messageFunc());
                                
                                // Log4Net doesn't have a 'Trace' level, so all Trace messages are written as 'Debug'
                        }

                        break;
                }
            }

            /// <summary>The log.</summary>
            /// <param name="logLevel">The log level.</param>
            /// <param name="messageFunc">The message func.</param>
            /// <param name="exception">The exception.</param>
            /// <typeparam name="TException"></typeparam>
            public void Log<TException>(LogLevel logLevel, Func<string> messageFunc, TException exception)
                where TException : Exception
            {
                switch (logLevel)
                {
                    case LogLevel.Info:
                        if (this.logger.IsDebugEnabled) this.logger.Info(messageFunc(), exception);
                        break;
                    case LogLevel.Warn:
                        if (this.logger.IsWarnEnabled) this.logger.Warn(messageFunc(), exception);
                        break;
                    case LogLevel.Error:
                        if (this.logger.IsErrorEnabled) this.logger.Error(messageFunc(), exception);
                        break;
                    case LogLevel.Fatal:
                        if (this.logger.IsFatalEnabled) this.logger.Fatal(messageFunc(), exception);
                        break;
                    default:
                        if (this.logger.IsDebugEnabled) this.logger.Debug(messageFunc(), exception);
                        break;
                }
            }
        }
#else
        public class Log4NetLogger : ILog
        {
            private readonly object logger;
            private static readonly Type LoggerType = Type.GetType("log4net.ILog, log4net");

            private static readonly Func<object, bool> IsDebugEnabledDelegate;
            private static readonly Action<object, string> DebugDelegate;
            private static readonly Action<object, string, Exception> DebugExceptionDelegate;

            private static readonly Func<object, bool> IsInfoEnabledDelegate;
            private static readonly Action<object, string> InfoDelegate;
            private static readonly Action<object, string, Exception> InfoExceptionDelegate;

            private static readonly Func<object, bool> IsWarnEnabledDelegate;
            private static readonly Action<object, string> WarnDelegate;
            private static readonly Action<object, string, Exception> WarnExceptionDelegate;

            private static readonly Func<object, bool> IsErrorEnabledDelegate;
            private static readonly Action<object, string> ErrorDelegate;
            private static readonly Action<object, string, Exception> ErrorExceptionDelegate;

            private static readonly Func<object, bool> IsFatalEnabledDelegate;
            private static readonly Action<object, string> FatalDelegate;
            private static readonly Action<object, string, Exception> FatalExceptionDelegate;

            static Log4NetLogger()
            {
                IsDebugEnabledDelegate = GetPropertyGetter("IsDebugEnabled");
                DebugDelegate = GetMethodCallForMessage("Debug");
                DebugExceptionDelegate = GetMethodCallForMessageException("Debug");

                IsInfoEnabledDelegate = GetPropertyGetter("IsInfoEnabled");
                InfoDelegate = GetMethodCallForMessage("Info");
                InfoExceptionDelegate = GetMethodCallForMessageException("Info");

                IsErrorEnabledDelegate = GetPropertyGetter("IsErrorEnabled");
                ErrorDelegate = GetMethodCallForMessage("Error");
                ErrorExceptionDelegate = GetMethodCallForMessageException("Error");

                IsWarnEnabledDelegate = GetPropertyGetter("IsWarnEnabled");
                WarnDelegate = GetMethodCallForMessage("Warn");
                WarnExceptionDelegate = GetMethodCallForMessageException("Warn");

                IsFatalEnabledDelegate = GetPropertyGetter("IsFatalEnabled");
                FatalDelegate = GetMethodCallForMessage("Fatal");
                FatalExceptionDelegate = GetMethodCallForMessageException("Fatal");
            }

            public Log4NetLogger(object logger)
            {
                this.logger = logger;
            }

            public void Log(LogLevel logLevel, Func<string> messageFunc)
            {
                switch (logLevel)
                {
                    case LogLevel.Info:
                        if (IsInfoEnabledDelegate(logger))
                        {
                            InfoDelegate(logger, messageFunc());
                        }
                        break;
                    case LogLevel.Warn:
                        if (IsWarnEnabledDelegate(logger))
                        {
                            WarnDelegate(logger, messageFunc());
                        }
                        break;
                    case LogLevel.Error:
                        if (IsErrorEnabledDelegate(logger))
                        {
                            ErrorDelegate(logger, messageFunc());
                        }
                        break;
                    case LogLevel.Fatal:
                        if (IsFatalEnabledDelegate(logger))
                        {
                            FatalDelegate(logger, messageFunc());
                        }
                        break;
                    default:
                        if (IsDebugEnabledDelegate(logger))
                        {
                            DebugDelegate(logger, messageFunc());
                        }
                        break;
                }
            }

            public void Log<TException>(LogLevel logLevel, Func<string> messageFunc, TException exception)
                where TException : Exception
            {
                switch (logLevel)
                {
                    case LogLevel.Info:
                        if (IsInfoEnabledDelegate(logger))
                        {
                            InfoExceptionDelegate(logger, messageFunc(), exception);
                        }
                        break;
                    case LogLevel.Warn:
                        if (IsWarnEnabledDelegate(logger))
                        {
                            WarnExceptionDelegate(logger, messageFunc(), exception);
                        }
                        break;
                    case LogLevel.Error:
                        if (IsErrorEnabledDelegate(logger))
                        {
                            ErrorExceptionDelegate(logger, messageFunc(), exception);
                        }
                        break;
                    case LogLevel.Fatal:
                        if (IsFatalEnabledDelegate(logger))
                        {
                            FatalExceptionDelegate(logger, messageFunc(), exception);
                        }
                        break;
                    default:
                        if (IsDebugEnabledDelegate(logger))
                        {
                            DebugExceptionDelegate(logger, messageFunc(), exception);
                        }
                        break;
                }
            }

            private static Func<object, bool> GetPropertyGetter(string propertyName)
            {
                ParameterExpression funcParam = Expression.Parameter(typeof(object), "l");
                Expression convertedParam = Expression.Convert(funcParam, LoggerType);
                Expression property = Expression.Property(convertedParam, propertyName);
                return (Func<object, bool>)Expression.Lambda(property, funcParam).Compile();
            }

            private static Action<object, string> GetMethodCallForMessage(string methodName)
            {
                ParameterExpression loggerParam = Expression.Parameter(typeof(object), "l");
                ParameterExpression messageParam = Expression.Parameter(typeof(string), "o");
                Expression convertedParam = Expression.Convert(loggerParam, LoggerType);
                var method = LoggerType.GetMethod(methodName, new[] {typeof(string)});
                MethodCallExpression methodCall = Expression.Call(convertedParam, method, messageParam);
                return (Action<object, string>)Expression.Lambda(methodCall, new[] {loggerParam, messageParam}).Compile();
            }

            private static Action<object, string, Exception> GetMethodCallForMessageException(string methodName)
            {
                ParameterExpression loggerParam = Expression.Parameter(typeof(object), "l");
                ParameterExpression messageParam = Expression.Parameter(typeof(string), "o");
                ParameterExpression exceptionParam = Expression.Parameter(typeof(Exception), "e");
                Expression convertedParam = Expression.Convert(loggerParam, LoggerType);
                var method = LoggerType.GetMethod(methodName, new[] {typeof(string), typeof(Exception)});
                MethodCallExpression methodCall = Expression.Call(convertedParam, method, messageParam, exceptionParam);
                return (Action<object, string, Exception>)Expression.Lambda(methodCall, new[] {loggerParam, messageParam, exceptionParam}).Compile();
            }
        }
#endif
    }
}