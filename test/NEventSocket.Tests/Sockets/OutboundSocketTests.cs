namespace NEventSocket.Tests.Sockets
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Common.Logging;
    using Common.Logging.Simple;

    using Xunit;

    public class OutboundSocketTests
    {
        public OutboundSocketTests()
        {
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(
                   LogLevel.All, true, true, true, "yyyy-MM-dd hh:mm:ss");
        }

        [Fact]
        public async Task On_calling_connect_it_should_populate_the_channel_data()
        {
            using (var listener = new OutboundListener(8084))
            {
                listener.Start();
                bool wasConnected = false;

                listener.Connections.Subscribe(
                    async (socket) =>
                    {
                        await socket.Connect();
                        wasConnected = socket.ChannelData != null;
                        await socket.SendCommandAsync("say hello!");
                    });

                var fakeSocket = new FakeFreeSwitchOutbound(8084);
                fakeSocket.MessagesReceived.Subscribe(x => Console.WriteLine(x));
                await fakeSocket.SendChannelDataEvent();

                Thread.Sleep(1000);
                Assert.True(wasConnected);
            }
        }
    }
}