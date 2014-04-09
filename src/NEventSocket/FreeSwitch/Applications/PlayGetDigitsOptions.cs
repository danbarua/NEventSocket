namespace NEventSocket.FreeSwitch.Applications
{
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Represents a call to the play_and_get_digits application
    /// </summary>
    public class PlayGetDigitsOptions
    {
        private string channelVariableName = "play_get_digits_result";

        private int maxDigits = 128;

        private int minDigits = 1;

        private string terminatorDigits = "#";

        private string digitsRegex = @"^(0|1|2|3|4|5|6|7|8|9|\*|#)+"; //or "\d+";

        private int maxTries = 5;

        private string promptAudioFile = "silence_stream://10";

        private int digitTimeoutMs = 5000;

        private string badInputAudioFile = "silence_stream://150";

        private int timeoutMs = 5000;

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
        /// Number of tries for the sound to play (maxiumum 128)
        /// </summary>
        public int MaxTries
        {
            get
            {
                return this.maxTries;
            }
            set
            {
                this.maxTries = value;
            }
        }

        /// <summary>
        /// Number of milliseconds to wait for a dialed response after the file playback ends and before PAGD does a retry.
        /// </summary>
        public int TimeoutMs
        {
            get
            {
                return this.timeoutMs;
            }
            set
            {
                this.timeoutMs = value;
            }
        }

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
        public string PromptAudioFile
        {
            get
            {
                return this.promptAudioFile;
            }
            set
            {
                this.promptAudioFile = value;
            }
        }

        /// <summary>
        /// Sound file to play when digits don't match the regexp
        /// </summary>
        public string BadInputAudioFile
        {
            get
            {
                return this.badInputAudioFile;
            }
            set
            {
                this.badInputAudioFile = value;
            }
        }

        /// <summary>
        /// ValidDigits
        /// </summary>
        public string ValidDigits
        {
            set
            {
                //todo: Freeswitch is not excluding "*" when set to numbers only - investigate

                //converts "12345" into "^(1|2|3|4|5)+"
                var sb = new StringBuilder("^(");

                for (int i = 0; i < value.Length; i++)
                {
                    char digit = value[i];
                    if (digit == '*') sb.Append(@"\*");
                    else sb.Append(digit);

                    if (i != value.Length - 1)
                        sb.Append("|");
                }

                sb.Append(")+");
                digitsRegex = sb.ToString();
            }
        }

        /// <summary>
        /// Inter-digit timeout; number of milliseconds allowed between digits; once this number is reached, PAGD assumes that the caller has no more digits to dial
        /// </summary>
        public int DigitTimeoutMs
        {
            get
            {
                return this.digitTimeoutMs;
            }
            set
            {
                this.digitTimeoutMs = value;
            }
        }

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
                "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                MinDigits,
                MaxDigits,
                MaxTries,
                TimeoutMs,
                TerminatorDigits,
                PromptAudioFile,
                BadInputAudioFile,
                channelVariableName,
                digitsRegex,
                DigitTimeoutMs);
        }
    }
}