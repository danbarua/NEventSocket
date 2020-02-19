// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AttendedTransferResult.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   Defines the AttendedTransferResult type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using System;

    using NEventSocket.Util;

    /// <summary>
    /// Represents the result of an Attended Transfer
    /// </summary>
    public class AttendedTransferResult
    {
        protected AttendedTransferResult()
        {
        }

        /// <summary>
        /// Gets the <seealso cref="AttendedTransferResultStatus"/> of the application.
        /// </summary>
        public AttendedTransferResultStatus Status { get; private set; }

        /// <summary>
        /// Gets the hangup cause of the C-Leg.
        /// </summary>
        public HangupCause? HangupCause { get; private set; }

        protected internal static AttendedTransferResult Hangup(ChannelEvent hangupMessage)
        {
            if (hangupMessage.EventName != EventName.ChannelHangup)
            {
                throw new InvalidOperationException("Expected event of type ChannelHangup, got {0} instead".Fmt(hangupMessage.EventName));
            }

            return new AttendedTransferResult() { HangupCause = hangupMessage.HangupCause, Status = AttendedTransferResultStatus.Failed };
        }

        protected internal static AttendedTransferResult Success(
            AttendedTransferResultStatus status = AttendedTransferResultStatus.Transferred)
        {
            return new AttendedTransferResult() { Status = status };
        }

        protected internal static AttendedTransferResult Aborted()
        {
            return new AttendedTransferResult() { Status = AttendedTransferResultStatus.Aborted };
        }

        protected internal static AttendedTransferResult Failed(HangupCause hangupCause)
        {
            return new AttendedTransferResult() { Status = AttendedTransferResultStatus.Failed, HangupCause = hangupCause };
        }
    }
}