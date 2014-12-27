namespace NEventSocket
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.FreeSwitch.Applications;
    using NEventSocket.Logging;
    using NEventSocket.Sockets;
    using NEventSocket.Util;

    public static class EventSocketExensions
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(EventSocket));

        public static Task<CommandReply> Auth(this EventSocket eventSocket, string password)
        {
            if (password == null) throw new ArgumentNullException("password");
            return eventSocket.SendCommand("auth {0}".Fmt(password));
        }

        public static Task<ApiResponse> Api(this EventSocket eventSocket, string command, string arg = null)
        {
            if (command == null) throw new ArgumentNullException("command");
            return eventSocket.SendApi(arg != null ? "{0} {1}".Fmt(command, arg) : command);
        }

        public static Task<ApiResponse> SetChannelVariable(this EventSocket eventSocket, string uuid, string variable, object value)
        {
            return eventSocket.SendApi("uuid_setvar {0} {1} {2}".Fmt(uuid, variable, value));
        }

        /// <summary>
        /// Set Multiple Channel Variables in one go
        /// </summary>
        /// <param name="eventSocket">The EventSocket instance.</param>
        /// <param name="uuid">The Channel UUID</param>
        /// <param name="assignments">Array of assignments in the form "foo=value", "bar=value".</param>
        /// <returns>A Task[EventMessage] representing the CHANNEL_EXECUTE_COMPLETE event.</returns>
        public static Task<ApiResponse> SetMultipleChannelVariables(
            this EventSocket eventSocket, string uuid, params string[] assignments)
        {
            return
                eventSocket.SendApi(
                    "uuid_setvar_multi {0} {1}".Fmt(
                        uuid, assignments.Aggregate(StringBuilderPool.Allocate(), (sb, s) => { sb.Append(s); sb.Append(";"); return sb; }, StringBuilderPool.ReturnAndFree)));
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
        public static async Task<PlayResult> Play(this EventSocket eventSocket, string uuid, string file, PlayOptions options = null)
        {
            //todo: implement options for playback eg a-leg, b-leg, both, using uuid_displace
            if (options == null) options = new PlayOptions();

            //todo: what if applicationresult is null (hang up occurs before the application completes)
            return new PlayResult(await eventSocket.ExecuteApplication(uuid, "playback", applicationArguments: file, loops: options.Loops));
        }

        public static async Task<PlayGetDigitsResult> PlayGetDigits(
            this EventSocket eventSocket, string uuid, PlayGetDigitsOptions options)
        {
            //todo: what if applicationresult is null (hang up occurs before the application completes)
            return new PlayGetDigitsResult(
                await eventSocket.ExecuteApplication(uuid, "play_and_get_digits", options.ToString()), options.ChannelVariableName);
        }

        public static async Task<ReadResult> Read(
            this EventSocket eventSocket, string uuid, ReadOptions options)
        {
            //todo: what if applicationresult is null (hang up occurs before the application completes)
            return new ReadResult(
                await eventSocket.ExecuteApplication(uuid, "read", options.ToString()), options.ChannelVariableName);
        }

        public static Task<EventMessage> Say(this EventSocket eventSocket, string uuid, SayOptions options)
        {
            return eventSocket.ExecuteApplication(uuid, "say", options.ToString());
        }

        public static Task<EventMessage> StartDtmf(this EventSocket eventSocket, string uuid)
        {
            return eventSocket.ExecuteApplication(uuid, "spandsp_start_dtmf");
        }

        public static Task<EventMessage> Stoptmf(this EventSocket eventSocket, string uuid)
        {
            return eventSocket.ExecuteApplication(uuid, "spandsp_stop_dtmf");
        }

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
            if (header == null) throw new ArgumentNullException("header");
            if (value == null) throw new ArgumentNullException("value");
            return eventSocket.SendCommand("filter {0} {1}".Fmt(header, value));
        }

        public static Task<CommandReply> FilterDelete(this EventSocket eventSocket, EventName eventName)
        {
            return eventSocket.FilterDelete("Event-Name", eventName.ToString().ToUpperWithUnderscores());
        }

        public static Task<CommandReply> FilterDelete(this EventSocket eventSocket, string header)
        {
            if (header == null) throw new ArgumentNullException("header");
            return eventSocket.SendCommand("filter delete {0}".Fmt(header));
        }

        public static Task<CommandReply> FilterDelete(this EventSocket eventSocket, string header, string value)
        {
            if (header == null) throw new ArgumentNullException("header");
            if (value == null) throw new ArgumentNullException("value");
            return eventSocket.SendCommand("filter delete {0} {1}".Fmt(header, value));
        }

        public static Task<CommandReply> SendEvent(
            this EventSocket eventSocket, EventName eventName, IDictionary<string, string> headers = null)
        {
            return SendEvent(eventSocket, eventName.ToString().ToUpperWithUnderscores(), headers);
        }

        public static Task<CommandReply> SendEvent(this EventSocket eventSocket, string eventName, IDictionary<string, string> headers = null)
        {
            if (eventName == null) throw new ArgumentNullException("eventName");
            if (headers == null) headers = new Dictionary<string, string>();

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

        public static Task<CommandReply> Hangup(this EventSocket eventSocket, string uuid, HangupCause hangupCause = HangupCause.NormalClearing)
        {
            if (uuid == null) throw new ArgumentNullException("uuid");
            return
                eventSocket.SendCommand(
                    "sendmsg {0}\ncall-command: hangup\nhangup-cause: {1}".Fmt(
                        uuid, hangupCause.ToString().ToUpperWithUnderscores()));
        }

        public static void Exit(this EventSocket eventSocket)
        {
            eventSocket.SendCommand("exit"); //will disconnect, a reply might not arrive in time to be read
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