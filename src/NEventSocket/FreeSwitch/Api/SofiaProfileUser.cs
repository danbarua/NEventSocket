namespace NEventSocket.FreeSwitch.Api
{
    using System;

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
            return string.Format("sofia/{0}/{1}", this.profile, this.user);
        }
    }
}