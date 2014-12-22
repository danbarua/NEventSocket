namespace NEventSocket.FreeSwitch
{
    using System;

    using NEventSocket.Util;

    /// <summary>
    /// ApiResponses contain the response in the body text
    /// </summary>
    [Serializable]
    public class ApiResponse : BasicMessage
    {
        public ApiResponse(BasicMessage basicMessage)
        {
            if (basicMessage.ContentType != ContentTypes.ApiResponse)
                throw new ArgumentException(
                    "Expected content type api/response, got {0} instead.".Fmt(basicMessage.ContentType));


            this.Headers = basicMessage.Headers;
            this.BodyText = basicMessage.BodyText;
        }

        public bool Success
        {
            get { return this.BodyText != null && this.BodyText[0] == '+'; }
        }
        
        public string ErrorMessage
        {
            get
            {
                return this.BodyText != null && this.BodyText.StartsWith("-ERR") ? this.BodyText.Substring(5, this.BodyText.Length - 5) : string.Empty;
            }
        }
    }
}