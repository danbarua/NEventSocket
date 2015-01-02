// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BasicChannel.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   Defines the BasicChannel type.
// </summary>
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

    public abstract class BasicChannel
    {
        protected readonly ILog Log;

        protected readonly CompositeDisposable Disposables = new CompositeDisposable();

        protected EventSocket eventSocket;

        protected EventMessage lastEvent;

        private Action<EventMessage> hangupCallback = (e) => { };

        private bool disposed;

        ~BasicChannel()
        {
            this.Dispose(false);
        }

        protected BasicChannel(EventMessage eventMessage, EventSocket eventSocket)
        {
            this.Log = LogProvider.GetLogger(this.GetType());

            this.UUID = eventMessage.UUID;
            this.lastEvent = eventMessage;
            this.eventSocket = eventSocket;

            this.Disposables.Add(
                eventSocket.Events
                           .Where(x => x.UUID == this.UUID)
                           .Subscribe(
                               e =>
                                   {
                                       this.lastEvent = e;

                                       if (e.EventName == EventName.ChannelAnswer)
                                       {
                                           this.Log.Info(() => "Channel [{0}] Answered".Fmt(this.UUID));
                                       }

                                       if (e.EventName == EventName.ChannelHangup)
                                       {
                                           this.Log.Info(() => "Channel [{0}] Hangup Detected [{1}]".Fmt(this.UUID, e.HangupCause));
                                           this.HangupCallBack(e);
                                       }
                                   }));
        }

        public string UUID { get; protected set; }

        public string this[string variableName]
        {
            get
            {
                return this.lastEvent.GetVariable(variableName);
            }
        }

        public ChannelState ChannelState
        {
            get
            {
                return this.lastEvent.ChannelState;
            }
        }

        public AnswerState? Answered
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

        public bool IsBridged
        {
            get
            {
                return this.lastEvent != null && this.lastEvent.Headers.ContainsKey(HeaderNames.OtherLegUniqueId) && this.lastEvent.Headers[HeaderNames.OtherLegUniqueId] != null; //this.BridgedChannel != null; // 
            }
        }

        public bool IsAnswered
        {
            get
            {
                return this.Answered.HasValue && this.Answered.Value == AnswerState.Answered;
            }
        }

        public IObservable<string> FeatureCodes(string prefix = "#")
        {
            return this.eventSocket
                       .Events.Where(x => x.EventName == EventName.Dtmf && x.UUID == this.UUID).Select(x => x.Headers[HeaderNames.DtmfDigit])
                       .Buffer(TimeSpan.FromSeconds(2), 2)
                       .Where(x => x.Count == 2 && x[0] == prefix)
                       .Select(x => string.Concat(x))
                       .Do(x => this.Log.Debug(() => "Channel {0} detected Feature Code {1}".Fmt(this.UUID, x)));
        }

        public Task Hangup(HangupCause hangupCause = FreeSwitch.HangupCause.NormalClearing)
        {
            return this.RunIfAnswered(() => this.eventSocket.SendApi("uuid_kill {0} {1}".Fmt(this.UUID, hangupCause.ToString().ToUpperWithUnderscores())));
        }

        public async Task PlayFile(string file, Leg leg = Leg.ALeg, string terminator = null)
        {
            if (!this.IsAnswered)
            {
                return;
            }

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
                            this.eventSocket.ExecuteApplication(this.UUID, "displace_session", "{0} m{1}".Fmt(file, "w"), false, true),
                            this.eventSocket.ExecuteApplication(this.UUID, "displace_session", "{0} m{1}".Fmt(file, "r"), false, true));
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
            if (!this.IsAnswered)
            {
                return string.Empty;
            }

            var result = await this.eventSocket.PlayGetDigits(this.UUID, options);
            return result.Digits;
        }

        public Task<ReadResult> Read(ReadOptions options)
        {
            if (!this.IsAnswered)
            {
                return Task.FromResult(new ReadResult(null, null));
            }

            return this.eventSocket.Read(this.UUID, options);
        }

        public Task Say(SayOptions options)
        {
            return this.RunIfAnswered(() => this.eventSocket.Say(this.UUID, options));
        }

        public async Task AttendedTransfer(string endpoint)
        {
            var result = await eventSocket.ExecuteApplication(UUID, "att_xfer", endpoint);
            Console.WriteLine(result);
            return;
        }

        public async Task StartDetectingInbandDtmf()
        {
            if (!this.IsAnswered)
            {
                return;
            }

            await this.eventSocket.SubscribeEvents(EventName.Dtmf);
            await this.eventSocket.StartDtmf(this.UUID);
        }

        public Task StopDetectingInbandDtmf()
        {
            return this.RunIfAnswered(() => this.eventSocket.Stoptmf(this.UUID));
        }

        public Task SetChannelVariable(string name, string value)
        {
            if (!this.IsAnswered)
            {
                return TaskHelper.Completed;
            }

            this.Log.Debug(() => "Channel {0} setting variable '{1}' to '{2}'".Fmt(this.UUID, name, value));
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
                    if (this.Disposables != null)
                    {
                        this.Disposables.Dispose();
                    }
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Runs the given async function if the Channel is still connected, otherwise a completed Task.
        /// </summary>
        /// <param name="toRun">An Async function.</param>
        protected Task RunIfAnswered(Func<Task> toRun)
        {
            if (!this.IsAnswered)
            {
                return TaskHelper.Completed;
            }

            return toRun();
        }
    }
}