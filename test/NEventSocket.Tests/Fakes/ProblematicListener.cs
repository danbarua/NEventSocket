// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProblematicListener.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Tests.Fakes
{
    using System;
    using System.Net.Sockets;
    using System.Threading;

    using NEventSocket.Logging;
    using NEventSocket.Sockets;

    public class ProblematicListener : ObservableListener<ProblematicSocket>
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        public static int Counter = 0;

        public ProblematicListener(int port) : base(port, ProblematicSocketFactory)
        {
        }

        public static ProblematicSocket ProblematicSocketFactory(TcpClient client)
        {
            if (Interlocked.Increment(ref Counter) % 2 != 0)
            {
                Log.Warn($"Counter is {Counter}, will throw an exception");
                throw new Exception("This will fail on every other call");
            }

            Log.Info($"Counter is {Counter}, will return a socket");
            return new ProblematicSocket(client);
        }
    }
}