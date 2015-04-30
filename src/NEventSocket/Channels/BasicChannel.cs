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
            Dispose(false);
        }

        protected BasicChannel(EventMessage eventMessage, EventSocket eventSocket)
        {
            Log = LogProvider.GetLogger(GetType());

            UUID = eventMessage.UUID;
            lastEvent = eventMessage;
            this.eventSocket = eventSocket;

            Disposables.Add(
                eventSocket.Events
                           .Where(x => x.UUID == UUID)
                           .Subscribe(
                               e =>
                                   {
                                       lastEvent = e;

                                       if (e.EventName == EventName.ChannelAnswer)
                                       {
                                           Log.Info(() => "Channel [{0}] Answered".Fmt(UUID));
                                       }

                                       if (e.EventName == EventName.ChannelHangup)
                                       {
                                           Log.Info(() => "Channel [{0}] Hangup Detected [{1}]".Fmt(UUID, e.HangupCause));
                                           HangupCallBack(e);
                                       }
                                   }));
        }

        public string UUID { get; protected set; }

        public string this[string variableName]
        {
            get
            {
                return lastEvent.GetVariable(variableName);
            }
        }

        public ChannelState ChannelState
        {
            get
            {
                return lastEvent.ChannelState;
            }
        }

        public AnswerState? Answered
        {
            get
            {
                return lastEvent.AnswerState;
            }
        }

        public HangupCause? HangupCause
        {
            get
            {
                return lastEvent.HangupCause;
            }
        }

        public Action<EventMessage> HangupCallBack
        {
            get
            {
                return hangupCallback;
            }

            set
            {
                hangupCallback = value;
            }
        }

        public IObservable<string> Dtmf
        {
            get
            {
                return
                    eventSocket.Events.Where(x => x.UUID == UUID && x.EventName == EventName.Dtmf)
                        .Select(x => x.Headers[HeaderNames.DtmfDigit]);
            }
        }

        public bool IsBridged
        {
            get
            {
                return lastEvent != null && lastEvent.Headers.ContainsKey(HeaderNames.OtherLegUniqueId) && lastEvent.Headers[HeaderNames.OtherLegUniqueId] != null; //this.BridgedChannel != null; // 
            }
        }

        public bool IsAnswered
        {
            get
            {
                return Answered.HasValue && Answered.Value == AnswerState.Answered;
            }
        }

        public bool IsPreAnswered
        {
            get
            {
                return Answered.HasValue && Answered.Value == AnswerState.Early;
            }
        }

        public IObservable<string> FeatureCodes(string prefix = "#")
        {
            return eventSocket
                       .Events.Where(x => x.EventName == EventName.Dtmf && x.UUID == UUID).Select(x => x.Headers[HeaderNames.DtmfDigit])
                       .Buffer(TimeSpan.FromSeconds(2), 2)
                       .Where(x => x.Count == 2 && x[0] == prefix)
                       .Select(x => string.Concat(x))
                       .Do(x => Log.Debug(() => "Channel {0} detected Feature Code {1}".Fmt(UUID, x)));
        }

        public Task<ApiResponse> SendApi(string command)
        {
            return eventSocket.SendApi(command);
        }

        public Task<CommandReply> SendCommand(string command)
        {
            return eventSocket.SendCommand(command);
        }

        public Task Hangup(HangupCause hangupCause = FreeSwitch.HangupCause.NormalClearing)
        {
            return
                RunIfAnswered(
                    () =>
                    eventSocket.SendApi("uuid_kill {0} {1}".Fmt(UUID, hangupCause.ToString().ToUpperWithUnderscores())),
                    true);
        }

        public async Task PlayFile(string file, Leg leg = Leg.ALeg, bool mix = false, string terminator = null)
        {
            if (!IsAnswered)
            {
                return;
            }

            if (terminator != null)
            {
                await SetChannelVariable("playback_terminators", terminator);
            }

            if (leg == Leg.ALeg) //!this.IsBridged)
            {
                await eventSocket.Play(UUID, file, new PlayOptions()).ConfigureAwait(false);
                return;
            }

            // uuid displace only works on one leg
            switch (leg)
            {
                case Leg.Both:
                    await
                        Task.WhenAll(
                        eventSocket.ExecuteApplication(UUID, "displace_session", "{0} {1}{2}".Fmt(file, mix ? "m" : string.Empty, "w"), false, false),
                            eventSocket.ExecuteApplication(UUID, "displace_session", "{0} {1}{2}".Fmt(file, mix ? "m" : string.Empty, "r"), false, false));
                    break;
                case Leg.ALeg:
                    await eventSocket.ExecuteApplication(UUID, "displace_session", "{0} {1}{2}".Fmt(file, mix ? "m" : string.Empty, "w"), false, false).ConfigureAwait(false);
                    break;
                case Leg.BLeg:
                    await eventSocket.ExecuteApplication(UUID, "displace_session", "{0} {1}{2}".Fmt(file, mix ? "m" : string.Empty, "r"), false, false).ConfigureAwait(false);
                    break;
                default:
                    throw new NotSupportedException("Leg {0} is not supported".Fmt(leg));
            }
        }

        public async Task<string> PlayGetDigits(PlayGetDigitsOptions options)
        {
            if (!IsAnswered)
            {
                return string.Empty;
            }

            var result = await eventSocket.PlayGetDigits(UUID, options).ConfigureAwait(false);
            return result.Digits;
        }

        public Task<ReadResult> Read(ReadOptions options)
        {
            if (!IsAnswered)
            {
                return Task.FromResult(new ReadResult(null, null));
            }

            return eventSocket.Read(UUID, options);
        }

        public Task Say(SayOptions options)
        {
            return RunIfAnswered(() => eventSocket.Say(UUID, options));
        }

        /// <summary>
        /// Performs an attended transfer. If succeded, it will replace the Bridged Channel of the other Leg.
        /// </summary>
        /// <remarks>
        /// See https://freeswitch.org/confluence/display/FREESWITCH/Attended+Transfer
        /// </remarks>
        /// <param name="endpoint">The endpoint to transfer to eg. user/1000, sofia/foo@bar.com etc</param>
        public Task<AttendedTransferResult> AttendedTransfer(string endpoint)
        {
            try
            {
                var tcs = new TaskCompletionSource<AttendedTransferResult>();
                var subscriptions = new CompositeDisposable();

                var otherChannel = lastEvent.Headers[HeaderNames.OtherLegUniqueId];

                subscriptions.Add(
                    eventSocket.Events.FirstOrDefaultAsync(x => x.UUID == otherChannel && x.EventName == EventName.ChannelHangup)
                               .Subscribe(x => tcs.TrySetResult(new AttendedTransferResult(null))));

                eventSocket.ExecuteApplication(UUID, "att_xfer", endpoint)
                           .ContinueWithCompleted(tcs, evt => new AttendedTransferResult(evt))
                           .ContinueOnFaultedOrCancelled(tcs, subscriptions.Dispose);

                return tcs.Task;
            }
            catch (TaskCanceledException ex)
            {
                return Task.FromResult(new AttendedTransferResult(null));
            }
        }

        public async Task StartDetectingInbandDtmf()
        {
            if (!IsAnswered)
            {
                return;
            }

            await eventSocket.SubscribeEvents(EventName.Dtmf).ConfigureAwait(false);
            await eventSocket.StartDtmf(UUID).ConfigureAwait(false);
        }

        public Task StopDetectingInbandDtmf()
        {
            return RunIfAnswered(() => eventSocket.Stoptmf(UUID));
        }

        public Task SetChannelVariable(string name, string value)
        {
            if (!IsAnswered)
            {
                return TaskHelper.Completed;
            }

            Log.Debug(() => "Channel {0} setting variable '{1}' to '{2}'".Fmt(UUID, name, value));
            return eventSocket.SendApi("uuid_setvar {0} {1} {2}".Fmt(UUID, name, value));
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
            return this.eventSocket.ExecuteApplication(this.UUID, "send_dtmf", "{0}@{1}".Fmt(digits, durationMs));
        }

        public Task Exit()
        {
            return eventSocket.Exit();
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
                    if (Disposables != null)
                    {
                        Disposables.Dispose();
                    }
                }

                disposed = true;
            }
        }

        /// <summary>
        /// Runs the given async function if the Channel is still connected, otherwise a completed Task.
        /// </summary>
        /// <param name="toRun">An Async function.</param>
        /// <param name="orPreAnswered">Function also run in pre answer state</param>
        protected Task RunIfAnswered(Func<Task> toRun, bool orPreAnswered = false)
        {
            if (!IsAnswered && (!orPreAnswered || !IsPreAnswered))
            {
                return TaskHelper.Completed;
            }

            return toRun();
        }
    }
}