// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayGetDigitsOptions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using NEventSocket.Util.ObjectPooling;

    /// <summary>
    /// Represents a call to the play_and_get_digits application
    /// </summary>
    public class PlayGetDigitsOptions
    {
        private string channelVariableName = "play_get_digits_result";

        private int maxDigits = 128;

        private int minDigits = 1;

        private string terminatorDigits = "#";

        private string digitsRegex = @"^(0|1|2|3|4|5|6|7|8|9|\*|#)+"; // or "\d+";

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
                return minDigits;
            }

            set
            {
                minDigits = value;
            }
        }

        /// <summary>
        /// Maximum number of digits to fetch (maximum value of 128)
        /// </summary>
        public int MaxDigits
        {
            get
            {
                return maxDigits;
            }

            set
            {
                maxDigits = value;
            }
        }

        /// <summary>
        /// Number of tries for the sound to play (maxiumum 128)
        /// </summary>
        public int MaxTries
        {
            get
            {
                return maxTries;
            }

            set
            {
                maxTries = value;
            }
        }

        /// <summary>
        /// Number of milliseconds to wait for a dialed response after the file playback ends and before PAGD does a retry.
        /// </summary>
        public int TimeoutMs
        {
            get
            {
                return timeoutMs;
            }

            set
            {
                timeoutMs = value;
            }
        }

        /// <summary>
        /// Digits used to end input if less than MaxDigits digits have been pressed. (Typically '#')
        /// </summary>
        public string TerminatorDigits
        {
            get
            {
                return terminatorDigits;
            }

            set
            {
                terminatorDigits = value;
            }
        }

        /// <summary>
        /// Sound file to play while digits are fetched
        /// </summary>
        public string PromptAudioFile
        {
            get
            {
                return promptAudioFile;
            }

            set
            {
                promptAudioFile = value;
            }
        }

        /// <summary>
        /// Sound file to play when digits don't match the regexp
        /// </summary>
        public string BadInputAudioFile
        {
            get
            {
                return badInputAudioFile;
            }

            set
            {
                badInputAudioFile = value;
            }
        }

        /// <summary>
        /// Valid Digits helper property - converts "12345" into a Regex
        /// </summary>
        public string ValidDigits
        {
            set
            {
                // todo: Freeswitch is not excluding "*" when set to numbers only - investigate

                // converts "12345" into "^(1|2|3|4|5)+"
                var sb = StringBuilderPool.Allocate();
                sb.Append("^(");

                for (var i = 0; i < value.Length; i++)
                {
                    var digit = value[i];
                    if (digit == '*')
                    {
                        sb.Append(@"\*");
                    }
                    else
                    {
                        sb.Append(digit);
                    }

                    if (i != value.Length - 1)
                    {
                        sb.Append("|");
                    }
                }

                sb.Append(")+");
                digitsRegex = StringBuilderPool.ReturnAndFree(sb);
            }
        }

        /// <summary>
        /// Regex used to validate input
        /// </summary>
        public string ValidDigitsRegex
        {
            get
            {
                return digitsRegex;
            }

            set
            {
                digitsRegex = value;
            }
        }

        /// <summary>
        /// Inter-digit timeout; number of milliseconds allowed between digits; once this number is reached, PAGD assumes that the caller has no more digits to dial
        /// </summary>
        public int DigitTimeoutMs
        {
            get
            {
                return digitTimeoutMs;
            }

            set
            {
                digitTimeoutMs = value;
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

            set
            {
                channelVariableName = value;
            }
        }

        /// <summary>
        /// Converts the <seealso cref="PlayGetDigitsOptions"/> instance to an application argument string.
        /// </summary>
        /// <returns>An application argument to pass to the play_and_get_digits application.</returns>
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