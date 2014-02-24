namespace NEventSocket.FreeSwitch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Common.Logging;

    using NEventSocket.Sockets.Protocol;
    using NEventSocket.Util;

    /// <summary>
    ///     Represents an Event Message received through the EventSocket
    /// </summary>
    [Serializable]
    public class EventMessage : BasicMessage
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

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
                        // Normally the body text consists of key-value-pair event headers
                        this.Headers = new Dictionary<string, string>(
                            basicMessage.BodyText.ParseKeyValuePairs("\n", ": "), StringComparer.OrdinalIgnoreCase);
                        this.BodyText = null;
                    }
                    else
                    {
                        //...but some Event Messages also carry a body payload, eg. BACKGROUND_JOB events 
                        // a message inside an EventMessage inside a BasicMessage...

                        //todo: this is really inefficient but a quick lazy way of turning a string into a message.
                        var parser = new Parser();
                        foreach (char c in basicMessage.BodyText)
                        {
                            parser.Append(c);
                        }

                        BasicMessage payload = parser.ParseMessage();

                        this.Headers = payload.Headers;
                        this.BodyText = payload.BodyText.Trim();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to parse body of event", ex);
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

        public EventType EventType
        {
            get
            {
                return (EventType)Enum.Parse(typeof(EventType), Headers[HeaderNames.EventName]);
            }
        }

        public ChannelState ChannelState
        {
            get
            {
                return (ChannelState)Enum.Parse(typeof(ChannelState), Headers[HeaderNames.ChannelState]);
            }
        }

        public string AnswerState
        {
            get
            {
                //possible values: answered, hangup, ringing
                return Headers[HeaderNames.AnswerState];
            }
        }

        public string GetVariable(string variable)
        {
            return Headers["variable_" + variable];
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