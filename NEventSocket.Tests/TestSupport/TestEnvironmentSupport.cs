using System;
using Xunit;

namespace NEventSocket.Tests.TestSupport
{   
    public class TestEnvironmentSupport
    {
        static TestEnvironmentSupport()
        {
            //issues logging to stdout in AppVeyor and Travis environments, best to turn it off
            if (Environment.GetEnvironmentVariable("APPVEYOR_BUILD_NUMBER") == null && Environment.GetEnvironmentVariable("TRAVIS") == null)
            {
                //LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider(LogLevel.Trace));
            }
        }

        [Fact]
        public void EmptyTest()
        {
        }
    }
}
