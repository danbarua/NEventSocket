using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using NEventSocket.FreeSwitch;
using NEventSocket.Sockets;
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OriginateExtensions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// ------

namespace NEventSocket
{
    public static class OriginateExtensions
    {
        /// <summary>
        ///     Originate a new call
        /// </summary>
        /// <remarks>
        ///     See https://freeswitch.org/confluence/display/FREESWITCH/mod_commands#mod_commands-originate
        /// </remarks>
        /// <param name="socket">the <seealso cref="EventSocket"/> instance.</param>
        /// <param name="endpoint">The destination to call.</param>
        /// <param name="extension">Destination number to search in dialplan</param>
        /// <param name="dialplan">(Optional) defaults to 'XML' if not specified</param>
        /// <param name="context">(Optional) defaults to 'default' if not specified</param>
        /// <param name="options">(Optional) <seealso cref="OriginateOptions" /> to configure the call.</param>
        /// <returns>A Task of <seealso cref="OriginateResult" />.</returns>
        public static Task<OriginateResult> Originate(
            this EventSocket socket,
            string endpoint,
            string extension,
            string dialplan = "XML",
            string context = "default",
            OriginateOptions options = null)
        {
            return InternalOriginate(socket, endpoint, string.Format("{0} {1} {2}", extension, dialplan, context), options);
        }

        /// <summary>
        ///     Originate a new call.
        /// </summary>
        /// <remarks>
        ///     See https://freeswitch.org/confluence/display/FREESWITCH/mod_commands#mod_commands-originate
        /// </remarks>
        /// <param name="socket">the <seealso cref="EventSocket"/> instance.</param>
        /// <param name="endpoint">The destination to call.</param>
        /// <param name="options">(Optional) <seealso cref="OriginateOptions" /> to configure the call.</param>
        /// <param name="application">(Default: park) The DialPlan application to execute on answer</param>
        /// <param name="applicationArgs"></param>
        /// <returns>A Task of <seealso cref="OriginateResult" />.</returns>
        public static Task<OriginateResult> Originate(
            this EventSocket socket,
            string endpoint,
            OriginateOptions options = null,
            string application = "park",
            string applicationArgs = null)
        {
            return InternalOriginate(socket, endpoint, string.Format("'&{0}({1})'", application, applicationArgs), options);
        }

        private static async Task<OriginateResult> InternalOriginate(EventSocket socket, string endpoint, string destination, OriginateOptions options = null)
        {
            if (options == null)
            {
                options = new OriginateOptions();
            }
            var originateString = $"{options}{endpoint} {destination}";

            await socket.SubscribeEvents(EventName.BackgroundJob).ConfigureAwait(false);
            return
                await
                    socket.BackgroundJob("originate", originateString)
                        .ToObservable()
                        .Select(OriginateResult.FromBackgroundJobResultOrChannelEvent)
                        .ToTask()
                        .ConfigureAwait(false);


        }
    }
}