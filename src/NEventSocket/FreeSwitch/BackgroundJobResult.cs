namespace NEventSocket.FreeSwitch
{
    using System;

    [Serializable]
    public class BackgroundJobResult : BasicMessage
    {
        public BackgroundJobResult(EventMessage basicMessage)
        {
            this.Headers = basicMessage.Headers;
            this.BodyText = basicMessage.BodyText;
        }

        public string JobUUID
        {
            get
            {
                return this.Headers["Job-UUID"];
            }
        }

        public bool Success
        {
            get
            {
                return this.BodyText != null && this.BodyText[0] == '+';
            }
        }

        public string ErrorMessage
        {
            get
            {
                return this.BodyText != null && this.BodyText.StartsWith("-ERR ") ? this.BodyText.Substring(5, this.BodyText.Length - 5) : string.Empty;
            }
        }
    }
}