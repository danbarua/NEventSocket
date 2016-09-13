// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Channel.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
    using NEventSocket.Sockets;
    using NEventSocket.Util;

    public class Channel : BasicChannel
    {
        private readonly InterlockedBoolean initialized = new InterlockedBoolean();
        
        private readonly InterlockedBoolean disposed = new InterlockedBoolean();

        private Subject<BridgedChannel> bridgedChannels = new Subject<BridgedChannel>();

        private string bridgedUUID;

        protected internal Channel(OutboundSocket outboundSocket) : this(outboundSocket.ChannelData, outboundSocket)
        {
        }

        protected internal Channel(ChannelEvent eventMessage, EventSocket eventSocket) : base(eventMessage, eventSocket)
        {
            LingerTime = 10;
        }

        internal static async Task<Channel> Create(OutboundSocket outboundSocket)
        {
            var channel = new Channel(outboundSocket);
            
            channel.ExitOnHangup = true;

            await outboundSocket.Linger().ConfigureAwait(false);


            await outboundSocket.SubscribeEvents(
               EventName.ChannelProgress,
               EventName.ChannelBridge,
               EventName.ChannelUnbridge,
               EventName.ChannelAnswer,
               EventName.ChannelHangup,
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
        public IObservable<ChannelEvent> Events { get { return Socket.ChannelEvents.Where(x => x.UUID == UUID).AsObservable(); } }

        public IObservable<BridgedChannel> BridgedChannels { get { return bridgedChannels.AsObservable(); } }

        public BridgedChannel OtherLeg { get; private set; }

        public bool ExitOnHangup { get; set; }

        public int LingerTime { get; set; }

        public async Task BridgeTo(string destination, BridgeOptions options, Action<EventMessage> onProgress = null)
        {
            if (!IsAnswered && !IsPreAnswered)
            {
                return;
            }

            Log.Debug(() => "Channel {0} is attempting a bridge to {1}".Fmt(UUID, destination));

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

            var result = await eventSocket.Bridge(UUID, destination, options).ConfigureAwait(false);

            Log.Debug(() => "Channel {0} bridge complete {1} {2}".Fmt(UUID, result.Success, result.ResponseText));
            subscriptions.Dispose();
        }

        public Task Execute(string application, string args)
        {
            return eventSocket.ExecuteApplication(UUID, application, args);
        }

        public Task Execute(string uuid, string application, string args)
        {
            return eventSocket.ExecuteApplication(uuid, application, args);
        }

        public Task HoldToggle()
        {
            return RunIfAnswered(() => eventSocket.SendApi("uuid_hold toggle " + UUID));
        }

        public Task HoldOn()
        {
            return RunIfAnswered(() => eventSocket.SendApi("uuid_hold " + UUID));
        }

        public Task HoldOff()
        {
            return RunIfAnswered(() => eventSocket.SendApi("uuid_hold off " + UUID));
        }

        public Task Park()
        {
            return RunIfAnswered(() => eventSocket.ExecuteApplication(UUID, "park"));
        }

        public Task RingReady()
        {
            return eventSocket.ExecuteApplication(UUID, "ring_ready");
        }

        public Task Answer()
        {
            return eventSocket.ExecuteApplication(UUID, "answer");
        }

        public Task EnableHeartBeat(int intervalSeconds = 60)
        {
            return RunIfAnswered(
                async () =>
                {
                    await eventSocket.SubscribeEvents(EventName.SessionHeartbeat).ConfigureAwait(false);
                    await eventSocket.ExecuteApplication(UUID, "enable_heartbeat", intervalSeconds.ToString()).ConfigureAwait(false);
                }, true);
        }

        public Task PreAnswer()
        {
            return eventSocket.ExecuteApplication(UUID, "pre_answer");
        }

        public Task Sleep(int milliseconds)
        {
            return eventSocket.ExecuteApplication(UUID, "sleep", milliseconds.ToString());
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

                    if (bridgedChannels != null)
                    {
                        bridgedChannels.Dispose();
                        bridgedChannels = null;
                    }

                    if (OtherLeg != null)
                    {
                        OtherLeg.Dispose();
                        OtherLeg = null;
                    }
                }

                if (eventSocket != null && eventSocket is OutboundSocket)
                {
                    // todo: should we close the socket associated with the channel here?
                    eventSocket.Dispose();
                }

                eventSocket = null;

                Log.Debug(() => "Channel Disposed.");
            }

            base.Dispose(disposing);
        }
        
        private void InitializeSubscriptions()
        {
            if (initialized.EnsureCalledOnce())
            {
                Log.Warn(() => "Channel already initialized");
                return;
            }

            Disposables.Add(
                    eventSocket.ChannelEvents.Where(x => x.UUID == UUID 
                                            && x.EventName == EventName.ChannelBridge
                                            && x.OtherLegUUID != bridgedUUID)
                    .Subscribe(
                        async x =>
                        {
                            Log.Info(() => "Channel [{0}] Bridged to [{1}] CHANNEL_BRIDGE".Fmt(UUID, x.GetHeader(HeaderNames.OtherLegUniqueId)));

                            var apiResponse = await eventSocket.Api("uuid_dump", x.OtherLegUUID);

                            if (apiResponse.Success && apiResponse.BodyText != "+OK")
                            {
                                var eventMessage = new ChannelEvent(apiResponse);
                                bridgedChannels.OnNext(new BridgedChannel(eventMessage, eventSocket));
                            }
                            else
                            {
                                Log.Error(() => "Unable to get CHANNEL_DATA info from 'api uuid_dump {0}' - received '{1}'.".Fmt(x.OtherLegUUID, apiResponse.BodyText));
                            }
                        }));

            Disposables.Add(
                eventSocket.ChannelEvents.Where(x => x.UUID == UUID && x.EventName == EventName.ChannelUnbridge)
                           .Subscribe(x =>Log.Info(() =>"Channel [{0}] Unbridged from [{1}] {2}".Fmt(UUID, x.GetVariable("last_bridge_to"), x.GetVariable("bridge_hangup_cause")))));

            Disposables.Add(bridgedChannels.Subscribe(
                async b =>
                {
                    if (bridgedUUID != null && bridgedUUID != b.UUID)
                    {
                        await eventSocket.FilterDelete(HeaderNames.UniqueId, bridgedUUID).ConfigureAwait(false);
                        await eventSocket.FilterDelete(HeaderNames.OtherLegUniqueId, bridgedUUID).ConfigureAwait(false);
                        await eventSocket.FilterDelete(HeaderNames.ChannelCallUniqueId, bridgedUUID).ConfigureAwait(false);
                    }

                    bridgedUUID = b.UUID;

                    await eventSocket.Filter(HeaderNames.UniqueId, bridgedUUID).ConfigureAwait(false); 
                    await eventSocket.Filter(HeaderNames.OtherLegUniqueId, bridgedUUID).ConfigureAwait(false);
                    await eventSocket.Filter(HeaderNames.ChannelCallUniqueId, bridgedUUID).ConfigureAwait(false);

                    Log.Trace(() => "Channel [{0}] setting OtherLeg to [{1}]".Fmt(UUID, b.UUID));
                    this.OtherLeg = b;
                }));

            Disposables.Add(
                eventSocket.ChannelEvents.Where(
                    x =>
                    x.EventName == EventName.ChannelBridge
                    && x.UUID != UUID
                    && x.GetHeader(HeaderNames.OtherLegUniqueId) == UUID
                    && x.UUID != bridgedUUID)
                    .Subscribe(
                        x =>
                        {
                            //there is another channel out there that has bridged to us but we didn't get the CHANNEL_BRIDGE event on this channel
                            Log.Info(() => "Channel [{0}] bridged to [{1}]] on CHANNEL_BRIDGE received on other channel".Fmt(UUID, x.UUID));
                            bridgedChannels.OnNext(new BridgedChannel(x, eventSocket));
                        }));


            if (eventSocket is OutboundSocket)
            {
                Disposables.Add(
                    eventSocket.ChannelEvents.Where(x => x.UUID == UUID && x.EventName == EventName.ChannelHangup)
                               .Subscribe(
                                   async e =>
                                   {
                                       if (ExitOnHangup)
                                       {
                                           //give event subscribers time to complete
                                           if (LingerTime > 0)
                                           {
                                               await Task.Delay(LingerTime * 1000);
                                           }

                                           if (eventSocket != null)
                                           {
                                               Log.Info(() => "Channel [{0}] exiting".Fmt(UUID));
                                               await eventSocket.Exit().ConfigureAwait(false);
                                           }

                                           Dispose();
                                       }
                                   }));
            }

            Log.Trace(() => "Channel [{0}] subscriptions initialized".Fmt(UUID));
        }
    }
}