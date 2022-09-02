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
    /// Represents the result of an ESL command
    /// </summary>
    [Serializable]
    public class CommandReply : BasicMessage
    {
        internal CommandReply(BasicMessage basicMessage)
        {
            if (basicMessage.ContentType != ContentTypes.CommandReply)
            {
                throw new ArgumentException("Expected content type command/reply, got {0} instead.".Fmt(basicMessage.ContentType));
            }

            Headers = basicMessage.Headers;
            BodyText = basicMessage.BodyText;
        }

        /// <summary>
        /// Gets a boolean indicating whether the command succeeded.
        /// </summary>
        public bool Success
        {
            get
            {
                return ReplyText != null && ReplyText[0] == '+';
            }
        }

        /// <summary>
        /// Gets the reply text
        /// </summary>
        public string ReplyText
        {
            get
            {
                return Headers[HeaderNames.ReplyText];
            }
        }

        /// <summary>
        /// Gets an error message associated with a failed command
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                return ReplyText != null && ReplyText.StartsWith("-ERR")
                           ? ReplyText.Substring(5, ReplyText.Length - 5)
                           : string.Empty;
            }
        }
    }
}