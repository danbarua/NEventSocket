// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Channel.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.Channels
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
    using NEventSocket.Sockets;
    using NEventSocket.Util;

    public class Channel : BasicChannel
    {
        private bool disposed;

        private string recordingPath;

        public Channel(OutboundSocket outboundSocket) : this(outboundSocket.ChannelData, outboundSocket)
        {
            outboundSocket.Linger().Wait();
        }

        protected Channel(EventMessage eventMessage, EventSocket eventSocket) : base(eventMessage, eventSocket)
        {
            eventSocket.SubscribeEvents(EventName.ChannelCreate).Wait();

            Disposables.Add(
                eventSocket.Events.Where(x => x.UUID == UUID && x.EventName == EventName.ChannelBridge).Subscribe(
                    x =>
                        {
                            Log.Trace(() => "Channel [{0}] Bridged to [{1}]".Fmt(UUID, x.GetHeader(HeaderNames.OtherLegUniqueId)));

                            if (Bridge.Channel != null && x.GetHeader(HeaderNames.OtherLegUniqueId) != Bridge.Channel.UUID)
                            {
                                //possibly changed bridge partner as part of att_xfer
                                Log.Warn(() => "Channel [{0}] was Bridged to [{1}] but now changed to [{2}]".Fmt(UUID, Bridge.Channel.UUID, x.UUID));

                                Bridge.Channel.Dispose();
                                Bridge = new BridgeStatus(true, "TRANSFERRED", new BridgedChannel(x, eventSocket));
                            }
                        }));

            Disposables.Add(
                eventSocket.Events.Where(x => x.UUID == UUID && x.EventName == EventName.ChannelUnbridge).Subscribe(
                    x => Log.Trace(() => "Channel [{0}] Unbridged from [{1}] {2}".Fmt(UUID, Bridge.Channel.UUID, x.GetVariable("bridge_hangup_cause")))));

            Disposables.Add(
                eventSocket.Events.Where(x => x.EventName == EventName.ChannelBridge 
                                                && x.UUID != UUID
                                                && x.GetHeader(HeaderNames.OtherLegUniqueId) == UUID
                                                && (Bridge.Channel != null && x.UUID != Bridge.Channel.UUID))
                    .Subscribe(x =>
                        {
                            //there is another channel out there that has bridged to us but we didn't get the CHANNEL_BRIDGE event on this channel
                            //possibly an attended transfer. We'll swap our bridge partner so we can get its events

                            Log.Warn(() => "Channel [{0}] was Bridged to [{1}] but now changed to [{2}]".Fmt(UUID, Bridge.Channel.UUID, x.UUID));

                            Bridge.Channel.Dispose();
                            Bridge = new BridgeStatus(true, "TRANSFERRED", new BridgedChannel(x, eventSocket));
                        }));

            if (this.eventSocket is OutboundSocket)
            {
                Disposables.Add(
                    eventSocket.Events.Where(x => x.UUID == UUID && x.EventName == EventName.ChannelHangup)
                               .Subscribe(async e =>
                                   {
                                       if (ExitOnHangup)
                                       {
                                           await eventSocket.Exit();
                                           Log.Info(() => "Channel [{0}] exited".Fmt(UUID));
                                       }
                                   }));
             }

            //populate empty bridge status
            Bridge = new BridgeStatus(false, null);
            ExitOnHangup = true;
        }

        ~Channel()
        {
            Dispose(false);
        }

        public BridgeStatus Bridge { get; private set; }

        public async Task BridgeTo(Channel other)
        {
            if (IsBridged)
            {
                throw new InvalidOperationException("Channel {0} is already bridged to {1}".Fmt(UUID, Bridge.Channel.UUID));
            }

            if (Answered != AnswerState.Answered && other.Answered != AnswerState.Answered)
            {
                throw new InvalidOperationException("At least one channel must be Answered to bridge them");
            }

            var result = await eventSocket.SendApi("uuid_bridge {0} {1}".Fmt(UUID, other.UUID));

            if (result.Success)
            {
                Bridge = new BridgeStatus(result.Success, result.BodyText, new BridgedChannel(other.lastEvent, eventSocket));
            }
            else
            {
                Bridge = new BridgeStatus(result.Success, result.ErrorMessage);
            }
        }

        public async Task BridgeTo(string destination, BridgeOptions options, Action<EventMessage> onProgress = null)
        {
            if (!IsAnswered)
            {
                return;
            }

            Log.Debug(() => "Channel {0} is attempting a bridge to {1}".Fmt(UUID, destination));

            if (string.IsNullOrEmpty(options.UUID))
            {
                options.UUID = Guid.NewGuid().ToString();
            }

            var subscriptions = new CompositeDisposable();

            subscriptions.Add(
                eventSocket.Events.Where(x => x.UUID == options.UUID)
                    .Take(1)
                    .Subscribe(x => Bridge = new BridgeStatus(false, "In Progress", new BridgedChannel(x, eventSocket))));

            if (onProgress != null)
            {
                subscriptions.Add(
                    eventSocket.Events.Where(x => x.UUID == options.UUID && x.EventName == EventName.ChannelProgress)
                        .Take(1)
                        .Subscribe(onProgress));
            }

            var result = await eventSocket.Bridge(UUID, destination, options);

            Log.Debug(() => "Channel {0} bridge complete {1} {2}".Fmt(UUID, result.Success, result.ResponseText));
            subscriptions.Dispose();

            Bridge = new BridgeStatus(result.Success, result.ResponseText, Bridge.Channel);
        }

        public Task Execute(string application, string args)
        {
            return eventSocket.ExecuteApplication(UUID, application, args);
        }

        public Task Execute(string uuid, string application, string args)
        {
            return eventSocket.ExecuteApplication(uuid, application, args);
        }

        public Task Hold()
        {
            return RunIfAnswered(() => eventSocket.SendApi("uuid_hold toggle " + UUID));
        }

        public Task HoldOn()
        {
            return RunIfAnswered(() => eventSocket.SendApi("uuid_hold " + UUID));
        }

        public Task HoldOff()
        {
            return RunIfAnswered(() => eventSocket.SendApi("uuid_hold off " + UUID));
        }

        public Task Park()
        {
            return RunIfAnswered(() => eventSocket.ExecuteApplication(UUID, "park"));
        }

        public Task RingReady()
        {
            return eventSocket.ExecuteApplication(UUID, "ring_ready");
        }

        public Task Answer()
        {
            return eventSocket.ExecuteApplication(UUID, "answer");
        }

        public Task Sleep(int milliseconds)
        {
            return eventSocket.ExecuteApplication(UUID, "sleep", milliseconds.ToString());
        }

        /// <summary>
        /// Send DTMF digits to the channel
        /// </summary>
        /// <param name="digits">String with digits or characters</param>
        /// <param name="duration">Duration of each symbol (default -- 2000ms)</param>
        /// <returns></returns>
        public Task SendDTMF(string digits, TimeSpan? duration = null)
        {
            var durationMs = duration.HasValue ? duration.Value.TotalMilliseconds : 2000; // default value in freeswitch
            return eventSocket.ExecuteApplication(UUID, "send_dtmf", "{0}@{1}".Fmt(digits, durationMs));
        }

        public async Task StartRecording(string file, int? maxSeconds = null)
        {
            if (!IsAnswered)
            {
                return;
            }

            if (file == recordingPath)
            {
                return;
            }

            if (recordingPath != null)
            {
                Log.Warn(
                    () =>
                    "Channel {0} received a request to record to file {1} while currently recording to file {2}. Channel will stop recording and start recording to the new file."
                        .Fmt(UUID, file, recordingPath));
                await StopRecording();
            }

            recordingPath = file;
            await eventSocket.SendApi("uuid_record {0} start {1} {2}".Fmt(UUID, recordingPath, maxSeconds));
            Log.Debug(() => "Channel {0} is recording to {1}".Fmt(UUID, recordingPath));
        }

        public async Task MaskRecording()
        {
            if (!IsAnswered)
            {
                return;
            }

            if (string.IsNullOrEmpty(recordingPath))
            {
                Log.Warn(() => "Channel {0} is not recording".Fmt(UUID));
            }
            else
            {
                await eventSocket.SendApi("uuid_record {0} mask {1}".Fmt(UUID, recordingPath));
                Log.Debug(() => "Channel {0} has masked recording to {1}".Fmt(UUID, recordingPath));
            }
        }

        public async Task UnmaskRecording()
        {
            if (!IsAnswered)
            {
                return;
            }

            if (string.IsNullOrEmpty(recordingPath))
            {
                Log.Warn(() => "Channel {0} is not recording".Fmt(UUID));
            }
            else
            {
                await eventSocket.SendApi("uuid_record {0} unmask {1}".Fmt(UUID, recordingPath));
                Log.Debug(() => "Channel {0} has unmasked recording to {1}".Fmt(UUID, recordingPath));
            }
        }

        public async Task StopRecording()
        {
            if (!IsAnswered)
            {
                return;
            }

            if (string.IsNullOrEmpty(recordingPath))
            {
                Log.Warn(() => "Channel {0} is not recording".Fmt(UUID));
            }
            else
            {
                await eventSocket.SendApi("uuid_record {0} stop {1}".Fmt(UUID, recordingPath));
                recordingPath = null;
                Log.Debug(() => "Channel {0} has stopped recording to {1}".Fmt(UUID, recordingPath));
            }
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (!Disposables.IsDisposed)
                    {
                        Disposables.Dispose();
                    }

                    if (Bridge.Channel != null)
                    {
                        Bridge.Channel.Dispose();
                    }
                }

                if (eventSocket is OutboundSocket)
                {
                    // todo: should we close the socket associated with the channel here?
                    eventSocket.Dispose();
                }

                eventSocket = null;

                disposed = true;
            }
        }

        public bool ExitOnHangup { get; set; }
    }
}