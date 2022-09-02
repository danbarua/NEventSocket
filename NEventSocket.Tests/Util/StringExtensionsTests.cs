using System;
using FluentAssertions;
using NEventSocket;
using Xunit;
using NEventSocket.FreeSwitch;
using NEventSocket.Util;

namespace NEventSocket.Tests.Util
{


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
            Assert.Equal("ThisIsAStringInUpperCase", Input.ToPascalCase());
        }

        [Fact]
        public void can_convert_uppercaseunderscore_to_enum()
        {
            const string Input = "UNALLOCATED_NUMBER";
            var output = Input.HeaderToEnum<HangupCause>();

            Assert.Equal(HangupCause.UnallocatedNumber, output);
        }

        [Fact]
        public void if_unable_to_convert_string_to_nullable_enum_it_should_return_null()
        {
            const string Input = "THIS_IS_AN_INVALID_HANGUPCAUSE";
            var output = Input.HeaderToEnumOrNull<HangupCause>();

            Assert.Null(output);
        }

        [Fact]
        public void if_unable_to_convert_string_to_enum_it_should_throw_an_ArgumentException()
        {
            const string Input = "THIS_IS_AN_INVALID_HANGUPCAUSE";
            Assert.Throws<ArgumentException>(() => Input.HeaderToEnum<HangupCause>());
        }

        [Theory]
        [InlineData(0, "digits/0.wav")]
        [InlineData(1, "digits/1.wav")]
        [InlineData(2, "digits/2.wav")]
        [InlineData(10, "digits/10.wav")]
        [InlineData(11, "digits/11.wav")]
        [InlineData(12, "digits/12.wav")]
        [InlineData(20, "digits/20.wav")]
        [InlineData(23, "digits/20.wav!digits/3.wav")]
        [InlineData(36, "digits/30.wav!digits/6.wav")]
        [InlineData(100, "digits/1.wav!digits/hundred.wav")]
        [InlineData(110, "digits/1.wav!digits/hundred.wav!digits/10.wav")]
        [InlineData(116, "digits/1.wav!digits/hundred.wav!digits/16.wav")]
        [InlineData(123, "digits/1.wav!digits/hundred.wav!digits/20.wav!digits/3.wav")]
        [InlineData(199, "digits/1.wav!digits/hundred.wav!digits/90.wav!digits/9.wav")]
        [InlineData(1000, "digits/1.wav!digits/thousand.wav")]
        [InlineData(1005, "digits/1.wav!digits/thousand.wav!digits/5.wav")]
        [InlineData(1010, "digits/1.wav!digits/thousand.wav!digits/10.wav")]
        [InlineData(1016, "digits/1.wav!digits/thousand.wav!digits/16.wav")]
        [InlineData(1023, "digits/1.wav!digits/thousand.wav!digits/20.wav!digits/3.wav")]
        [InlineData(1099, "digits/1.wav!digits/thousand.wav!digits/90.wav!digits/9.wav")]
        [InlineData(1200, "digits/1.wav!digits/thousand.wav!digits/2.wav!digits/hundred.wav")]
        [InlineData(1305, "digits/1.wav!digits/thousand.wav!digits/3.wav!digits/hundred.wav!digits/5.wav")]
        [InlineData(1310, "digits/1.wav!digits/thousand.wav!digits/3.wav!digits/hundred.wav!digits/10.wav")]
        [InlineData(2316, "digits/2.wav!digits/thousand.wav!digits/3.wav!digits/hundred.wav!digits/16.wav")]
        [InlineData(2323, "digits/2.wav!digits/thousand.wav!digits/3.wav!digits/hundred.wav!digits/20.wav!digits/3.wav")]
        [InlineData(2399, "digits/2.wav!digits/thousand.wav!digits/3.wav!digits/hundred.wav!digits/90.wav!digits/9.wav")]
        [InlineData(20009, "digits/20.wav!digits/thousand.wav!digits/9.wav")]
        [InlineData(21239, "digits/20.wav!digits/1.wav!digits/thousand.wav!digits/2.wav!digits/hundred.wav!digits/30.wav!digits/9.wav")]
        [InlineData(123456, "digits/1.wav!digits/hundred.wav!digits/20.wav!digits/3.wav!digits/thousand.wav!digits/4.wav!digits/hundred.wav!digits/50.wav!digits/6.wav")]
        [InlineData(999999, "digits/9.wav!digits/hundred.wav!digits/90.wav!digits/9.wav!digits/thousand.wav!digits/9.wav!digits/hundred.wav!digits/90.wav!digits/9.wav")]
        [InlineData(2123456, "digits/2.wav!digits/million.wav!digits/1.wav!digits/hundred.wav!digits/20.wav!digits/3.wav!digits/thousand.wav!digits/4.wav!digits/hundred.wav!digits/50.wav!digits/6.wav")]
        [InlineData(9999999, "digits/9.wav!digits/million.wav!digits/9.wav!digits/hundred.wav!digits/90.wav!digits/9.wav!digits/thousand.wav!digits/9.wav!digits/hundred.wav!digits/90.wav!digits/9.wav")]
        [InlineData(1000023, "digits/1.wav!digits/million.wav!digits/20.wav!digits/3.wav")]
        [InlineData(123000000, "digits/1.wav!digits/hundred.wav!digits/20.wav!digits/3.wav!digits/million.wav")]
        public void can_convert_digits_to_file_strings(int input, string expectedOutput)
        {
            var output = Digits.ToFileString(input);
            output.Should().Be(
                expectedOutput, 
                "\nexpected: '{0}'\nactual: '{1}'\n".Fmt(
                    expectedOutput.Replace("digits/","").Replace(".wav","")
                    ,output.Replace("digits/", "").Replace(".wav", "")));
        }
    }
}