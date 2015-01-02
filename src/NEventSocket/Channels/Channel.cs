// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Channel.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            eventSocket.SubscribeEvents(EventName.ChannelCreate, EventName.ChannelOriginate, EventName.ChannelDestroy).Wait();

            this.Disposables.Add(
                eventSocket.Events.Where(x => x.UUID == this.UUID && x.EventName == EventName.ChannelUnbridge).Subscribe(
                    x =>
                        {
                            Log.Debug(
                                () =>
                                "Channel [{0}] B-Leg [{1}] hungup [{2}]".Fmt(
                                    this.UUID, this.BridgedChannel.UUID, x.GetVariable("bridge_hangup_cause")));
                            Log.Trace(() => "Channel [{0}] Unbridged from [{1}]".Fmt(this.UUID, this.BridgedChannel.UUID));
                        }));

            this.Disposables.Add(
                eventSocket.Events.Where(x => x.UUID == this.UUID && x.EventName == EventName.ChannelBridge).Subscribe(
                    x =>
                        {
                            Log.Trace(() => "Channel [{0}] Bridged to [{1}]".Fmt(this.UUID, x.GetHeader(HeaderNames.OtherLegUniqueId)));
                            if (x.UUID != BridgedChannel.UUID)
                            {
                                Log.Warn(
                                    () =>
                                    "Channel [{0}] was Bridged to [{1}] but now changed to [{2}]".Fmt(UUID, BridgedChannel.UUID, x.UUID));
                            }
                        }));

            if (this.eventSocket is OutboundSocket)
            {
                this.Disposables.Add(
                    eventSocket.Events.Where(x => x.UUID == this.UUID && x.EventName == EventName.ChannelHangup)
                               .Subscribe(e => eventSocket.Exit()));
            }

            this.Bridge = new BridgeStatus(false, null);
        }
    

        ~Channel()
        {
            this.Dispose(false);
        }

        protected BridgedChannel BridgedChannel { get; private set; }

        public BridgeStatus Bridge { get; private set; }

        public async Task BridgeTo(Channel other)
        {
            if (this.IsBridged)
            {
                throw new InvalidOperationException("Channel {0} is already bridged to {1}".Fmt(this.UUID, this.BridgedChannel.UUID));
            }

            if (this.Answered != AnswerState.Answered && other.Answered != AnswerState.Answered)
            {
                throw new InvalidOperationException("At least one channel must be Answered to bridge them");
            }

            var result = await this.eventSocket.SendApi("uuid_bridge {0} {1}".Fmt(this.UUID, other.UUID));

            if (result.Success)
            {
                this.BridgedChannel = new BridgedChannel(other.lastEvent, eventSocket);

                this.eventSocket.Events.Where(x => x.UUID == other.UUID && x.EventName == EventName.ChannelHangup)
                    .Take(1)
                    .Subscribe(x => this.Log.Debug(() => "Channel {0} B-Leg {1} hungup {2}".Fmt(this.UUID, other.UUID, x.HangupCause)));
            }
        }

        public async Task BridgeTo(string destination, BridgeOptions options, Action<EventMessage> onProgress = null)
        {
            if (!this.IsAnswered)
            {
                return;
            }

            Log.Debug(() => "Channel {0} is attempting a bridge to {1}".Fmt(this.UUID, destination));

            if (string.IsNullOrEmpty(options.UUID))
            {
                options.UUID = Guid.NewGuid().ToString();
            }

            var subscriptions = new CompositeDisposable();

            subscriptions.Add(
                this.eventSocket.Events.Where(x => x.UUID == options.UUID)
                    .Take(1)
                    .Subscribe(x => this.BridgedChannel = new BridgedChannel(x, eventSocket)));

            if (onProgress != null)
            {
                subscriptions.Add(
                    this.eventSocket.Events.Where(x => x.UUID == options.UUID && x.EventName == EventName.ChannelProgress)
                        .Take(1)
                        .Subscribe(onProgress));
            }

            var result = await this.eventSocket.Bridge(this.UUID, destination, options);

            Log.Debug(() => "Channel {0} bridge complete {1} {2}".Fmt(this.UUID, result.Success, result.ResponseText));
            subscriptions.Dispose();

            this.Bridge = new BridgeStatus(result.Success, result.ResponseText, this.BridgedChannel);
        }

        public Task Execute(string application, string args)
        {
            return this.eventSocket.ExecuteApplication(this.UUID, application, args);
        }

        public Task Execute(string uuid, string application, string args)
        {
            return this.eventSocket.ExecuteApplication(uuid, application, args);
        }

        public Task Hold()
        {
            return this.RunIfAnswered(() => this.eventSocket.SendApi("uuid_hold toggle " + this.UUID));
        }

        public Task Park()
        {
            return this.RunIfAnswered(() => this.eventSocket.ExecuteApplication(this.UUID, "park"));
        }

        public Task RingReady()
        {
            return this.eventSocket.ExecuteApplication(this.UUID, "ring_ready");
        }

        public Task Answer()
        {
            return this.eventSocket.ExecuteApplication(this.UUID, "answer");
        }

        public Task Sleep(int milliseconds)
        {
            return this.eventSocket.ExecuteApplication(this.UUID, "sleep", milliseconds.ToString());
        }

        public async Task StartRecording(string file, int? maxSeconds = null)
        {
            if (!this.IsAnswered)
            {
                return;
            }

            if (file == this.recordingPath)
            {
                return;
            }

            if (this.recordingPath != null)
            {
                Log.Warn(
                    () =>
                    "Channel {0} received a request to record to file {1} while currently recording to file {2}. Channel will stop recording and start recording to the new file."
                        .Fmt(this.UUID, file, this.recordingPath));
                await StopRecording();
            }

            this.recordingPath = file;
            await this.eventSocket.SendApi("uuid_record {0} start {1} {2}".Fmt(this.UUID, this.recordingPath, maxSeconds));
            Log.Debug(() => "Channel {0} is recording to {1}".Fmt(this.UUID, this.recordingPath));
        }

        public async Task MaskRecording()
        {
            if (!this.IsAnswered)
            {
                return;
            }

            if (string.IsNullOrEmpty(this.recordingPath))
            {
                Log.Warn(() => "Channel {0} is not recording".Fmt(this.UUID));
            }
            else
            {
                await this.eventSocket.SendApi("uuid_record {0} mask {1}".Fmt(this.UUID, this.recordingPath));
                Log.Debug(() => "Channel {0} has masked recording to {1}".Fmt(this.UUID, this.recordingPath));
            }
        }

        public async Task UnmaskRecording()
        {
            if (!this.IsAnswered)
            {
                return;
            }

            if (string.IsNullOrEmpty(this.recordingPath))
            {
                Log.Warn(() => "Channel {0} is not recording".Fmt(this.UUID));
            }
            else
            {
                await this.eventSocket.SendApi("uuid_record {0} unmask {1}".Fmt(this.UUID, this.recordingPath));
                Log.Debug(() => "Channel {0} has unmasked recording to {1}".Fmt(this.UUID, this.recordingPath));
            }
        }

        public async Task StopRecording()
        {
            if (!this.IsAnswered)
            {
                return;
            }

            if (string.IsNullOrEmpty(this.recordingPath))
            {
                Log.Warn(() => "Channel {0} is not recording".Fmt(this.UUID));
            }
            else
            {
                await this.eventSocket.SendApi("uuid_record {0} stop {1}".Fmt(this.UUID, this.recordingPath));
                this.recordingPath = null;
                Log.Debug(() => "Channel {0} has stopped recording to {1}".Fmt(this.UUID, this.recordingPath));
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (!this.Disposables.IsDisposed)
                    {
                        this.Disposables.Dispose();
                    }
                }

                if (this.eventSocket is OutboundSocket)
                {
                    // todo: should we close the socket associated with the channel here?
                    this.eventSocket.Dispose();
                }

                this.eventSocket = null;

                this.disposed = true;
            }
        }
    }
}