namespace NEventSocket.FreeSwitch.Channel
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch.Api;
    using NEventSocket.FreeSwitch.Applications;
    using NEventSocket.Sockets;

    public class Channel : IChannel
    {
        private readonly EventSocket eventSocket;
        
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private bool disposed;

        private Action<EventMessage> hangupCallback = (e) => { };

        private EventMessage lastEvent;

        public Channel(EventMessage eventMessage, EventSocket eventSocket)
        {
            lastEvent = eventMessage;
            UUID = eventMessage.UUID;
            ChannelState = eventMessage.ChannelState;
            AnswerState = eventMessage.AnswerState;
            HangupCause = eventMessage.HangupCause;

            this.eventSocket = eventSocket;

            disposables.Add(eventSocket.Events.Where(x => x.UUID == this.UUID).Subscribe(
                e =>
                    {
                        this.lastEvent = e;
                        ChannelState = e.ChannelState;
                        AnswerState = e.AnswerState;
                        HangupCause = e.HangupCause;

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

        public ChannelState ChannelState { get; private set; }

        public AnswerState AnswerState { get; private set; }

        public HangupCause? HangupCause { get; private set; }

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

        public string GetChannelVariable(string variableName)
        {
            return lastEvent.GetVariable(variableName);
        }

        public Task SetChannelVariable(string variableName, string value)
        {
            return eventSocket.SetChannelVariable(UUID, variableName, value);
        }

        public async Task<Channel> Bridge(string destination, BridgeOptions options = null)
        {
            return new Channel((await eventSocket.Bridge(UUID, destination, options)).ChannelData, eventSocket);
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

        public Task PlayFile(string file, Leg leg = Leg.Both, string terminator = null)
        {
            if (terminator != null) this.SetChannelVariable("playback_terminators", terminator);
            return eventSocket.Play(UUID, file, new PlayOptions());
        }

        public Task<string> PlayGetDigits(string file, int numDigits, int timeout)
        {
            throw new NotImplementedException();
        }

        public Task StartRecording(string file, int maxSeconds = 0)
        {
            throw new NotImplementedException();
        }

        public Task MaskRecording()
        {
            throw new NotImplementedException();
        }

        public Task UnmaskRecording()
        {
            throw new NotImplementedException();
        }

        public Task StopRecording()
        {
            throw new NotImplementedException();
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

                disposed = true;
            }
        }
    }
}