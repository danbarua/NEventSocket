// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReadOptions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   The read options.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>The read options.</summary>
    public class ReadOptions
    {
        private const string channelVariableName = "read_digits_result";

        /// <summary>Gets or sets the min digits.</summary>
        public int MinDigits { get; set; }

        /// <summary>Gets or sets the max digits.</summary>
        public int MaxDigits { get; set; }

        /// <summary>Gets or sets the prompt.</summary>
        public string Prompt { get; set; }

        /// <summary>Gets or sets the timeout ms.</summary>
        public int TimeoutMs { get; set; }

        /// <summary>Gets or sets the terminators.</summary>
        public string Terminators { get; set; }

        public string ChannelVariableName
        {
            get
            {
                return channelVariableName;
            }
        }

        /// <summary>The to string.</summary>
        /// <returns>The <see cref="string"/>.</returns>
        public override string ToString()
        {
            return string.Format(
                "{0} {1} {2} {3} {4} {5}", 
                this.MinDigits, 
                this.MaxDigits, 
                this.Prompt,
                this.ChannelVariableName, 
                this.TimeoutMs, 
                this.Terminators);
        }
    }
}