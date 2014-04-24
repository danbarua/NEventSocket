// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReadResult.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   Defines the ReadResult type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch.Applications
{
    using System;

    public class ReadResult : ApplicationResult
    {
        public ReadResult(EventMessage eventMessage, string channelVariable)
            : base(eventMessage)
        {
            this.Digits = eventMessage.GetVariable(channelVariable);
            this.Result = (Status)Enum.Parse(typeof(Status), eventMessage.GetVariable("read_result"), true);
        }

        public enum Status
        {
            Success,
            Timeout,
            Failure
        }

        /// <summary>
        /// Gets a string indicating the status of the read operation, "success", "timeout" or "failure"
        /// </summary>
        public Status Result { get; private set; }

        /// <summary>
        /// Gets the digits read from the Channel.
        /// </summary>
        public string Digits { get; private set; }
    }
}