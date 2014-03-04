namespace NEventSocket.Tests.Util
{
    using System.Threading.Tasks;

    using NEventSocket.Util;

    using Xunit;
    using Xunit.Extensions;

    public class StringExtensionsTests
    {
        [Fact]
        public void can_format_strings()
        {
            const string format = "{0} {1} {2}";
            var output = format.Fmt(1, 2, 3);
            Assert.Equal("1 2 3", output);
        }

        [Fact]
        public void can_convert_camelcase_to_uppercaseunderscore()
        {
            const string input = "ThisIsAStringInCamelCase";
            Assert.Equal("THIS_IS_A_STRING_IN_CAMEL_CASE", input.ToUpperWithUnderscores());
        }

        [Fact]
        public void can_convert_uppercaseunderscore_to_camelcase()
        {
            const string input = "THIS_IS_A_STRING_IN_UPPER_CASE";
            Assert.Equal("ThisIsAStringInUpperCase", input.ToCamelCase());
        }
    }
}