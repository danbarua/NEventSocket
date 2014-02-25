namespace NEventSocket.FreeSwitch.Api
{
    using System;

    using NEventSocket.Util;

    public class User : IEndpoint
    {
        private readonly string user;

        public User(string user)
        {
            if (user == null) throw new ArgumentNullException("user");
            this.user = user;
        }

        public override string ToString()
        {
            return "user/{0}".Fmt(user);
        }
    }
}