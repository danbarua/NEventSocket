namespace NEventSocket.Tests.Sockets
{
    using System;

    using NEventSocket.Logging;
    using NEventSocket.Logging.LogProviders;
    using NEventSocket.Tests.Fakes;
    using NEventSocket.Tests.TestSupport;

    using Xunit;

    public class OutboundListenerTests
    {
        public OutboundListenerTests()
        {
            LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider());
        }

        [Fact(Timeout = 2000)]
        public void Disposing_the_listener_completes_the_connections_observable()
        {
            using (var listener = new OutboundListener(8084))
            {
                listener.Start();

                bool completed = false;

                listener.Connections.Subscribe(_ => { }, () => completed = true);

                listener.Dispose();

                Assert.True(completed);
            }
        }

        [Fact(Timeout = 2000)]
        public void Disposing_the_listener_disposes_any_connected_clients()
        {
            using (var listener = new OutboundListener(8084))
            {
                listener.Start();

                bool connected = false;
                bool disposed = false;

                listener.Connections.Subscribe((socket) =>
                {
                    connected = true;
                    socket.Disposed += (o, e) => disposed = true;
                });

                var client = new FakeFreeSwitchSocket(8084);

                ThreadUtils.WaitUntil(() => connected);
                listener.Dispose();

                Assert.True(disposed);
            }
        }

        [Fact(Timeout = 2000)]
        public void a_new_connection_produces_an_outbound_socket()
        {
            using (var listener = new OutboundListener(8084))
            {
                listener.Start();

                bool connected = false;

                listener.Connections.Subscribe((socket) => connected = true);

                var client = new FakeFreeSwitchSocket(8084);

                ThreadUtils.WaitUntil(() => connected);
                Assert.True(connected);
            }
        }

        [Fact(Timeout = 2000)]
        public void each_new_connection_produces_a_new_outbound_socket_from_the_Connections_observable()
        {
            const int NumberOfConnections = 3;

            using (var listener = new OutboundListener(8084))
            {
                listener.Start();

                var connected = 0;

                listener.Connections.Subscribe((socket) => connected++);

                for (int i = 0; i < NumberOfConnections; i++)
                {
                    var client = new FakeFreeSwitchSocket(8084);
                }

                ThreadUtils.WaitUntil(() => connected == NumberOfConnections);
                Assert.Equal(NumberOfConnections, connected);
            }
        }
    }
}