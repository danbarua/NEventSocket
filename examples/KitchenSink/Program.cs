namespace NEventSocket.Example
{
    using System;
    using System.Globalization;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using ColoredConsole;

    using NEventSocket.Channels;
    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
    using NEventSocket.Logging.LogProviders;
    using NEventSocket.Util;

    internal class Program
    {
        private static void Main(string[] args)
        {
            // set logger factory
            LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider());
            ColouredConsoleLogProvider.MessageFormatter = (loggerName, level, message, e) =>
                {
                    var stringBuilder = new StringBuilder();

                    stringBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture));

                    stringBuilder.Append(" ");

                    stringBuilder.Append("TID[" + Thread.CurrentThread.ManagedThreadId + "] ");

                    // Append a readable representation of the log level
                    stringBuilder.Append(("[" + level.ToString().ToUpper() + "]").PadRight(8));

                    stringBuilder.Append("(" + loggerName + ") ");

                    // Append the message
                    stringBuilder.Append(message);

                    // Append stack trace if there is an exception
                    if (e != null)
                    {
                        stringBuilder.Append(Environment.NewLine).Append(e.GetType());
                        stringBuilder.Append(Environment.NewLine).Append(e.Message);
                        stringBuilder.Append(Environment.NewLine).Append(e.StackTrace);
                    }

                    return stringBuilder.ToString();
                };

            ColorConsole.WriteLine("Starting...".Green());

            ApiTest();

            //InboundSocketTest();

            TaskScheduler.UnobservedTaskException += (o, e) => Console.WriteLine("unobserved " + e.Exception);

            ColorConsole.WriteLine("Press [Enter] to exit.".Green());
            Console.ReadLine();
        }

        private static async void ApiTest()
        {
            using (var client = await InboundSocket.Connect("localhost", 8021, "ClueCon"))
            {
                ColorConsole.WriteLine((await client.SendApi("status")).BodyText.DarkBlue());
                ColorConsole.WriteLine((await client.SendApi("blah")).BodyText.DarkBlue());
                ColorConsole.WriteLine((await client.SendApi("status")).BodyText.DarkBlue());
            }
        }

        private static async void CallTracking()
        {
            var client = await InboundSocket.Connect("10.10.10.36", 8021, "ClueCon");

            string uuid = null;

            client.Events.Where(x => x.EventName == EventName.ChannelAnswer)
                  .Subscribe(x =>
                      {
                          uuid = x.UUID;
                          ColorConsole.WriteLine("Channel Answer Event ".Blue(), x.UUID);
                      });

            client.Events.Where(x => x.EventName == EventName.ChannelHangup)
                  .Subscribe(x =>
                      {
                          uuid = null;
                          ColorConsole.WriteLine("Channel Hangup Event ".Blue(), x.UUID);
                      });

            ColorConsole.WriteLine("Press enter to hang up the current call".Green());
            Console.ReadLine();

            if (uuid != null)
            {
                ColorConsole.WriteLine("Hanging up ".Green(), uuid);
                await client.Play(uuid, "ivr/8000/ivr-call_rejected.wav");
                await client.Hangup(uuid, HangupCause.CallRejected);
            }

            client.Exit();
        }

        private static async Task PlayGetDigitsTest()
        {
            var client = await InboundSocket.Connect("10.10.10.36", 8021, "ClueCon");
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
                            TimeoutSeconds = 20
                        });

            if (!originate.Success)
            {
                ColorConsole.WriteLine("Originate Failed ".Blue(), originate.HangupCause.ToString());
                client.Exit();
            }
            else
            {
                ColorConsole.WriteLine("{0} {1} {2}".Fmt(originate.ChannelData.EventName, originate.ChannelData.AnswerState, originate.ChannelData.ChannelState).Blue());
                var uuid = originate.ChannelData.UUID;
                await client.SetChannelVariable(uuid, "dtmf_verbose", "true");
                await client.StartDtmf(uuid);

                client.Events.Where(x => x.UUID == uuid && x.EventName == EventName.ChannelHangup)
                    .Subscribe(
                    e =>
                        {
                              ColorConsole.WriteLine("Hangup Detected on A-Leg ".Red(), e.UUID, e.HangupCause.ToString());
                              client.Exit();
                          });

                client.Events.Where(x => x.UUID == uuid && x.EventName == EventName.Dtmf)
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
                                     "ivr/8000/ivr-please_enter_pin_followed_by_pound.wav",
                                 BadInputAudioFile = "ivr/8000/ivr-that_was_an_invalid_entry.wav",
                                 DigitTimeoutMs = 2000,
                             });

                ColorConsole.WriteLine("Got digits: ".Blue(), playGetDigitsResult.Digits);

                if (playGetDigitsResult.Success)
                {
                    await client.Play(uuid, "ivr/8000/ivr-you_entered.wav");
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
                            uuid, "ivr/8000/ivr-you_may_exit_by_hanging_up.wav", new PlayOptions() { Loops = 3 });
                    await client.Hangup(uuid, HangupCause.CallRejected);
                }
            }
        }

        private static async void DtmfTest()
        {
            var client = await InboundSocket.Connect("10.10.10.36", 8021, "ClueCon");

            var originate =
                await
                client.Originate(
                    "user/1005",
                    new OriginateOptions
                        {
                            CallerIdNumber = "123456789", 
                            CallerIdName = "Dan Leg A", 
                            HangupAfterBridge = false,
                            TimeoutSeconds = 20
                        });

            if (!originate.Success)
            {
                ColorConsole.WriteLine("Originate Failed ".Red(), originate.HangupCause.ToString());
                client.Exit();
            }
            else
            {
                var uuid = originate.ChannelData.UUID;
                await client.SubscribeEvents(EventName.Dtmf);

                await client.SetMultipleChannelVariables(uuid, "dtmf_verbose=true", "drop_dtmf=true" );
                        //"min_dup_digit_spacing_ms=500",
                        //"spandsp_dtmf_rx_threshold=-32");
                    //"spandsp_dtmf_rx_twist=32",
                    //"spandsp_dtmf_rx_reverse_twist=7");
                await client.ExecuteApplication(uuid, "spandsp_start_dtmf");

                client.OnHangup(uuid,
                          e =>
                          {
                              ColorConsole.WriteLine("Hangup Detected on A-Leg {0} {1}".Red(),
                                                    e.Headers[HeaderNames.CallerUniqueId],
                                                    e.Headers[HeaderNames.HangupCause]);

                              client.Exit();
                          });

                client.Events.Where(x => x.UUID == uuid && x.EventName == EventName.Dtmf)
                      .Subscribe(e => Console.WriteLine(e.Headers[HeaderNames.DtmfDigit]));
            }
        }

        private static async void InboundSocketTest()
        {
            var client = await InboundSocket.Connect();
            Console.WriteLine("Authenticated!");

            try
            {
                await client.SubscribeEvents(EventName.Dtmf);

                var originate =
                    await
                    client.Originate(
                        "user/1001",
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
                    client.Exit();
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
                                    CallerIdName = "Dan B Leg",
                                    CallerIdNumber = "987654321",
                                    HangupAfterBridge = false,
                                    IgnoreEarlyMedia = true,
                                    ContinueOnFail = true,
                                    RingBack = "tone_stream://${uk-ring};loops=-1",
                                    ConfirmPrompt = "ivr/8000/ivr-to_accept_press_one.wav",
                                    ConfirmInvalidPrompt = "ivr/8000/ivr-that_was_an_invalid_entry.wav",
                                    ConfirmKey = "1234",
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

                        await client.StartDtmf(uuid);

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
        }

        private static void OutboundSocketTest()
        {
            var listener = new OutboundListener(8084);

            listener.Connections.Subscribe(
                async connection =>
                    {
                        await connection.Connect();
                        Console.WriteLine("New Socket connected");
                        
                        connection.Events.Where(x => x.EventName == EventName.ChannelHangup).Take(1).Subscribe(
                            e =>
                                {
                                    ColorConsole.WriteLine("Hangup Detected on A-Leg ".Red(),
                                                        e.Headers[HeaderNames.CallerUniqueId],
                                                        " ",
                                                        e.Headers[HeaderNames.HangupCause]);

                                    connection.Exit();
                                });

                        var uuid = connection.ChannelData.Headers[HeaderNames.UniqueId];

                        await
                            connection.SubscribeEvents(
                                EventName.Dtmf);

                        await connection.Linger();
                        await connection.ExecuteApplication(uuid, "answer", null, true, false);

                        var result =
                            await
                            connection.Play(
                                uuid, 
                                "$${base_dir}/sounds/en/us/callie/misc/8000/misc-freeswitch_is_state_of_the_art.wav");
                        
                        //await connection.ExecuteAppAsync(uuid, "conference", "test+1234");
                        //if (result.ChannelData.AnswerState != AnswerState.Hangup) await connection.Hangup(uuid, "NORMAL_CLEARING");
                    });

            listener.Start();
        }
    }
}