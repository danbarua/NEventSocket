namespace NEventSocket.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;

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
        /// Returns a lower-case boolean string
        /// </summary>
        /// <param name="input">The boolean</param>
        /// <returns>A lower-case string.</returns>
        [DebuggerStepThrough]
        public static string ToLowerBooleanString(this bool input)
        {
            return input.ToString().ToLowerInvariant();
        }

        [DebuggerStepThrough]
        public static string ToUpperWithUnderscores(this string camelCaseString)
        {
            if (string.IsNullOrEmpty(camelCaseString)) return camelCaseString;

            var sb = new StringBuilder(camelCaseString.Length);

            for (int i = 0; i < camelCaseString.Length; i++)
            {
                var c = camelCaseString[i];
                if (char.IsUpper(c))
                {
                    if (i != 0) sb.Append('_');
                    sb.Append(char.ToUpper(c));
                }
                else sb.Append(char.ToUpper(c));
            }

            return sb.ToString();
        }

        [DebuggerStepThrough]
        public static string ToCamelCase(this string underscoreString)
        {
            if (string.IsNullOrEmpty(underscoreString)) return underscoreString;

            var sb = new StringBuilder(underscoreString.Length);
            bool capitalizeNext = true;

            foreach (var c in underscoreString)
            {
                if (capitalizeNext)
                {
                    sb.Append(char.ToUpper(c));
                    capitalizeNext = false;
                }
                else
                {
                    if (c == '_')
                    {
                        capitalizeNext = true;
                    }
                    else
                    {
                        sb.Append(char.ToLowerInvariant(c));
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Parses a string of delimited key-value pairs into an <see cref="IDictionary{string,string}"/>.
        /// </summary>
        /// <param name="inputString">The input string.</param>
        /// <param name="keyValuePairDelimiter">The delimiter which separates key-value pairs, e.g. a newline.</param>
        /// <param name="keyValueDelimiter">The delimiter which separates keys and values, e.g. a colon.</param>
        /// <returns>A <see cref="System.Collections.Generic.IDictionary{string, string}"/> containing the key-value pairs.</returns>
        /// <exception cref="FormatException">Thrown when an invalid key-value pair is encountered.</exception>
        [DebuggerStepThrough]
        public static IDictionary<string, string> ParseKeyValuePairs(this string inputString, string keyValuePairDelimiter, string keyValueDelimiter)
        {
            var dictionary = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(inputString)) return dictionary;

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

        /// <summary>
        /// Parses a FreeSwitch UPPER_CASE_UNDERSCORE header value into a C# CamelCase NullableEnumType.
        /// </summary>
        [DebuggerStepThrough]
        public static TEnum? HeaderToEnumOrNull<TEnum>(this string inputString) where TEnum : struct
        {
            TEnum result;

            if (Enum.TryParse(inputString.ToCamelCase(), out result)) return result;

            return null;
        }

        /// <summary>
        /// Parses a FreeSwitch UPPER_CASE_UNDERSCORE header value into a C# CamelCase Enum, throwing exceptions if unable to do so.
        /// </summary>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an <seealso cref="System.Enum"/>.</exception>
        /// <exception cref="OverflowException"><paramref name="inputString"/> is outside the range of the underlying type of <typeparamref name="TEnum"/>.</exception>
        [DebuggerStepThrough]
        public static TEnum HeaderToEnum<TEnum>(this string inputString) where TEnum : struct
        {
            return (TEnum)Enum.Parse(typeof(TEnum), inputString.ToCamelCase());
        }

        /// <summary>
        /// Gets a value from the given dictionary, returning default(<typeparamref name="TValue"/>) if not found.
        /// </summary>
        [DebuggerStepThrough]
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            dictionary.TryGetValue(key, out value);
            return value;
        }

        /// <summary>
        /// Joins a set of key-value pairs into an originate parameters string.
        /// </summary>
        public static string ToOriginateString(this IDictionary<string, string> dictionary)
        {
            var sb = new StringBuilder();

            foreach (var kvp in dictionary)
            {
                sb.AppendFormat("{0}='{1}',", kvp.Key, kvp.Value);
            }

            return sb.ToString();
        }
    }
}