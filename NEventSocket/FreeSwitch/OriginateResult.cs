// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OriginateResult.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using System;

    using NEventSocket.Util;

    /// <summary>
    /// Represents the result of an originate command
    /// </summary>
    public class OriginateResult
    {
        private OriginateResult(ChannelEvent channelEvent)
        {
            ChannelData = channelEvent;
            Success = channelEvent.AnswerState != AnswerState.Hangup;
            HangupCause = channelEvent.HangupCause;
        }

        private OriginateResult(BackgroundJobResult backgroundJobResult)
        {
            Success = backgroundJobResult.Success;

            if (!Success)
            {
                HangupCause = backgroundJobResult.ErrorMessage.HeaderToEnumOrNull<HangupCause>();
            }

            ResponseText = backgroundJobResult.ErrorMessage;
        }

        /// <summary>
        /// Gets the <seealso cref="HangupCause"/> if the originate failed.
        /// </summary>
        public HangupCause? HangupCause { get; private set; }

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
        public ChannelEvent ChannelData { get; protected set; }

        /// <summary>
        /// Creates an <see cref="OriginateResult"/> from either a BackgroundJobResult or an EventMessage
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>An <see cref="OriginateResult"/>.</returns>
        /// <exception cref="ArgumentException">If the wrong message type is passed.</exception>
        public static OriginateResult FromBackgroundJobResultOrChannelEvent(BasicMessage message)
        {
            var channelEvent = message as ChannelEvent;
            if (channelEvent != null)
            {
                return new OriginateResult(channelEvent);
            }

            var backgroundJobResult = message as BackgroundJobResult;
            if (backgroundJobResult != null)
            {
                return new OriginateResult(backgroundJobResult);
            }

            throw new ArgumentException("Message Type {0} is not valid to create an OriginateResult from.".Fmt(message.GetType()));
        }
    }
}