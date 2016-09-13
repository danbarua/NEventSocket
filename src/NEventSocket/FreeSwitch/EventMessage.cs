// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventMessage.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using NEventSocket.Logging;
    using NEventSocket.Sockets;
    using NEventSocket.Util;
    using NEventSocket.Util.ObjectPooling;

    /// <summary>
    ///     Represents an Event Message received through the EventSocket
    /// </summary>
    [Serializable]
    public class EventMessage : BasicMessage
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        internal EventMessage(BasicMessage basicMessage)
        {
            if (basicMessage is EventMessage)
            {
                Headers = basicMessage.Headers;
                BodyText = basicMessage.BodyText;
                return;
            }

            if (basicMessage.ContentType == ContentTypes.CommandReply)
            {
                /* 
                 * Special Case:
                 * When an Outbound Socket sends the "connect" command, FreeSwitch responds with a Command-Reply.
                 * This Command-Reply message contains a CHANNEL_DATA event message buried in its headers.
                 * In this case, we can hydrate an event message from a message of content type Command-Reply.
                 */
                if (basicMessage.Headers.ContainsKey(HeaderNames.EventName))
                {
                    Headers = basicMessage.Headers;
                    BodyText = basicMessage.BodyText;
                    return;
                }

                /* otherwise, we'll throw an exception if we get the wrong content-type message passed in*/
                throw new InvalidOperationException("Expected content type event/plain, got {0} instead.".Fmt(basicMessage.ContentType));
            }

            // normally, the content of the event will be in the BasicMessage's body text and will need to be parsed to produce an EventMessage
            if (string.IsNullOrEmpty(basicMessage.BodyText))
            {
                throw new ArgumentException("Message did not contain an event body.");
            }

            try
            {
                var delimiterIndex = basicMessage.BodyText.IndexOf("\n\n", StringComparison.Ordinal);
                if (delimiterIndex == -1 || delimiterIndex == basicMessage.BodyText.Length - 2)
                {
                    // body text consists of key-value-pair event headers, no body
                    Headers = basicMessage.BodyText.ParseKeyValuePairs(": ");
                    BodyText = null;
                }
                else
                {
                    // ...but some Event Messages also carry a body payload, eg. a BACKGROUND_JOB event
                    // which is a message carried inside an EventMessage carried inside a BasicMessage..
                    Headers = basicMessage.BodyText.Substring(0, delimiterIndex).ParseKeyValuePairs(": ");

                    Debug.Assert(Headers.ContainsKey(HeaderNames.ContentLength));
                    var contentLength = int.Parse(Headers[HeaderNames.ContentLength]);

                    Debug.Assert(delimiterIndex + 2 + contentLength <= basicMessage.BodyText.Length, "Message cut off mid-transmission");
                    var body = basicMessage.BodyText.Substring(delimiterIndex + 2, contentLength);

                    //remove any \n\n if any
                    var index = body.IndexOf("\n\n", StringComparison.Ordinal);
                    BodyText = index > 0 ? body.Substring(0, index) : body;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorException("Failed to parse body of event", ex);
                Log.Error(BodyText);
                throw;
            }

        }

        /// <summary>
        /// Default constructor
        /// </summary>
        protected EventMessage()
        {
        }

        

        /// <summary>
        /// Gets the <see cref="EventName"/> of this instance.
        /// </summary>
        public EventName EventName
        {
            get
            {
                return Headers.GetValueOrDefault(HeaderNames.EventName).HeaderToEnum<EventName>();
            }
        }

        /// <summary>
        /// Retrieves a header from the Headers dictionary, returning null if the key is not found.
        /// </summary>
        /// <param name="header">The Header Name.</param>
        /// <returns>The Header Value.</returns>
        public string GetHeader(string header)
        {
            return Headers.GetValueOrDefault(header);
        }

        /// <summary>
        /// Retrieves a Channel Variable from the Headers dictionary, returning null if the key is not found.
        /// </summary>
        /// <param name="variable">The Channel Variable Name</param>
        /// <returns>The Channel Variable value.</returns>
        public string GetVariable(string variable)
        {
            return GetHeader("variable_" + variable);
        }

        /// <summary>
        /// Provides a string representation of the <see cref="EventMessage"/> instance for debugging.
        /// </summary>
        public override string ToString()
        {
            var sb = StringBuilderPool.Allocate();
            sb.AppendLine("Event Headers:");

            foreach (var h in Headers.OrderBy(x => x.Key))
            {
                sb.AppendLine("\t" + h.Key + " : " + h.Value);
            }

            if (!string.IsNullOrEmpty(BodyText))
            {
                sb.AppendLine("Body:");
                sb.AppendLine(BodyText);
            }

            return StringBuilderPool.ReturnAndFree(sb);
        }
    }
}