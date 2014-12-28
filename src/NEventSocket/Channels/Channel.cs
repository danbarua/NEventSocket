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

    public class Channel : IChannel
    {
        private const string FeatureCodeEvent = "NEventSocket::FeatureCode";

        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        private readonly EventSocket eventSocket;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private bool disposed;

        private Action<EventMessage> hangupCallback = (e) => { };

        private EventMessage lastEvent;

        private string recordingPath;

        private string bridgedLegUUID;

        public Channel(OutboundSocket outboundSocket) : this(outboundSocket.ChannelData, outboundSocket)
        {
            outboundSocket.Linger().Wait();
            outboundSocket.SubscribeEvents().Wait();
        }

        public Channel(EventMessage eventMessage, EventSocket eventSocket) : this(eventMessage.UUID, eventMessage, eventSocket)
        {
        }

        protected Channel(string uuid, EventMessage eventMessage, EventSocket eventSocket)
        {
            this.UUID = uuid;
            this.lastEvent = eventMessage;
            this.eventSocket = eventSocket;

            this.disposables.Add(
                eventSocket.Events.Where(x => x.UUID == this.UUID && x.EventName == EventName.ChannelUnbridge).Subscribe(
                    x =>
                        {
                            Log.Debug(
                                () =>
                                "Channel {0} B-Leg {1} hungup {2}".Fmt(this.UUID, this.bridgedLegUUID, x.GetVariable("bridge_hangup_cause")));
                            Log.Trace(() => "Channel {0} Unbridged from {1}".Fmt(this.UUID, this.bridgedLegUUID));
                            this.bridgedLegUUID = null;
                        }));

            this.disposables.Add(
                eventSocket.Events.Where(x => x.UUID == this.UUID && x.EventName == EventName.ChannelBridge).Subscribe(
                    x =>
                        {
                            Console.WriteLine(x.EventName);
                            Log.Trace(() => "Channel {0} Bridged to {1}".Fmt(this.UUID, x.GetHeader(HeaderNames.OtherLegUniqueId)));
                            this.bridgedLegUUID = x.GetHeader(HeaderNames.OtherLegUniqueId);
                        }));

            this.disposables.Add(
                eventSocket.Events.Where(x => x.UUID == this.UUID).Subscribe(
                    e =>
                        {
                            this.lastEvent = e;

                            if (e.EventName == EventName.ChannelHangup)
                            {
                                this.HangupCallBack(e);
                            }
                        }));
        }

        ~Channel()
        {
            this.Dispose(false);
        }

        public string UUID { get; private set; }

        public ChannelState ChannelState
        {
            get
            {
                return this.lastEvent.ChannelState;
            }
        }

        public AnswerState AnswerState
        {
            get
            {
                return this.lastEvent.AnswerState;
            }
        }

        public HangupCause? HangupCause
        {
            get
            {
                return this.lastEvent.HangupCause;
            }
        }

        public Action<EventMessage> HangupCallBack
        {
            get
            {
                return this.hangupCallback;
            }

            set
            {
                this.hangupCallback = value;
            }
        }

        public IObservable<string> Dtmf
        {
            get
            {
                return
                    this.eventSocket.Events.Where(x => x.UUID == this.UUID && x.EventName == EventName.Dtmf)
                        .Select(x => x.Headers[HeaderNames.DtmfDigit]);
            }
        }

        public IObservable<string> FeatureCodes
        {
            get
            {
                return
                    this.eventSocket.Events.Where(
                        x =>
                        x.UUID == this.UUID && x.EventName == EventName.Custom && x.GetHeader(HeaderNames.EventSubclass) == FeatureCodeEvent)
                        .Do(
                            x =>
                            Log.Trace(() => "Channel {0} Detected Feature Code {1}".Fmt(this.UUID, x.GetVariable("last_matching_digits"))))
                        .Select(x => x.GetVariable("last_matching_digits"));
            }
        }

        public bool IsBridged
        {
            get
            {
                return this.bridgedLegUUID != null; // this.lastEvent.Headers[HeaderNames.OtherLegUniqueId] != null;
            }
        }

        public string this[string variableName]
        {
            get
            {
                return this.lastEvent.GetVariable(variableName);
            }
        }

        public async Task Bridge(IChannel other)
        {
            if (this.IsBridged)
            {
                throw new InvalidOperationException("Channel {0} is already bridged to {1}".Fmt(this.UUID, this.bridgedLegUUID));
            }

            if (this.AnswerState != AnswerState.Answered && other.AnswerState != AnswerState.Answered)
            {
                throw new InvalidOperationException("At least one channel must be Answered to bridge them");
            }

            var result = await this.eventSocket.SendApi("uuid_bridge {0} {1}".Fmt(this.UUID, other.UUID));
            if (result.Success)
            {
                this.bridgedLegUUID = other.UUID;

                this.eventSocket.Events.Where(x => x.UUID == this.bridgedLegUUID && x.EventName == EventName.ChannelHangup)
                    .Take(1)
                    .Subscribe(
                        x =>
                            {
                                Log.Debug(() => "Channel {0} B-Leg {1} hungup {2}".Fmt(this.UUID, this.bridgedLegUUID, x.HangupCause));
                                this.bridgedLegUUID = null;
                            });
            }
        }

        public async Task<BridgeResult> Bridge(string destination, BridgeOptions options, Action<EventMessage> onProgress = null)
        {
            Log.Debug(() => "Channel {0} is attempting a bridge to {1}".Fmt(this.UUID, destination));
            if (string.IsNullOrEmpty(options.UUID))
            {
                options.UUID = Guid.NewGuid().ToString();
            }

            var subscriptions = new CompositeDisposable();

            if (onProgress != null)
            {
                // only works on inbound sockets
                subscriptions.Add(
                    this.eventSocket.Events.Where(x => x.UUID == options.UUID && x.EventName == EventName.ChannelProgress)
                        .Take(1)
                        .Subscribe(onProgress));
            }

            var result = await this.eventSocket.Bridge(this.UUID, destination, options);

            subscriptions.Dispose();

            Log.Debug(() => "Channel {0} bridge complete {1} {2}".Fmt(this.UUID, result.Success, result.ResponseText));

            // if (result.Success)
            // {
            // this.bridgedLegUUID = result.BridgeUUID;

            // eventSocket.Events.Where(x => x.UUID == this.UUID && x.EventName == EventName.ChannelUnbridge)
            // .Take(1)
            // .Subscribe(
            // x =>
            // {
            // Log.DebugFormat(
            // "Channel {0} B-Leg {1} hungup {2}",
            // UUID,
            // bridgedLegUUID,
            // x.GetVariable("bridge_hangup_cause"));
            // //this.bridgedLegUUID = null;
            // });
            // }
            return result;
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
            return this.eventSocket.SendApi("uuid_hold toggle " + this.UUID);
        }

        public Task Park()
        {
            return this.eventSocket.ExecuteApplication(this.UUID, "park");
        }

        public Task RingReady()
        {
            return this.eventSocket.ExecuteApplication(this.UUID, "ring_ready");
        }

        public Task Answer()
        {
            return this.eventSocket.ExecuteApplication(this.UUID, "answer");
        }

        public Task Hangup(HangupCause hangupCause = FreeSwitch.HangupCause.NormalClearing)
        {
            return this.eventSocket.SendApi("uuid_kill {0} {1}".Fmt(this.UUID, hangupCause.ToString().ToUpperWithUnderscores()));
        }

        public Task Sleep(int milliseconds)
        {
            return this.eventSocket.ExecuteApplication(this.UUID, "sleep", milliseconds.ToString());
        }

        public async Task PlayFile(string file, Leg leg = Leg.ALeg, string terminator = null)
        {
            if (terminator != null)
            {
                await this.SetChannelVariable("playback_terminators", terminator);
            }

            if (!this.IsBridged)
            {
                await this.eventSocket.Play(this.UUID, file, new PlayOptions());
                return;
            }

            // uuid displace only works on one leg
            switch (leg)
            {
                case Leg.Both:
                    await
                        Task.WhenAll(
                            this.eventSocket.ExecuteApplication(this.UUID, "displace_session", "{0} m{1}".Fmt(file, "w")), 
                            this.eventSocket.ExecuteApplication(this.UUID, "displace_session", "{0} m{1}".Fmt(file, "r")));
                    break;
                case Leg.ALeg:
                    await this.eventSocket.ExecuteApplication(this.UUID, "displace_session", "{0} m{1}".Fmt(file, "w"));
                    break;
                case Leg.BLeg:
                    await this.eventSocket.ExecuteApplication(this.UUID, "displace_session", "{0} m{1}".Fmt(file, "r"));
                    break;
                default:
                    throw new NotSupportedException("Leg {0} is not supported".Fmt(leg));
            }
        }

        public async Task<string> PlayGetDigits(PlayGetDigitsOptions options)
        {
            var result = await this.eventSocket.PlayGetDigits(this.UUID, options);
            return result.Digits;
        }

        public Task Say(SayOptions options)
        {
            return this.eventSocket.Say(this.UUID, options);
        }

        public async Task StartRecording(string file, int? maxSeconds = null)
        {
            if (this.recordingPath != null)
            {
                Log.Warn(
                    () =>
                    "Channel {0} received a request to record to file {1} while currently recording to file {2}. Channel will stop recording and start recording to the new file."
                        .Fmt(this.UUID, file, this.recordingPath));
                await this.StopRecording();
            }

            this.recordingPath = file;
            await this.eventSocket.SendApi("uuid_record {0} start {1} {2}".Fmt(this.UUID, this.recordingPath, maxSeconds));
            Log.Debug(() => "Channel {0} is recording to {1}".Fmt(this.UUID, this.recordingPath));
        }

        public async Task MaskRecording()
        {
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

        public async Task StartDetectingInbandDtmf()
        {
            await this.eventSocket.SubscribeEvents(EventName.Dtmf);
            await this.eventSocket.StartDtmf(this.UUID);
        }

        public Task StopDetectingInbandDtmf()
        {
            return this.eventSocket.Stoptmf(this.UUID);
        }

        public Task SetChannelVariable(string name, string value)
        {
            Log.Debug(() => "Channel {0} setting variable '{1}' to '{2}'".Fmt(this.UUID, name, value));
            return this.eventSocket.SendApi("uuid_setvar {0} {1} {2}".Fmt(this.UUID, name, value));
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
                    if (!this.disposables.IsDisposed)
                    {
                        this.disposables.Dispose();
                    }
                }

                if (this.eventSocket is OutboundSocket)
                {
                    // todo: should we close the socket associated with the channel here?
                    this.eventSocket.Dispose();
                }

                this.disposed = true;
            }
        }
    }
}