// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Endpoint.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using System.Diagnostics;

    public static class Endpoint
    {
        [DebuggerStepThrough]
        public static string User(string user)
        {
            return string.Format("user/{0}", user);
        }

        public static class Sofia
        {
            [DebuggerStepThrough]
            public static string Extension(string profile, string extension)
            {
                return string.Format("sofia/{0}/{1}", profile, extension);
            }

            [DebuggerStepThrough]
            public static string Gateway(string gateway, string destination)
            {
                return string.Format("sofia/{0}/{1}", gateway, destination);
            }
        }
    }
}