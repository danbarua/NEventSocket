// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AttendedTransferResultStatus.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   The result of an Attended Transfer attempt on a bridged call
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// The result of an Attended Transfer attempt on a bridged call
    /// </summary>
    public enum AttendedTransferResultStatus
    {
        /// <summary>
        /// The attempt failed. Inspect the HangupCause for reasons.
        /// </summary>
        Failed,

        /// <summary>
        /// The call was successfully transferred.
        /// </summary>
        Transferred,

        /// <summary>
        /// The bridged call was converted to a three-way conversation
        /// </summary>
        Threeway,

        /// <summary>
        /// Special case: the b-leg hung up before the c-leg answered.
        /// If the c-leg rejects the call, FreeSwitch will call back
        /// the b-leg and attempt to reconnect them to the a-leg.
        /// </summary>
        AttendedTransfer,

        /// <summary>
        /// The transfer was aborted
        /// </summary>
        Aborted
    }
}