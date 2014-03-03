namespace NEventSocket.FreeSwitch.Applications
{
    public class PlayGetDigitsResult : ApplicationResult
    {
        public PlayGetDigitsResult(EventMessage eventMessage, string channelVariable)
            : base(eventMessage)
        {
            this.Digits = eventMessage.GetVariable(channelVariable);

            this.TerminatorUsed = eventMessage.GetVariable("read_terminator_used");

            this.Success = !string.IsNullOrEmpty(this.Digits);
        }

        public string Digits { get; private set; }

        public string TerminatorUsed { get; private set; }
    }
}