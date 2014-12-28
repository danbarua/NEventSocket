// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BridgeResult.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    public class BridgeResult : ApplicationResult
    {
        public BridgeResult(EventMessage eventMessage) : base(eventMessage)
        {
            this.Success = eventMessage.Headers.ContainsKey(HeaderNames.OtherLegUniqueId);
            this.ResponseText = eventMessage.GetVariable("DIALSTATUS");

            if (this.Success)
            {
                this.BridgeUUID = eventMessage.Headers[HeaderNames.OtherLegUniqueId];
            }
        }

        public string BridgeUUID { get; set; }
    }
}