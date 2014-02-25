// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Parser.cs" company="Business Systems (UK) Ltd">
//   Copyright © Business Systems (UK) Ltd and contributors. All rights reserved.
// </copyright>
// <summary>
//   A simple state-machine for parsing incoming EventSocket messages.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Sockets
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Common.Logging;

    using NEventSocket.FreeSwitch;
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
                return this.contentLength.HasValue;
            }
        }

        /// <summary>
        /// Appends the given <see cref="char"/> to the message.
        /// </summary>
        /// <param name="next">The next <see cref="char"/> of the message.</param>
        /// <returns>The same instance of the <see cref="Parser"/>.</returns>
        public Parser Append(char next)
        {
            if (this.Completed)
            {
                return new Parser().Append(next);
            }

            this.buffer.Append(next);

            if (!this.HasBody)
            {
                // we're parsing the headers
                if (this.previous == '\n' && next == '\n')
                {
                    // \n\n denotes the end of the Headers
                    this.headers = this.buffer.ToString().ParseKeyValuePairs("\n", ": ");

                    if (this.headers.ContainsKey(HeaderNames.ContentLength))
                    {
                        this.contentLength = int.Parse(this.headers[HeaderNames.ContentLength]);

                        // start parsing the body content
                        this.buffer.Clear();

                        // allocate the buffer up front given that we now know the expected size
                        this.buffer.EnsureCapacity(this.contentLength.Value);
                    }
                    else
                    {
                        // end of message
                        this.Completed = true;
                    }
                }
                else
                {
                    this.previous = next;
                }
            }
            else
            {
                // if we've read the Content-Length amount of bytes then we're done
                this.Completed = this.buffer.Length == this.contentLength.GetValueOrDefault();
            }

            return this;
        }

        public BasicMessage ParseMessage()
        {
            if (!this.Completed)
                throw new InvalidOperationException("The message was not completely parsed.");

            return this.HasBody ? new BasicMessage(this.headers, this.buffer.ToString()) : new BasicMessage(this.headers);
        }
    }
}