// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Parser.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.Sockets
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
    using NEventSocket.Util;
    using NEventSocket.Util.ObjectPooling;

    /// <summary>
    /// A parser for converting a stream of strings or chars into a stream of <seealso cref="BasicMessage"/>s from FreeSwitch.
    /// </summary>
    public class Parser : IDisposable
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        // StringBuilder in .Net 4 uses a Linked List internally to avoid expensive reallocations. Faster but uses marginally more memory.
        private StringBuilder buffer = StringBuilderPool.Allocate();

        private char previous;

        private int? contentLength;

        private IDictionary<string, string> headers;

        private readonly InterlockedBoolean disposed = new InterlockedBoolean();

        ~Parser()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets a value indicating whether parsing an incoming message has completed.
        /// </summary>
        public bool Completed { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the incoming message has a body.
        /// </summary>
        public bool HasBody
        {
            get
            {
                return contentLength.HasValue && contentLength > 0;
            }
        }

        /// <summary>
        /// Appends the given <see cref="char"/> to the message.
        /// </summary>
        /// <param name="next">The next <see cref="char"/> of the message.</param>
        /// <returns>The same instance of the <see cref="Parser"/>.</returns>
        public Parser Append(char next)
        {
            if (Completed)
            {
                return new Parser().Append(next);
            }

            buffer.Append(next);

            if (!HasBody)
            {
                // we're parsing the headers
                if (previous == '\n' && next == '\n')
                {
                    // \n\n denotes the end of the Headers
                    var headerString = buffer.ToString();

                    headers = headerString.ParseKeyValuePairs(": ");

                    if (headers.ContainsKey(HeaderNames.ContentLength))
                    {
                        contentLength = int.Parse(headers[HeaderNames.ContentLength]);

                        if (contentLength == 0)
                        {
                            Completed = true;
                        }
                        else
                        {
                            // start parsing the body content
                            buffer.Clear();

                            // allocate the buffer up front given that we now know the expected size
                            buffer.EnsureCapacity(contentLength.Value);
                        }
                    }
                    else
                    {
                        // end of message
                        Completed = true;
                    }
                }
                else
                {
                    previous = next;
                }
            }
            else
            {
                // if we've read the Content-Length amount of bytes then we're done
                Completed = buffer.Length == contentLength.GetValueOrDefault() || contentLength == 0;
            }

            return this;
        }

        /// <summary>
        /// Appends the provided string to the internal buffer.
        /// </summary>
        public Parser Append(string next)
        {
            var parser = this;

            foreach (var c in next)
            {
                parser = parser.Append(next);
            }

            return parser;
        }

        /// <summary>
        /// Extracts a <seealso cref="BasicMessage"/> from the internal buffer.
        /// </summary>
        /// <returns>A new <seealso cref="BasicMessage"/> instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// When the parser has not received a complete message.
        /// Can be indicative of multiple threads attempting to read from the network stream.
        /// </exception>
        public BasicMessage ExtractMessage()
        {
            if (disposed.Value)
            {
                throw new ObjectDisposedException(GetType().Name, "Should only call ExtractMessage() once per parser.");
            }

            if (!Completed)
            {
                var errorMessage = "The message was not completely parsed.";

                if (HasBody)
                {
                    errorMessage += "expected a body with length {0}, got {1} instead.".Fmt(contentLength, buffer.Length);
                }

                throw new InvalidOperationException(errorMessage);
            }

            var result = HasBody ? new BasicMessage(headers, buffer.ToString()) : new BasicMessage(headers);

            if (HasBody)
            {
                Debug.Assert(result.BodyText.Length == result.ContentLength);
            }

            Dispose();
            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed != null && !disposed.EnsureCalledOnce())
            {
                if (buffer != null)
                {
                    StringBuilderPool.Free(buffer);
                    buffer = null;
                }
            }
        }
    }
}