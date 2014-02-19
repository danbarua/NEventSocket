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
            var listener = new OutboundListener(8084);
            listener.Start();

            bool connected = false;
            bool completed = false;

            listener.Connections.Subscribe((connection) =>
                { 
                    connected = true;
                    connection.MessagesReceived.Subscribe(_ => { }, () => completed = true);
                });

            var client = new FakeOutboundSocket(8084);

            Thread.Sleep(100);
            listener.Dispose(); // will dispose the socket

            Assert.True(connected, "Expect a connection to have been made.");
            Assert.True(completed, "Expect the observable to be completed");
        }
    }
}