using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEventSocket.Tests.TestSupport
{
    using NEventSocket.Logging;
    using NEventSocket.Logging.LogProviders;

    using Xunit;

    public class TestEnvironmentSupport
    {
        static TestEnvironmentSupport()
        {
            //issues logging to stdout in AppVeyor and Travis environments, best to turn it off
            if (Environment.GetEnvironmentVariable("APPVEYOR_BUILD_NUMBER") == null && Environment.GetEnvironmentVariable("TRAVIS") == null)
            {
                LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider(LogLevel.Trace));
            }
        }

        [Fact]
        public void EmptyTest()
        {
        }
    }
}
