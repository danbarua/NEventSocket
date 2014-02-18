namespace NEventSocket.FreeSwitch
{
    using System;

    [Serializable]
    public class BackgroundJobResult : EventMessage
    {
        public BackgroundJobResult(EventMessage eventMessage)
        {
            this.Headers = eventMessage.Headers;
            this.EventHeaders = eventMessage.EventHeaders;
            this.BodyText = eventMessage.BodyText;
        }

        public string JobUUID
        {
            get
            {
                return this.EventHeaders["Job-UUID"];
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
                return this.BodyText != null && this.BodyText.StartsWith("-ERR") ? this.BodyText.Substring(4, this.BodyText.Length) : string.Empty;
            }
        }
    }
}