namespace NEventSocket
{
    using System.Threading.Tasks;

    using NEventSocket.Channels;
    using NEventSocket.FreeSwitch;

    public static class OutboundSocketExtensions
    {
        public static Task<CommandReply> Linger(this OutboundSocket eventSocket)
        {
            return eventSocket.SendCommand("linger");
        }

        public static Task<CommandReply> NoLinger(this OutboundSocket eventSocket)
        {
            return eventSocket.SendCommand("nolinger");
        }

        public static async Task<IChannel> GetChannel(this OutboundSocket eventSocket)
        {
            await eventSocket.Connect();
            return new Channel(eventSocket.ChannelData, eventSocket);
        }
    }
}