// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Parser.cs" company="Business Systems (UK) Ltd">
//   Copyright © Business Systems (UK) Ltd and contributors. All rights reserved.
// </copyright>
// <summary>
//   A simple state-machine for parsing incoming EventSocket messages.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Sockets.Protocol
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Common.Logging;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Messages;
    using NEventSocket.Util;

    /// <summary>
    /// A simple state-machine for parsing incoming EventSocket messages.
    /// </summary>
    public class Parser
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly StringBuilder buffer = new StringBuilder(); // StringBuilder in .Net 4 uses a Linked List internally to avoid expensive reallocations. Faster but uses marginally more memory.

        private char previous;

        private int? contentLength;

        private IDictionary<string, string> headers;

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
                return contentLength.HasValue;
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
                    headers = buffer.ToString().ParseKeyValuePairs("\n", ": ");

                    if (headers.ContainsKey(HeaderNames.ContentLength))
                    {
                        contentLength = int.Parse(headers[HeaderNames.ContentLength]);

                        // start parsing the body content
                        buffer.Clear();

                        // allocate the buffer up front given that we now know the expected size
                        buffer.EnsureCapacity(contentLength.Value);
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
                Completed = buffer.Length == contentLength.GetValueOrDefault();
            }

            return this;
        }

        public BasicMessage ParseMessage()
        {
            if (!Completed)
                throw new InvalidOperationException("The message was not completely parsed.");

            return HasBody ? new BasicMessage(headers, buffer.ToString()) : new BasicMessage(headers);
        }
    }
}