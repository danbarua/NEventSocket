// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InboundSocketExtensions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket
{
    using System;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Util;

    /// <summary>
    /// Defines ESL operations that operate on a <seealso cref="InboundSocket"/>.
    /// </summary>
    public static class InboundSocketExtensions
    {
        /// <summary>
        /// Issues an authentication response to an authentication challenge from FreeSwitch.
        /// </summary>
        /// <param name="eventSocket">The <seealso cref="InboundSocket"/> instance to operate on.</param>
        /// <param name="password">The password to pass to FreeSwitch.</param>
        /// <returns>A Task of <seealso cref="CommandReply"/>.</returns>
        public static Task<CommandReply> Auth(this InboundSocket eventSocket, string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            return eventSocket.SendCommand("auth {0}".Fmt(password));
        }

        /// <summary>
        /// The 'myevents' subscription allows your inbound socket connection to behave like an outbound socket connection. 
        /// </summary>
        /// <remarks> 
        /// It will "lock on" to the events for a particular uuid and will ignore all other events, closing the socket when
        /// the channel goes away or closing the channel when the socket disconnects and all applications have finished executing.
        /// https://freeswitch.org/confluence/display/FREESWITCH/mod_event_socket#mod_event_socket-SpecialCase-'myevents'
        /// Once the socket connection has locked on to the events for this particular uuid it will NEVER see any events that are 
        /// not related to the channel, even if subsequent event commands are sent. If you need to monitor a specific channel/uuid 
        /// and you need watch for other events as well then it is best to use a filter.
        /// </remarks>
        /// <param name="eventSocket">The <seealso cref="InboundSocket"/> instance to operate on.</param>
        /// <param name="uuid">The UUID of the Channel to operate on.</param>
        /// <returns>A Task of <seealso cref="CommandReply"/>.</returns>
        public static Task<CommandReply> MyEvents(this InboundSocket eventSocket, string uuid)
        {
            return eventSocket.SendCommand("myevents {0} plain".Fmt(uuid));
        }
    }
}