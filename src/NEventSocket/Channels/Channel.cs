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

            this.Disposables.Add(
                eventSocket.Events.Where(x => x.UUID == this.UUID && x.EventName == EventName.ChannelBridge).Subscribe(
                    x =>
                        {
                            this.Log.Trace(() => "Channel [{0}] Bridged to [{1}]".Fmt(this.UUID, x.GetHeader(HeaderNames.OtherLegUniqueId)));

                            if (Bridge.Channel != null && x.GetHeader(HeaderNames.OtherLegUniqueId) != Bridge.Channel.UUID)
                            {
                                //possibly changed bridge partner as part of att_xfer
                                Log.Warn(() => "Channel [{0}] was Bridged to [{1}] but now changed to [{2}]".Fmt(UUID, Bridge.Channel.UUID, x.UUID));

                                this.Bridge.Channel.Dispose();
                                this.Bridge = new BridgeStatus(true, "TRANSFERRED", new BridgedChannel(x, eventSocket));
                            }
                        }));

            this.Disposables.Add(
                eventSocket.Events.Where(x => x.UUID == this.UUID && x.EventName == EventName.ChannelUnbridge).Subscribe(
                    x => this.Log.Trace(() => "Channel [{0}] Unbridged from [{1}] {2}".Fmt(this.UUID, this.Bridge.Channel.UUID, x.GetVariable("bridge_hangup_cause")))));

            this.Disposables.Add(
                eventSocket.Events.Where(x => x.EventName == EventName.ChannelBridge 
                                                && x.UUID != UUID
                                                && x.GetHeader(HeaderNames.OtherLegUniqueId) == UUID
                                                && (Bridge.Channel != null && x.UUID != Bridge.Channel.UUID))
                    .Subscribe(x =>
                        {
                            //there is another channel out there that has bridged to us but we didn't get the CHANNEL_BRIDGE event on this channel
                            //possibly an attended transfer. We'll swap our bridge partner so we can get its events

                            Log.Warn(() => "Channel [{0}] was Bridged to [{1}] but now changed to [{2}]".Fmt(UUID, Bridge.Channel.UUID, x.UUID));

                            this.Bridge.Channel.Dispose();
                            this.Bridge = new BridgeStatus(true, "TRANSFERRED", new BridgedChannel(x, eventSocket));
                        }));

            if (this.eventSocket is OutboundSocket)
            {
                this.Disposables.Add(
                    eventSocket.Events.Where(x => x.UUID == this.UUID && x.EventName == EventName.ChannelHangup)
                               .Subscribe(async e =>
                                   {
                                       await eventSocket.Exit();
                                   }));
             }

            //populate empty bridge status
            this.Bridge = new BridgeStatus(false, null);
        }

        ~Channel()
        {
            this.Dispose(false);
        }

        public BridgeStatus Bridge { get; private set; }

        public async Task BridgeTo(Channel other)
        {
            if (this.IsBridged)
            {
                throw new InvalidOperationException("Channel {0} is already bridged to {1}".Fmt(this.UUID, this.Bridge.Channel.UUID));
            }

            if (this.Answered != AnswerState.Answered && other.Answered != AnswerState.Answered)
            {
                throw new InvalidOperationException("At least one channel must be Answered to bridge them");
            }

            var result = await this.eventSocket.SendApi("uuid_bridge {0} {1}".Fmt(this.UUID, other.UUID));

            if (result.Success)
            {
                this.Bridge = new BridgeStatus(result.Success, result.BodyText, new BridgedChannel(other.lastEvent, eventSocket));
            }
            else
            {
                this.Bridge = new BridgeStatus(result.Success, result.ErrorMessage);
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
                    .Subscribe(x => this.Bridge = new BridgeStatus(false, "In Progress", new BridgedChannel(x, eventSocket))));

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

            this.Bridge = new BridgeStatus(result.Success, result.ResponseText, this.Bridge.Channel);
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

                    if (this.Bridge.Channel != null)
                    {
                        this.Bridge.Channel.Dispose();
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