namespace NEventSocket.Tests.Sockets
{
    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    using NEventSocket.Sockets;
    using NEventSocket.Tests.Fakes;
    using NEventSocket.Tests.TestSupport;

    using Xunit;

    public class OutboundListenerTests
    {
        public OutboundListenerTests()
        {
            PreventThreadPoolStarvation.Init();
        }

        [Fact(Timeout = 2000)]
        public void Disposing_the_listener_completes_the_connections_observable()
        {
            using (var listener = new OutboundListener(0))
            {
                listener.Start();

                bool completed = false;

                listener.Connections.Subscribe(_ => { }, () => completed = true);

                listener.Dispose();

                Assert.True(completed);
            }
        }

        [Fact(Timeout = 2000)]
        public async Task Disposing_the_listener_disposes_any_connected_clients()
        {
            using (var listener = new OutboundListener(0))
            {
                listener.Start();

                bool connected = false;
                bool disposed = false;

                listener.Connections.Subscribe((socket) =>
                {
                    connected = true;
                    socket.Disposed += (o, e) => disposed = true;
                });

               var client = new FakeFreeSwitchSocket(listener.Port);

                await Wait.Until(() => connected);
                listener.Dispose();

                Assert.True(disposed);
            }
        }

        [Fact(Timeout = 2000)]
        public async Task Stopping_the_listener_does_not_dispose_any_connected_clients()
        {
            using (var listener = new OutboundListener(0))
            {
                listener.Start();

                bool connected = false;
                bool disposed = false;

                listener.Connections.Subscribe((socket) =>
                {
                    connected = true;
                    socket.Disposed += (o, e) => disposed = true;
                });

                var _ = new FakeFreeSwitchSocket(listener.Port);

                await Wait.Until(() => connected);

                listener.Stop();

                Assert.False(disposed);

                listener.Dispose();
                Assert.True(disposed);
            }
        }

        [Fact(Timeout = 2000)]
        public async Task a_new_connection_produces_an_outbound_socket()
        {
            using (var listener = new OutboundListener(0))
            {
                listener.Start();

                bool connected = false;

                listener.Connections.Subscribe((socket) => connected = true);

                var client = new FakeFreeSwitchSocket(listener.Port);

                await Wait.Until(() => connected);
                Assert.True(connected);
            }
        }

        [Fact(Timeout = 2000)]
        public async Task each_new_connection_produces_a_new_outbound_socket_from_the_Connections_observable()
        {
            const int NumberOfConnections = 3;

            using (var listener = new OutboundListener(0))
            {
                listener.Start();

                var connected = 0;

                listener.Connections.Subscribe((socket) => connected++);

                for (int i = 0; i < NumberOfConnections; i++)
                {
                    var client = new FakeFreeSwitchSocket(listener.Port);
                }

                await Wait.Until(() => connected == NumberOfConnections);
                Assert.Equal(NumberOfConnections, connected);
            }
        }

        [Fact(Timeout = TimeOut.TestTimeOutMs)]
        public async Task ProblematicSocket_connect_errors_should_not_cause_subsequent_connections_to_fail()
        {
            var connectionsHandled = 0;
            var observableCompleted = false;

            using (var listener = new ProblematicListener(0))
            {
                listener.Start();


                listener.Connections.Subscribe(_ => connectionsHandled++, ex => { }, () => observableCompleted = true);

                using (var client = new FakeFreeSwitchSocket(listener.Port))
                {
                    Assert.Equal(0, connectionsHandled);
                    Assert.Equal(1, ProblematicListener.Counter);
                    Assert.False(observableCompleted);
                }

                using (var client = new FakeFreeSwitchSocket(listener.Port))
                {
                    await Wait.Until(() => connectionsHandled == 1);
                    Assert.Equal(1, connectionsHandled);
                    Assert.Equal(2, ProblematicListener.Counter);
                    Assert.False(observableCompleted);
                }
            }

            Assert.True(observableCompleted);
        }
    }
}