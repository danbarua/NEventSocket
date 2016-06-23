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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
    using NEventSocket.Sockets;
    using NEventSocket.Util;
    using NEventSocket.Util.ObjectPooling;

    public abstract class BasicChannel
    {
        protected readonly ILog Log;

        protected readonly CompositeDisposable Disposables = new CompositeDisposable();

        protected EventSocket eventSocket;

        protected EventMessage lastEvent;

        private Action<EventMessage> hangupCallback = (e) => { };

        private readonly InterlockedBoolean disposed = new InterlockedBoolean(false);

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
                                           Dispose();
                                           HangupCallBack(e);
                                       }
                                   }));
        }

        public string UUID { get; protected set; }

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
        
        public EventSocket Socket { get { return eventSocket; } }

        public IDictionary<string,string> Headers { get {  return lastEvent.Headers; } } 

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

        public string GetHeader(string headerName)
        {
            return lastEvent.GetHeader(headerName);
        }

        public string GetVariable(string variableName)
        {
            return lastEvent.GetVariable(variableName);
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

        public Task Hangup(HangupCause hangupCause = FreeSwitch.HangupCause.NormalClearing)
        {
            return
                RunIfAnswered(
                    () =>
                    eventSocket.SendApi("uuid_kill {0} {1}".Fmt(UUID, hangupCause.ToString().ToUpperWithUnderscores())),
                    true);
        }

        public async Task<PlayResult> Play(string file, Leg leg = Leg.ALeg, string terminator = null)
        {
            if (!CanPlayBackAudio)
            {
                return new PlayResult(null);
            }

            if (terminator != null && lastEvent.GetVariable("playback_terminators") != terminator)
            {
                await SetChannelVariable("playback_terminators", terminator).ConfigureAwait(false);
            }

            var bLegUUID = lastEvent.GetHeader(HeaderNames.OtherLegUniqueId);

            if (leg == Leg.ALeg || bLegUUID == null)
            {
                return await eventSocket.Play(UUID, file, new PlayOptions()).ConfigureAwait(false);
            }
            switch (leg)
            {
                case Leg.Both:
                    return (await
                        Task.WhenAll(
                            eventSocket.Play(UUID, file, new PlayOptions()),
                            eventSocket.Play(bLegUUID, file, new PlayOptions()))
                            .ConfigureAwait(false)).First<PlayResult>();
                case Leg.BLeg:
                    return await eventSocket.Play(bLegUUID, file, new PlayOptions()).ConfigureAwait(false);
                default:
                    throw new NotSupportedException("Leg {0} is not supported".Fmt(leg));
            }
        }

        public Task Play(IEnumerable<string> files, Leg leg = Leg.ALeg, string terminator = null)
        {
            var sb = StringBuilderPool.Allocate();
            var first = true;

            sb.Append("file_string://");

            foreach (var file in files)
            {
                if (!first)
                {
                    sb.Append("!");
                }
                sb.Append(file);
                first = false;
            }

            return Play(StringBuilderPool.ReturnAndFree(sb), leg, terminator);
        }

        /// <summary>
        /// Plays the provided audio source to the A-Leg.
        /// Dispose the returned token to cancel playback.
        /// </summary>
        /// <param name="file">The audio source.</param>
        /// <returns>An <seealso cref="IDisposable"/> which can be disposed to stop the audio.</returns>
        public async Task<IDisposable> PlayUntilCancelled(string file)
        {
            if (!CanPlayBackAudio)
            {
                Log.Warn(() => "Channel [{0}] attempted to play hold music when not answered".Fmt(UUID));
                return Task.FromResult(new DisposableAction());
            }

            // essentially, we'll do a playback application call without waiting for the ChannelExecuteComplete event
            // the caller can .Dispose() the returned token to do a uuid_break on the channel to kill audio.
            await eventSocket.SendCommand(string.Format("sendmsg {0}\ncall-command: execute\nexecute-app-name: playback\nexecute-app-arg:{1}\nloops:-1", UUID, file));

            var cancellation = new DisposableAction(
                async () =>
                {
                    if (!CanPlayBackAudio)
                    {
                        return;
                    }

                    try
                    {
                        await eventSocket.Api("uuid_break", UUID);
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorException("Error calling 'api uuid_break {0}'".Fmt(UUID), ex);
                    }
                });

            return cancellation;
        }

        /// Returns true if audio playback is currently possible, false otherwise.
        bool CanPlayBackAudio => IsAnswered || IsPreAnswered;

        public async Task<PlayGetDigitsResult> PlayGetDigits(PlayGetDigitsOptions options)
        {
            if (!IsAnswered)
            {
                return new PlayGetDigitsResult(null, null);
            }

            return await eventSocket.PlayGetDigits(UUID, options).ConfigureAwait(false);
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

                var aLegUUID = lastEvent.Headers[HeaderNames.OtherLegUniqueId];
                var bLegUUID = UUID;

                var events = eventSocket.Events;

                Log.Debug(() => "Att XFer Starting A-Leg [{0}] B-Leg [{1}]".Fmt(aLegUUID, bLegUUID));

                var aLegHangup = events.Where(x => x.EventName == EventName.ChannelHangup && x.UUID == aLegUUID)
                                        .Do(x => Log.Debug(() => "Att XFer Hangup Detected on A-Leg [{0}]".Fmt(x.UUID)));

                var bLegHangup = events.Where(x => x.EventName == EventName.ChannelHangup && x.UUID == bLegUUID)
                                        .Do(x => Log.Debug(() => "Att XFer Hangup Detected on B-Leg [{0}]".Fmt(x.UUID)));

                var cLegHangup = events.Where(x => x.EventName == EventName.ChannelHangup && x.UUID != bLegUUID && x.UUID != aLegUUID)
                                        .Do(x => Log.Debug(() => "Att XFer Hangup Detected on C-Leg[{0}]".Fmt(x.UUID)));

                var cLegAnswer =
                    events.Where(x => x.EventName == EventName.ChannelAnswer && x.UUID != bLegUUID && x.UUID != aLegUUID)
                          .Do(x => Log.Debug(() => "Att XFer Answer Detected on C-Leg [{0}]".Fmt(x.UUID)));

                var bLegUnbridge =
                    events.Where(x => x.EventName == EventName.ChannelUnbridge && x.UUID == bLegUUID)
                          .Do(x => Log.Debug(() => "Att XFer Unbridge Detected on C-Leg [{0}]".Fmt(x.UUID)));

                var cLegUnbridge =
                    events.Where(x => x.EventName == EventName.ChannelUnbridge && x.UUID != bLegUUID && x.UUID != aLegUUID)
                          .Do(x => Log.Debug(() => "Att XFer Unbridge Detected on C-Leg [{0}]".Fmt(x.UUID)));

                var aLegBridge =
                    events.Where(x => x.EventName == EventName.ChannelBridge && x.UUID == aLegUUID)
                          .Do(x => Log.Debug(() => "Att XFer Bridge Detected on A-Leg [{0}]".Fmt(x.UUID)));

                var cLegBridge =
                    events.Where(x => x.EventName == EventName.ChannelBridge && x.UUID != bLegUUID && x.UUID != aLegUUID)
                          .Do(x => Log.Debug(() => "Att XFer Bridge Detected on C-Leg [{0}]".Fmt(x.UUID)));


                var channelExecuteComplete =
                    events.Where(
                        x =>
                            x.EventName == EventName.ChannelExecuteComplete
                            && x.UUID == bLegUUID
                            && x.GetHeader(HeaderNames.Application) == "att_xfer");

                var cFailed = cLegHangup.And(channelExecuteComplete.Where(x => x.GetVariable("originate_disposition") != "SUCCESS"));

                var cAnsweredThenHungUp =
                    cLegAnswer.And(cLegHangup)
                        .And(channelExecuteComplete.Where(
                                x =>
                                    x.GetVariable("att_xfer_result") == "success"
                                    && x.GetVariable("last_bridge_hangup_cause") == "NORMAL_CLEARING"
                                    && x.GetVariable("originate_disposition") == "SUCCESS"));

                var cAnsweredThenBPressedStarOrHungUp =
                    cLegAnswer.And(bLegHangup)
                        .And(cLegBridge.Where(x => x.OtherLegUUID == aLegUUID));

                subscriptions.Add(Observable.When(cFailed.Then((hangup, execComplete) => new { hangup, execComplete }))
                                            .Subscribe(
                                                x =>
                                                {
                                                    Log.Debug(() => "Att Xfer Not Answered");
                                                    tcs.TrySetResult(AttendedTransferResult.Failed(x.execComplete.GetVariable("originate_disposition").HeaderToEnum<HangupCause>()));
                                                }));

                subscriptions.Add(Observable.When(cAnsweredThenHungUp.Then((answer, hangup, execComplete) => new { answer, hangup, execComplete }))
                                            .Subscribe(
                                                x =>
                                                {
                                                    Log.Debug(() => "Att Xfer Rejected after C Hungup");
                                                    tcs.TrySetResult(AttendedTransferResult.Failed(FreeSwitch.HangupCause.NormalClearing));
                                                }));

                subscriptions.Add(channelExecuteComplete.Where(x => !string.IsNullOrEmpty(x.GetVariable("xfer_uuids")))
                                            .Subscribe(x => {
                                                    Log.Debug(() => "Att Xfer Success (threeway)");
                                                    tcs.TrySetResult(AttendedTransferResult.Success(AttendedTransferResultStatus.Threeway));
                                                }));

                subscriptions.Add(Observable.When(cAnsweredThenBPressedStarOrHungUp.Then((answer, hangup, bridge) => new { answer, hangup, bridge }))
                                            .Subscribe(
                                                x =>
                                                {
                                                    Log.Debug(() => "Att Xfer Succeeded after B pressed *");
                                                    tcs.TrySetResult(AttendedTransferResult.Success());
                                                }));

                subscriptions.Add(Observable.When(bLegHangup.And(cLegAnswer).And(aLegBridge.Where(x => x.OtherLegUUID != bLegUUID)).Then((hangup, answer, bridge) => new { answer, hangup, bridge }))
                                            .Subscribe(
                                                x =>
                                                {
                                                    Log.Debug(() => "Att Xfer Succeeded after B hung up and C answered");
                                                    tcs.TrySetResult(AttendedTransferResult.Success());
                                                }));

                subscriptions.Add(aLegHangup.Subscribe(
                    x =>
                    {
                        Log.Debug(() => "Att Xfer Failed after A-Leg Hung Up");
                        tcs.TrySetResult(AttendedTransferResult.Hangup(x));
                    }));

                eventSocket.ExecuteApplication(UUID, "att_xfer", endpoint, false, true)
                           .ContinueOnFaultedOrCancelled(tcs, subscriptions.Dispose);

                return tcs.Task.Then(() => subscriptions.Dispose());
            }
            catch (TaskCanceledException)
            {
                return Task.FromResult(AttendedTransferResult.Failed(FreeSwitch.HangupCause.None));
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
            return RunIfAnswered(() => eventSocket.StopDtmf(UUID));
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
            return eventSocket.ExecuteApplication(UUID, "send_dtmf", "{0}@{1}".Fmt(digits, durationMs));
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
            if (disposed != null && disposed.EnsureCalledOnce())
            {
                if (disposing)
                {
                    if (Disposables != null)
                    {
                        Disposables.Dispose();
                    }
                }

                Log.Debug(() => "BasicChannel Disposed.");
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