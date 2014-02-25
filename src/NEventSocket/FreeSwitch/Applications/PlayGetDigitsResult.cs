namespace NEventSocket.FreeSwitch.Applications
{
    public class PlayGetDigitsResult : ApplicationResult
    {
        public PlayGetDigitsResult(EventMessage eventMessage)
            : base(eventMessage)
        {
            this.Digits = eventMessage.Headers[HeaderNames.ApplicationResponse];
            this.Success = !string.IsNullOrEmpty(this.Digits);
        }

        public string Digits { get; set; }
    }
}