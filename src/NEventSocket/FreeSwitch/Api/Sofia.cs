namespace NEventSocket.FreeSwitch.Api
{
    public class Sofia
    {
         public static IEndpoint Extension(string profile, string extension)
         {
             return new SofiaProfileUser(profile, extension);
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
}