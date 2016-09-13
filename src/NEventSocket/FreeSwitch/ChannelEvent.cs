namespace NEventSocket.FreeSwitch
{
    using NEventSocket.Util;

    public class ChannelEvent : EventMessage
    {
        internal ChannelEvent(BasicMessage basicMessage) : base(basicMessage)
        {
        }

        /// <summary>
        /// Gets the Unique Id for the Channel.
        /// </summary>
        public string UUID
        {
            get
            {
                return Headers.GetValueOrDefault(HeaderNames.UniqueId);
            }
        }

        public string OtherLegUUID
        {
            get
            {
                return Headers.GetValueOrDefault(HeaderNames.OtherLegUniqueId);
            }
        }

        public string ChannelCallUUID
        {
            get
            {
                return Headers.GetValueOrDefault(HeaderNames.ChannelCallUniqueId);
            }
        }

        /// <summary>
        /// Gets the <see cref="ChannelState"/> of the Channel.
        /// </summary>
        public ChannelState? ChannelState
        {
            get
            {
                // channel state = "CS_NEW"
                // strip first 3 chars and then parse it to ChannelState enum.
                var channelState = Headers.GetValueOrDefault(HeaderNames.ChannelState);

                if (channelState == null)
                {
                    return null;
                }

                channelState = channelState.Substring(3, channelState.Length - 3);
                return channelState.HeaderToEnum<ChannelState>();
            }
        }

        /// <summary>
        /// Gets the <see cref="AnswerState"/> of the Channel.
        /// </summary>
        public AnswerState? AnswerState
        {
            get
            {
                return Headers.GetValueOrDefault(HeaderNames.AnswerState).HeaderToEnumOrNull<AnswerState>();
            }
        }

        /// <summary>
        /// Gets the <see cref="HangupCause"/> of the Channel, if it has been hung up otherwise null.
        /// </summary>
        public HangupCause? HangupCause
        {
            get
            {
                return Headers.GetValueOrDefault(HeaderNames.HangupCause).HeaderToEnumOrNull<HangupCause>();
            }
        }
    }
}