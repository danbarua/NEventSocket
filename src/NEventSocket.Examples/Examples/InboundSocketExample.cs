namespace NEventSocket.Examples.Examples
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Net.CommandLine;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Util;

    public class InboundSocketExample : ICommandLineTask, IDisposable
    {
        private InboundSocket client;

        public async Task Run(CancellationToken cancellationToken)
        {
            client = await InboundSocket.Connect();
            Console.WriteLine("Authenticated!");

            try
            {
                await client.SubscribeEvents(EventName.Dtmf);

                var originate =
                    await
                        client.Originate(

                            "user/1000",
                            new OriginateOptions
                            {
                                CallerIdNumber = "123456789",
                                CallerIdName = "Dan Leg A",
                                HangupAfterBridge = false,
                                TimeoutSeconds = 20,
                            });

                if (!originate.Success)
                {
                    ColorConsole.WriteLine("Originate Failed ".Red(), originate.HangupCause.ToString());
                    await client.Exit();
                }
                else
                {
                    var uuid = originate.ChannelData.Headers[HeaderNames.CallerUniqueId];

                    ColorConsole.WriteLine("Originate success ".Green(), originate.ChannelData.Headers[HeaderNames.AnswerState]);

                    var recordingPath = "{0}.wav".Fmt(uuid);
                    //"c:/temp/recording_{0}.wav".Fmt(uuid); //"$${recordings_dir}/" + "{0}.wav".Fmt(uuid); //"c:/temp/recording_{0}.wav".Fmt(uuid);

                    client.OnHangup(
                        uuid,
                        e =>
                        {
                            ColorConsole.WriteLine(
                                "Hangup Detected on A-Leg ".Red(),
                                e.Headers[HeaderNames.CallerUniqueId],
                                " ",
                                e.Headers[HeaderNames.HangupCause]);

                            client.Exit();
                        });

                    var playResult = await client.Play(uuid, "ivr/8000/ivr-call_being_transferred.wav");

                    var bridgeUUID = Guid.NewGuid().ToString();

                    var ringingHandler =
                        client.Events.Where(x => x.UUID == bridgeUUID && x.EventName == EventName.ChannelProgress)
                            .Take(1)
                            .Subscribe(e => ColorConsole.WriteLine("Progress {0} on {1}".Fmt(e.AnswerState, e.UUID).Blue()));

                    var bridge =
                        await
                            client.Bridge(
                                uuid,
                                "user/1003",
                                new BridgeOptions()
                                {
                                    UUID = bridgeUUID,
                                    TimeoutSeconds = 20,
                                    //CallerIdName = "Dan B Leg",
                                    //CallerIdNumber = "987654321",
                                    //HangupAfterBridge = false,
                                    //IgnoreEarlyMedia = true,
                                    //ContinueOnFail = true,
                                    //RingBack = "tone_stream://${uk-ring};loops=-1",
                                    //ConfirmPrompt = "ivr/8000/ivr-to_accept_press_one.wav",
                                    //ConfirmInvalidPrompt = "ivr/8000/ivr-that_was_an_invalid_entry.wav",
                                    //ConfirmKey = "1234",
                                });

                    if (!bridge.Success)
                    {
                        ringingHandler.Dispose();

                        ColorConsole.WriteLine("Bridge failed ".Red(), bridge.ResponseText);

                        await client.Play(uuid, "ivr/8000/ivr-call_rejected.wav");
                        await client.Hangup(uuid, HangupCause.CallRejected);
                    }
                    else
                    {
                        ColorConsole.WriteLine(
                            "Bridge succeeded from {0} to {1} - {2}".Fmt(bridge.ChannelData.UUID, bridge.BridgeUUID, bridge.ResponseText)
                                .Green());
                        
                        //when b-leg hangs up, play a notification to a-leg
                        client.OnHangup(
                            bridge.BridgeUUID,
                            async e =>
                            {
                                ColorConsole.WriteLine(
                                    "Hangup Detected on B-Leg ".Red(),
                                    e.Headers[HeaderNames.CallerUniqueId],
                                    " ",
                                    e.Headers[HeaderNames.HangupCause]);

                                await client.Play(uuid, "ivr/8000/ivr-you_may_exit_by_hanging_up.wav");
                                await client.Hangup(uuid, HangupCause.NormalClearing);
                            });

                        await client.SetChannelVariable(uuid, "RECORD_ARTIST", "'Opex Hosting Ltd'");
                        await client.SetChannelVariable(uuid, "RECORD_MIN_SEC", 0);
                        await client.SetChannelVariable(uuid, "RECORD_STEREO", "true");

                        var recordingResult = await client.SendApi("uuid_record {0} start {1}".Fmt(uuid, recordingPath));
                        ColorConsole.WriteLine(("Recording... " + recordingResult.Success).Green());

                        if (recordingResult.Success)
                        {
                            client.Events.Where(x => x.UUID == uuid && x.EventName == EventName.Dtmf).Subscribe(
                                async (e) =>
                                {
                                    var dtmf = e.Headers[HeaderNames.DtmfDigit];
                                    switch (dtmf)
                                    {
                                        case "1":
                                            ColorConsole.WriteLine("Mask recording".Green());
                                            await client.SendApi("uuid_record {0} mask {1}".Fmt(uuid, recordingPath));
                                            await
                                                client.ExecuteApplication(
                                                    uuid,
                                                    "displace_session",
                                                    applicationArguments: "{0} m".Fmt("ivr/8000/ivr-recording_paused.wav"));
                                            break;
                                        case "2":
                                            ColorConsole.WriteLine("Unmask recording".Green());
                                            await client.SendApi("uuid_record {0} unmask {1}".Fmt(uuid, recordingPath));
                                            await
                                                client.ExecuteApplication(
                                                    uuid,
                                                    "displace_session",
                                                    applicationArguments: "{0} m".Fmt("ivr/8000/ivr-begin_recording.wav"));
                                            break;
                                        case "3":
                                            ColorConsole.WriteLine("Stop recording".Green());
                                            await client.SendApi("uuid_record {0} stop {1}".Fmt(uuid, recordingPath));
                                            await
                                                client.ExecuteApplication(
                                                    uuid,
                                                    "displace_session",
                                                    applicationArguments: "{0} m".Fmt("ivr/8000/ivr-recording_stopped.wav"));
                                            break;
                                    }
                                });
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                ColorConsole.WriteLine("TaskCancelled - shutting down".OnRed());
                client.Dispose();
            }
            finally
            {
            }

            ColorConsole.WriteLine("Press [Enter] to exit.".Green());
            await Util.WaitForEnterKeyPress(cancellationToken);
        }

        public void Dispose()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
        }
    }
}