// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackgroundJobResult.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using System;

    /// <summary>
    /// Represents the result of a bgapi call.
    /// </summary>
    [Serializable]
    public class BackgroundJobResult : BasicMessage
    {
        internal BackgroundJobResult(EventMessage basicMessage)
        {
            this.Headers = basicMessage.Headers;
            this.BodyText = basicMessage.BodyText;
        }

        /// <summary>
        /// Gets the Unique Id of the Job
        /// </summary>
        public string JobUUID
        {
            get
            {
                return this.Headers[HeaderNames.JobUUID];
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether the job succeeded or not.
        /// </summary>
        public bool Success
        {
            get
            {
                return this.BodyText != null && this.BodyText[0] == '+';
            }
        }

        /// <summary>
        /// Gets the error message associated with a failed bgapi call.
        /// </summary>
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