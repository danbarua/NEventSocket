using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ColoredConsole;
using NEventSocket.Examples.NetCore;
using NEventSocket.FreeSwitch;
using NEventSocket.Util;

namespace NEventSocket.Examples.Examples
{
    public class PlayGetDigitsExample : ICommandLineTask, IDisposable
    {
        private InboundSocket client;

        public async Task Run(CancellationToken cancellationToken)
        {
            client = await InboundSocket.Connect("127.0.0.1", 8021, "ClueCon", TimeSpan.FromSeconds(20));

            await client.SubscribeEvents(EventName.Dtmf, EventName.ChannelHangup);

            var originate =
                await
                client.Originate(
                    "user/1000",
                    new OriginateOptions
                    {
                        CallerIdNumber = "123456789",
                        CallerIdName = "Dan Leg A",
                        HangupAfterBridge = false,
                        TimeoutSeconds = 20
                    });

            if (!originate.Success)
            {
                ColorConsole.WriteLine("Originate Failed ".Blue(), originate.HangupCause.ToString());
                await client.Exit();
            }
            else
            {
                ColorConsole.WriteLine("{0} {1} {2}".Fmt(originate.ChannelData.EventName, originate.ChannelData.AnswerState, originate.ChannelData.ChannelState).Blue());
                var uuid = originate.ChannelData.UUID;
                await client.SetChannelVariable(uuid, "dtmf_verbose", "true");
                //await client.StartDtmf(uuid);

                client.ChannelEvents.Where(x => x.UUID == uuid && x.EventName == EventName.ChannelHangup)
                    .Subscribe(
                    e =>
                    {
                        ColorConsole.WriteLine("Hangup Detected on A-Leg ".Red(), e.UUID, e.HangupCause.ToString());
                        client.Exit();
                    });

                client.ChannelEvents.Where(x => x.UUID == uuid && x.EventName == EventName.Dtmf)
                    .Subscribe(e => ColorConsole.WriteLine("DTMF Detected ".Blue(), e.Headers[HeaderNames.DtmfDigit]));

                var playGetDigitsResult = await
                     client.PlayGetDigits(
                         uuid,
                         new PlayGetDigitsOptions()
                         {
                             MinDigits = 4,
                             MaxDigits = 8,
                             MaxTries = 3,
                             TimeoutMs = 4000,
                             TerminatorDigits = "#",
                             PromptAudioFile =
                                     "ivr/ivr-please_enter_pin_followed_by_pound.wav",
                             BadInputAudioFile = "ivr/ivr-that_was_an_invalid_entry.wav",
                             DigitTimeoutMs = 2000,
                         });

                ColorConsole.WriteLine("Got digits: ".Blue(), playGetDigitsResult.Digits);

                if (playGetDigitsResult.Success)
                {
                    await client.Play(uuid, "ivr/ivr-you_entered.wav");
                    await
                        client.Say(
                            uuid,
                            new SayOptions()
                            {
                                Text = playGetDigitsResult.Digits,
                                Type = SayType.Number,
                                Method = SayMethod.Iterated
                            });
                    await
                        client.Play(
                            uuid, "ivr/ivr-you_may_exit_by_hanging_up.wav", new PlayOptions() { Loops = 3 });
                    await client.Hangup(uuid, HangupCause.CallRejected);
                }
            }
        }

        public void Dispose()
        {
            if (client != null) {
                client.Dispose();
                client = null;
            }
        }
    }
}