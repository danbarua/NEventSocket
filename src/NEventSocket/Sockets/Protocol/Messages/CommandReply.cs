namespace NEventSocket.Messages
{
    using System;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Sockets.Protocol;
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
                throw new ArgumentException(
                    "Expected content type command/reply, got {0} instead.".Fmt(basicMessage.ContentType));


            this.Headers = basicMessage.Headers;
            this.BodyText = basicMessage.BodyText;
        }

        public bool Success
        {
            get { return this.ReplyText != null && this.ReplyText[0] == '+'; }
        }

        public string ReplyText
        {
            get { return this.Headers["Reply-Text"]; }
        }
    }
}