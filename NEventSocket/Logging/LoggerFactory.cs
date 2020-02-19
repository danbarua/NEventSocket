namespace NEventSocket.Logging
{
    using Microsoft.Extensions.Logging;

    public static class Logger
    {
        private static ILoggerFactory internalFactory;

        public static void Configure(ILoggerFactory factory)
        {
            internalFactory = factory;
        }

        public static ILogger<T> Get<T>()
        {
            return internalFactory.CreateLogger<T>();
        }
    }
}
