namespace NEventSocket.Tests
{
    using System;

    using NEventSocket.FreeSwitch.Api;

    using Xunit;

    public class OriginateTests
    {
        [Fact]
        public void Can_format_originate_options()
        {
            var options = new OriginateOptions()
                              {
                                  CallerIdName = "Dan",
                                  CallerIdNumber = "0123457890",
                                  ExecuteOnOriginate = "my_app::my_arg",
                                  Retries = 5,
                                  RetrySleepMs = 200,
                                  ReturnRingReady = true,
                                  Timeout = 60,
                                  UUID = Guid.NewGuid().ToString(),
                                  IgnoreEarlyMedia = true,
                              };

            Console.WriteLine(options.ToString());
        }
    }
}