// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutboundSocketExtensions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket
{
    using System.Threading.Tasks;

    using NEventSocket.Channels;
    using NEventSocket.FreeSwitch;

    /// <summary>
    /// Defines ESL operations that operate on a <seealso cref="OutboundSocket"/>.
    /// </summary>
    public static class OutboundSocketExtensions
    {
        /// <summary>
        /// Tells FreeSWITCH not to close the socket connect when a channel hangs up.
        /// Instead, it keeps the socket connection open until the last event related to the channel has been received by the socket client.
        /// IMPORTANT: If you do this, you are responsible for calling .Exit() after the call completes, otherwise the socket will not get closed down and will leak.
        /// </summary>
        /// <remarks>
        /// See https://freeswitch.org/confluence/display/FREESWITCH/mod_event_socket#mod_event_socket-linger
        /// </remarks>
        /// <param name="eventSocket">The <seealso cref="OutboundSocket"/> instance to execute on.</param>
        /// <returns>A Task of <seealso cref="CommandReply"/></returns>
        public static Task<CommandReply> Linger(this OutboundSocket eventSocket)
        {
            return eventSocket.SendCommand("linger");
        }

        /// <summary>
        /// Disable socket lingering. See <see cref="Linger"/> above
        /// </summary>
        /// <remarks>
        /// See https://freeswitch.org/confluence/display/FREESWITCH/mod_event_socket#mod_event_socket-nolinger
        /// </remarks>
        /// <param name="eventSocket">The <seealso cref="OutboundSocket"/> instance to execute on.</param>
        /// <returns>A Task of <seealso cref="CommandReply"/></returns>
        public static Task<CommandReply> NoLinger(this OutboundSocket eventSocket)
        {
            return eventSocket.SendCommand("nolinger");
        }

        /// <summary>
        /// Gets an <seealso cref="Channel"/> abstraction wrapper using the <seealso cref="OutboundSocket"/> instance.
        /// </summary>
        /// <param name="eventSocket">The <seealso cref="OutboundSocket"/> instance to use.</param>
        /// <returns>A Task of <seealso cref="Channel"/>.</returns>
        public static async Task<Channel> GetChannel(this OutboundSocket eventSocket)
        {
            await eventSocket.Connect().ConfigureAwait(false);
            return new Channel(eventSocket);
        }
    }
}