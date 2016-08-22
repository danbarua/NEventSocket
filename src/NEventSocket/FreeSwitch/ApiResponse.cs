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

            Headers = basicMessage.Headers;
            BodyText = basicMessage.BodyText.TrimEnd('\n');
        }

        /// <summary>
        /// Gets a boolean indicating whether the operation succeeded or not.
        /// </summary>
        public bool Success
        {
            get
            {
                //API Commands that don't return a response get turned into "-ERR no reply"
                //this is probably not an error condition
                //see mod_event_socket.c line 1553

                return BodyText != null && (BodyText.StartsWith("-ERR no reply") || BodyText[0] != '-');
            }
        }

        /// <summary>
        /// Gets the error message for a failed api call.
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                return BodyText != null && BodyText.StartsWith("-ERR")
                           ? BodyText.Substring(5, BodyText.Length - 5)
                           : string.Empty;
            }
        }
    }
}