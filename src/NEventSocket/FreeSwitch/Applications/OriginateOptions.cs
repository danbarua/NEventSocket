namespace NEventSocket.FreeSwitch.Applications
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents options that may be used with the Originate api command
    /// </summary>
    /// <remarks>
    /// See https://wiki.freeswitch.org/wiki/Channel_Variables#Originate_related_variables
    /// </remarks>
    public class OriginateOptions
    {
        public OriginateOptions()
        {
            ChannelVariables = new Dictionary<string, string>();
        }

        public string UUID { get; set; }

        /// <summary>
        /// Sets the origination callerid name (LEG A). 
        /// </summary>
        public string CallerIdName { get; set; }

        /// <summary>
        /// Sets the origination callerid number. (LEG A) 
        /// </summary>
        public string CallerIdNumber { get; set; }

        /// <summary>
        /// Number of retries before giving up on originating a call (default is 0).
        /// </summary>
        public int Retries { get; set; }

        /// <summary>
        /// This will set how long FreeSWITCH is going to wait between sending invite messages to the receiving gateway
        /// </summary>
        public int RetrySleepMs { get; set; }

        /// <summary>
        /// Determines how long FreeSWITCH is going to wait for a response from the invite message sent to the gateway.
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// Executes code on successful origination. Use the '{app} {arg}' format to execute in the origination thread or use '{app}::{arg}' to execute asynchronously. 
        /// Successful origination means the remote server responds, NOT when the call is actually answered. 
        /// </summary>
        public string ExecuteOnOriginate { get; set; }

        /// <summary>
        /// Whether this call should hang up after completing a bridge to another leg
        /// </summary>
        public bool HangupAfterBridge { get; set; }

        /// <summary>
        /// Whether the originate command should complete on receiving a RingReady event.
        /// Can be used to route the call to an outbound socket to recive the CHANNEL_ANSWER event.
        /// </summary>
        /// <remarks>
        /// See http://blog.godson.in/2010/12/use-of-returnringready-originate.html
        /// </remarks>
        public bool ReturnRingReady { get; set; }

        /// <summary>
        /// Whether Early Media responses should be ignored when determining whether an originate or bridge has completed successfully.
        /// </summary>
        public bool IgnoreEarlyMedia { get; set; }

        /// <summary>
        /// When bridging a call, No media mode is an SDP Passthrough feature that permits two endpoints that can see each other (no funky NAT's) to connect their media sessions directly while FreeSWITCH maintains control of the SIP signaling.
        /// </summary>
        /// <remarks>
        /// Before executing the bridge action you must set the "bypass_media" flag to true. bypass_media must only be set on the A leg of a call.
        /// https://wiki.freeswitch.org/wiki/Misc._Dialplan_Tools_bridge
        /// https://wiki.freeswitch.org/wiki/Bypass_Media
        /// </remarks>
        public bool BypassMedia { get; set; }

        /// <summary>
        /// Container for any Channel Variables to be set before executing the origination
        /// </summary>
        public IDictionary<string, string> ChannelVariables { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{");

            if (!string.IsNullOrEmpty(this.UUID)) sb.AppendFormat("origination_uuid='{0}',", this.UUID);

            if (!string.IsNullOrEmpty(this.CallerIdName)) sb.AppendFormat("origination_caller_id_name='{0}',", this.CallerIdName);
            if (!string.IsNullOrEmpty(this.CallerIdNumber)) sb.AppendFormat("origination_caller_id_number={0},", this.CallerIdNumber);

            if (!string.IsNullOrEmpty(this.ExecuteOnOriginate)) sb.AppendFormat("execute_on_originate='{0}',", this.ExecuteOnOriginate);

            if (this.Retries > 0) sb.AppendFormat("originate_retries={0},", this.Retries);

            if (this.RetrySleepMs > 0) sb.AppendFormat("originate_retry_sleep_ms={0},", this.RetrySleepMs);

            if (this.TimeoutSeconds > 0) sb.AppendFormat("originate_timeout={0},", this.TimeoutSeconds);

            if (this.ReturnRingReady) sb.Append("return_ring_ready=true,");

            if (this.IgnoreEarlyMedia) sb.Append("ignore_early_media=true,");

            if (this.BypassMedia) sb.Append("bypass_media=true,");

            sb.AppendFormat("hangup_after_bridge={0},", HangupAfterBridge.ToString().ToLower());

            foreach (var kvp in ChannelVariables)
            {
                sb.AppendFormat("{0}='{1}',", kvp.Key, kvp.Value);
            }
            
            if (sb.Length > 1)
                sb.Remove(sb.Length - 1, 1);

            sb.Append("}");

            return sb.ToString();
        }
    }
}