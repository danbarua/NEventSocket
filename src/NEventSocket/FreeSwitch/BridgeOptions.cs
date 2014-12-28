// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BridgeOptions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using NEventSocket.Util;

    /// <summary>
    /// Defines options for executing a bridge
    /// </summary>
    /// <remarks>
    /// See https://wiki.freeswitch.org/wiki/Misc._Dialplan_Tools_bridge
    /// </remarks>
    public class BridgeOptions
    {
        private readonly IDictionary<string, string> parameters = new Dictionary<string, string>(); 

        public BridgeOptions()
        {
            this.ChannelVariables = new Dictionary<string, string>();
        }

        /// <summary>
        /// Optionally set the UUID of the outbound leg before initiating the bridge.
        /// </summary>
        /// <remarks>
        /// NEventSocket will set the UUID if it is not set, so that we can catch events of interest on the outbound channel.
        /// </remarks>
        public string UUID
        {
            get
            {
                return this.parameters.GetValueOrDefault("origination_uuid");
            }

            set
            {
                this.parameters["origination_uuid"] = value;
            }
        }

        /// <summary>
        /// Sets the outbound callerid name
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Cid
        /// </remarks>
        public string CallerIdName { set { this.parameters["origination_caller_id_name"] = value; } }

        /// <summary>
        /// Sets the outbound callerid number.
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Cid
        /// </remarks>
        public string CallerIdNumber { set { this.parameters["origination_caller_id_number"] = value; } }

        /// <summary>
        /// By default when bridging, the first endpoint to provide media (as opposed to actually answering) 
        /// will win, and the other endpoints will stop ringing. For internal endpoints, this usually doesn't matter. 
        /// However, in the case of cell phone providers, any custom music that plays for the caller while ringing counts as media. 
        /// In some cases, the ringing sound itself is media. If your bridge command includes a cell phone number and your internal endpoints
        /// stop ringing as soon as the cell phone starts, you will need to enable the 'ignore_early_media' option
        /// </summary>
        public bool IgnoreEarlyMedia { set { this.parameters["ignore_early_media"] = value.ToLowerBooleanString(); } }

        /// <summary>
        /// If set to true, the call will terminate when the bridge completes.
        /// </summary>
        /// <remarks>
        /// Defaults to true if unset.
        /// </remarks>
        public bool HangupAfterBridge { set { this.ChannelVariables["hangup_after_bridge"] = value.ToLowerBooleanString(); } }

        /// <summary>
        /// Sets the ringback channel variable on the A-Leg to the given sound.
        /// </summary>
        public string RingBack { set { this.ChannelVariables["ringback"] = value; } } //{ get; set; }

        /// <summary>
        /// The maximum number of seconds to wait for an answer from a remote endpoint.
        /// </summary>
        public int TimeoutSeconds { set { this.parameters["call_timeout"] = value.ToString(); } } //todo: test with this or originate_timeout ?

        /// <summary>
        /// If set to true, the dial-plan will continue to execute when the bridge fails instead of terminating the a-leg.
        /// </summary>
        public bool ContinueOnFail { set { this.ChannelVariables["continue_on_fail"] = value.ToString().ToLowerInvariant(); } }

        /// <summary>
        /// Setting this variable to true will prevent DTMF digits received on this channel when bridged from being sent to the other channel.
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Variable_bridge_filter_dtmf
        /// </remarks>
        public bool FilterDtmf { set { this.ChannelVariables["bridge_filter_dtmf"] = value.ToString().ToLowerInvariant(); } }

        /// <summary>
        /// Execute an API command after bridge.
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Variable_api_after_bridge
        /// </remarks>
        public string ApiAfterBridge { set { this.ChannelVariables["api_after_bridge"] = value; } }

        /// <summary>
        /// Sets a prompt for the callee to accept the call by pressing a DTMF key or PIN code
        /// Will not work unless ConfirmKey is also set.
        /// </summary>
        /// <remarks>
        /// If you want to just playback an audio prompt to the callee before bridging the call, without asking any confirmation, here's an example:
        /// {group_confirm_file=playback /path/to/prompt.wav,group_confirm_key=exec}
        /// See https://wiki.freeswitch.org/wiki/Freeswitch_IVR_Originate#Answer_confirmation
        /// </remarks>
        public string ConfirmPrompt { set { this.parameters["group_confirm_file"] = value; } }

        /// <summary>
        /// Sets a prompt to be played on invalid input.
        /// Will not work unless ConfirmKey is also set. 
        /// </summary>
        public string ConfirmInvalidPrompt { set { this.parameters["group_confirm_error_file"] = value; } }

        /// <summary>
        /// Sets a DTMF key or PIN code to be inputted to accept the call. Set to "exec" to just play a whisper prompt before connecting the bridge.
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Freeswitch_IVR_Originate#Answer_confirmation
        /// </remarks>
        public string ConfirmKey { set { this.parameters["group_confirm_key"] = value; } }

        /// <summary>
        /// Sets a timeout for inputting a confirmation Key or PIN (Defaults to 5000ms)
        /// </summary>
        public int ConfirmReadTimeoutMs { set { this.parameters["group_confirm_read_timeout"] = value.ToString(); } }

        /// <summary>
        /// Unknown - not documented see https://wiki.freeswitch.org/wiki/Freeswitch_IVR_Originate#Answer_confirmation
        /// </summary>
        public bool FailOnSingleReject { set { this.parameters["fail_on_single_reject"] = value.ToString().ToLowerInvariant(); } }

        /// <summary>
        /// Unknown - not documented see https://wiki.freeswitch.org/wiki/Freeswitch_IVR_Originate#Answer_confirmation
        /// </summary>
        public bool ConfirmCancelTimeout { set { this.parameters["group_confirm_cancel_timeout "] = value.ToString().ToLowerInvariant(); } }

        /// <summary>
        /// Container for any Channel Variables to be set before executing the bridge
        /// </summary>
        public IDictionary<string, string> ChannelVariables { get; private set; }

        [DebuggerStepThrough]
        public override string ToString()
        {
            var sb = StringBuilderPool.Allocate();
            sb.Append("{");
            
            sb.Append(this.parameters.ToOriginateString());

            if (sb.Length > 1) sb.Remove(sb.Length - 1, 1);

            sb.Append("}");

            return StringBuilderPool.ReturnAndFree(sb);
        }
    }
}