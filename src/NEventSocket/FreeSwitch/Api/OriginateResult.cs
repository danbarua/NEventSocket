namespace NEventSocket.FreeSwitch.Api
{
    /// <summary>
    /// Represents the result of an originate command
    /// </summary>
    public class OriginateResult
    {
        public OriginateResult(EventMessage answerEvent)
        {
            this.Success = answerEvent.Headers[HeaderNames.AnswerState] != AnswerState.Hangup;
            if (!Success)
                this.HangupCause = answerEvent.Headers[HeaderNames.HangupCause];
            this.ChannelData = answerEvent;
        }

        public OriginateResult(BackgroundJobResult backgroundJobResult)
        {
            this.Success = backgroundJobResult.Success;
            this.HangupCause = backgroundJobResult.ErrorMessage;
        }

        /// <summary>
        /// Gets a boolean indicating whether the originate command succeeded
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Gets a string indicating why the originate command failed
        /// </summary>
        public string HangupCause { get; private set; }

        /// <summary>
        /// Gets an <see cref="EventMessage">EventMessage</see> contanining the ChannelData for the call.
        /// </summary>
        public EventMessage ChannelData { get; private set; }
    }
}