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
        /// <param name="eventMessage">The <seealso cref="ChannelEvent"/> to wrap.</param>
        protected ApplicationResult(ChannelEvent eventMessage)
        {
            //eventMessage may be null, this is the case where the other side disconnected and closed down
            //the socket before we got the CHANNEL_EXECUTE_COMPLETE event.

            if (eventMessage != null)
            {
                ChannelData = eventMessage;

                if (ChannelData.Headers.ContainsKey(HeaderNames.ApplicationResponse))
                {
                    ResponseText = ChannelData.Headers[HeaderNames.ApplicationResponse];
                }
            }
            else
            {
                Success = false;
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
        /// Gets an <see cref="ChannelEvent">ChannelEvent</see> containing the ChannelData for the call.
        /// </summary>
        public ChannelEvent ChannelData { get; protected set; }
    }
}