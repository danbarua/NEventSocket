namespace NEventSocket.Tests.Sockets
{
    using System;
    using System.Reactive.Linq;
    using System.Security;
    using System.Threading.Tasks;

    using NEventSocket.Logging.LogProviders;
    using NEventSocket.Sockets;
    using NEventSocket.Tests.Fakes;
    using NEventSocket.Tests.TestSupport;

    using Xunit;

    public class InboundSocketTests
    {
        public InboundSocketTests()
        {
            Logging.LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider());
        }

        [Fact(Timeout = 2000)]
        public async Task sending_a_correct_password_should_connect()
        {
            using (var listener = new FakeFreeSwitchListener(8084))
            {
                listener.Start();

                bool authRequestReceived = false;
                bool exitRequestReceived = false;

                listener.Connections.Subscribe(
                    async socket =>
                    {
                        socket.MessagesReceived.Where(m => m.Equals("auth ClueCon"))
                              .Subscribe(async m =>
                              {
                                  authRequestReceived = true;
                                      await socket.SendCommandReplyOk();
                                  });

                        socket.MessagesReceived.Where(m => m.Equals("exit"))
                              .Subscribe(
                                  _ =>
                                      {
                                          exitRequestReceived = true;
                                          socket.Dispose();
                                      });
                        
                        await socket.Send("Content-Type: auth/request");
                    });

                var client = InboundSocket.Connect("127.0.0.1", 8084, "ClueCon").Result;
                ThreadUtils.WaitUntil(() => authRequestReceived);
                Assert.True(authRequestReceived);

                client.Exit();
                ThreadUtils.WaitUntil(() => exitRequestReceived);
                Assert.True(exitRequestReceived);
            }
        }

        [Fact(Timeout = 2000)]
        public async Task an_invalid_password_should_throw_a_SecurityException()
        {
            using (var listener = new FakeFreeSwitchListener(8084))
            {
                listener.Start();

                bool authRequestReceived = false;

                listener.Connections.Subscribe(
                    async socket =>
                    {
                        socket.MessagesReceived.Where(m => m.StartsWith("auth"))
                          .Subscribe(async m =>
                          {
                              authRequestReceived = true;
                              await socket.SendCommandReplyError("Invalid Password");
                          });

                        await socket.Send("Content-Type: auth/request");
                    });

                var aggregateException = Record.Exception(() => InboundSocket.Connect("127.0.0.1", 8084, "WrongPassword").Wait());
                Assert.IsType<SecurityException>(aggregateException.InnerException);
            }
        }

        [Fact(Timeout = 2000)]
        public void when_no_AuthRequest_received_it_should_throw_TimeoutException()
        {
            using (var listener = new FakeFreeSwitchListener(8084))
            {
                listener.Start();
                EventSocket.TimeOut = TimeSpan.FromMilliseconds(100);

                var aggregateException = Record.Exception(() => InboundSocket.Connect("127.0.0.1", 8084, "ClueCon").Wait());
                Assert.IsType<TimeoutException>(aggregateException.InnerException);
            }
        }
    }
}