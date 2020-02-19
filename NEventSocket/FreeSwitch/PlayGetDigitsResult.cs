// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayGetDigitsResult.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// Represents the result of the play_and_get_digits application
    /// </summary>
    public class PlayGetDigitsResult : ApplicationResult
    {
        internal PlayGetDigitsResult(ChannelEvent eventMessage, string channelVariable) : base(eventMessage)
        {
            Digits = eventMessage.GetVariable(channelVariable);

            TerminatorUsed = eventMessage.GetVariable("read_terminator_used");

            Success = !string.IsNullOrEmpty(Digits);
        }

        /// <summary>
        /// Gets the digits returned by the application
        /// </summary>
        public string Digits { get; private set; }

        /// <summary>
        /// Gets the terminating digit inputted by the user, if any
        /// </summary>
        public string TerminatorUsed { get; private set; }
    }
}