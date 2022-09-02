// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AnswerState.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// Represents the AnswerState of a Channel
    /// </summary>
    public enum AnswerState
    {
        /// <summary>
        /// The Channel is pre answered
        /// </summary>
        Early,

        /// <summary>
        /// The Channel is Answered
        /// </summary>
        Answered, 

        /// <summary>
        /// The Channel has Hung Up
        /// </summary>
        Hangup, 

        /// <summary>
        /// The Channel is Ringing
        /// </summary>
        Ringing
    }
}