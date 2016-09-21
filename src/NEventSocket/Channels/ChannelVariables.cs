namespace NEventSocket.Channels
{
    using NEventSocket.FreeSwitch;
    using NEventSocket.Util;

    /// <summary>
    /// Strongly-Typed wrapper around commonly used channel variables
    /// </summary>
    public class ChannelVariables
    {
        private readonly BasicChannel channel;

        public ChannelVariables(BasicChannel channel)
        {
            this.channel = channel;
        }

        public string this[string variableName] => this.channel.GetVariable(variableName);

        public HangupCause? BridgeHangupCause => (this["last_bridge_hangup_cause"] ?? this["originate_disposition"]).HeaderToEnumOrNull<HangupCause>();
    }
}