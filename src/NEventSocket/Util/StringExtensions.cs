// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using NEventSocket.Util.ObjectPooling;

    /// <summary>
    /// Provides helper extension methods for strings
    /// </summary>
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

        /// <summary>
        /// Converts a PascalCaseString to UPPER_WITH_UNDERSCORES
        /// </summary>
        /// <param name="pascalCaseString">The pascal case string.</param>
        /// <returns>A FreeSwitch enum string</returns>
        [DebuggerStepThrough]
        public static string ToUpperWithUnderscores(this string pascalCaseString)
        {
            if (string.IsNullOrEmpty(pascalCaseString))
            {
                return pascalCaseString;
            }

            var sb = StringBuilderPool.Allocate();
            sb.EnsureCapacity(pascalCaseString.Length);

            for (var i = 0; i < pascalCaseString.Length; i++)
            {
                var c = pascalCaseString[i];
                if (char.IsUpper(c))
                {
                    if (i != 0)
                    {
                        sb.Append('_');
                    }

                    sb.Append(char.ToUpper(c));
                }
                else
                {
                    sb.Append(char.ToUpper(c));
                }
            }

            return StringBuilderPool.ReturnAndFree(sb);
        }

        /// <summary>
        /// Converts a UPPER_WITH_UNDERSCORES string to a PascalCaseString
        /// </summary>
        /// <param name="underscoreString">The underscore string.</param>
        /// <returns>A Pascal Case string</returns>
        [DebuggerStepThrough]
        public static string ToPascalCase(this string underscoreString)
        {
            if (string.IsNullOrEmpty(underscoreString))
            {
                return underscoreString;
            }

            var sb = StringBuilderPool.Allocate();
            sb.EnsureCapacity(underscoreString.Length);

            var capitalizeNext = true;

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

            return StringBuilderPool.ReturnAndFree(sb);
        }

        /// <summary>
        /// Parses a string of delimited key-value pairs into a dictionary.
        /// </summary>
        /// <param name="inputString">The input string.</param>
        /// <param name="delimiter">The delimiter which separates keys and values, e.g. a colon.</param>
        /// <returns>A dictionary containing the key-value pairs.</returns>
        /// <exception cref="FormatException">Thrown when an invalid key-value pair is encountered.</exception>
        [DebuggerStepThrough]
        public static IDictionary<string, string> ParseKeyValuePairs(this string inputString, string delimiter)
        {
            var dictionary = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(inputString))
            {
                return dictionary;
            }

            using (var reader = new StringReader(inputString))
            {
                string line;
                while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
                {
                    var index = line.IndexOf(delimiter, StringComparison.Ordinal);
                    if (index == -1)
                    {
                        throw new FormatException("Value provided was not a valid key-value pair - '{0}'".Fmt(line));
                    }

                    var name = line.Substring(0, index);
                    var value = line.Substring(index + delimiter.Length, line.Length - (index + delimiter.Length));

                    if (value.IndexOf("%", StringComparison.Ordinal) < 0)
                    {
                        dictionary[name] = value;
                    }
                    else
                    {
                        //some values are UriEncoded so they fit on a single line
                        try
                        {
                            dictionary[name] = Uri.UnescapeDataString(value);
                        }
                        catch (UriFormatException)
                        {
                            //oh well, we tried
                            dictionary[name] = value;
                        }
                    }
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Parses a FreeSwitch UPPER_CASE_UNDERSCORE header value into a C# PascalCase NullableEnumType.
        /// </summary>
        [DebuggerStepThrough]
        public static TEnum? HeaderToEnumOrNull<TEnum>(this string inputString) where TEnum : struct
        {
            TEnum result;

            if (Enum.TryParse(inputString.ToPascalCase(), out result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Parses a FreeSwitch UPPER_CASE_UNDERSCORE header value into a C# PascalCase Enum, throwing exceptions if unable to do so.
        /// </summary>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an <seealso cref="System.Enum"/>.</exception>
        /// <exception cref="OverflowException"><paramref name="inputString"/> is outside the range of the underlying type of <typeparamref name="TEnum"/>.</exception>
        [DebuggerStepThrough]
        public static TEnum HeaderToEnum<TEnum>(this string inputString) where TEnum : struct
        {
            return (TEnum)Enum.Parse(typeof(TEnum), inputString.ToPascalCase());
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
        [DebuggerStepThrough]
        public static string ToOriginateString(this IDictionary<string, string> dictionary)
        {
            var sb = StringBuilderPool.Allocate();

            foreach (var kvp in dictionary)
            {
                sb.AppendFormat("{0}='{1}',", kvp.Key, kvp.Value);
            }

            return StringBuilderPool.ReturnAndFree(sb);
        }
    }
}