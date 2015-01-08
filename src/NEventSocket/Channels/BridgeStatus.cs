// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BridgeStatus.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   Defines the BridgeStatus type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Channels
{
    using NEventSocket.FreeSwitch;
    using NEventSocket.Util;

    public class BridgeStatus
    {
        private bool success;

        public BridgeStatus(bool success, string responseText)
        {
            this.success = success;
            ResponseText = responseText;
        }

        public BridgeStatus(bool success, string responseText, BridgedChannel channel)
            : this(success, responseText)
        {
            Channel = channel;
        }

        public string ResponseText { get; private set; }

        public BridgedChannel Channel { get; private set; }

        public bool IsBridged
        {
            get
            {
                if (Channel != null)
                {
                    return Channel.IsAnswered;
                }

                return success;
            }
        }

        public HangupCause? HangupCause
        {
            get
            {
                if (Channel != null)
                {
                    return Channel.HangupCause;
                }

                return ResponseText.HeaderToEnumOrNull<HangupCause>();
            }
        }
    }
}