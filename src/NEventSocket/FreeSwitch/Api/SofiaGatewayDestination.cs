namespace NEventSocket.FreeSwitch.Api
{
    using System;

    public class SofiaGatewayDestination : IEndpoint
    {
        private readonly string gateway;

        private readonly string destination;

        public SofiaGatewayDestination(string gateway, string destination)
        {
            this.gateway = gateway;
            this.destination = destination;
            if (gateway == null) throw new ArgumentNullException("gateway");
            if (destination == null) throw new ArgumentNullException("destination");
        }

        public override string ToString()
        {
            return string.Format("sofia/gateway/{0}/{1}", this.gateway, this.destination);
        }
    }
}