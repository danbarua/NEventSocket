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

        [Fact(Timeout = 1000)]
        public async Task On_calling_connect_it_should_populate_the_channel_data()
        {
            using (var listener = new OutboundListener(8084))
            {
                listener.Start();
                bool gotChannelData = false;

                listener.Connections.Subscribe(
                    async (socket) =>
                    {
                        await socket.Connect();
                        gotChannelData = socket.ChannelData != null;
                    });

                var fakeSocket = new FakeFreeSwitchOutbound(8084);
                await fakeSocket.SendChannelDataEvent();

                Thread.Sleep(100);
                Assert.True(gotChannelData);
            }
        }
    }
}