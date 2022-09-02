// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandExtensions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   Defines ESL operations that operate on either an <seealso cref="InboundSocket" /> or an <seealso cref="OutboundSocket" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Sockets;
    using NEventSocket.Util;
    using NEventSocket.Util.ObjectPooling;

    /// <summary>
    /// Defines ESL operations that operate on either an <seealso cref="InboundSocket"/> or an <seealso cref="OutboundSocket"/>.
    /// </summary>
    public static class CommandExtensions
    {
        public static Task<CommandReply> DivertEventsOn(this EventSocket eventSocket)
        {
            return eventSocket.SendCommand("divert_events on");
        }

        public static Task<CommandReply> DivertEventsOff(this EventSocket eventSocket)
        {
            return eventSocket.SendCommand("divert_events off");
        }

        public static Task<CommandReply> Filter(this EventSocket eventSocket, EventName eventName)
        {
            return eventSocket.Filter(eventName.ToString().ToUpperWithUnderscores());
        }

        public static Task<CommandReply> Filter(this EventSocket eventSocket, string eventName)
        {
            return eventSocket.Filter("Event-Name", eventName);
        }

        public static Task<CommandReply> Filter(this EventSocket eventSocket, string header, string value)
        {
            if (header == null)
            {
                throw new ArgumentNullException("header");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return eventSocket.SendCommand("filter {0} {1}".Fmt(header, value));
        }

        public static Task<CommandReply> FilterDelete(this EventSocket eventSocket, EventName eventName)
        {
            return eventSocket.FilterDelete("Event-Name", eventName.ToString().ToUpperWithUnderscores());
        }

        public static Task<CommandReply> FilterDelete(this EventSocket eventSocket, string header)
        {
            if (header == null)
            {
                throw new ArgumentNullException("header");
            }

            return eventSocket.SendCommand("filter delete {0}".Fmt(header));
        }

        public static Task<CommandReply> FilterDelete(this EventSocket eventSocket, string header, string value)
        {
            if (header == null)
            {
                throw new ArgumentNullException("header");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return eventSocket.SendCommand("filter delete {0} {1}".Fmt(header, value));
        }

        public static Task<CommandReply> SendEvent(
            this EventSocket eventSocket, EventName eventName, IDictionary<string, string> headers = null)
        {
            return SendEvent(eventSocket, eventName.ToString().ToUpperWithUnderscores(), headers);
        }

        public static Task<CommandReply> SendEvent(
            this EventSocket eventSocket, string eventName, IDictionary<string, string> headers = null)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException("eventName");
            }

            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }

            var headersString = headers.Aggregate(
                StringBuilderPool.Allocate(), 
                (sb, kvp) =>
                    {
                        sb.AppendFormat("{0}: {1}", kvp.Key, kvp.Value);
                        sb.Append("\n");
                        return sb;
                    }, 
                StringBuilderPool.ReturnAndFree);

            return eventSocket.SendCommand("sendevent {0}\n{1}".Fmt(eventName, headersString));
        }

        public static Task<CommandReply> Hangup(
            this EventSocket eventSocket, string uuid, HangupCause hangupCause = HangupCause.NormalClearing)
        {
            if (uuid == null)
            {
                throw new ArgumentNullException("uuid");
            }

            return
                eventSocket.SendCommand(
                    "sendmsg {0}\ncall-command: hangup\nhangup-cause: {1}".Fmt(uuid, hangupCause.ToString().ToUpperWithUnderscores()));
        }

        public static Task<CommandReply> FsLog(this EventSocket eventSocket, string logLevel)
        {
            return eventSocket.SendCommand("log " + logLevel);
        }

        public static Task<CommandReply> NoLog(this EventSocket eventSocket)
        {
            return eventSocket.SendCommand("nolog");
        }

        public static Task<CommandReply> NoEvents(this EventSocket eventSocket)
        {
            return eventSocket.SendCommand("noevents");
        }
    }
}