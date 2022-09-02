// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BridgeOptions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.FreeSwitch
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;

    using NEventSocket.Util;
    using NEventSocket.Util.ObjectPooling;

    /// <summary>
    /// Defines options for executing a bridge
    /// </summary>
    /// <remarks>
    /// See https://wiki.freeswitch.org/wiki/Misc._Dialplan_Tools_bridge
    /// </remarks>
    [Serializable]
    public class BridgeOptions : ISerializable
    {
        private readonly IDictionary<string, string> parameters = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of a <seealso cref="BridgeOptions"/>.
        /// </summary>
        public BridgeOptions()
        {
            ChannelVariables = new Dictionary<string, string>();
        }

        /// <summary>
        /// The special constructor is used to deserialize options
        /// </summary>
        public BridgeOptions(SerializationInfo info, StreamingContext context)
        {
            parameters =
                (Dictionary<string, string>)info.GetValue("parameters", typeof(Dictionary<string, string>));
            ChannelVariables =
                (Dictionary<string, string>)info.GetValue("ChannelVariables", typeof(Dictionary<string, string>));
        }

        /// <summary>
        /// Gets or sets the raw <see cref="System.String"/> value of the specified parameter.
        /// </summary>
        /// <value>
        /// The raw value.
        /// </value>
        /// <param name="parameter">The parameter name.</param>
        /// <returns></returns>
        public string this[string parameter]
        {
            get
            {
                return parameters[parameter];
            }

            set
            {
                parameters[parameter] = value;
            }
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
                return parameters.GetValueOrDefault("origination_uuid");
            }

            set
            {
                parameters["origination_uuid"] = value;
            }
        }

        /// <summary>
        /// Sets the outbound callerid name
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Cid
        /// </remarks>
        public string CallerIdName
        {
            set
            {
                parameters["origination_caller_id_name"] = value;
            }
        }

        /// <summary>
        /// Sets the outbound callerid number.
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Cid
        /// </remarks>
        public string CallerIdNumber
        {
            set
            {
                parameters["origination_caller_id_number"] = value;
            }
        }

        /// <summary>
        /// By default when bridging, the first endpoint to provide media (as opposed to actually answering) 
        /// will win, and the other endpoints will stop ringing. For internal endpoints, this usually doesn't matter. 
        /// However, in the case of cell phone providers, any custom music that plays for the caller while ringing counts as media. 
        /// In some cases, the ringing sound itself is media. If your bridge command includes a cell phone number and your internal endpoints
        /// stop ringing as soon as the cell phone starts, you will need to enable the 'ignore_early_media' option
        /// </summary>
        public bool IgnoreEarlyMedia
        {
            set
            {
                parameters["ignore_early_media"] = value.ToLowerBooleanString();
            }
        }

        /// <summary>
        /// If set to true, the call will terminate when the bridge completes.
        /// </summary>
        /// <remarks>
        /// Defaults to true if unset.
        /// </remarks>
        public bool HangupAfterBridge
        {
            set
            {
                ChannelVariables["hangup_after_bridge"] = value.ToLowerBooleanString();
            }
        }

        /// <summary>
        /// Sets the ringback channel variable on the A-Leg to the given sound.
        /// </summary>
        public string RingBack
        {
            set
            {
                ChannelVariables["ringback"] = value;
            }
        }

        /// <summary>
        /// The maximum number of seconds to wait for an answer from a remote endpoint.
        /// </summary>
        public int TimeoutSeconds
        {
            set
            {
                parameters["leg_timeout"] = value.ToString();
            }
        }

        // todo: test with this or originate_timeout ?

        /// <summary>
        /// If set to true, the dial-plan will continue to execute when the bridge fails instead of terminating the a-leg.
        /// </summary>
        public bool ContinueOnFail
        {
            set
            {
                ChannelVariables["continue_on_fail"] = value.ToString().ToLowerInvariant();
            }
        }

        /// <summary>
        /// Setting this variable to true will prevent DTMF digits received on this channel when bridged from being sent to the other channel.
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Variable_bridge_filter_dtmf
        /// </remarks>
        public Leg FilterDtmf
        {
            set
            {
                switch (value)
                {
                    case Leg.ALeg:
                        ChannelVariables["bridge_filter_dtmf"] = "true";
                        break;
                    case Leg.BLeg:
                        parameters["bridge_filter_dtmf"] = "true";
                        break;
                    case Leg.Both:
                        ChannelVariables["bridge_filter_dtmf"] = "true";
                        parameters["bridge_filter_dtmf"] = "true";
                        break;
                }
            }
        }

        /// <summary>
        /// Execute an API command after bridge.
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Variable_api_after_bridge
        /// </remarks>
        public string ApiAfterBridge
        {
            set
            {
                ChannelVariables["api_after_bridge"] = value;
            }
        }

        /// <summary>
        /// Sets a prompt for the callee to accept the call by pressing a DTMF key or PIN code
        /// Will not work unless ConfirmKey is also set.
        /// </summary>
        /// <remarks>
        /// If you want to just playback an audio prompt to the callee before bridging the call, without asking any confirmation, here's an example:
        /// {group_confirm_file=playback /path/to/prompt.wav,group_confirm_key=exec}
        /// See https://wiki.freeswitch.org/wiki/Freeswitch_IVR_Originate#Answer_confirmation
        /// </remarks>
        public string ConfirmPrompt
        {
            set
            {
                parameters["group_confirm_file"] = value;
            }
        }

        /// <summary>
        /// Sets a prompt to be played on invalid input.
        /// Will not work unless ConfirmKey is also set. 
        /// </summary>
        public string ConfirmInvalidPrompt
        {
            set
            {
                parameters["group_confirm_error_file"] = value;
            }
        }

        /// <summary>
        /// Sets a DTMF key or PIN code to be inputted to accept the call. Set to "exec" to just play a whisper prompt before connecting the bridge.
        /// </summary>
        /// <remarks>
        /// See https://wiki.freeswitch.org/wiki/Freeswitch_IVR_Originate#Answer_confirmation
        /// </remarks>
        public string ConfirmKey
        {
            set
            {
                parameters["group_confirm_key"] = value;
            }
        }

        /// <summary>
        /// Sets a timeout for inputting a confirmation Key or PIN (Defaults to 5000ms)
        /// </summary>
        public int ConfirmReadTimeoutMs
        {
            set
            {
                parameters["group_confirm_read_timeout"] = value.ToString();
            }
        }

        /// <summary>
        /// Unknown - not documented see https://wiki.freeswitch.org/wiki/Freeswitch_IVR_Originate#Answer_confirmation
        /// </summary>
        public bool FailOnSingleReject
        {
            set
            {
                parameters["fail_on_single_reject"] = value.ToString().ToLowerInvariant();
            }
        }

        /// <summary>
        /// Unknown - not documented see https://wiki.freeswitch.org/wiki/Freeswitch_IVR_Originate#Answer_confirmation
        /// </summary>
        public bool ConfirmCancelTimeout
        {
            set
            {
                parameters["group_confirm_cancel_timeout "] = value.ToString().ToLowerInvariant();
            }
        }

        /// <summary>
        /// Privacy ID type - default is Remote-Party-ID header.
        /// See https://wiki.freeswitch.org/wiki/Variable_sip_cid_type
        /// </summary>
        public SipCallerIdType SipCallerIdType
        {
            set
            {
                parameters["sip_cid_type"] = value.ToString().ToLowerInvariant();
            }
        }

        /// <summary>
        /// Sets the origination privacy.
        /// See https://wiki.freeswitch.org/wiki/Variable_origination_privacy
        /// Can be ORed together
        /// </summary>
        public OriginationPrivacy OriginationPrivacy
        {
            set
            {
                var flags = value.GetUniqueFlags();
                var sb = StringBuilderPool.Allocate();
                foreach (var flag in flags)
                {
                    sb.Append(flag.ToString().ToUpperWithUnderscores().ToLowerInvariant());
                    sb.Append(":");
                }

                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                }

                parameters["origination_privacy"] = StringBuilderPool.ReturnAndFree(sb);
            }
        }

        /// <summary>
        /// Container for any Channel Variables to be set on the A-Leg before executing the bridge
        /// </summary>
        public IDictionary<string, string> ChannelVariables { get; private set; }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(BridgeOptions left, BridgeOptions right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(BridgeOptions left, BridgeOptions right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Converts the <seealso cref="BridgeOptions"/> instance into a command string.
        /// </summary>
        /// <returns>An originate string.</returns>
        [DebuggerStepThrough]
        public override string ToString()
        {
            var sb = StringBuilderPool.Allocate();
            sb.Append("{");

            sb.Append(parameters.ToOriginateString());

            if (sb.Length > 1)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            sb.Append("}");

            return StringBuilderPool.ReturnAndFree(sb);
        }

        /// <summary>
        /// Implementation of ISerializable.GetObjectData
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("parameters", parameters, typeof(Dictionary<string, string>));
            info.AddValue("ChannelVariables", ChannelVariables, typeof(Dictionary<string, string>));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((BridgeOptions)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (parameters.GetHashCode() * 397) ^ ChannelVariables.GetHashCode();
            }
        }

        protected bool Equals(BridgeOptions other)
        {
            return parameters.SequenceEqual(other.parameters) && ChannelVariables.SequenceEqual(other.ChannelVariables);
        }
    }
}