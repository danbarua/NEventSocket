namespace NEventSocket.FreeSwitch.Applications
{
    using System;

    using NEventSocket.Util;

    /// <summary>
    /// Represents the result of an originate command
    /// </summary>
    public class OriginateResult
    {
        public OriginateResult(BasicMessage message)
        {
            var channelEvent = message as EventMessage;
            if (channelEvent != null)
            {
                this.ChannelData = channelEvent;
                this.Success = channelEvent.AnswerState != AnswerState.Hangup;
                this.HangupCause = channelEvent.HangupCause;
            }
            else
            {
                var backgroundJobResult = message as BackgroundJobResult;
                if (backgroundJobResult != null)
                {
                    this.Success = backgroundJobResult.Success;

                    if (!Success) this.HangupCause = backgroundJobResult.ErrorMessage.HeaderToEnumOrNull<HangupCause>();

                    ResponseText = backgroundJobResult.ErrorMessage;
                }
                else
                {
                    throw new ArgumentException("Message Type {0} is not valid to create an OriginateResult from.".Fmt(message.GetType()));
                }
            }
        }

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
        public EventMessage ChannelData { get; protected set; }
    }
}