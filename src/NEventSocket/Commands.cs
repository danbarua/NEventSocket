namespace NEventSocket
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.FreeSwitch.Api;
    using NEventSocket.FreeSwitch.Applications;
    using NEventSocket.Util;

    public static class Commands
    {
        public static Task<CommandReply> Auth(this IEventSocketCommands eventSocket, string password)
        {
            if (password == null) throw new ArgumentNullException("password");
            return eventSocket.SendCommandAsync("auth {0}".Fmt(password));
        }

        public static Task<ApiResponse> Api(this IEventSocketCommands eventSocket, string command, string arg = null)
        {
            if (command == null) throw new ArgumentNullException("command");
            return eventSocket.SendApiAsync(arg != null ? "{0} {1}".Fmt(command, arg) : command);
        }

        public static Task<EventMessage> SetChannelVariable(this IEventSocketCommands eventSocket, string uuid, string variable, object value)
        {
            return eventSocket.ExecuteAppAsync(uuid, "set", appArg: "{0}={1}".Fmt(variable, value));
        }

        /// <summary>
        /// Set Multiple Channel Variables in one go
        /// </summary>
        /// <param name="eventSocket">The EventSocket instance.</param>
        /// <param name="uuid">The Channel UUID</param>
        /// <param name="assignments">Array of assignments in the form "foo=value", "bar=value".</param>
        /// <returns>A Task[EventMessage] representing the CHANNEL_EXECUTE_COMPLETE event.</returns>
        public static Task<EventMessage> SetMultipleChannelVariables(
            this IEventSocketCommands eventSocket, string uuid, params string[] assignments)
        {
            return eventSocket.ExecuteAppAsync(uuid, "multiset", "^^:" + assignments.Aggregate(string.Empty, (a, s) => a += s.Replace(":", @"\:") + ":", s => s));
        }

        public static async Task<PlayResult> Play(this IEventSocketCommands eventSocket, string uuid, string file, PlayOptions options = null)
        {
            //todo: implement options for playback eg a-leg, b-leg, both, using uuid_displace
            return new PlayResult(await eventSocket.ExecuteAppAsync(uuid, "playback", appArg: file));
        }

        public static async Task<PlayGetDigitsResult> PlayGetDigits(
            this IEventSocketCommands eventSocket, string uuid, PlayGetDigitsOptions options)
        {
            return new PlayGetDigitsResult(
                await eventSocket.ExecuteAppAsync(uuid, "play_and_get_digits", options.ToString()), options.ChannelVariableName);
        }

        public static Task<EventMessage> StartDtmf(this IEventSocketCommands eventSocket, string uuid)
        {
            return eventSocket.ExecuteAppAsync(uuid, "spandsp_start_dtmf");
        }

        public static Task<CommandReply> Linger(this IEventSocketCommands eventSocket)
        {
            return eventSocket.SendCommandAsync("linger");
        }

        public static Task<CommandReply> NoLinger(this IEventSocketCommands eventSocket)
        {
            return eventSocket.SendCommandAsync("nolinger");
        }

        public static Task<CommandReply> MyEvents(this IEventSocketCommands eventSocket, string uuid)
        {
            return eventSocket.SendCommandAsync("myevents {0} plain".Fmt(uuid));
        }

        public static Task<CommandReply> DivertEventsOn(this IEventSocketCommands eventSocket)
        {
            return eventSocket.SendCommandAsync("divert_events on");
        }

        public static Task<CommandReply> DivertEventsOff(this IEventSocketCommands eventSocket)
        {
            return eventSocket.SendCommandAsync("divert_events off");
        }

        public static Task<CommandReply> Filter(this IEventSocketCommands eventSocket, EventType eventType)
        {
            return eventSocket.Filter(eventType.ToString());
        }

        public static Task<CommandReply> Filter(this IEventSocketCommands eventSocket, string eventName)
        {
            return eventSocket.Filter("Event-Name", eventName);
        }

        public static Task<CommandReply> Filter(this IEventSocketCommands eventSocket, string header, string value)
        {
            if (header == null) throw new ArgumentNullException("header");
            if (value == null) throw new ArgumentNullException("value");
            return eventSocket.SendCommandAsync("filter {0} {1}".Fmt(header, value));
        }

        public static Task<CommandReply> FilterDelete(this IEventSocketCommands eventSocket, EventType eventType)
        {
            return eventSocket.FilterDelete("Event-Name", eventType.ToString());
        }

        public static Task<CommandReply> FilterDelete(this IEventSocketCommands eventSocket, string header)
        {
            if (header == null) throw new ArgumentNullException("header");
            return eventSocket.SendCommandAsync("filter delete {0}".Fmt(header));
        }

        public static Task<CommandReply> FilterDelete(this IEventSocketCommands eventSocket, string header, string value)
        {
            if (header == null) throw new ArgumentNullException("header");
            if (value == null) throw new ArgumentNullException("value");
            return eventSocket.SendCommandAsync("filter delete {0} {1}".Fmt(header, value));
        }

        public static Task<CommandReply> SendEvent(this IEventSocketCommands eventSocket, string eventName, IDictionary<string, string> headers, string body = null)
        {
            throw new NotImplementedException("Todo");

            if (eventName == null) throw new ArgumentNullException("eventName");
            if (headers == null) throw new ArgumentNullException("headers");
            return eventSocket.SendCommandAsync("sendevent {0}\n{1}".Fmt(eventName, body));
        }

        public static Task<CommandReply> SendMessage(this IEventSocketCommands eventSocket, string uuid, string message)
        {
            if (uuid == null) throw new ArgumentNullException("uuid");
            if (message == null) throw new ArgumentNullException("message");
            return eventSocket.SendCommandAsync("sendmsg {0}\n{1}".Fmt(uuid, message));
        }

        public static Task<CommandReply> Hangup(this IEventSocketCommands eventSocket, string uuid, string hangupCause)
        {
            if (uuid == null) throw new ArgumentNullException("uuid");
            if (hangupCause == null) throw new ArgumentNullException("hangupCause");
            return eventSocket.SendMessage(uuid, "call-command: hangup\nhangup-cause: " + hangupCause);
        }

        public static void Exit(this IEventSocketCommands eventSocket)
        {
            eventSocket.SendCommandAsync("exit"); //will disconnect, no reply will arrive
        }

        public static Task<CommandReply> FsLog(this IEventSocketCommands eventSocket, string logLevel)
        {
            return eventSocket.SendCommandAsync("log " + logLevel);
        }

        public static Task<CommandReply> NoLog(this IEventSocketCommands eventSocket)
        {
            return eventSocket.SendCommandAsync("nolog");
        }

        public static Task<CommandReply> NoEvents(this IEventSocketCommands eventSocket)
        {
            return eventSocket.SendCommandAsync("noevents");
        }
    }
}