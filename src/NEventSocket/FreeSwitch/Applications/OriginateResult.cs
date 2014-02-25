namespace NEventSocket.FreeSwitch.Applications
{
    /// <summary>
    /// Represents the result of an originate command
    /// </summary>
    public class OriginateResult : ApplicationResult
    {
        public OriginateResult(EventMessage channelEvent) : base(channelEvent)
        {
            this.Success = channelEvent.AnswerState != AnswerState.Hangup;
            if (!this.Success)
                this.HangupCause = channelEvent.Headers[HeaderNames.HangupCause];
        }

        public OriginateResult(BackgroundJobResult backgroundJobResult)
        {
            this.Success = backgroundJobResult.Success;
            this.HangupCause = backgroundJobResult.ErrorMessage;
        }

        /// <summary>
        /// Gets a string indicating why the originate command failed
        /// </summary>
        public string HangupCause { get; private set; }
    }
}