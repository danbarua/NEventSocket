// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FakeFreeSwitchOutbound.cs" company="Business Systems (UK) Ltd">
//   Copyright © Business Systems (UK) Ltd and contributors. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Tests
{
    using System;
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using NEventSocket.Sockets;

    public class FakeFreeSwitchOutbound : ObservableSocket
    {
        public FakeFreeSwitchOutbound(int port)
            : base(new TcpClient("127.0.0.1", port))
        {
        }

        public IObservable<string> MessagesReceived
        {
            get
            {
                return Receiver.Select(x => Encoding.ASCII.GetString(x).Remove(x.Length - 2, 2));
            }
        }

        public async Task SendChannelDataEvent()
        {
            var msg = "Content-Type: command/reply\nReply-Text: +OK\n" + TestMessages.ConnectEvent.Replace("\r\n", "\n")
                      + "\n\n";
            await this.SendAsync(Encoding.ASCII.GetBytes(msg));
        }
    }
}