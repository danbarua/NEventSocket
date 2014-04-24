// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NLogLogProvider.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   The n log log provider.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Logging.LogProviders
{
    using System;
    using System.Linq.Expressions;

    /// <summary>The n log log provider.</summary>
    public class NLogLogProvider : ILogProvider
    {
        private readonly Func<string, object> getLoggerByNameDelegate;

        private static bool _providerIsAvailableOverride = true;

        /// <summary>Initialises a new instance of the <see cref="NLogLogProvider"/> class.</summary>
        /// <exception cref="InvalidOperationException"></exception>
        public NLogLogProvider()
        {
            if (!IsLoggerAvailable()) throw new InvalidOperationException("NLog.LogManager not found");
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
            return new NLogLogger(this.getLoggerByNameDelegate(name));
        }

        /// <summary>The is logger available.</summary>
        /// <returns>The <see cref="bool"/>.</returns>
        public static bool IsLoggerAvailable()
        {
            return ProviderIsAvailableOverride && GetLogManagerType() != null;
        }

        private static Type GetLogManagerType()
        {
            return Type.GetType("NLog.LogManager, nlog");
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

        /// <summary>The n log logger.</summary>
        public class NLogLogger : ILog
        {
            private readonly dynamic logger;

            /// <summary>Initialises a new instance of the <see cref="NLogLogger"/> class.</summary>
            /// <param name="logger">The logger.</param>
            internal NLogLogger(object logger)
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
                    case LogLevel.Debug:
                        if (this.logger.IsDebugEnabled) this.logger.Debug(messageFunc());
                        break;
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
                        if (this.logger.IsTraceEnabled) this.logger.Trace(messageFunc());
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
                    case LogLevel.Debug:
                        if (this.logger.IsDebugEnabled) this.logger.DebugException(messageFunc(), exception);
                        break;
                    case LogLevel.Info:
                        if (this.logger.IsInfoEnabled) this.logger.InfoException(messageFunc(), exception);
                        break;
                    case LogLevel.Warn:
                        if (this.logger.IsWarnEnabled) this.logger.WarnException(messageFunc(), exception);
                        break;
                    case LogLevel.Error:
                        if (this.logger.IsErrorEnabled) this.logger.ErrorException(messageFunc(), exception);
                        break;
                    case LogLevel.Fatal:
                        if (this.logger.IsFatalEnabled) this.logger.FatalException(messageFunc(), exception);
                        break;
                    default:
                        if (this.logger.IsTraceEnabled) this.logger.TraceException(messageFunc(), exception);
                        break;
                }
            }
        }
#else
        public class NLogLogger : ILog
        {
            private readonly object logger;
            private static readonly Type LoggerType = Type.GetType("NLog.Logger, NLog");
            private static readonly Func<object, bool> IsTraceEnabledDelegate;
            private static readonly Action<object, string> TraceDelegate;
            private static readonly Action<object, string, Exception> TraceExceptionDelegate;

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

            static NLogLogger()
            {
                IsTraceEnabledDelegate = GetPropertyGetter("IsTraceEnabled");
                TraceDelegate = GetMethodCallForMessage("Trace");
                TraceExceptionDelegate = GetMethodCallForMessageException("TraceException");

                IsDebugEnabledDelegate = GetPropertyGetter("IsDebugEnabled");
                DebugDelegate = GetMethodCallForMessage("Debug");
                DebugExceptionDelegate = GetMethodCallForMessageException("DebugException");

                IsInfoEnabledDelegate = GetPropertyGetter("IsInfoEnabled");
                InfoDelegate = GetMethodCallForMessage("Info");
                InfoExceptionDelegate = GetMethodCallForMessageException("InfoException");

                IsErrorEnabledDelegate = GetPropertyGetter("IsErrorEnabled");
                ErrorDelegate = GetMethodCallForMessage("Error");
                ErrorExceptionDelegate = GetMethodCallForMessageException("ErrorException");

                IsWarnEnabledDelegate = GetPropertyGetter("IsWarnEnabled");
                WarnDelegate = GetMethodCallForMessage("Warn");
                WarnExceptionDelegate = GetMethodCallForMessageException("WarnException");

                IsFatalEnabledDelegate = GetPropertyGetter("IsFatalEnabled");
                FatalDelegate = GetMethodCallForMessage("Fatal");
                FatalExceptionDelegate = GetMethodCallForMessageException("FatalException");
            }

            public NLogLogger(object logger)
            {
                this.logger = logger;
            }

            public void Log(LogLevel logLevel, Func<string> messageFunc)
            {
                switch (logLevel)
                {
                    case LogLevel.Debug:
                        if (IsDebugEnabledDelegate(logger))
                        {
                            DebugDelegate(logger, messageFunc());
                        }
                        break;
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
                        if (IsTraceEnabledDelegate(logger))
                        {
                            TraceDelegate(logger, messageFunc());
                        }
                        break;
                }
            }

            public void Log<TException>(LogLevel logLevel, Func<string> messageFunc, TException exception)
                where TException : Exception
            {
                switch (logLevel)
                {
                    case LogLevel.Debug:
                        if (IsDebugEnabledDelegate(logger))
                        {
                            DebugExceptionDelegate(logger, messageFunc(), exception);
                        }
                        break;
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
                        if (IsTraceEnabledDelegate(logger))
                        {
                            TraceExceptionDelegate(logger, messageFunc(), exception);
                        }
                        break;
                }
            }

            private static Func<object, bool> GetPropertyGetter(string propertyName)
            {
                ParameterExpression funcParam = Expression.Parameter(typeof(object), "l");
                Expression convertedParam = Expression.Convert(funcParam, LoggerType);
                Expression property = Expression.Property(convertedParam, propertyName);
                return (Func<object, bool>) Expression.Lambda(property, funcParam).Compile();
            }

            private static Action<object, string> GetMethodCallForMessage(string methodName)
            {
                ParameterExpression loggerParam = Expression.Parameter(typeof(object), "l");
                ParameterExpression messageParam = Expression.Parameter(typeof(string), "o");
                Expression convertedParam = Expression.Convert(loggerParam, LoggerType);
                MethodCallExpression methodCall = Expression.Call(convertedParam, 
                                                                  LoggerType.GetMethod(methodName, new[] {typeof(object)}), 
                                                                  messageParam);
                return (Action<object, string>) Expression.Lambda(methodCall, new[] {loggerParam, messageParam}).Compile();
            }

            private static Action<object, string, Exception> GetMethodCallForMessageException(string methodName)
            {
                ParameterExpression loggerParam = Expression.Parameter(typeof(object), "l");
                ParameterExpression messageParam = Expression.Parameter(typeof(string), "o");
                ParameterExpression exceptionParam = Expression.Parameter(typeof(Exception), "e");
                Expression convertedParam = Expression.Convert(loggerParam, LoggerType);
                var method = LoggerType.GetMethod(methodName, new[] {typeof(string), typeof(Exception)});
                MethodCallExpression methodCall = Expression.Call(convertedParam, method, messageParam, exceptionParam);
                return (Action<object, string, Exception>) Expression.Lambda(methodCall, 
                                                                             new[] {loggerParam, messageParam, exceptionParam}).Compile();
            }
        }
#endif
    }
}