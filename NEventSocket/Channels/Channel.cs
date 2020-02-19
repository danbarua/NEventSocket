using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NEventSocket.FreeSwitch;
using NEventSocket.Sockets;
using NEventSocket.Util;

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Channel.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.Channels
{
    public class Channel : BasicChannel
    {
        private readonly InterlockedBoolean initialized = new InterlockedBoolean();
        
        private readonly InterlockedBoolean disposed = new InterlockedBoolean();

        private readonly BehaviorSubject<BridgedChannel> bridgedChannelsSubject = new BehaviorSubject<BridgedChannel>(null);

        private string bridgedUuid;

        protected internal Channel(OutboundSocket outboundSocket) : this(outboundSocket.ChannelData, outboundSocket)
        {
        }

        protected internal Channel(ChannelEvent eventMessage, EventSocket eventSocket) : base(eventMessage, eventSocket)
        {
            LingerTime = 10;
        }

        internal static async Task<Channel> Create(OutboundSocket outboundSocket)
        {
            var channel = new Channel(outboundSocket) { ExitOnHangup = true };
            await outboundSocket.Linger().ConfigureAwait(false);
            await outboundSocket.SubscribeEvents(
               EventName.ChannelProgress,
               EventName.ChannelBridge,
               EventName.ChannelUnbridge,
               EventName.ChannelAnswer,
               EventName.ChannelHangup,
               EventName.ChannelHangupComplete,
               EventName.Dtmf).ConfigureAwait(false); //subscribe to minimum events

            await outboundSocket.Filter(HeaderNames.UniqueId, outboundSocket.ChannelData.UUID).ConfigureAwait(false); //filter for our unique id (in case using full socket mode)
            await outboundSocket.Filter(HeaderNames.OtherLegUniqueId, outboundSocket.ChannelData.UUID).ConfigureAwait(false); //filter for channels bridging to our unique id
            await outboundSocket.Filter(HeaderNames.ChannelCallUniqueId, outboundSocket.ChannelData.UUID).ConfigureAwait(false); //filter for channels bridging to our unique id

            channel.InitializeSubscriptions();
            return channel;
        }

        ~Channel()
        {
            Dispose(false);
        }
        public IObservable<ChannelEvent> Events { get { return Socket.ChannelEvents.Where(x => x.UUID == Uuid).AsObservable(); } }

        public IObservable<BridgedChannel> BridgedChannels { get { return bridgedChannelsSubject.Where(x => x != null).AsObservable(); } }

        public BridgedChannel OtherLeg => bridgedChannelsSubject.Value;

        public bool ExitOnHangup { get; set; }

        public int LingerTime { get; set; }

        public async Task BridgeTo(string destination, BridgeOptions options, Action<EventMessage> onProgress = null)
        {
            if (!IsAnswered && !IsPreAnswered)
            {
                return;
            }

            Log.LogDebug("Channel {0} is attempting a bridge to {1}".Fmt(Uuid, destination));

            if (string.IsNullOrEmpty(options.UUID))
            {
                options.UUID = Guid.NewGuid().ToString();
            }

            var subscriptions = new CompositeDisposable();

            if (onProgress != null)
            {
                subscriptions.Add(
                    eventSocket.ChannelEvents.Where(x => x.UUID == options.UUID && x.EventName == EventName.ChannelProgress)
                        .Take(1)
                        .Subscribe(onProgress));
            }

            var bridgedChannel = this.BridgedChannels.FirstAsync(x => x.Uuid == options.UUID);
            var result = await eventSocket.Bridge(Uuid, destination, options).ConfigureAwait(false);

            Log.LogDebug("Channel {0} bridge complete {1} {2}".Fmt(Uuid, result.Success, result.ResponseText));
            subscriptions.Dispose();

            if (result.Success)
            {
                //wait for this.OtherLeg to be set before completing
                await bridgedChannel;
            }
        }

        public Task Execute(string application, string args)
        {
            return eventSocket.ExecuteApplication(Uuid, application, args);
        }

        public Task Execute(string uuid, string application, string args)
        {
            return eventSocket.ExecuteApplication(uuid, application, args);
        }

        public Task HoldToggle()
        {
            return RunIfAnswered(() => eventSocket.SendApi("uuid_hold toggle " + Uuid));
        }

        public Task HoldOn()
        {
            return RunIfAnswered(() => eventSocket.SendApi("uuid_hold " + Uuid));
        }

        public Task HoldOff()
        {
            return RunIfAnswered(() => eventSocket.SendApi("uuid_hold off " + Uuid));
        }

        public Task Park()
        {
            return RunIfAnswered(() => eventSocket.ExecuteApplication(Uuid, "park"));
        }

        public Task RingReady()
        {
            return eventSocket.ExecuteApplication(Uuid, "ring_ready");
        }

        public Task Answer()
        {
            return eventSocket.ExecuteApplication(Uuid, "answer");
        }

        public Task EnableHeartBeat(int intervalSeconds = 60)
        {
            return RunIfAnswered(
                async () =>
                {
                    await eventSocket.SubscribeEvents(EventName.SessionHeartbeat).ConfigureAwait(false);
                    await eventSocket.ExecuteApplication(Uuid, "enable_heartbeat", intervalSeconds.ToString()).ConfigureAwait(false);
                }, true);
        }

        public Task PreAnswer()
        {
            return eventSocket.ExecuteApplication(Uuid, "pre_answer");
        }

        public Task Sleep(int milliseconds)
        {
            return eventSocket.ExecuteApplication(Uuid, "sleep", milliseconds.ToString());
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed != null && !disposed.EnsureCalledOnce())
            {
                if (disposing)
                {
                    if (!Disposables.IsDisposed)
                    {
                        Disposables.Dispose();
                    }

                    OtherLeg?.Dispose();
                    bridgedChannelsSubject.Dispose();
                }

                if (eventSocket != null && eventSocket is OutboundSocket)
                {
                    // todo: should we close the socket associated with the channel here?
                    eventSocket.Dispose();
                }

                eventSocket = null;

                Log.LogDebug("Channel Disposed.");
            }

            base.Dispose(disposing);
        }
        
        private void InitializeSubscriptions()
        {
            if (initialized.EnsureCalledOnce())
            {
                Log.LogWarning("Channel already initialized");
                return;
            }

            Disposables.Add(
                    eventSocket.ChannelEvents.Where(x => x.UUID == Uuid 
                                            && x.EventName == EventName.ChannelBridge
                                            && x.OtherLegUUID != bridgedUuid)
                    .Subscribe(
                        async x =>
                        {
                            Log.LogInformation("Channel [{0}] Bridged to [{1}] CHANNEL_BRIDGE".Fmt(Uuid, x.GetHeader(HeaderNames.OtherLegUniqueId)));

                            var apiResponse = await eventSocket.Api("uuid_dump", x.OtherLegUUID);

                            if (apiResponse.Success && apiResponse.BodyText != "+OK")
                            {
                                var eventMessage = new ChannelEvent(apiResponse);
                                bridgedChannelsSubject.OnNext(new BridgedChannel(eventMessage, eventSocket));
                            }
                            else
                            {
                                Log.LogError("Unable to get CHANNEL_DATA info from 'api uuid_dump {0}' - received '{1}'.".Fmt(x.OtherLegUUID, apiResponse.BodyText));
                            }
                        }));

            Disposables.Add(
                eventSocket.ChannelEvents.Where(x => 
                                    x.UUID == Uuid 
                                    && x.EventName == EventName.ChannelUnbridge
                                    && x.GetVariable("bridge_hangup_cause") != null)
                           .Subscribe(
                               x =>
                               {
                                   /* side effects:
                                    * the att_xfer application is evil
                                    * if after speaking to C, B presses '#' to cancel,
                                    * the A channel fires an unbridge event, even though it is still bridged to B
                                    * in this case, bridge_hangup_cause will be empty so we'll ignore those events
                                    * however, this may break if this channel has had any completed bridges before this. */

                                   Log.LogInformation(
                                           "Channel [{0}] Unbridged from [{1}] {2}".Fmt(
                                               Uuid,
                                               x.GetVariable("last_bridge_to"),
                                               x.GetVariable("bridge_hangup_cause")));

                                   bridgedChannelsSubject.OnNext(null); //clears out OtherLeg
                               }));

            Disposables.Add(BridgedChannels.Subscribe(
                async b =>
                {
                    if (bridgedUuid != null && bridgedUuid != b.Uuid)
                    {
                        await eventSocket.FilterDelete(HeaderNames.UniqueId, bridgedUuid).ConfigureAwait(false);
                        await eventSocket.FilterDelete(HeaderNames.OtherLegUniqueId, bridgedUuid).ConfigureAwait(false);
                        await eventSocket.FilterDelete(HeaderNames.ChannelCallUniqueId, bridgedUuid).ConfigureAwait(false);
                    }

                    bridgedUuid = b.Uuid;

                    await eventSocket.Filter(HeaderNames.UniqueId, bridgedUuid).ConfigureAwait(false); 
                    await eventSocket.Filter(HeaderNames.OtherLegUniqueId, bridgedUuid).ConfigureAwait(false);
                    await eventSocket.Filter(HeaderNames.ChannelCallUniqueId, bridgedUuid).ConfigureAwait(false);

                    Log.LogTrace("Channel [{0}] setting OtherLeg to [{1}]".Fmt(Uuid, b.Uuid));
                }));

            Disposables.Add(
                eventSocket.ChannelEvents.Where(
                    x =>
                    x.EventName == EventName.ChannelBridge
                    && x.UUID != Uuid
                    && x.GetHeader(HeaderNames.OtherLegUniqueId) == Uuid
                    && x.UUID != bridgedUuid)
                    .Subscribe(
                        x =>
                        {
                            //there is another channel out there that has bridged to us but we didn't get the CHANNEL_BRIDGE event on this channel
                            Log.LogInformation("Channel [{0}] bridged to [{1}]] on CHANNEL_BRIDGE received on other channel".Fmt(Uuid, x.UUID));
                            bridgedChannelsSubject.OnNext(new BridgedChannel(x, eventSocket));
                        }));


            if (eventSocket is OutboundSocket)
            {
                Disposables.Add(
                    eventSocket.ChannelEvents.Where(x => x.UUID == Uuid && x.EventName == EventName.ChannelHangupComplete)
                               .Subscribe(
                                   async e =>
                                   {
                                       if (ExitOnHangup)
                                       {
                                           //give event subscribers time to complete
                                           if (LingerTime > 0)
                                           {
                                               Log.LogDebug("Channel[{0}] will exit in {1} seconds...".Fmt(Uuid, LingerTime));
                                               await Task.Delay(LingerTime * 1000);
                                           }

                                           if (eventSocket != null)
                                           {
                                               Log.LogInformation("Channel [{0}] exiting".Fmt(Uuid));
                                               await eventSocket.Exit().ConfigureAwait(false);
                                           }

                                           Dispose();
                                       }
                                   }));
            }

            Log.LogTrace("Channel [{0}] subscriptions initialized".Fmt(Uuid));
        }
    }
}