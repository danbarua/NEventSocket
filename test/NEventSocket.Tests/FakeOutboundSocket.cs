namespace NEventSocket.Tests
{
    using System.Linq;
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using NEventSocket.Sockets;
    using NEventSocket.Util;

    public class FakeOutboundSocket : ObservableSocket
    {
        public FakeOutboundSocket(int port)
            : base(new TcpClient("127.0.0.1", port))
        {
        }
    }
}