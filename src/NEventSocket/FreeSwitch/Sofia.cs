namespace NEventSocket.FreeSwitch
{
    using System;

    public class Sofia
    {
         public static IEndpoint User(string profile, string user)
         {
             return new SofiaProfileUser(profile, user);
         }

        public static IEndpoint External(string destination)
        {
            return new SofiaProfileUser("external", destination);
        }

        public static IEndpoint Gateway(string gateway, string destination)
        {
            return new SofiaGatewayDestination(gateway, destination);
        }
    }

    public interface IEndpoint
    {
        string ToString();
    }

    public class SofiaProfileUser : IEndpoint
    {
        private readonly string profile;

        private readonly string user;

        public SofiaProfileUser(string profile, string user)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            if (user == null) throw new ArgumentNullException("user");
            this.profile = profile;
            this.user = user;
         }

        public override string ToString()
        {
           return string.Format("sofia/{0}/{1}", profile, user);
        }
    }

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
            return string.Format("sofia/gateway/{0}/{1}", gateway, destination);
        }
    }
}