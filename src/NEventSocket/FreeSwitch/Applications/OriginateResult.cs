namespace NEventSocket.FreeSwitch.Applications
{
    using System;

    using NEventSocket.Util;

    /// <summary>
    /// Represents the result of an originate command
    /// </summary>
    public class OriginateResult : ApplicationResult
    {
        public OriginateResult(EventMessage channelEvent) : base(channelEvent)
        {
            this.Success = channelEvent.AnswerState != AnswerState.Hangup;
            this.HangupCause = channelEvent.HangupCause;
        }

        public OriginateResult(BackgroundJobResult backgroundJobResult)
        {
            this.Success = backgroundJobResult.Success;

            if (!Success)
                this.HangupCause = backgroundJobResult.ErrorMessage.ToEnumFromUppercaseUnderscore<HangupCause>();

            ResponseText = backgroundJobResult.ErrorMessage;
        }

        /// <summary>
        /// Gets a string indicating why the originate command failed
        /// </summary>
        public HangupCause? HangupCause { get; private set; }
    }
}