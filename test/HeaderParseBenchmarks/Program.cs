namespace HeaderParseBenchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    using NEventSocket.Tests.TestSupport;
    using NEventSocket.Util;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            var msg = TestMessages.ConnectEvent;
            const int Iterations = 100 * 1000;

            Console.WriteLine("String Split\n-----");
            BenchMark(Iterations, () => ParseKeyValuePairsUsingStringSplit(msg, "\n", ": "));

            Console.WriteLine("String Split Skip UriDecode if not needed\n-----");
            BenchMark(Iterations, () => ParseKeyValuePairsUsingStringSplitNoUriDecodeIfNeeded(msg, "\n", ": "));

            Console.WriteLine("StringReader\n-----");
            BenchMark(Iterations, () => ParseKeyValuePairsUsingStringReader(msg, ": "));

            Console.WriteLine("StringReader Skip UriDecode if not needed\n-----");
            BenchMark(Iterations, () => ParseKeyValuePairsUsingStringReaderNoUriDecodeIfNeeded(msg, ": "));

            Console.WriteLine("Press [Enter] to Exit.");
            Console.ReadLine();
        }

        private static void BenchMark(int Iterations, Action benchMarkThis)
        {
            // Give the test as good a chance as possible
            // of avoiding garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            benchMarkThis(); //JIT compile the method first

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            for (var i = 0; i < Iterations; i++)
            {
                benchMarkThis();
            }

            stopWatch.Stop();
            Console.WriteLine("Memory Used: " + GC.GetTotalMemory(false));
            Console.WriteLine("Time Taken: " + stopWatch.ElapsedTicks);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public static IDictionary<string, string> ParseKeyValuePairsUsingStringSplit(
            this string inputString, string keyValuePairDelimiter, string keyValueDelimiter)
        {
            var dictionary = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(inputString))
            {
                return dictionary;
            }

            var split = inputString.Split(new[] { keyValuePairDelimiter }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var s in split)
            {
                if (s.IndexOf(keyValueDelimiter, StringComparison.Ordinal) == -1)
                {
                    throw new FormatException("Value provided was not a valid key-value pair - '{0}'".Fmt(s));
                }

                var kvp = s.Split(new[] { keyValueDelimiter }, StringSplitOptions.RemoveEmptyEntries);
                var name = kvp[0];
                var value = kvp[1];

                try
                {
                    dictionary[name] = Uri.UnescapeDataString(value);
                }
                catch (UriFormatException)
                {
                    // oh well, we tried
                    dictionary[name] = value;
                }
            }

            return dictionary;
        }

        public static IDictionary<string, string> ParseKeyValuePairsUsingStringSplitNoUriDecodeIfNeeded(
            this string inputString, string keyValuePairDelimiter, string keyValueDelimiter)
        {
            var dictionary = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(inputString))
            {
                return dictionary;
            }

            var split = inputString.Split(new[] { keyValuePairDelimiter }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var s in split)
            {
                if (s.IndexOf(keyValueDelimiter, StringComparison.Ordinal) == -1)
                {
                    throw new FormatException("Value provided was not a valid key-value pair - '{0}'".Fmt(s));
                }

                var kvp = s.Split(new[] { keyValueDelimiter }, StringSplitOptions.RemoveEmptyEntries);
                var name = kvp[0];
                var value = kvp[1];

                if (value.IndexOf("%", StringComparison.Ordinal) > 0)
                {
                    try
                    {
                        dictionary[name] = Uri.UnescapeDataString(value);
                    }
                    catch (UriFormatException)
                    {
                        // oh well, we tried
                        dictionary[name] = value;
                    }
                }
                else
                {
                    dictionary[name] = value;
                }
            }

            return dictionary;
        }

        public static IDictionary<string, string> ParseKeyValuePairsUsingStringReader(this string inputString, string delimiter)
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

                    try
                    {
                        dictionary[name] = Uri.UnescapeDataString(value);
                    }
                    catch (UriFormatException)
                    {
                        // oh well, we tried
                        dictionary[name] = value;
                    }
                }
            }

            return dictionary;
        }

        private static IDictionary<string, string> ParseKeyValuePairsUsingStringReaderNoUriDecodeIfNeeded(
            string inputString, string delimiter)
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

                    if (value.IndexOf("%", StringComparison.Ordinal) > 0)
                    {
                        try
                        {
                            dictionary[name] = Uri.UnescapeDataString(value);
                        }
                        catch (UriFormatException)
                        {
                            // oh well, we tried
                            dictionary[name] = value;
                        }
                    }
                    else
                    {
                        dictionary[name] = value;
                    }
                }
            }

            return dictionary;
        }
    }
}