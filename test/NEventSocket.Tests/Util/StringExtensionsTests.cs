namespace NEventSocket.Tests.Util
{
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Util;

    using Xunit;
    using Xunit.Extensions;

    public class StringExtensionsTests
    {
        [Fact]
        public void can_format_strings()
        {
            const string Format = "{0} {1} {2}";
            var output = Format.Fmt(1, 2, 3);
            Assert.Equal("1 2 3", output);
        }

        [Fact]
        public void can_convert_camelcase_to_uppercaseunderscore()
        {
            const string Input = "ThisIsAStringInCamelCase";
            Assert.Equal("THIS_IS_A_STRING_IN_CAMEL_CASE", Input.ToUpperWithUnderscores());
        }

        [Fact]
        public void can_convert_uppercaseunderscore_to_camelcase()
        {
            const string Input = "THIS_IS_A_STRING_IN_UPPER_CASE";
            Assert.Equal("ThisIsAStringInUpperCase", Input.ToCamelCase());
        }

        [Fact]
        public void can_convert_uppercaseunderscore_to_enum()
        {
            const string Input = "UNALLOCATED_NUMBER";
            var output = Input.ToEnumFromUppercaseUnderscore<HangupCause>();

            Assert.NotNull(output);
            Assert.Equal(HangupCause.UnallocatedNumber, output);
        }

        [Fact]
        public void if_unable_to_convert_string_to_enum_it_returns_null()
        {
            const string Input = "THIS_IS_AN_INVALID_HANGUPCAUSE";
            var output = Input.ToEnumFromUppercaseUnderscore<HangupCause>();

            Assert.Null(output);
        }
    }
}