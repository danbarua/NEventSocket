// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FakeFreeSwitchSocket.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Tests.Fakes
{
    using System;
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
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
        private readonly ISubject<string> incomingMessages = new Subject<string>();

        public FakeFreeSwitchSocket(TcpClient client)
            : base(client)
        {
            Receiver.SelectMany(x => Encoding.ASCII.GetString(x))
                .AggregateUntil(
                    () => new StringBuilder(), (sb, c) => sb.Append(c), sb => sb.ToString().EndsWith("\n\n"))
                .Select(x => x.ToString().Remove(x.Length - 2, 2))
                .Subscribe(
                    x => incomingMessages.OnNext(x),
                    ex => incomingMessages.OnError(ex),
                    () => incomingMessages.OnCompleted());
        }

        public FakeFreeSwitchSocket(int port)
            : this(new TcpClient("127.0.0.1", port))
        {
        }

        public IObservable<string> MessagesReceived
        {
            get
            {
                return incomingMessages;
            }
        }

        public Task Send(string message)
        {
            return SendAsync(message + "\n\n", CancellationToken.None);
        }

        public Task SendChannelDataEvent()
        {
            var msg = TestMessages.ConnectEvent.Replace("\r\n", "\n") + "\n\n";
            return SendAsync(msg, CancellationToken.None);
        }

        public Task SendDisconnectNotice()
        {
            return SendAsync("Content-Type: text/disconnect-notice\nContent-Length: 67\n\nDisconnected, goodbye.\nSee you at ClueCon! http://www.cluecon.com/\n", CancellationToken.None);
        }

        public Task SendCommandReplyOk(string message = null)
        {
            return SendAsync("Content-Type: command/reply\nReply-Text: +OK {0}\n\n".Fmt(message), CancellationToken.None);
        }

        public Task SendCommandReplyError(string error)
        {
            return SendAsync("Content-Type: command/reply\nReply-Text: -ERR {0}\n\n".Fmt(error), CancellationToken.None);
        }

        public Task SendApiResponseOk()
        {
            return SendAsync("Content-Type: api/response\nContent-Length: 3\n\n+OK", CancellationToken.None);
        }

        public Task SendApiResponseError(string error)
        {
            return SendAsync("Content-Type: api/response\nContent-Length: {0}\n\n-ERR {1}".Fmt(5 + error.Length, error), CancellationToken.None);
        }
    }
}