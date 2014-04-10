// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BridgeOptions.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch.Api
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Defines options for executing a bridge
    /// </summary>
    /// <remarks>
    /// See https://wiki.freeswitch.org/wiki/Misc._Dialplan_Tools_bridge
    /// </remarks>
    public class BridgeOptions
    {
        public BridgeOptions()
        {
            ChannelVariables = new Dictionary<string, string>();
        }

        /// <summary>
        /// Optionally set the UUID of the outbound leg before initiating the bridge.
        /// </summary>
        public string UUID { get; set; }

        /// <summary>
        /// Sets the outbound callerid name
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Cid
        /// </remarks>
        public string CallerIdName { get; set; }

        /// <summary>
        /// Sets the outbound callerid number.
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Cid
        /// </remarks>
        public string CallerIdNumber { get; set; }

        /// <summary>
        /// By default when bridging, the first endpoint to provide media (as opposed to actually answering) 
        /// will win, and the other endpoints will stop ringing. For internal endpoints, this usually doesn't matter. 
        /// However, in the case of cell phone providers, any custom music that plays for the caller while ringing counts as media. 
        /// In some cases, the ringing sound itself is media. If your bridge command includes a cell phone number and your internal endpoints
        /// stop ringing as soon as the cell phone starts, you will need to enable the 'ignore_early_media' option
        /// </summary>
        public bool IgnoreEarlyMedia { get; set; }

        /// <summary>
        /// If set to true, the call will terminate when the bridge completes.
        /// </summary>
        public bool HangupAfterBridge { set { ChannelVariables["hangup_after_bridge"] = value.ToString().ToLowerInvariant(); } }

        /// <summary>
        /// If not null, will set the ringback channel variable on the A-Leg to the given sound.
        /// </summary>
        public string RingBack { set { ChannelVariables["ringback"] = value; } }

        /// <summary>
        /// The maximum number of seconds to wait for an answer state from a remote endpoint.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// If set to true, the call will not terminate when the bridge fails.
        /// </summary>
        public bool ContinueOnFail { set { ChannelVariables["continue_on_fail"] = value.ToString().ToLowerInvariant(); } }

        /// <summary>
        /// Setting this variable to true will prevent DTMF digits received on this channel when bridged from being sent to the other channel.
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Variable_bridge_filter_dtmf
        /// </remarks>
        public bool FilterDtmf { set { ChannelVariables["bridge_filter_dtmf"] = value.ToString().ToLowerInvariant(); } }

        /// <summary>
        /// Execute an API command after bridge.
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Variable_api_after_bridge
        /// </remarks>
        public string ApiAfterBridge { set { ChannelVariables["api_after_bridge"] = value; } }

        /// <summary>
        /// Sets a prompt for the callee to accept the call by pressing a DTMF key or PIN code
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Freeswitch_IVR_Originate#Answer_confirmation
        /// </remarks>
        public string ConfirmPrompt { set { ChannelVariables["group_confirm_file"] = value; } }

        /// <summary>
        /// Sets a prompt to be played on invalid input
        /// </summary>
        public string ConfirmInvalidPrompt { set { ChannelVariables["group_confirm_error_file"] = value; } }

        /// <summary>
        /// Sets a DTMF key or PIN code to be inputted to accept the call
        /// </summary>
        public string ConfirmKey { set { ChannelVariables["group_confirm_key"] = value; } }

        /// <summary>
        /// Sets a timeout for inputting a confirmation Key or PIN (Defaults to 5000ms)
        /// </summary>
        public int ConfirmReadTimeoutMs { set { ChannelVariables["group_confirm_read_timeout"] = value.ToString(); } }

        /// <summary>
        /// Unknown - not documented see https://wiki.freeswitch.org/wiki/Freeswitch_IVR_Originate#Answer_confirmation
        /// </summary>
        public bool FailOnSingleReject { set { ChannelVariables["fail_on_single_reject"] = value.ToString().ToLowerInvariant(); } }

        /// <summary>
        /// Unknown - not documented see https://wiki.freeswitch.org/wiki/Freeswitch_IVR_Originate#Answer_confirmation
        /// </summary>
        public bool ConfirmCancelTimeout { set { ChannelVariables["fail_on_single_reject"] = value.ToString().ToLowerInvariant(); } }

        /// <summary>
        /// Container for any Channel Variables to be set before executing the bridge
        /// </summary>
        public IDictionary<string, string> ChannelVariables { get; private set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{");
          
            if (Timeout > 0) sb.AppendFormat("call_timeout={0},", this.Timeout);

            if (!string.IsNullOrEmpty(this.UUID)) sb.AppendFormat("origination_uuid='{0}',", this.UUID);

            /* https://wiki.freeswitch.org/wiki/Variable_effective_caller_id_name
            /*  sets the effective callerid name. This is automatically exported to the B-leg; however, it is not valid in an origination string.
             * In other words, set this before calling bridge, otherwise use origination_caller_id_name */
            if (!string.IsNullOrEmpty(this.CallerIdName)) sb.AppendFormat("origination_caller_id_name='{0}',", this.CallerIdName);
            if (!string.IsNullOrEmpty(this.CallerIdNumber)) sb.AppendFormat("origination_caller_id_number={0},", this.CallerIdNumber);

            if (this.Timeout > 0) sb.AppendFormat("originate_timeout={0},", this.Timeout);
            if (this.IgnoreEarlyMedia) sb.Append("ignore_early_media=true,");

            if (sb.Length > 1) sb.Remove(sb.Length - 1, 1);

            sb.Append("}");

            return sb.ToString();
        }
    }
}