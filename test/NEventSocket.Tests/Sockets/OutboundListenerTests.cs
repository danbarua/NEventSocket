namespace NEventSocket.Tests.Sockets
{
    using System;
    using System.Threading;

    using Common.Logging;
    using Common.Logging.Simple;

    using Xunit;

    public class OutboundListenerTests
    {
        public OutboundListenerTests()
        {
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(
                   LogLevel.All, true, true, true, "yyyy-MM-dd hh:mm:ss");
        }

        [Fact]
        public void Disposing_the_listener_completes_the_observable()
        {

            var listener = new OutboundListener(8084);
            listener.Start();

            bool completed = false;

            listener.Connections.Subscribe(x => { }, () => completed = true);

            listener.Dispose();

            Assert.True(completed);
        }

        [Fact]
        public void New_connections_produce_an_outbound_socket()
        {
            var listener = new OutboundListener(8084);
            listener.Start();

            bool connected = false;

            listener.Connections.Subscribe((socket) => connected = true);

            var client = new FakeOutboundSocket(8084);

            Thread.Sleep(100);
            listener.Dispose();

            Assert.True(connected);
        }

        [Fact]
        public void Disposing_the_listener_disposes_the_clients()
        {
            var listener = new OutboundListener(8084);
            listener.Start();

            bool disposed = false;

            listener.Connections.Subscribe((socket) => socket.Disposed += (o, e) => disposed = true);

            var client = new FakeOutboundSocket(8084);

            Thread.Sleep(100);
            listener.Dispose();

            Assert.True(disposed);
        }
    }
}