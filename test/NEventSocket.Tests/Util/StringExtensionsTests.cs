namespace NEventSocket.Tests.Util
{
    using System.Threading.Tasks;

    using NEventSocket.Sockets.Implementation;
    using NEventSocket.Sockets.Protocol;
    using NEventSocket.Util;

    using Xunit;
    using Xunit.Extensions;

    public class StringExtensionsTests
    {
        [Theory]
        public void can_format_strings()
        {
            const string format = "{0} {1} {2}";
            var output = format.Fmt(1, 2, 3);
            Assert.Equal("1 2 3", output);
        }

        [Fact]
        public async Task can_connect()
        {
            
        }
    }
}