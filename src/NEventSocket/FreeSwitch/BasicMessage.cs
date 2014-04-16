namespace NEventSocket.FreeSwitch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using NEventSocket.Util;

    /// <summary>
    /// Represents a Message recieved through the Event Socket.
    /// </summary>
    [Serializable]
    public class BasicMessage
    {
        protected static readonly Regex ContentLengthPattern = new Regex("^\\s*Content-Length\\s*:\\s*(\\d+)\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        protected static readonly Regex ReplyTextPattern = new Regex("^\\s*Reply-Text\\s*:\\s*([\\S ]+)\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        protected static readonly Regex CommandErrorPattern = new Regex("^\\s*Content-Length\\s*:\\s*(\\d+)\\s*$.*^$^.*(-ERR)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public BasicMessage(IDictionary<string, string> headers)
        {
            Headers = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        }

        public BasicMessage(IDictionary<string, string> headers, string body)
        {
            Headers = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
            BodyText = body;
        }

        /// <summary>Initializes a new instance of the <see cref="BasicMessage"/> class.</summary>
        protected BasicMessage()
        {
        }

        public IDictionary<string, string> Headers { get; protected set; }

        public string BodyText { get; protected set; }

        /// <summary>Gets the Content Type header.</summary>
        public string ContentType
        {
            get { return Headers.GetValueOrDefault(HeaderNames.ContentType); }
        }

        /// <summary>Gets the content length.</summary>
        public int ContentLength
        {
            get
            {
                int contentLength;
                return int.TryParse(Headers.GetValueOrDefault(HeaderNames.ContentLength), out contentLength)
                           ? contentLength
                           : 0;
            }
        }

        /// <summary>ToString helper.</summary>
        /// <returns>A <see cref="string"/> representation of the BasicMessage instance.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Headers:\n");

            foreach (var h in Headers.OrderBy(x => x.Key))
                sb.AppendFormat("\t{0}:{1}\n".Fmt(h.Key, h.Value));

            if (BodyText != null)
            {
                sb.AppendLine("Body:\n");
                sb.Append("\t");
                sb.AppendLine(BodyText);
            }

            return sb.ToString();
        }
    }
}