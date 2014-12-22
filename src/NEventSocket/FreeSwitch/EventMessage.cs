namespace NEventSocket.FreeSwitch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using NEventSocket.Logging;

    using NEventSocket.Sockets;
    using NEventSocket.Util;

    /// <summary>
    ///     Represents an Event Message received through the EventSocket
    /// </summary>
    [Serializable]
    public class EventMessage : BasicMessage
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        protected EventMessage()
        {
        }

        public EventMessage(BasicMessage basicMessage)
        {
            if (basicMessage.ContentType == ContentTypes.EventPlain)
            {
                // normally, the content of the event will be in the BasicMessage's body text and will need to be parsed to produce an EventMessage

                if (string.IsNullOrEmpty(basicMessage.BodyText)) throw new ArgumentException("Message did not contain an event body.");

                try
                {
                    if (!basicMessage.BodyText.Contains(HeaderNames.ContentLength))
                    {
                        //body text consists of key-value-pair event headers
                        this.Headers = basicMessage.BodyText.ParseKeyValuePairs("\n", ": ");
                        this.BodyText = null;
                    }
                    else
                    {
                        //...but some Event Messages also carry a body payload, eg. a BACKGROUND_JOB event
                        // which is a message carried inside an EventMessage carried inside a BasicMessage...

                        //todo: this is really inefficient but a quick lazy way of turning a string into a message.
                        var parser = new Parser();
                        foreach (char c in basicMessage.BodyText)
                        {
                            parser.Append(c);
                        }

                        BasicMessage payload = parser.ExtractMessage();

                        this.Headers = payload.Headers;
                        this.BodyText = payload.BodyText.Trim();
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorException("Failed to parse body of event", ex);
                    Log.Error(this.BodyText);
                    throw;
                }
            }
            else
            {
                /* 
                 * Special Case:
                 * When an Outbound Socket sends the "connect" command, FreeSwitch replies
                 * with a Command-Reply. This Command-Reply message contains a CHANNEL_DATA event message in its headers.
                 * In this case, a Command-Reply message also functions as an Event message, so we'll just copy the properties over
                 * and return an EventMessage.
                 */

                if (basicMessage.Headers.ContainsKey(HeaderNames.EventName))
                {
                    this.Headers = basicMessage.Headers;
                    this.BodyText = basicMessage.BodyText;
                }
                else
                {
                    throw new ArgumentException("Expected content type event/plain, got {0} instead.".Fmt(basicMessage.ContentType));
                }
            }
        }

        /// <summary>
        /// Gets the Unique Id for the Channel.
        /// </summary>
        public string UUID
        {
            get
            {
                return Headers.GetValueOrDefault(HeaderNames.UniqueId);
            }
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
        /// Gets the <see cref="ChannelState"/> of the Channel.
        /// </summary>
        public ChannelState ChannelState
        {
            get
            {
                //channel state = "CS_NEW"
                //strip first 3 chars and then parse it to ChannelState enum.

                var channelState = Headers.GetValueOrDefault(HeaderNames.ChannelState);
                channelState = channelState.Substring(3, channelState.Length - 3);
                return channelState.HeaderToEnum<ChannelState>();
            }
        }

        /// <summary>
        /// Gets the <see cref="AnswerState"/> of the Channel.
        /// </summary>
        public AnswerState AnswerState
        {
            get
            {
                return Headers.GetValueOrDefault(HeaderNames.AnswerState).HeaderToEnum<AnswerState>();
            }
        }

        /// <summary>
        /// Gets the <see cref="HangupCause"/> of the Channel, if it has been hung up otherwise null.
        /// </summary>
        public HangupCause? HangupCause
        {
            get
            {
                return Headers.GetValueOrDefault(HeaderNames.HangupCause).HeaderToEnumOrNull<HangupCause>();
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
            return this.GetHeader("variable_" + variable);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Event Headers:");

            foreach (var h in Headers.OrderBy(x => x.Key))
                sb.AppendLine("\t" + h.Key + " : " + h.Value);

            if (!string.IsNullOrEmpty(BodyText))
            {
                sb.AppendLine("Body:");
                sb.AppendLine(BodyText);
            }

            return sb.ToString();
        }
    }
}