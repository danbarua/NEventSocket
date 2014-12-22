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
        private const int Port = 8084;

        public InboundSocketTests()
        {
            Logging.LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider());
        }

        [Fact(Timeout = 10000)]
        public async Task sending_a_correct_password_should_connect()
        {
            using (var listener = new FakeFreeSwitchListener(Port))
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

                using (var client = await InboundSocket.Connect("127.0.0.1", Port, "ClueCon"))
                {

                    Assert.True(authRequestReceived);

                    client.Exit();
                    ThreadUtils.WaitUntil(() => exitRequestReceived);
                    Assert.True(exitRequestReceived);
                }
            }
        }

        [Fact(Timeout = 10000)]
        public async Task an_invalid_password_should_throw_a_SecurityException()
        {
            using (var listener = new FakeFreeSwitchListener(Port))
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

                var aggregateException = Record.Exception(() => InboundSocket.Connect("127.0.0.1", Port, "WrongPassword").Wait());
                Assert.IsType<SecurityException>(aggregateException.InnerException);
            }
        }

        [Fact(Timeout = 10000)]
        public void when_no_AuthRequest_received_it_should_throw_TimeoutException()
        {
            using (var listener = new FakeFreeSwitchListener(Port))
            {
                listener.Start();

                var aggregateException = Record.Exception(() => InboundSocket.Connect("127.0.0.1", Port, "ClueCon", TimeSpan.FromMilliseconds(100)).Wait());
                Assert.IsType<TimeoutException>(aggregateException.InnerException);
            }
        }

        [Fact(Timeout = 10000)]
        public async Task can_send_api()
        {
            using (var listener = new FakeFreeSwitchListener(Port))
            {
                listener.Start();

                listener.Connections.Subscribe(
                    async socket =>
                    {
                        socket.MessagesReceived.Where(m => m.Equals("auth ClueCon"))
                              .Take(1)
                              .Subscribe(async m =>
                              {
                                  await socket.SendCommandReplyOk();
                              });

                        socket.MessagesReceived.Where(m => m.StartsWith("api"))
                              .Take(1)
                              .Subscribe(
                                  async m =>
                                  {
                                      await socket.SendApiResponseOk();
                                  });

                        await socket.Send("Content-Type: auth/request");
                    });

                using (var client = await InboundSocket.Connect("127.0.0.1", Port, "ClueCon"))
                {
                    var result = await client.Api("status");
                    Assert.True(result.Success);

                    client.Exit();
                }
            }
        }

        [Fact(Timeout = 10000)]
        public async Task when_no_api_response_received_it_should_throw_a_TimeOutException()
        {
            using (var listener = new FakeFreeSwitchListener(Port))
            {
                listener.Start();

                listener.Connections.Subscribe(
                    async socket =>
                    {
                        socket.MessagesReceived.Where(m => m.Equals("auth ClueCon"))
                              .Take(1)
                              .Subscribe(async m =>
                              {
                                  await socket.SendCommandReplyOk();
                              });

                        await socket.Send("Content-Type: auth/request");
                    });

                using (var client = await InboundSocket.Connect("127.0.0.1", Port, "ClueCon"))
                {
                    client.TimeOut = TimeSpan.FromSeconds(1);
                    var ex = Record.Exception(() => client.Api("status").Wait());
                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex.InnerException);

                    client.Exit();
                }
            }
        }

        [Fact(Timeout = 10000)]
        public async Task when_no_subsequent_api_response_received_it_should_throw_a_TimeOutException()
        {
            using (var listener = new FakeFreeSwitchListener(Port))
            {
                listener.Start();

                listener.Connections.Subscribe(
                    async socket =>
                    {
                        socket.MessagesReceived.Where(m => m.Equals("auth ClueCon"))
                              .Take(1)
                              .Subscribe(async m =>
                              {
                                  await socket.SendCommandReplyOk();
                              });

                        socket.MessagesReceived.Where(m => m.StartsWith("api first"))
                              .Take(1)
                              .Subscribe(
                                  async m =>
                                  {
                                      await socket.SendApiResponseOk();
                                  });

                        await socket.Send("Content-Type: auth/request");
                    });

                using (var client = await InboundSocket.Connect("127.0.0.1", Port, "ClueCon"))
                {
                    client.Messages.Subscribe(m => Console.WriteLine("TEST: " + m));

                    var result = await client.Api("first");
                    Assert.True(result.Success);

                    client.TimeOut = TimeSpan.FromMilliseconds(500);
                    var ex = Record.Exception(() => client.Api("second").Wait());
                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex.InnerException);
                }
            }
        }

        [Fact(Timeout = 10000)]
        public async Task can_send_command()
        {
            using (var listener = new FakeFreeSwitchListener(Port))
            {
                listener.Start();

                listener.Connections.Subscribe(
                    async socket =>
                    {
                        socket.MessagesReceived.Where(m => m.Equals("auth ClueCon"))
                              .Take(1)
                              .Subscribe(async m =>
                              {
                                  await socket.SendCommandReplyOk();
                              });

                        socket.MessagesReceived.Where(m => m.StartsWith("test"))
                              .Take(1)
                              .Subscribe(
                                  async m =>
                                  {
                                      await socket.SendCommandReplyOk();
                                  });

                        await socket.Send("Content-Type: auth/request");
                    });

                using (var client = await InboundSocket.Connect("127.0.0.1", Port, "ClueCon"))
                {
                    var result = await client.SendCommand("test");
                    Assert.True(result.Success);

                    client.Exit();
                }
            }
        }

        [Fact(Timeout = 10000)]
        public async Task when_no_command_reply_received_it_should_throw_a_TimeOutException()
        {
            using (var listener = new FakeFreeSwitchListener(Port))
            {
                listener.Start();

                listener.Connections.Subscribe(
                    async socket =>
                    {
                        socket.MessagesReceived.Where(m => m.Equals("auth ClueCon"))
                              .Take(1)
                              .Subscribe(async m =>
                              {
                                  await socket.SendCommandReplyOk();
                              });

                        await socket.Send("Content-Type: auth/request");
                    });

                using (var client = await InboundSocket.Connect("127.0.0.1", Port, "ClueCon"))
                {
                    client.Messages.Subscribe(m => Console.WriteLine("TEST: " + m));
                    client.TimeOut = TimeSpan.FromSeconds(1);
                    var ex = Record.Exception(() => client.SendCommand("test").Result);
                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex.InnerException);

                    client.Exit();
                }
            }
        }

        [Fact(Timeout = 10000)]
        public async Task when_no_subsequent_command_reply_received_it_should_throw_a_TimeOutException()
        {
            using (var listener = new FakeFreeSwitchListener(Port))
            {
                listener.Start();

                listener.Connections.Subscribe(
                    async socket =>
                    {
                        socket.MessagesReceived.Where(m => m.Equals("auth ClueCon"))
                              .Take(1)
                              .Subscribe(async m =>
                              {
                                  await socket.SendCommandReplyOk();
                              });

                        socket.MessagesReceived.Where(m => m.Equals("test first"))
                              .Take(1)
                              .Subscribe(async m =>
                              {
                                  await socket.SendCommandReplyOk();
                              });

                        await socket.Send("Content-Type: auth/request");
                    });

                using (var client = await InboundSocket.Connect("127.0.0.1", Port, "ClueCon"))
                {
                    client.Messages.Subscribe(m => Console.WriteLine("TEST: " + m));

                    var response = await client.SendCommand("test first");
                    Assert.True(response.Success);

                    client.TimeOut = TimeSpan.FromSeconds(1);
                    var ex = Record.Exception(() => client.SendCommand("test second").Result);
                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex.InnerException);

                    client.Exit();
                }
            }
        }
    }
}