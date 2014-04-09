// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Channel.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   Defines the Channel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch.Channel
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Common.Logging;

    using NEventSocket.FreeSwitch.Api;
    using NEventSocket.FreeSwitch.Applications;
    using NEventSocket.Sockets;
    using NEventSocket.Util;

    public class Channel : IChannel
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly EventSocket eventSocket;
        
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private bool disposed;

        private Action<EventMessage> hangupCallback = (e) => { };

        private EventMessage lastEvent;

        private string recordingPath;

        private string bridgedLegUUID;

        public Channel(EventMessage eventMessage, EventSocket eventSocket) : this(eventMessage.UUID, eventMessage, eventSocket)
        {
        }

        protected Channel(string uuid, EventMessage eventMessage, EventSocket eventSocket)
        {
            this.UUID = uuid;
            this.lastEvent = eventMessage;
            this.eventSocket = eventSocket;

            disposables.Add(eventSocket.Events.Where(x => x.UUID == this.UUID).Subscribe(
                e =>
                {
                    this.lastEvent = e;

                    if (e.EventName == EventName.ChannelHangup)
                    {
                        HangupCallBack(e);
                    }
                }));
        }

        ~Channel()
        {
            Dispose(false);
        }

        public string UUID { get; private set; }

        public ChannelState ChannelState { get { return lastEvent.ChannelState; } }

        public AnswerState AnswerState { get { return lastEvent.AnswerState; } }

        public HangupCause? HangupCause { get { return lastEvent.HangupCause; } }

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

        public bool IsBridged
        {
            get
            {
                return this.bridgedLegUUID != null; //this.lastEvent.Headers[HeaderNames.OtherLegUniqueId] != null;
            }
        }

        public string this[string variableName]
        {
            get
            {
                return this.GetChannelVariable(variableName);
            }

            set
            {
                this.SetChannelVariable(variableName, value);
            }
        }

        public async Task<BridgeResult> Bridge(string destination, BridgeOptions options, Action<EventMessage> onProgress = null)
        {
            Log.DebugFormat("Channel {0} is attempting a bridge to {1}", UUID, destination);
            if (string.IsNullOrEmpty(options.UUID)) options.UUID = Guid.NewGuid().ToString();

            var subscriptions = new CompositeDisposable();

            if (onProgress != null)
            {
                subscriptions.Add(
                    eventSocket.Events.Where(x => x.UUID == options.UUID && x.EventName == EventName.ChannelProgress)
                        .Take(1)
                        .Subscribe(onProgress));
            }
            
            var result = await eventSocket.Bridge(UUID, destination, options);

            subscriptions.Dispose();

            Log.DebugFormat("Channel {0} bridge complete {1} {2}", UUID, result.Success, result.ResponseText);

            if (result.Success)
            {
                this.bridgedLegUUID = result.BridgeUUID;

                eventSocket.Events.Where(x => x.UUID == this.UUID && x.EventName == EventName.ChannelUnbridge)
                           .Take(1)
                           .Subscribe(x => this.bridgedLegUUID = null);
            }
            
            return result;
        }

        public Task Hold()
        {
            return eventSocket.Execute(UUID, "hold");
        }

        public Task Park()
        {
            return eventSocket.Execute(UUID, "park");
        }

        public Task RingReady()
        {
            return eventSocket.Execute(UUID, "ring_ready");
        }

        public Task Answer()
        {
            return eventSocket.Execute(UUID, "answer");
        }

        public Task Hangup(HangupCause hangupCause = FreeSwitch.HangupCause.NormalClearing)
        {
            return eventSocket.Hangup(UUID, hangupCause);
        }

        public Task Sleep(int milliseconds)
        {
            return eventSocket.Execute(UUID, "sleep", milliseconds.ToString());
        }

        public Task PlayFile(string file, Leg leg = Leg.ALeg, string terminator = null)
        {
            if (terminator != null) this["playback_terminators"] = terminator;

            if (!IsBridged)
            {
                return eventSocket.Play(UUID, file, new PlayOptions());
            }

            string whichLeg;

            switch (leg)
            {
                case Leg.Both:
                    whichLeg = "m"; //mux
                    break;
                case Leg.ALeg:
                    whichLeg = "w"; //write
                    break;
                case Leg.BLeg:
                    whichLeg = "r"; //read
                    break;
                default:
                    throw new NotSupportedException("Leg {0} is not supported".Fmt(leg));
            }

            string args = "{0} {1}".Fmt(file, whichLeg);
            Log.TraceFormat("Channel {0} calling displace_session with args '{1}'", this.UUID, args);
            return this.eventSocket.Execute(this.UUID, "displace_session", args);
        }

        public async Task<string> PlayGetDigits(PlayGetDigitsOptions options)
        {
            var result = await eventSocket.PlayGetDigits(UUID, options);
            return result.Digits;
        }

        public Task Say(SayOptions options)
        {
            return eventSocket.Say(UUID, options);
        }

        public async Task StartRecording(string file, int maxSeconds = 0)
        {
            if (this.recordingPath != file)
            {
                Log.WarnFormat(
                    "Channel {0} received a request to record to file {1} while currently recording to file {2}. Channel will stop recording and start recording to the new file.", UUID, recordingPath, file);
                await this.StopRecording();
            }

            this.recordingPath = file;
            await eventSocket.Api("uuid_record {0} start {1}".Fmt(UUID, recordingPath));
            Log.DebugFormat("Channel {0} is recording to {1}", UUID, recordingPath);
        }

        public async Task MaskRecording()
        {
            if (string.IsNullOrEmpty(recordingPath))
            {
                Log.WarnFormat("Channel {0} is not recording", UUID);
            }
            else
            {
                await eventSocket.Api("uuid_record {0} mask {1}".Fmt(UUID, recordingPath));
                Log.DebugFormat("Channel {0} has masked recording to {1}", UUID, recordingPath);
            }
        }

        public async Task UnmaskRecording()
        {
            if (string.IsNullOrEmpty(recordingPath))
            {
                Log.WarnFormat("Channel {0} is not recording", UUID);
            }
            else
            {
                await eventSocket.Api("uuid_record {0} unmask {1}".Fmt(UUID, recordingPath));
                Log.DebugFormat("Channel {0} has unmasked recording to {1}", UUID, recordingPath);
            }
        }

        public async Task StopRecording()
        {
            if (string.IsNullOrEmpty(recordingPath))
            {
                Log.WarnFormat("Channel {0} is not recording", UUID);
            }
            else
            {
                await eventSocket.Api("uuid_record {0} stop {1}".Fmt(UUID, recordingPath));
                this.recordingPath = null;
                Log.DebugFormat("Channel {0} has stopped recording to {1}", UUID, recordingPath);
            }
        }

        public void Dispose()
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
                    if (!disposables.IsDisposed)
                    {
                        disposables.Dispose();
                    }
                }

                if (eventSocket is OutboundSocket)
                {
                    //todo: should we close the socket associated with the channel here?
                    eventSocket.Dispose();
                }

                disposed = true;
            }
        }

        private string GetChannelVariable(string variableName)
        {
            return lastEvent.GetVariable(variableName);
        }

        private Task SetChannelVariable(string variableName, string value)
        {
            Log.DebugFormat("Channel {0} setting variable '{1}' to '{2}'", UUID, variableName, value);
            return eventSocket.SetChannelVariable(UUID, variableName, value).Then((result) => lastEvent = result);
        }
    }
}