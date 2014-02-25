// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationResult.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch.Applications
{
    public abstract class ApplicationResult
    {
        protected ApplicationResult()
        {
        }

        protected ApplicationResult(EventMessage eventMessage)
        {
            this.ChannelData = eventMessage;
        }

        /// <summary>
        /// Gets a boolean indicating whether the command succeeded
        /// </summary>
        public bool Success { get; protected set; }

        /// <summary>
        /// Gets the response text from the application
        /// </summary>
        public string ResponseText
        {
            get
            {
                return ChannelData.Headers[HeaderNames.ApplicationResponse];
            }
        }

        /// <summary>
        /// Gets an <see cref="EventMessage">EventMessage</see> contanining the ChannelData for the call.
        /// </summary>
        public EventMessage ChannelData { get; protected set; }
    }
}