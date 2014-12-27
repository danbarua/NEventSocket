// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutboundSocketTests.cs" company="Business Systems (UK) Ltd">
//   Copyright © Business Systems (UK) Ltd and contributors. All rights reserved.
// </copyright>
// <summary>
//   Defines the OutboundSocketTests type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Tests.Sockets
{
    using System;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
    using NEventSocket.Logging.LogProviders;
    using NEventSocket.Tests.Fakes;
    using NEventSocket.Tests.TestSupport;

    using Xunit;

    public class OutboundSocketTests
    {
        public OutboundSocketTests()
        {
            LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider());
        }

        [Fact(Timeout = 5000)]
        public void Disposing_the_listener_completes_the_message_observables()
        {
            using (var listener = new OutboundListener(0))
            {
                listener.Start();

                bool connected = false;
                bool messagesObservableCompleted = false;
                bool eventsObservableCompleted = false;

                listener.Connections.Subscribe((connection) =>
                {
                    connected = true;
                    connection.Messages.Subscribe(_ => { }, () => messagesObservableCompleted = true);
                    connection.Events.Subscribe(_ => { }, () => eventsObservableCompleted = true);
                });

                using (var client = new FakeFreeSwitchSocket(listener.Port))
                {
                    ThreadUtils.WaitUntil(() => connected);
                    listener.Dispose(); // will dispose the socket

                    ThreadUtils.WaitUntil(() => messagesObservableCompleted);
                    ThreadUtils.WaitUntil(() => eventsObservableCompleted);

                    Assert.True(connected, "Expect a connection to have been made.");
                    Assert.True(messagesObservableCompleted, "Expect the BasicMessage observable to be completed");
                    Assert.True(eventsObservableCompleted, "Expect the EventMessage observable to be completed");
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void When_FreeSwitch_disconnects_it_completes_the_message_observables()
        {
            using (var listener = new OutboundListener(0))
            {
                listener.Start();

                bool connected = false;
                bool messagesObservableCompleted = false;
                bool eventsObservableCompleted = false;

                listener.Connections.Subscribe((connection) =>
                {
                    connected = true;
                    connection.Messages.Subscribe(_ => { }, () => messagesObservableCompleted = true);
                    connection.Events.Subscribe(_ => { }, () => eventsObservableCompleted = true);
                });

                using (var client = new FakeFreeSwitchSocket(listener.Port))
                {
                    ThreadUtils.WaitUntil(() => connected);
                    client.Dispose();

                    ThreadUtils.WaitUntil(() => messagesObservableCompleted);
                    ThreadUtils.WaitUntil(() => eventsObservableCompleted);

                    Assert.True(connected, "Expect a connection to have been made.");
                    Assert.True(messagesObservableCompleted, "Expect the BasicMessage observable to be completed");
                    Assert.True(eventsObservableCompleted, "Expect the EventMessage observable to be completed");
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void Calling_Connect_on_a_new_OutboundSocket_should_populate_the_ChannelData()
        {
            using (var listener = new OutboundListener(0))
            {
                listener.Start();
                EventMessage channelData = null;

                listener.Connections.Subscribe(
                    async (socket) =>
                    {
                        channelData = await socket.Connect();
                    });

                using (var freeSwitch = new FakeFreeSwitchSocket(listener.Port))
                {
                    freeSwitch.MessagesReceived.FirstAsync(m => m.StartsWith("connect"))
                          .Subscribe(async _ => await freeSwitch.SendChannelDataEvent());

                    ThreadUtils.WaitUntil(() => channelData != null);

                    Assert.NotNull(channelData);
                    Assert.Equal(ChannelState.Execute, channelData.ChannelState);
                    Assert.Equal("RINGING", channelData.Headers["Channel-Call-State"]);
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void can_send_api()
        {
            using (var listener = new OutboundListener(0))
            {
                listener.Start();

                bool apiRequestReceived = false;
                ApiResponse apiResponse = null;

                listener.Connections.Subscribe(
                    async (socket) =>
                        {
                            await socket.Connect();

                            apiResponse = await socket.SendApi("status");
                        });

                using (var freeSwitch = new FakeFreeSwitchSocket(listener.Port))
                {
                    freeSwitch.MessagesReceived.FirstAsync(m => m.StartsWith("connect"))
                              .Subscribe(async _ => await freeSwitch.SendChannelDataEvent());

                    freeSwitch.MessagesReceived.FirstAsync(m => m.StartsWith("api")).Subscribe(
                        async _ =>
                            {
                                apiRequestReceived = true;
                                await freeSwitch.SendApiResponseOk();
                            });

                    ThreadUtils.WaitUntil(() => apiRequestReceived);

                    Assert.True(apiRequestReceived);
                    Assert.NotNull(apiResponse);
                    Assert.True(apiResponse.Success);
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void can_send_command()
        {
            using (var listener = new OutboundListener(0))
            {
                listener.Start();

                bool commandRequestReceived = false;
                CommandReply commandReply = null;

                listener.Connections.Subscribe(
                    async (socket) =>
                        {
                            await socket.Connect();

                            commandReply = await socket.Linger();
                        });

                using (var freeSwitch = new FakeFreeSwitchSocket(listener.Port))
                {
                    freeSwitch.MessagesReceived.FirstAsync(m => m.StartsWith("connect"))
                          .Subscribe(async _ => await freeSwitch.SendChannelDataEvent());

                    freeSwitch.MessagesReceived.FirstAsync(m => m.StartsWith("linger"))
                          .Subscribe(async _ =>
                              {
                                  commandRequestReceived = true;
                                  await freeSwitch.SendCommandReplyOk();
                              });

                    ThreadUtils.WaitUntil(() => commandRequestReceived);

                    Assert.True(commandRequestReceived);
                    Assert.NotNull(commandReply);
                    Assert.True(commandReply.Success);
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void can_send_multple_commands()
        {
            using (var listener = new OutboundListener(0))
            {
                listener.Start();

                bool commandRequestReceived = false;
                CommandReply commandReply = null;

                listener.Connections.Subscribe(
                    async (socket) =>
                    {
                        await socket.Connect();

                        commandReply = await socket.Linger();

                        commandReply = await socket.NoLinger();
                    });

                using (var freeSwitch = new FakeFreeSwitchSocket(listener.Port))
                {
                    freeSwitch.MessagesReceived.FirstAsync(m => m.StartsWith("connect"))
                          .Subscribe(async _ => await freeSwitch.SendChannelDataEvent());

                    freeSwitch.MessagesReceived.FirstAsync(m => m.StartsWith("linger"))
                          .Subscribe(async _ =>
                          {
                              await freeSwitch.SendCommandReplyOk();
                          });

                    freeSwitch.MessagesReceived.FirstAsync(m => m.StartsWith("nolinger"))
                          .Subscribe(async _ =>
                          {
                              await freeSwitch.SendCommandReplyError("FAILED");
                              commandRequestReceived = true;
                          });

                    ThreadUtils.WaitUntil(() => commandRequestReceived);

                    Assert.True(commandRequestReceived);
                    Assert.NotNull(commandReply);
                    Assert.False(commandReply.Success);
                }
            }
        }
    }
}