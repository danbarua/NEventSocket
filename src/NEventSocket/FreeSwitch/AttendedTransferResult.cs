namespace NEventSocket.FreeSwitch
{
    using NEventSocket.Util;

    public class AttendedTransferResult : ApplicationResult
    {
        public AttendedTransferResult(EventMessage eventMessage) : base(eventMessage)
        {
            if (eventMessage == null)
            {
                Status = AttendedTransferResultStatus.Failed;
                return;
            }

            var xferResult = eventMessage.GetVariable("att_xfer_result");
            if (xferResult != null && xferResult == "success")
            {
                if (eventMessage.AnswerState == AnswerState.Answered)
                {
                    Status = AttendedTransferResultStatus.Threeway;
                }
                else
                {
                    Status = AttendedTransferResultStatus.Transferred;
                }
            }
            else
            {
                var originateDisposition = eventMessage.GetVariable("originate_disposition");
                if (originateDisposition != null)
                {
                    HangupCause = originateDisposition.HeaderToEnum<HangupCause>();

                    if (HangupCause == HangupCause.Success) 
                    {
                        Status = AttendedTransferResultStatus.Transferred;
                    }
                    else if (HangupCause == HangupCause.AttendedTransfer)
                    {
                        Status = AttendedTransferResultStatus.AttendedTransfer;
                    }
                }
            }
        }

        public AttendedTransferResultStatus Status { get; private set; }

        public HangupCause HangupCause { get; set; }
    }

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
        AttendedTransfer
    }
}