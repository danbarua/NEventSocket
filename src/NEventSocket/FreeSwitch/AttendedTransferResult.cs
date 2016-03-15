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

        //protected internal AttendedTransferResult(EventMessage eventMessage)
        //{
        //    if (eventMessage == null)
        //    {
        //        Status = AttendedTransferResultStatus.Failed;
        //        return;
        //    }

        //    var lastBridgeHangupCause = eventMessage.GetVariable("last_bridge_hangup_cause").HeaderToEnumOrNull<HangupCause>();
        //    var originateDisposition = eventMessage.GetVariable("originate_disposition").HeaderToEnumOrNull<HangupCause>();
        //    var xferResult = eventMessage.GetVariable("att_xfer_result");
        //    var xferUuids = eventMessage.GetVariable("xfer_uuids");

        //    //sometimes xferResult is populated, sometimes it isn't!
        //    if (xferResult != null && xferResult == "success")
        //    {
        //        if (eventMessage.AnswerState == AnswerState.Answered)
        //        {
        //            //b-leg is still connected, let's see if we're in a three-way
        //            if (xferUuids != null)
        //            {
        //                Status = AttendedTransferResultStatus.Threeway;
        //                HangupCause = HangupCause.Success;
        //            }
        //            else if (lastBridgeHangupCause.Value == HangupCause.NormalClearing)
        //            {
        //                //the c-leg hung up, b-leg is still bridged to a-leg
        //                Status = AttendedTransferResultStatus.Failed;
        //                HangupCause = HangupCause.CallRejected;
        //            }
        //        }
        //        else
        //        {
        //            //b-leg is no longer connected, let's assume success
        //            Status = AttendedTransferResultStatus.Transferred;
        //            HangupCause = HangupCause.Success;
        //        }
        //    }
        //    else if (originateDisposition != null)
        //    {
        //        //xferResult wasn't populated, here be dragons..
        //        //let's try to figure out what happened
        //        HangupCause = originateDisposition.Value;

        //        if (HangupCause.Value == HangupCause.Success)
        //        {
        //            if (eventMessage.AnswerState == AnswerState.Hangup)
        //            {
        //                //b-leg has hungup, we've transferred to the c-leg
        //                Status = AttendedTransferResultStatus.Transferred;
        //            }
        //            else
        //            {
        //                //b-leg is still connected
        //                Status = AttendedTransferResultStatus.Failed;
        //                HangupCause = HangupCause.CallRejected;
        //            }
        //        }
        //        else if (HangupCause.Value == HangupCause.AttendedTransfer)
        //        {
        //            //we'll get here if the b-leg hung up while the c-leg was ringing
        //            //in this case, FreeSwitch will keep ringing the c-leg
        //            //if the c-leg answers, a-leg will be bridged to the c-leg
        //            //if the c-leg does not answer, then FreeSwitch will attempt to ring
        //            //back the b-leg and bridge the a-leg to the b-leg WITH A NEW CHANNEL.
        //            Status = AttendedTransferResultStatus.AttendedTransfer;
        //        }
        //    }
        //}

        /// <summary>
        /// Gets the <seealso cref="AttendedTransferResultStatus"/> of the application.
        /// </summary>
        public AttendedTransferResultStatus Status { get; private set; }

        /// <summary>
        /// Gets the hangup cause of the C-Leg.
        /// </summary>
        public HangupCause? HangupCause { get; private set; }

        protected static internal AttendedTransferResult Hangup(EventMessage hangupMessage)
        {
            if (hangupMessage.EventName != EventName.ChannelHangup)
            {
                throw new InvalidOperationException("Expected event of type ChannelHangup, got {0} instead".Fmt(hangupMessage.EventName));
            }

            return new AttendedTransferResult()
            {
                HangupCause = hangupMessage.HangupCause,
                Status = AttendedTransferResultStatus.Failed
            };
        }

        protected static internal AttendedTransferResult Success(AttendedTransferResultStatus status = AttendedTransferResultStatus.Transferred )
        {
            return new AttendedTransferResult()
            {
                Status = status
            };
        }

        protected static internal AttendedTransferResult Aborted()
        {
            return new AttendedTransferResult()
            {
                Status = AttendedTransferResultStatus.Aborted
            };
        }

        protected static internal AttendedTransferResult Failed(HangupCause hangupCause)
        {
            return new AttendedTransferResult()
            {
                Status = AttendedTransferResultStatus.Failed,
                HangupCause = hangupCause
            };
        }
    }
}