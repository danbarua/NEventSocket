using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NEventSocket.FreeSwitch;
using NEventSocket.Logging;
using NEventSocket.Sockets;
using NEventSocket.Util;
using NEventSocket.Util.ObjectPooling;
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
    public abstract class BasicChannel
    {
        private readonly InterlockedBoolean disposed = new InterlockedBoolean();
        private Action<ChannelEvent> hangupCallback = (e) => { };
        private string recordingPath;
        private RecordingStatus recordingStatus = RecordingStatus.NotRecording;

        protected readonly ILogger<BasicChannel> Log;
        protected readonly CompositeDisposable Disposables = new CompositeDisposable();
        protected EventSocket eventSocket;
        protected ChannelEvent lastEvent;

        ~BasicChannel()
        {
            Dispose(false);
        }

        protected BasicChannel(ChannelEvent eventMessage, EventSocket eventSocket)
        {
            Log = Logger.Get<BasicChannel>();

            Uuid = eventMessage.UUID;
            lastEvent = eventMessage;
            this.eventSocket = eventSocket;

            Variables = new ChannelVariables(this);

            Disposables.Add(
                eventSocket.ChannelEvents
                           .Where(x => x.UUID == Uuid)
                           .Subscribe(
                               e =>
                                   {
                                       lastEvent = e;

                                       if (e.EventName == EventName.ChannelAnswer)
                                       {
                                           Log.LogInformation("Channel [{0}] Answered".Fmt(Uuid));
                                       }

                                       if (e.EventName == EventName.ChannelHangupComplete)
                                       {
                                           Log.LogInformation("Channel [{0}] Hangup Detected [{1}]".Fmt(Uuid, e.HangupCause));

                                           try
                                           {
                                               HangupCallBack(e);
                                           }
                                           catch (Exception ex)
                                           {
                                               Log.LogError(ex, "Channel [{0}] error calling hangup callback".Fmt(Uuid));
                                           }
                                           
                                           Dispose();
                                       }
                                   }));
        }

        public string Uuid { get; protected set; }
        // should always be populated, otherwise will throw invalidoperationexception
        // which means we've introduced a b-u-g and are listening to non-channel events
        public ChannelState ChannelState => lastEvent.ChannelState.Value;
        public AnswerState? Answered => lastEvent.AnswerState;
        public HangupCause? HangupCause => lastEvent.HangupCause;
        public RecordingStatus RecordingStatus => recordingStatus;

        public Action<ChannelEvent> HangupCallBack
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
                    eventSocket.ChannelEvents.Where(x => x.UUID == Uuid && x.EventName == EventName.Dtmf)
                        .Select(x => x.Headers[HeaderNames.DtmfDigit]);
            }
        }
        
        public EventSocket Socket => eventSocket;
        public IDictionary<string,string> Headers => lastEvent.Headers;
        public ChannelVariables Variables { get; }
        public bool IsBridged => lastEvent != null && lastEvent.Headers.ContainsKey(HeaderNames.OtherLegUniqueId) && lastEvent.Headers[HeaderNames.OtherLegUniqueId] != null; //this.BridgedChannel != null; // 
        public bool IsAnswered => Answered.HasValue && Answered.Value == AnswerState.Answered;
        public bool IsPreAnswered => Answered.HasValue && Answered.Value == AnswerState.Early;
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
                       .ChannelEvents.Where(x => x.UUID == Uuid && x.EventName == EventName.Dtmf)
                       .Select(x => x.Headers[HeaderNames.DtmfDigit])
                       .Buffer(TimeSpan.FromSeconds(2), 2)
                       .Where(x => x.Count == 2 && x[0] == prefix)
                       .Select(x => string.Concat(x))
                       .Do(x => Log.LogDebug("Channel {0} detected Feature Code {1}".Fmt(Uuid, x)));
        }

        public Task Hangup(HangupCause hangupCause = FreeSwitch.HangupCause.NormalClearing)
        {
            return
                RunIfAnswered(
                    () =>
                    eventSocket.SendApi("uuid_kill {0} {1}".Fmt(Uuid, hangupCause.ToString().ToUpperWithUnderscores())),
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

            var bLegUuid = lastEvent.GetHeader(HeaderNames.OtherLegUniqueId);

            if (leg == Leg.ALeg || bLegUuid == null)
            {
                return await eventSocket.Play(Uuid, file, new PlayOptions()).ConfigureAwait(false);
            }
            switch (leg)
            {
                case Leg.Both:
                    return (await
                        Task.WhenAll(
                            eventSocket.Play(Uuid, file, new PlayOptions()),
                            eventSocket.Play(bLegUuid, file, new PlayOptions()))
                            .ConfigureAwait(false)).First();
                case Leg.BLeg:
                    return await eventSocket.Play(bLegUuid, file, new PlayOptions()).ConfigureAwait(false);
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
                Log.LogWarning("Channel [{0}] attempted to play hold music when not answered".Fmt(Uuid));
                return Task.FromResult(new DisposableAction());
            }

            // essentially, we'll do a playback application call without waiting for the ChannelExecuteComplete event
            // the caller can .Dispose() the returned token to do a uuid_break on the channel to kill audio.
            await eventSocket.SendCommand(
                $"sendmsg {Uuid}\ncall-command: execute\nexecute-app-name: playback\nexecute-app-arg:{file}\nloops:-1");

            var cancellation = new DisposableAction(
                async () =>
                {
                    if (!CanPlayBackAudio)
                    {
                        return;
                    }

                    try
                    {
                        await eventSocket.Api("uuid_break", Uuid);
                    }
                    catch (Exception ex)
                    {
                        Log.LogError(ex,"Error calling 'api uuid_break {0}'".Fmt(Uuid));
                    }
                });

            return cancellation;
        }

        /// Returns true if audio playback is currently possible, false otherwise.
        bool CanPlayBackAudio => (IsAnswered || IsPreAnswered) && Socket?.IsConnected == true;

        public Task<PlayGetDigitsResult> PlayGetDigits(PlayGetDigitsOptions options)
        {
            return RunIfAnswered(() => eventSocket.PlayGetDigits(Uuid, options), () => new PlayGetDigitsResult(null, null));
        }

        public Task<ReadResult> Read(ReadOptions options)
        {
            return RunIfAnswered(() => eventSocket.Read(Uuid, options), () => new ReadResult(null, null));
        }

        public Task Say(SayOptions options)
        {
            return RunIfAnswered(() => eventSocket.Say(Uuid, options));
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

                var aLegUuid = lastEvent.Headers[HeaderNames.OtherLegUniqueId];
                var bLegUuid = Uuid;

                var events = eventSocket.ChannelEvents;

                Log.LogDebug("Att XFer Starting A-Leg [{0}] B-Leg [{1}]".Fmt(aLegUuid, bLegUuid));

                var aLegHangup = events.Where(x => x.EventName == EventName.ChannelHangup && x.UUID == aLegUuid)
                                        .Do(x => Log.LogDebug( "Att XFer Hangup Detected on A-Leg [{0}]".Fmt(x.UUID)));

                var bLegHangup = events.Where(x => x.EventName == EventName.ChannelHangup && x.UUID == bLegUuid)
                                        .Do(x => Log.LogDebug("Att XFer Hangup Detected on B-Leg [{0}]".Fmt(x.UUID)));

                var cLegHangup = events.Where(x => x.EventName == EventName.ChannelHangup && x.UUID != bLegUuid && x.UUID != aLegUuid)
                                        .Do(x => Log.LogDebug("Att XFer Hangup Detected on C-Leg[{0}]".Fmt(x.UUID)));

                var cLegAnswer =
                    events.Where(x => x.EventName == EventName.ChannelAnswer && x.UUID != bLegUuid && x.UUID != aLegUuid)
                          .Do(x => Log.LogDebug("Att XFer Answer Detected on C-Leg [{0}]".Fmt(x.UUID)));

                var aLegBridge =
                    events.Where(x => x.EventName == EventName.ChannelBridge && x.UUID == aLegUuid)
                          .Do(x => Log.LogDebug("Att XFer Bridge Detected on A-Leg [{0}]".Fmt(x.UUID)));

                var cLegBridge =
                    events.Where(x => x.EventName == EventName.ChannelBridge && x.UUID != bLegUuid && x.UUID != aLegUuid)
                          .Do(x => Log.LogDebug("Att XFer Bridge Detected on C-Leg [{0}]".Fmt(x.UUID)));


                var channelExecuteComplete =
                    events.Where(
                        x =>
                            x.EventName == EventName.ChannelExecuteComplete
                            && x.UUID == bLegUuid
                            && x.GetHeader(HeaderNames.Application) == "att_xfer");


                var cAnsweredThenHungUp =
                    cLegAnswer.And(cLegHangup)
                        .And(channelExecuteComplete.Where(
                                x =>
                                    x.GetVariable("att_xfer_result") == "success"
                                    && x.GetVariable("last_bridge_hangup_cause") == "NORMAL_CLEARING"
                                    && x.GetVariable("originate_disposition") == "SUCCESS"));

                var cAnsweredThenBPressedStarOrHungUp =
                    cLegAnswer.And(bLegHangup)
                        .And(cLegBridge.Where(x => x.OtherLegUUID == aLegUuid));

                subscriptions.Add(channelExecuteComplete.Where(x => x.GetVariable("originate_disposition") != "SUCCESS")
                    .Subscribe(
                        x =>
                        {
                            Log.LogDebug("Att Xfer Not Answered");
                            tcs.TrySetResult(AttendedTransferResult.Failed(x.GetVariable("originate_disposition").HeaderToEnum<HangupCause>()));

                        }));

                subscriptions.Add(Observable.When(cAnsweredThenHungUp.Then((answer, hangup, execComplete) => new { answer, hangup, execComplete }))
                                            .Subscribe(
                                                x =>
                                                {
                                                    Log.LogDebug("Att Xfer Rejected after C Hungup");
                                                    tcs.TrySetResult(AttendedTransferResult.Failed(FreeSwitch.HangupCause.NormalClearing));
                                                }));

                subscriptions.Add(channelExecuteComplete.Where(x => !string.IsNullOrEmpty(x.GetVariable("xfer_uuids")))
                                            .Subscribe(x => {
                                                    Log.LogDebug("Att Xfer Success (threeway)");
                                                    tcs.TrySetResult(AttendedTransferResult.Success(AttendedTransferResultStatus.Threeway));
                                                }));

                subscriptions.Add(Observable.When(cAnsweredThenBPressedStarOrHungUp.Then((answer, hangup, bridge) => new { answer, hangup, bridge }))
                                            .Subscribe(
                                                x =>
                                                {
                                                    Log.LogDebug("Att Xfer Succeeded after B pressed *");
                                                    tcs.TrySetResult(AttendedTransferResult.Success());
                                                }));

                subscriptions.Add(Observable.When(bLegHangup.And(cLegAnswer).And(aLegBridge.Where(x => x.OtherLegUUID != bLegUuid)).Then((hangup, answer, bridge) => new { answer, hangup, bridge }))
                                            .Subscribe(
                                                x =>
                                                {
                                                    Log.LogDebug("Att Xfer Succeeded after B hung up and C answered");
                                                    tcs.TrySetResult(AttendedTransferResult.Success());
                                                }));

                subscriptions.Add(aLegHangup.Subscribe(
                    x =>
                    {
                        Log.LogDebug("Att Xfer Failed after A-Leg Hung Up");
                        tcs.TrySetResult(AttendedTransferResult.Hangup(x));
                    }));

                eventSocket.ExecuteApplication(Uuid, "att_xfer", endpoint, false, true)
                           .ContinueOnFaultedOrCancelled(tcs, subscriptions.Dispose);

                return tcs.Task.Then(() => subscriptions.Dispose());
            }
            catch (TaskCanceledException)
            {
                return Task.FromResult(AttendedTransferResult.Failed(FreeSwitch.HangupCause.None));
            }
        }

        public Task StartDetectingInbandDtmf()
        {
            return RunIfAnswered(
                async () =>
                {
                    await eventSocket.SubscribeEvents(EventName.Dtmf).ConfigureAwait(false);
                    await eventSocket.StartDtmf(Uuid).ConfigureAwait(false);
                });
        }

        public Task StopDetectingInbandDtmf()
        {
            return RunIfAnswered(() => eventSocket.StopDtmf(Uuid));
        }

        public Task SetChannelVariable(string name, string value)
        {
            return RunIfAnswered(
                () =>
                {
                    Log.LogDebug("Channel {0} setting variable '{1}' to '{2}'".Fmt(Uuid, name, value));
                    return eventSocket.SendApi("uuid_setvar {0} {1} {2}".Fmt(Uuid, name, value));
                });
        }

        /// <summary>
        /// Send DTMF digits to the channel
        /// </summary>
        /// <param name="digits">String with digits or characters</param>
        /// <param name="duration">Duration of each symbol (default -- 2000ms)</param>
        /// <returns></returns>
        public Task SendDTMF(string digits, TimeSpan? duration = null)
        {
            var durationMs = duration?.TotalMilliseconds ?? 2000; // default value in freeswitch
            return eventSocket.ExecuteApplication(Uuid, "send_dtmf", "{0}@{1}".Fmt(digits, durationMs));
        }

        public Task StartRecording(string file, int? maxSeconds = null)
        {
            return RunIfAnswered(
                async () =>
                {
                    if (file == recordingPath)
                    {
                        return;
                    }

                    if (recordingPath != null)
                    {
                        Log.LogWarning(
                                "Channel {0} received a request to record to file {1} while currently recording to file {2}. Channel will stop recording and start recording to the new file."
                                    .Fmt(Uuid, file, recordingPath));
                        await StopRecording().ConfigureAwait(false);
                    }

                    recordingPath = file;
                    await eventSocket.SendApi("uuid_record {0} start {1} {2}".Fmt(Uuid, recordingPath, maxSeconds)).ConfigureAwait(false);
                    Log.LogDebug("Channel {0} is recording to {1}".Fmt(Uuid, recordingPath));
                    recordingStatus = RecordingStatus.Recording;
                });
        }

        public Task MaskRecording()
        {
            return RunIfAnswered(
                async () =>
                {
                    if (string.IsNullOrEmpty(recordingPath))
                    {
                        Log.LogWarning("Channel {0} is not recording".Fmt(Uuid));
                    }
                    else
                    {
                        await eventSocket.SendApi("uuid_record {0} mask {1}".Fmt(Uuid, recordingPath)).ConfigureAwait(false);
                        Log.LogDebug("Channel {0} has masked recording to {1}".Fmt(Uuid, recordingPath));
                        recordingStatus = RecordingStatus.Paused;
                    }
                });
        }

        public Task UnmaskRecording()
        {
            return RunIfAnswered(
                async () =>
                {
                    if (string.IsNullOrEmpty(recordingPath))
                    {
                        Log.LogWarning("Channel {0} is not recording".Fmt(Uuid));
                    }
                    else
                    {
                        await eventSocket.SendApi("uuid_record {0} unmask {1}".Fmt(Uuid, recordingPath)).ConfigureAwait(false);
                        Log.LogDebug("Channel {0} has unmasked recording to {1}".Fmt(Uuid, recordingPath));
                        recordingStatus = RecordingStatus.Recording;
                    }
                });
        }

        public Task StopRecording()
        {
            return RunIfAnswered(
                async () =>
                {
                    if (string.IsNullOrEmpty(recordingPath))
                    {
                        Log.LogWarning("Channel {0} is not recording".Fmt(Uuid));
                    }
                    else
                    {
                        await eventSocket.SendApi("uuid_record {0} stop {1}".Fmt(Uuid, recordingPath)).ConfigureAwait(false);
                        recordingPath = null;
                        Log.LogDebug("Channel {0} has stopped recording to {1}".Fmt(Uuid, recordingPath));
                        recordingStatus = RecordingStatus.NotRecording;
                    }
                });
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

                Log.LogDebug("BasicChannel Disposed.");
            }
        }

        /// <summary>
        /// Runs the given async function if the Channel is still connected, otherwise a completed Task.
        /// </summary>
        /// <param name="toRun">An Async function.</param>
        /// <param name="orPreAnswered">Function also run in pre answer state</param>
        protected Task RunIfAnswered(Func<Task> toRun, bool orPreAnswered = false)
        {
            //check not disposed, socket is not null and connected, no hangup event received
            if (disposed.Value || !eventSocket.IsConnected || !IsAnswered && (!orPreAnswered || !IsPreAnswered))
            {
                return TaskHelper.Completed;
            }

            return toRun();
        }

        /// <summary>
        /// Runs the given async function if the Channel is still connected, otherwise uses the provided function to return a default value.
        /// </summary>
        /// <param name="toRun">An Async function.</param>
        /// <param name="defaultValueProvider">Function returning the default value</param>
        protected Task<T> RunIfAnswered<T>(Func<Task<T>> toRun, Func<T> defaultValueProvider)
        {
            if (disposed.Value || !eventSocket.IsConnected || !IsAnswered)
            {
                return Task.FromResult(defaultValueProvider());
            }

            return toRun();
        }
    }
}

