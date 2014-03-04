namespace NEventSocket.FreeSwitch.Applications
{
    /// <summary>
    /// Represents a call to the play_and_get_digits application
    /// </summary>
    public class PlayGetDigitsOptions
    {
        private string channelVariableName = "play_get_digits_result";

        private int maxDigits = 128;

        private int minDigits = 0;

        private string terminatorDigits = "#";

        private string digitsRegex = @"\d+";

        /// <summary>
        /// Minimum number of digits to fetch (minimum value of 0)
        /// </summary>
        public int MinDigits
        {
            get
            {
                return this.minDigits;
            }

            set
            {
                this.minDigits = value;
            }
        }

        /// <summary>
        /// Maximum number of digits to fetch (maximum value of 128)
        /// </summary>
        public int MaxDigits
        {
            get
            {
                return this.maxDigits;
            }

            set
            {
                this.maxDigits = value;
            }
        }

        /// <summary>
        /// Number of tries for the sound to play
        /// </summary>
        public int MaxTries { get; set; }

        /// <summary>
        /// Number of milliseconds to wait for a dialed response after the file playback ends and before PAGD does a retry.
        /// </summary>
        public int TimeoutMs { get; set; }

        /// <summary>
        /// Digits used to end input if less than MaxDigits digits have been pressed. (Typically '#')
        /// </summary>
        public string TerminatorDigits
        {
            get
            {
                return this.terminatorDigits;
            }

            set
            {
                this.terminatorDigits = value;
            }
        }

        /// <summary>
        /// Sound file to play while digits are fetched
        /// </summary>
        public string PromptAudioFile { get; set; }

        /// <summary>
        /// Sound file to play when digits don't match the regexp
        /// </summary>
        public string BadInputAudioFile { get; set; }

        /// <summary>
        /// Regular expression to match digits
        /// </summary>
        public string DigitsRegex
        {
            get
            {
                return this.digitsRegex;
            }

            set
            {
                this.digitsRegex = value;
            }
        }

        /// <summary>
        /// Inter-digit timeout; number of milliseconds allowed between digits; once this number is reached, PAGD assumes that the caller has no more digits to dial
        /// </summary>
        public int DigitTimeoutMs { get; set; }

        /// <summary>
        /// where to transfer call when max tries has been reached, example: 1 XML hangup
        /// </summary>
        public string TransferOnFailure { get; set; }

        /// <summary>
        /// Gets the name of the channel variable which will contain the result
        /// </summary>
        public string ChannelVariableName 
        { 
            get
            {
                return channelVariableName;
            }
        }

        public override string ToString()
        {
            return string.Format(
                "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}",
                MinDigits,
                MaxDigits,
                MaxTries,
                TimeoutMs,
                TerminatorDigits,
                PromptAudioFile,
                BadInputAudioFile,
                channelVariableName,
                DigitsRegex,
                DigitTimeoutMs,
                TransferOnFailure);
        }
    }
}