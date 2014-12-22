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
    using System.Threading.Tasks;

    using NEventSocket.Logging;
    using NEventSocket.Logging.LogProviders;
    using NEventSocket.Tests.Fakes;
    using NEventSocket.Tests.TestSupport;

    using Xunit;

    public class OutboundSocketTests
    {
        public OutboundSocketTests()
        {
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());
        }

        [Fact(Timeout = 2000)]
        public void Disposing_the_listener_completes_the_messages_observable()
        {
            using (var listener = new OutboundListener(8084))
            {
                listener.Start();

                bool connected = false;
                bool messagesObservableCompleted = false;

                listener.Connections.Subscribe((connection) =>
                {
                    connected = true;
                    connection.Messages.Subscribe(_ => { }, () => messagesObservableCompleted = true);
                });

                var client = new FakeFreeSwitchOutbound(8084);
                ThreadUtils.WaitUntil(() => connected);
                listener.Dispose(); // will dispose the socket

                Assert.True(connected, "Expect a connection to have been made.");
                Assert.True(messagesObservableCompleted, "Expect the BasicMessage observable to be completed");
            }
        }


        [Fact(Timeout = 2000)]
        public void Disposing_the_listener_completes_the_events_observable()
        {
            using (var listener = new OutboundListener(8084))
            {
                listener.Start();

                bool connected = false;
                bool eventsObservableCompleted = false;

                listener.Connections.Subscribe((connection) =>
                {
                    connected = true;
                    connection.Events.Subscribe(_ => { }, () => eventsObservableCompleted = true);
                });

                var client = new FakeFreeSwitchOutbound(8084);
                ThreadUtils.WaitUntil(() => connected);
                listener.Dispose(); // will dispose the socket

                Assert.True(connected, "Expect a connection to have been made.");
                Assert.True(eventsObservableCompleted, "Expect the EventMessage observable to be completed");
            }
        }

        [Fact(Timeout = 2000)]
        public void When_FreeSwitch_disconnects_it_completes_the_messages_observable()
        {
            using (var listener = new OutboundListener(8084))
            {
                listener.Start();

                bool connected = false;
                bool messagesObservableCompleted = false;

                listener.Connections.Subscribe((connection) =>
                {
                    connected = true;
                    connection.Messages.Subscribe(_ => { }, () => messagesObservableCompleted = true);
                });
                
                var client = new FakeFreeSwitchOutbound(8084);
                ThreadUtils.WaitUntil(() => connected);
                client.Dispose();

                ThreadUtils.WaitUntil(() => messagesObservableCompleted);

                Assert.True(connected, "Expect a connection to have been made.");
                Assert.True(messagesObservableCompleted, "Expect the BasicMessage observable to be completed");
            }
        }

        [Fact(Timeout = 2000)]
        public void When_FreeSwitch_disconnects_it_completes_the_events_observable()
        {
            using (var listener = new OutboundListener(8084))
            {
                listener.Start();

                bool connected = false;
                bool eventsObservableCompleted = false;

                listener.Connections.Subscribe((connection) =>
                {
                    connected = true;
                    connection.Events.Subscribe(_ => { }, () => eventsObservableCompleted = true);
                });

                var client = new FakeFreeSwitchOutbound(8084);
                ThreadUtils.WaitUntil(() => connected);
                client.Dispose();

                ThreadUtils.WaitUntil(() => eventsObservableCompleted);

                Assert.True(connected, "Expect a connection to have been made.");
                Assert.True(eventsObservableCompleted, "Expect the EventMessage observable to be completed");
            }
        }

        [Fact(Timeout = 1000)]
        public async Task Calling_Connect_on_a_new_OutboundSocket_should_populate_the_ChannelData()
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

                ThreadUtils.WaitUntil(() => gotChannelData);
                Assert.True(gotChannelData);
            }
        }
    }
}