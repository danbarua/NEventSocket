// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Channel.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
    using NEventSocket.Sockets;
    using NEventSocket.Util;

    public class Channel : BasicChannel
    {
        private InterlockedBoolean initialized = new InterlockedBoolean();
        
        private readonly InterlockedBoolean disposed = new InterlockedBoolean();

        private string recordingPath;

        protected internal Channel(OutboundSocket outboundSocket) : this(outboundSocket.ChannelData, outboundSocket)
        {
        }

        protected internal Channel(EventMessage eventMessage, EventSocket eventSocket) : base(eventMessage, eventSocket)
        {
        }

        internal static async Task<Channel> Create(OutboundSocket outboundSocket)
        {
            var channel = new Channel(outboundSocket);

            //populate empty bridge status
            channel.Bridge = new BridgeStatus(false, null);
            channel.ExitOnHangup = true;

            await outboundSocket.Linger().ConfigureAwait(false);

            await outboundSocket.SubscribeEvents(
               EventName.ChannelProgress,
               EventName.ChannelBridge,
               EventName.ChannelUnbridge,
               EventName.ChannelAnswer,
               EventName.ChannelHangup,
               EventName.Dtmf).ConfigureAwait(false); //subscribe to minimum events

            await outboundSocket.Filter(HeaderNames.UniqueId, outboundSocket.ChannelData.UUID).ConfigureAwait(false); //filter for our unique id (in case using full socket mode)
            await outboundSocket.Filter(HeaderNames.OtherLegUniqueId, outboundSocket.ChannelData.UUID).ConfigureAwait(false); //filter for channels bridging to our unique id

            channel.InitializeSubscriptions();
            return channel;
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

            if (!(IsAnswered || IsPreAnswered) && !(other.IsAnswered || other.IsPreAnswered))
            {
                throw new InvalidOperationException("At least one channel must be Answered to bridge them");
            }

            var result = await eventSocket.SendApi("uuid_bridge {0} {1}".Fmt(UUID, other.UUID)).ConfigureAwait(false);

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
            if (!IsAnswered && !IsPreAnswered)
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

            var result = await eventSocket.Bridge(UUID, destination, options).ConfigureAwait(false);

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

        public Task HoldToggle()
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

        public Task PreAnswer()
        {
            return eventSocket.ExecuteApplication(UUID, "pre_answer");
        }

        public Task Sleep(int milliseconds)
        {
            return eventSocket.ExecuteApplication(UUID, "sleep", milliseconds.ToString());
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
                await StopRecording().ConfigureAwait(false);
            }

            recordingPath = file;
            await eventSocket.SendApi("uuid_record {0} start {1} {2}".Fmt(UUID, recordingPath, maxSeconds)).ConfigureAwait(false);
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
                await eventSocket.SendApi("uuid_record {0} mask {1}".Fmt(UUID, recordingPath)).ConfigureAwait(false);
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
                await eventSocket.SendApi("uuid_record {0} unmask {1}".Fmt(UUID, recordingPath)).ConfigureAwait(false);
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
                await eventSocket.SendApi("uuid_record {0} stop {1}".Fmt(UUID, recordingPath)).ConfigureAwait(false);
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
            if (!disposed.EnsureCalledOnce())
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
            }
        }
        
        private void InitializeSubscriptions()
        {
            if (initialized.EnsureCalledOnce())
            {
                Log.Warn(() => "Channel already initialized");
                return;
            }

            Disposables.Add(
                    eventSocket.Events.Where(x => x.UUID == UUID && x.EventName == EventName.ChannelBridge).Subscribe(
                        x =>
                        {
                            Log.Trace(
                                () =>
                                "Channel [{0}] Bridged to [{1}]".Fmt(UUID, x.GetHeader(HeaderNames.OtherLegUniqueId)));

                            if (Bridge.Channel != null
                                && x.GetHeader(HeaderNames.OtherLegUniqueId) != Bridge.Channel.UUID)
                            {
                                //possibly changed bridge partner as part of att_xfer
                                Log.Warn(
                                    () =>
                                    "Channel [{0}] was Bridged to [{1}] but now changed to [{2}]".Fmt(
                                        UUID, Bridge.Channel.UUID, x.UUID));

                                Bridge.Channel.Dispose();
                                Bridge = new BridgeStatus(true, "TRANSFERRED", new BridgedChannel(x, eventSocket));
                            }
                        }));

            Disposables.Add(
                eventSocket.Events.Where(x => x.UUID == UUID && x.EventName == EventName.ChannelUnbridge)
                           .Subscribe(
                               x =>
                               Log.Trace(
                                   () =>
                                   "Channel [{0}] Unbridged from [{1}] {2}".Fmt(
                                       UUID, Bridge.Channel.UUID, x.GetVariable("bridge_hangup_cause")))));

            Disposables.Add(
                eventSocket.Events.Where(
                    x =>
                    x.EventName == EventName.ChannelBridge && x.UUID != UUID
                    && x.GetHeader(HeaderNames.OtherLegUniqueId) == UUID
                    && (Bridge.Channel != null && x.UUID != Bridge.Channel.UUID)).Subscribe(
                        x =>
                        {
                            //there is another channel out there that has bridged to us but we didn't get the CHANNEL_BRIDGE event on this channel
                            //possibly an attended transfer. We'll swap our bridge partner so we can get its events

                            Log.Warn(
                                () =>
                                "Channel [{0}] was Bridged to [{1}] but now changed to [{2}]".Fmt(
                                    UUID, Bridge.Channel.UUID, x.UUID));

                            Bridge.Channel.Dispose();
                            Bridge = new BridgeStatus(true, "TRANSFERRED", new BridgedChannel(x, eventSocket));
                        }));

            if (this.eventSocket is OutboundSocket)
            {
                Disposables.Add(
                    eventSocket.Events.Where(x => x.UUID == UUID && x.EventName == EventName.ChannelHangup)
                               .Subscribe(
                                   async e =>
                                   {
                                       if (ExitOnHangup)
                                       {
                                           Log.Info(() => "Channel [{0}] exiting".Fmt(UUID));
                                           await eventSocket.Exit().ConfigureAwait(false); //don't care about the result, no need to wait
                                       }
                                   }));
            }

            Log.Trace(() => "Channel [{0}] subscriptions initialized".Fmt(UUID));
        }

        public bool ExitOnHangup { get; set; }
    }
}