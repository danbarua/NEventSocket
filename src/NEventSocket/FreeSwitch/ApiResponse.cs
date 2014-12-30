// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApiResponse.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using System;

    using NEventSocket.Util;

    /// <summary>
    /// A message representing the response to an Api call.
    /// </summary>
    [Serializable]
    public class ApiResponse : BasicMessage
    {
        internal ApiResponse(BasicMessage basicMessage)
        {
            if (basicMessage.ContentType != ContentTypes.ApiResponse)
            {
                throw new ArgumentException("Expected content type api/response, got {0} instead.".Fmt(basicMessage.ContentType));
            }

            this.Headers = basicMessage.Headers;
            this.BodyText = basicMessage.BodyText;
        }

        /// <summary>
        /// Gets a boolean indicating whether the operation succeeded or not.
        /// </summary>
        public bool Success
        {
            get
            {
                return this.BodyText != null && this.BodyText[0] == '+';
            }
        }

        /// <summary>
        /// Gets the error message for a failed api call.
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                return this.BodyText != null && this.BodyText.StartsWith("-ERR")
                           ? this.BodyText.Substring(5, this.BodyText.Length - 5)
                           : string.Empty;
            }
        }
    }
}