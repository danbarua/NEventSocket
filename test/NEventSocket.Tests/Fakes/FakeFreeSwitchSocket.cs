// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FakeFreeSwitchSocket.cs" company="Business Systems (UK) Ltd">
//   Copyright © Business Systems (UK) Ltd and contributors. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Tests.Fakes
{
    using System;
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using NEventSocket.Sockets;
    using NEventSocket.Tests.TestSupport;
    using NEventSocket.Util;

    /// <summary>
    /// A fake socket client used to simulate the FreeSwitch server on the other end
    /// of an Inbound or Outbound socket.
    /// </summary>
    public class FakeFreeSwitchSocket : ObservableSocket
    {
        public FakeFreeSwitchSocket(TcpClient client)
            : base(client)
        {
        }

        public FakeFreeSwitchSocket(int port)
            : base(new TcpClient("127.0.0.1", port))
        {
        }

        public IObservable<string> MessagesReceived
        {
            get
            {
                return
                    this.Receiver.SelectMany(x => Encoding.ASCII.GetString(x))
                        .AggregateUntil(
                            () => new StringBuilder(), (sb, c) => sb.Append(c), sb => sb.ToString().EndsWith("\n\n"))
                        .Select(x => x.ToString().Remove(x.Length - 2, 2));
            }
        }

        public Task Send(string message)
        {
            return this.SendAsync(message + "\n\n", CancellationToken.None);
        }

        public Task SendChannelDataEvent()
        {
            var msg = TestMessages.ConnectEvent.Replace("\r\n", "\n") + "\n\n";
            return this.SendAsync(msg, CancellationToken.None);
        }

        public Task SendCommandReplyOk()
        {
            return this.SendAsync("Content-Type: command/reply\nReply-Text: +OK\n\n", CancellationToken.None);
        }

        public Task SendCommandReplyError(string error)
        {
            return this.SendAsync("Content-Type: command/reply\nReply-Text: -ERR {0}\n\n".Fmt(error), CancellationToken.None);
        }
    }
}