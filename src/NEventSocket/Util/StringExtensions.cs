namespace NEventSocket.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    public static class StringExtensions
    {
        /// <summary>
        /// Calls String.Format().
        /// </summary>
        /// <param name="input">The format string</param>
        /// <param name="args">The input array.</param>
        /// <returns>A formatted string.</returns>
        [DebuggerStepThrough]
        public static string Fmt(this string input, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, input, args);
        }

        /// <summary>
        /// Parses a string of delimited key-value pairs into an <see cref="IDictionary{string,string}"/>.
        /// </summary>
        /// <param name="inputString">The input string.</param>
        /// <param name="keyValuePairDelimiter">The delimiter which separates key-value pairs, e.g. a newline.</param>
        /// <param name="keyValueDelimiter">The delimiter which separates keys and values, e.g. a colon.</param>
        /// <returns>A <see cref="System.Collections.Generic.IDictionary{string, string}"/> containing the key-value pairs.</returns>
        [DebuggerStepThrough]
        public static IDictionary<string, string> ParseKeyValuePairs(this string inputString, string keyValuePairDelimiter, string keyValueDelimiter)
        {
            var dictionary = new Dictionary<string, string>();
            var split = inputString.Split(new[] { keyValuePairDelimiter }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var s in split)
            {
                if (s.IndexOf(keyValueDelimiter, StringComparison.Ordinal) == -1)
                    throw new FormatException("Value provided was not a valid key-value pair - '{0}'".Fmt(s));

                var kvp = s.Split(new[] { keyValueDelimiter }, StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    dictionary[kvp[0]] = Uri.UnescapeDataString(kvp[1]);
                }
                catch (UriFormatException)
                {
                    // oh well, we tried.
                    dictionary[kvp[0]] = kvp[1];
                }
            }

            return dictionary;
        }
    }
}