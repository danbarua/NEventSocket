namespace NEventSocket.Tests.Sockets
{
    using System;
    using System.Threading;

    using Common.Logging;
    using Common.Logging.Simple;

    using Xunit;

    public class ObservableSocketTests
    { 
        public ObservableSocketTests()
        {
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(
                   LogLevel.All, true, true, true, "yyyy-MM-dd hh:mm:ss");
        }

        [Fact]
        public void Disposing_the_socket_completes_the_observable()
        {
            var listener = new OutboundListener(8021);

            bool completed = false;
            listener.Connections.Subscribe(
                connection => connection.MessagesReceived.Subscribe(x => { }, () => completed = true));
            
            listener.Start();

            var socket = new FakeOutboundSocket(8021);
            socket.Disconnect();
            listener.Dispose();

            Thread.Sleep(100);

            Assert.True(completed);
        }
    }
}