namespace NEventSocket.FreeSwitch.Applications
{
    public class ReadResult : ApplicationResult
    {
        public ReadResult(EventMessage eventMessage, string channelVariable)
            : base(eventMessage)
        {
            this.Digits = eventMessage.GetVariable(channelVariable);
        }

        public string Digits { get; private set; }
    }
}