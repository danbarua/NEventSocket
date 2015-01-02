// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InboundSocket.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket
{
    using System;
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Security;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
    using NEventSocket.Sockets;
    using NEventSocket.Util;

    /// <summary>
    /// Wraps an EventSocket connecting inbound to FreeSwitch
    /// </summary>
    public class InboundSocket : EventSocket
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        private InboundSocket(string host, int port, TimeSpan? timeout = null) : base(new TcpClient(host, port), timeout)
        {
        }

        /// <summary>
        /// Connects to FreeSwitch and authenticates
        /// </summary>
        /// <param name="host">(Default: localhost) The hostname or ip to connect to.</param>
        /// <param name="port">(Default: 8021) The Tcp port to connect to.</param>
        /// <param name="password">(Default: ClueCon) The password to authenticate with.</param>
        /// <param name="timeout">(Optional) The auth request timeout.</param>
        /// <returns>A task of <see cref="InboundSocket"/>.</returns>
        public static async Task<InboundSocket> Connect(
            string host = "localhost", int port = 8021, string password = "ClueCon", TimeSpan? timeout = null)
        {
            var socket = new InboundSocket(host, port, timeout);

            await
                socket.Messages.Where(x => x.ContentType == ContentTypes.AuthRequest)
                      .Take(1)
                      .Timeout(
                          socket.ResponseTimeOut, 
                          Observable.Throw<BasicMessage>(
                              new TimeoutException(
                                  "No Auth Request received within the specified timeout of {0}.".Fmt(socket.ResponseTimeOut))))
                      .Do(_ => Log.Trace(() => "Received Auth Request"), ex => Log.ErrorException("Error waiting for AuthRequest.", ex))
                      .ToTask();

            var result = await socket.Auth(password);

            if (!result.Success)
            {
                Log.Error("InboundSocket authentication failed ({0}).".Fmt(result.ErrorMessage));
                throw new SecurityException("Invalid password");
            }

            Log.Trace(() => "InboundSocket authentication succeeded.");

            return socket;
        }

        /// <summary>
        /// Originate a new call.
        /// </summary>
        /// <remarks>
        /// See https://freeswitch.org/confluence/display/FREESWITCH/mod_commands#mod_commands-originate
        /// </remarks>
        /// <param name="endpoint">The destination to call.</param>
        /// <param name="options">(Optional) <seealso cref="OriginateOptions"/> to configure the call.</param>
        /// <param name="application">(Default: park) The DialPlan application to execute on answer</param>
        /// <returns>A Task of <seealso cref="OriginateResult"/>.</returns>
        public Task<OriginateResult> Originate(string endpoint, OriginateOptions options = null, string application = "park")
        {
            if (options == null)
            {
                options = new OriginateOptions();
            }

            // if no UUID provided, we'll set one now and use that to filter for the correct channel events
            // this way, one inbound socket can originate many calls and we can complete the correct
            // TaskCompletionSource for each originated call.
            if (string.IsNullOrEmpty(options.UUID))
            {
                options.UUID = Guid.NewGuid().ToString();
            }

            var originateString = string.Format("{0}{1} &{2}", options, endpoint, application);

            return
                this.BackgroundJob("originate", originateString)
                    .ToObservable()
                    .Merge(
                        Events.FirstAsync(
                            x =>
                            x.UUID == options.UUID
                            && (x.EventName == EventName.ChannelAnswer || x.EventName == EventName.ChannelHangup
                                || (options.ReturnRingReady && x.EventName == EventName.ChannelProgress))).Cast<BasicMessage>())
                    .FirstAsync(x => ((x is BackgroundJobResult) && !((BackgroundJobResult)x).Success) || (x is EventMessage))
                    .Select(OriginateResult.FromBackgroundJobResultOrChannelEvent)
                    .ToTask();
        }
    }
}