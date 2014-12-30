// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationResult.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// Encapsulates a ChannelExecuteComplete event message
    /// </summary>
    public abstract class ApplicationResult
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        protected ApplicationResult()
        {
        }

        /// <summary>
        /// Instantiates an <seealso cref="ApplicationResult"/> from a ChannelExecuteComplete <seealso cref="EventMessage"/>.
        /// </summary>
        /// <param name="eventMessage">The <seealso cref="EventMessage"/> to wrap.</param>
        protected ApplicationResult(EventMessage eventMessage)
        {
            this.ChannelData = eventMessage;

            if (this.ChannelData.Headers.ContainsKey(HeaderNames.ApplicationResponse))
            {
                this.ResponseText = this.ChannelData.Headers[HeaderNames.ApplicationResponse];
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether the command succeeded
        /// </summary>
        public bool Success { get; protected set; }

        /// <summary>
        /// Gets the response text from the application
        /// </summary>
        public string ResponseText { get; protected set; }

        /// <summary>
        /// Gets an <see cref="EventMessage">EventMessage</see> contanining the ChannelData for the call.
        /// </summary>
        public EventMessage ChannelData { get; protected set; }
    }
}