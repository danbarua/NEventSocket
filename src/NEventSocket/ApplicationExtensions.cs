// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationExtensions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   Defines Application operations that can operate on either an <seealso cref="InboundSocket" /> or an <seealso cref="OutboundSocket" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket
{
    using System.IO;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Sockets;

    /// <summary>
    /// Defines Application operations that can operate on either an <seealso cref="InboundSocket"/> or an <seealso cref="OutboundSocket"/>.
    /// </summary>
    /// <remarks>
    /// See https://freeswitch.org/confluence/display/FREESWITCH/mod_dptools
    /// </remarks>
    public static class ApplicationExtensions
    {
        /// <summary>
        /// Plays the given file to the specified channel.
        /// </summary>
        /// <param name="eventSocket">The EventSocket instance.</param>
        /// <param name="uuid">The Channel UUID.</param>
        /// <param name="file">The Path to the file to be played. Note: use forward slashes for path separators.</param>
        /// <param name="options">Options to customize playback.</param>
        /// <returns>A PlayResult.</returns>
        /// <exception cref="FileNotFoundException">Throws FileNotFoundException if FreeSwitch is unable to play the file.</exception>//todo: should it?
        public static async Task<PlayResult> Play(this EventSocket eventSocket, string uuid, string file, PlayOptions options = null)
        {
            // todo: implement options for playback eg a-leg, b-leg, both, using uuid_displace
            if (options == null)
            {
                options = new PlayOptions();
            }

            // todo: what if applicationresult is null (hang up occurs before the application completes)
            return new PlayResult(await eventSocket.ExecuteApplication(uuid, "playback", applicationArguments: file, loops: options.Loops).ConfigureAwait(false));
        }

        public static async Task<PlayGetDigitsResult> PlayGetDigits(this EventSocket eventSocket, string uuid, PlayGetDigitsOptions options)
        {
            // todo: what if applicationresult is null (hang up occurs before the application completes)
            return new PlayGetDigitsResult(
                await eventSocket.ExecuteApplication(uuid, "play_and_get_digits", options.ToString()).ConfigureAwait(false), options.ChannelVariableName);
        }

        public static async Task<ReadResult> Read(this EventSocket eventSocket, string uuid, ReadOptions options)
        {
            // todo: what if applicationresult is null (hang up occurs before the application completes)
            return new ReadResult(await eventSocket.ExecuteApplication(uuid, "read", options.ToString()).ConfigureAwait(false), options.ChannelVariableName);
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
    }
}