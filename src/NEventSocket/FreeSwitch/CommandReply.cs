// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandReply.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using System;

    using NEventSocket.Util;

    /// <summary>
    /// CommandResponses contain the status in the Reply-Text header.
    /// </summary>
    [Serializable]
    public class CommandReply : BasicMessage
    {
        public CommandReply(BasicMessage basicMessage)
        {
            if (basicMessage.ContentType != ContentTypes.CommandReply)
            {
                throw new ArgumentException("Expected content type command/reply, got {0} instead.".Fmt(basicMessage.ContentType));
            }

            Headers = basicMessage.Headers;
            BodyText = basicMessage.BodyText;
        }

        public bool Success
        {
            get
            {
                return this.ReplyText != null && this.ReplyText[0] == '+';
            }
        }

        public string ReplyText
        {
            get
            {
                return this.Headers[HeaderNames.ReplyText];
            }
        }

        public string ErrorMessage
        {
            get
            {
                return this.ReplyText != null && this.ReplyText.StartsWith("-ERR")
                           ? this.ReplyText.Substring(5, this.ReplyText.Length - 5)
                           : string.Empty;
            }
        }
    }
}