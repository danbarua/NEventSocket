// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InboundSocket.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket
{
    using System;
    using System.IO;
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
        /// <exception cref="InboundSocketConnectionFailedException"></exception>
        public static async Task<InboundSocket> Connect(
            string host = "localhost", int port = 8021, string password = "ClueCon", TimeSpan? timeout = null)
        {
            try
            {
                var socket = new InboundSocket(host, port, timeout);

                var firstMessage =
                await socket.Messages.Where(
                              x => x.ContentType == ContentTypes.AuthRequest
                           || x.ContentType == ContentTypes.RudeRejection)
                          .Take(1)
                          .Timeout(
                              socket.ResponseTimeOut,
                              Observable.Throw<BasicMessage>(
                                  new TimeoutException(
                                      "No Auth Request received within the specified timeout of {0}.".Fmt(socket.ResponseTimeOut))))
                          .Do(_ => Log.Trace(() => "Received Auth Request"), ex => Log.ErrorException("Error waiting for AuthRequest.", ex))
                          .ToTask()
                          .ConfigureAwait(false);

                if (firstMessage.ContentType == ContentTypes.RudeRejection)
                {
                    Log.Error("InboundSocket connection rejected ({0}).".Fmt(firstMessage.BodyText));
                    throw new InboundSocketConnectionFailedException("Connection Rejected - '{0}'. Check the acl in eventsocket.conf".Fmt(firstMessage.BodyText));
                }

                var result = await socket.Auth(password).ConfigureAwait(false);

                if (!result.Success)
                {
                    Log.Error("InboundSocket authentication failed ({0}).".Fmt(result.ErrorMessage));
                    throw new InboundSocketConnectionFailedException("Invalid password when trying to connect to {0}:{1}".Fmt(host, port));
                }

                Log.Trace(() => "InboundSocket authentication succeeded.");

                return socket;
            }
            catch (SocketException ex)
            {
                throw new InboundSocketConnectionFailedException("Socket error when trying to connect to {0}:{1}".Fmt(host, port), ex);
            }
            catch (IOException ex)
            {
                throw new InboundSocketConnectionFailedException("IO error when trying to connect to {0}:{1}".Fmt(host, port), ex);
            }
            catch (TimeoutException ex)
            {
                throw new InboundSocketConnectionFailedException("Timeout when trying to connect to {0}:{1}.{2}".Fmt(host, port, ex.Message), ex);
            }
        }
    }
}