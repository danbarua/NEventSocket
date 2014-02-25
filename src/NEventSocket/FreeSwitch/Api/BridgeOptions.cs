// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BridgeOptions.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch.Api
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Defines options for executing a bridge
    /// </summary>
    /// <remarks>
    /// https://wiki.freeswitch.org/wiki/Misc._Dialplan_Tools_bridge
    /// </remarks>
    public class BridgeOptions
    {
        /// <summary>
        /// Sets the outbound callerid name
        /// </summary>
        /// <remarks>
        /// https://wiki.freeswitch.org/wiki/Cid
        /// </remarks>
        public string CallerIdName { get; set; }

        /// <summary>
        /// Sets the outbound callerid number.
        /// </summary>
        /// <remarks>
        /// https://wiki.freeswitch.org/wiki/Cid
        /// </remarks>
        public string CallerIdNumber { get; set; }

        /// <summary>
        /// By default when bridging, the first endpoint to provide media (as opposed to actually answering) will win, and the other endpoints will stop ringing. For internal endpoints, this usually doesn't matter. However, in the case of cell phone providers, any custom music that plays for the caller while ringing counts as media. In some cases, the ringing sound itself is media. If your bridge command includes a cell phone number and your internal endpoints stop ringing as soon as the cell phone starts, you will need to enable the 'ignore_early_media' option
        /// </summary>
        public bool IgnoreEarlyMedia { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool HangupAfterBridge { get; set; }

        /// <summary>
        /// The maximum number of seconds to wait for an answer state from a remote endpoint.
        /// </summary>
        public int Timeout { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{");

            if (!string.IsNullOrEmpty(CallerIdName)) sb.AppendFormat("effective_caller_id_name='{0}',", CallerIdName);
            if (!string.IsNullOrEmpty(CallerIdNumber)) sb.AppendFormat("effective_caller_id_number={0},", CallerIdNumber);
            if (IgnoreEarlyMedia) sb.Append("ignore_early_media=true,");
            if (Timeout > 0) sb.AppendFormat("call_timeout={0},", this.Timeout);
            if (HangupAfterBridge) sb.Append("hangup_after_bridge=true,");

            if (sb.Length > 1) sb.Remove(sb.Length - 1, 1);

            sb.Append("}");

            return sb.ToString();
        }
    }
}