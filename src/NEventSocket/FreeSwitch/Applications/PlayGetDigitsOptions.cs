namespace NEventSocket.FreeSwitch.Applications
{
    public class PlayGetDigitsOptions
    {
        public int MinDigits { get; set; }
        public int MaxDigits { get; set; }
        public int MaxTries { get; set; }
        public int Timeout { get; set; }
        public string TerminatorDigits { get; set; }
        public string PromptAudioFile { get; set; }
        public string BadInputAudioFile { get; set; }
        public string DigitsRegex { get; set; }
    }
}