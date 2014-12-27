namespace NEventSocket
{
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Sockets;
    using NEventSocket.Util;

    public static class InboundSocketExtensions
    {
        /// <summary>
        /// The 'myevents' subscription allows your inbound socket connection to behave like an outbound socket connect. 
        /// It will "lock on" to the events for a particular uuid and will ignore all other events, closing the socket when
        /// the channel goes away or closing the channel when the socket disconnects and all applications have finished executing.
        /// https://wiki.freeswitch.org/wiki/Mod_event_socket#event
        /// </summary>
        /// <remarks>
        /// Once the socket connection has locked on to the events for this particular uuid it will NEVER see any events that are 
        /// not related to the channel, even if subsequent event commands are sent. If you need to monitor a specific channel/uuid 
        /// and you need watch for other events as well then it is best to use a filter.
        /// </remarks>
        public static Task<CommandReply> MyEvents(this EventSocket eventSocket, string uuid)
        {
            return eventSocket.SendCommand("myevents {0} plain".Fmt(uuid));
        }
    }
}