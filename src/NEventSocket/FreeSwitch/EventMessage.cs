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
            if (basicMessage.ContentType != ContentTypes.EventPlain)
            {
                /* 
                 * Special Case:
                 * On a FreeSwitch Outbound socket, the Command-Reply response to the "connect"
                 * command contains a CHANNEL_DATA event. In this case, the Command-Reply message
                 * is also an event message, we'll just copy the headers over.
                 */
                if (basicMessage.Headers.ContainsKey(HeaderNames.EventName))
                {
                    this.Headers = basicMessage.Headers;
                    this.BodyText = basicMessage.BodyText;
                    this.EventHeaders = basicMessage.Headers;
                    return;
                }

                throw new ArgumentException(
                    "Expected content type text/plain, got {0} instead.".Fmt(basicMessage.ContentType));
            }

            if (string.IsNullOrEmpty(basicMessage.BodyText))
                throw new ArgumentException("Message did not contain an event body.");

            //...otherwise the content of the event will be in the BasicMessage's body

            Headers = basicMessage.Headers;
            BodyText = basicMessage.BodyText;

            try
            {
                if (!this.BodyText.Contains(HeaderNames.ContentLength))
                {
                    // Normally the body text consistes of key-value-pair event headers
                    this.EventHeaders = new Dictionary<string, string>(
                        this.BodyText.ParseKeyValuePairs("\n", ": "), StringComparer.OrdinalIgnoreCase);
                    this.BodyText = null;
                }
                else
                {
                    //...but some Event Messages, eg. BACKGROUND_JOB events contain a body payload.
                    var parser = new Parser();
                    foreach (char c in this.BodyText)
                    {
                        parser.Append(c);
                    }

                    BasicMessage payload = parser.ParseMessage();

                    this.EventHeaders = payload.Headers;
                    this.BodyText = payload.BodyText.Trim();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to parse body of event", ex);
                Log.Error(BodyText);
                throw;
            }
        }

        public EventType EventType
        {
            get
            {
                return (EventType)Enum.Parse(typeof(EventType), EventHeaders[HeaderNames.EventName]);
            }
        }

        public ChannelState ChannelState
        {
            get
            {
                return (ChannelState)Enum.Parse(typeof(ChannelState), EventHeaders[HeaderNames.ChannelState]);
            }
        }

        public string AnswerState
        {
            get
            {
                //possible values: answered, hangup, ringing
                return EventHeaders[HeaderNames.AnswerState];
            }
        }

        public IReadOnlyDictionary<string, string> EventHeaders { get; protected set; }

        public string GetVariable(string variable)
        {
            return EventHeaders["variable_" + variable];
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Message Headers:");

            foreach (var h in Headers.OrderBy(x => x.Key))
                sb.AppendLine("\t" + h.Key + " : " + h.Value);

            sb.AppendLine("Event Headers:");

            foreach (var h in EventHeaders.OrderBy(x => x.Key))
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