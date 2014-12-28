// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackgroundJobResult.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

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
                return this.Headers[HeaderNames.JobUUID];
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
                return this.BodyText != null && this.BodyText.StartsWith("-ERR ")
                           ? this.BodyText.Substring(5, this.BodyText.Length - 5)
                           : this.BodyText;
            }
        }
    }
}