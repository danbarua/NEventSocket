namespace NEventSocket
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
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
            return eventSocket.SendCommand("auth {0}".Fmt(password));
        }

        public static Task<ApiResponse> Api(this IEventSocketCommands eventSocket, string command, string arg = null)
        {
            if (command == null) throw new ArgumentNullException("command");
            return eventSocket.Api(arg != null ? "{0} {1}".Fmt(command, arg) : command);
        }

        public static Task<ApiResponse> SetChannelVariable(this IEventSocketCommands eventSocket, string uuid, string variable, object value)
        {
            return eventSocket.Api("uuid_setvar {0} {1} {2}".Fmt(uuid, variable, value));
        }

        /// <summary>
        /// Set Multiple Channel Variables in one go
        /// </summary>
        /// <param name="eventSocket">The EventSocket instance.</param>
        /// <param name="uuid">The Channel UUID</param>
        /// <param name="assignments">Array of assignments in the form "foo=value", "bar=value".</param>
        /// <returns>A Task[EventMessage] representing the CHANNEL_EXECUTE_COMPLETE event.</returns>
        public static Task<ApiResponse> SetMultipleChannelVariables(
            this IEventSocketCommands eventSocket, string uuid, params string[] assignments)
        {
            return
                eventSocket.Api(
                    "uuid_setvar_multi {0} {1}".Fmt(
                        uuid, assignments.Aggregate(string.Empty, (a, s) => a += s + ";", s => s)));
        }

        /// <summary>
        /// Plays the given file to the specified channel.
        /// </summary>
        /// <param name="eventSocket">The EventSocket instance.</param>
        /// <param name="uuid">The Channel UUID.</param>
        /// <param name="file">The Path to the file to be played. Note: use forward slashes for path separators.</param>
        /// <param name="options">Options to customize playback.</param>
        /// <returns>A PlayResult.</returns>
        /// <exception cref="FileNotFoundException">Throws FileNotFoundException if FreeSwitch is unable to play the file.</exception>
        public static async Task<PlayResult> Play(this IEventSocketCommands eventSocket, string uuid, string file, PlayOptions options = null)
        {
            //todo: implement options for playback eg a-leg, b-leg, both, using uuid_displace
            if (options == null) options = new PlayOptions();
            return new PlayResult(await eventSocket.Execute(uuid, "playback", applicationArguments: file, loops: options.Loops));
        }

        public static async Task<PlayGetDigitsResult> PlayGetDigits(
            this IEventSocketCommands eventSocket, string uuid, PlayGetDigitsOptions options)
        {
            return new PlayGetDigitsResult(
                await eventSocket.Execute(uuid, "play_and_get_digits", options.ToString()), options.ChannelVariableName);
        }

        public static Task<EventMessage> Say(this IEventSocketCommands eventSocket, string uuid, SayOptions options)
        {
            return eventSocket.Execute(uuid, "say", options.ToString());
        }

        public static Task<EventMessage> StartDtmf(this IEventSocketCommands eventSocket, string uuid)
        {
            return eventSocket.Execute(uuid, "spandsp_start_dtmf");
        }

        public static Task<EventMessage> Stoptmf(this IEventSocketCommands eventSocket, string uuid)
        {
            return eventSocket.Execute(uuid, "spandsp_stop_dtmf");
        }

        public static Task<CommandReply> Linger(this IEventSocketCommands eventSocket)
        {
            //todo: move to outbound socket
            return eventSocket.SendCommand("linger");
        }

        public static Task<CommandReply> NoLinger(this IEventSocketCommands eventSocket)
        {
            //todo: move to outbound socket
            return eventSocket.SendCommand("nolinger");
        }

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
        public static Task<CommandReply> MyEvents(this IEventSocketCommands eventSocket, string uuid)
        {
            //todo: move to outbound socket
            return eventSocket.SendCommand("myevents {0} plain".Fmt(uuid));
        }

        public static Task<CommandReply> DivertEventsOn(this IEventSocketCommands eventSocket)
        {
            return eventSocket.SendCommand("divert_events on");
        }

        public static Task<CommandReply> DivertEventsOff(this IEventSocketCommands eventSocket)
        {
            return eventSocket.SendCommand("divert_events off");
        }

        public static Task<CommandReply> Filter(this IEventSocketCommands eventSocket, EventName eventName)
        {
            return eventSocket.Filter(eventName.ToString().ToUpperWithUnderscores());
        }

        public static Task<CommandReply> Filter(this IEventSocketCommands eventSocket, string eventName)
        {
            return eventSocket.Filter("Event-Name", eventName);
        }

        public static Task<CommandReply> Filter(this IEventSocketCommands eventSocket, string header, string value)
        {
            if (header == null) throw new ArgumentNullException("header");
            if (value == null) throw new ArgumentNullException("value");
            return eventSocket.SendCommand("filter {0} {1}".Fmt(header, value));
        }

        public static Task<CommandReply> FilterDelete(this IEventSocketCommands eventSocket, EventName eventName)
        {
            return eventSocket.FilterDelete("Event-Name", eventName.ToString().ToUpperWithUnderscores());
        }

        public static Task<CommandReply> FilterDelete(this IEventSocketCommands eventSocket, string header)
        {
            if (header == null) throw new ArgumentNullException("header");
            return eventSocket.SendCommand("filter delete {0}".Fmt(header));
        }

        public static Task<CommandReply> FilterDelete(this IEventSocketCommands eventSocket, string header, string value)
        {
            if (header == null) throw new ArgumentNullException("header");
            if (value == null) throw new ArgumentNullException("value");
            return eventSocket.SendCommand("filter delete {0} {1}".Fmt(header, value));
        }

        public static Task<CommandReply> SendEvent(
            this IEventSocketCommands eventSocket, EventName eventName, IDictionary<string, string> headers = null)
        {
            return SendEvent(eventSocket, eventName.ToString().ToUpperWithUnderscores(), headers);
        }

        public static Task<CommandReply> SendEvent(this IEventSocketCommands eventSocket, string eventName, IDictionary<string, string> headers = null)
        {
            if (eventName == null) throw new ArgumentNullException("eventName");
            if (headers == null) headers = new Dictionary<string, string>();

            var headersString = headers.Aggregate(
                new StringBuilder(),
                (sb, kvp) =>
                    {
                        sb.AppendFormat("{0}: {1}", kvp.Key, kvp.Value);
                        sb.Append("\n");
                        return sb;
                    },
                sb => sb.ToString());

            return eventSocket.SendCommand("sendevent {0}\n{1}".Fmt(eventName, headersString));
        }

        public static Task<CommandReply> Hangup(this IEventSocketCommands eventSocket, string uuid, HangupCause hangupCause = HangupCause.NormalClearing)
        {
            if (uuid == null) throw new ArgumentNullException("uuid");
            return
                eventSocket.SendCommand(
                    "sendmsg {0}\ncall-command: hangup\nhangup-cause: {1}".Fmt(
                        uuid, hangupCause.ToString().ToUpperWithUnderscores()));
        }

        public static void Exit(this IEventSocketCommands eventSocket)
        {
            eventSocket.SendCommand("exit"); //will disconnect, no reply will arrive
        }

        public static Task<CommandReply> FsLog(this IEventSocketCommands eventSocket, string logLevel)
        {
            return eventSocket.SendCommand("log " + logLevel);
        }

        public static Task<CommandReply> NoLog(this IEventSocketCommands eventSocket)
        {
            return eventSocket.SendCommand("nolog");
        }

        public static Task<CommandReply> NoEvents(this IEventSocketCommands eventSocket)
        {
            return eventSocket.SendCommand("noevents");
        }
    }
}