namespace NEventSocket.Tests.Sockets
{
    using System;
    using System.Reactive.Linq;
    using System.Security;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
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

        [Fact(Timeout = 10000)]
        public async Task sending_a_correct_password_should_connect()
        {
            using (var listener = new FakeFreeSwitchListener(0))
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
                                  async _ =>
                                      {
                                          exitRequestReceived = true;
                                          await socket.SendCommandReplyOk();
                                      });
                        
                        await socket.Send("Content-Type: auth/request");
                    });

                using (var client = await InboundSocket.Connect("127.0.0.1", listener.Port, "ClueCon"))
                {
                    Assert.True(authRequestReceived);

                    client.Exit();

                    ThreadUtils.WaitUntil(() => exitRequestReceived);
                    Assert.True(exitRequestReceived);
                }
            }
        }

        [Fact(Timeout = 10000)]
        public void an_invalid_password_should_throw_a_SecurityException()
        {
            using (var listener = new FakeFreeSwitchListener(0))
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

                var aggregateException = Record.Exception(() => InboundSocket.Connect("127.0.0.1", listener.Port, "WrongPassword").Wait());
                Assert.True(authRequestReceived);
                Assert.IsType<SecurityException>(aggregateException.InnerException);
            }
        }

        [Fact(Timeout = 10000)]
        public void when_no_AuthRequest_received_it_should_throw_TimeoutException()
        {
            using (var listener = new FakeFreeSwitchListener(0))
            {
                listener.Start();

                var aggregateException = Record.Exception(() => InboundSocket.Connect("127.0.0.1", listener.Port, "ClueCon", TimeSpan.FromMilliseconds(100)).Wait());
                Assert.IsType<TimeoutException>(aggregateException.InnerException);
            }
        }

        [Fact(Timeout = 5000, Skip = "Removing timeouts")]
        public void when_no_response_to_auth_received_it_should_throw_TimeoutException()
        {
            using (var listener = new FakeFreeSwitchListener(0))
            {
                listener.Start();

                listener.Connections.Subscribe(
                    async socket =>
                    {
                        await socket.Send("Content-Type: auth/request");
                    });

                var aggregateException = Record.Exception(() => InboundSocket.Connect("127.0.0.1", listener.Port, "ClueCon", TimeSpan.FromMilliseconds(100)).Wait());
                Assert.IsType<TimeoutException>(aggregateException.InnerException);
            }
        }

        [Fact(Timeout = 10000)]
        public async Task can_send_api()
        {
            using (var listener = new FakeFreeSwitchListener(0))
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

                using (var client = await InboundSocket.Connect("127.0.0.1", listener.Port, "ClueCon"))
                {
                    var result = await client.SendApi("status");
                    Assert.True(result.Success);
                }
            }
        }

        [Fact(Timeout = 5000, Skip = "Removing timeouts")]
        public void when_no_api_response_received_it_should_throw_a_TimeOutException()
        {
            using (var listener = new FakeFreeSwitchListener(0))
            {
                listener.Start();

                bool apiRequestReceived = false;

                listener.Connections.Subscribe(
                    async socket =>
                    {
                        socket.MessagesReceived.FirstAsync(m => m.Equals("auth ClueCon"))
                              .Subscribe(async m =>
                              {
                                  await socket.SendCommandReplyOk();
                              });

                        socket.MessagesReceived.FirstAsync(m => m.StartsWith("api"))
                              .Subscribe(async m =>
                                  {
                                      apiRequestReceived = true;
                                      await Task.Delay(1000);
                                      await socket.SendApiResponseError("error");
                                  });

                        await socket.Send("Content-Type: auth/request");
                    });

                using (var client = InboundSocket.Connect("127.0.0.1", listener.Port, "ClueCon", TimeSpan.FromMilliseconds(100)).Result)
                {
                    client.ResponseTimeOut = TimeSpan.FromMilliseconds(100);
                    var ex = Record.Exception(() => client.SendApi("status").Wait());

                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex.InnerException);
                    Assert.True(apiRequestReceived);

                    client.Exit();
                }
            }
        }

        [Fact(Timeout = 10000)]
        public async Task can_send_command()
        {
            using (var listener = new FakeFreeSwitchListener(0))
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

                using (var client = await InboundSocket.Connect("127.0.0.1", listener.Port, "ClueCon"))
                {
                    var result = await client.SendCommand("test");
                    Assert.True(result.Success);
                }
            }
        }

        [Fact(Timeout = 10000)]
        public async Task can_send_multiple_commands()
        {
            using (var listener = new FakeFreeSwitchListener(0))
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

                       socket.MessagesReceived.FirstAsync(m => m.StartsWith("event"))
                              .Subscribe(
                                  async m =>
                                  {
                                      await socket.SendCommandReplyError("FAILED");
                                  });

                        await socket.Send("Content-Type: auth/request");
                    });

                using (var client = await InboundSocket.Connect("127.0.0.1", listener.Port, "ClueCon"))
                {
                    var result = await client.SendCommand("test");
                    Assert.True(result.Success);

                    result = await client.SendCommand("event CHANNEL_ANSWER");
                    Assert.False(result.Success);
                }
            }
        }

        [Fact(Timeout = 5000, Skip = "Removing timeouts")]
        public void when_no_command_reply_received_it_should_throw_a_TimeOutException()
        {
            using (var listener = new FakeFreeSwitchListener(0))
            {
                listener.Start();

                bool commandRequestReceived = false;

                listener.Connections.Subscribe(
                    async socket =>
                    {
                        socket.MessagesReceived.FirstAsync(m => m.Equals("auth ClueCon"))
                              .Subscribe(async m =>
                              {
                                  await socket.SendCommandReplyOk();
                              });

                        socket.MessagesReceived.FirstAsync(m => m.StartsWith("test"))
                              .Subscribe(async m =>
                              {
                                  commandRequestReceived = true;
                                  await Task.Delay(1000);
                                  await socket.SendCommandReplyError("error");
                              });

                        await socket.Send("Content-Type: auth/request");
                    });

                using (var client = InboundSocket.Connect("127.0.0.1", listener.Port, "ClueCon", TimeSpan.FromMilliseconds(100)).Result)
                {
                    var ex = Record.Exception(() => client.SendCommand("test").Wait());
                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex.InnerException);
                    Assert.True(commandRequestReceived);
                }
            }
        }
    }
}