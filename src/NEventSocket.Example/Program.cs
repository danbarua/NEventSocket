// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Example
{
    using System;
    using System.Reactive.Linq;
    using System.Security;
    using System.Threading.Tasks;

    using Common.Logging;
    using Common.Logging.Simple;

    using NEventSocket.FreeSwitch;
    using NEventSocket.FreeSwitch.Api;
    using NEventSocket.FreeSwitch.Applications;
    using NEventSocket.Util;

    /// <summary>The program.</summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            // set logger factory
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(
                LogLevel.All, true, true, true, "yyyy-MM-dd hh:mm:ss");

            Console.WriteLine("Starting...");

            //OutboundSocketTest();
            //InboundSocketTest();

            //DtmfTest();
            PlayGetDigitsTest();

            Console.WriteLine("Press [Enter] to exit.");
            Console.ReadLine();
        }

        private static async void PlayGetDigitsTest()
        {
            var client = await InboundSocket.Connect("10.10.10.36", 8021, "ClueCon");
            await client.SubscribeEvents(EventName.Dtmf);

            var originate =
                await
                client.Originate(
                    Endpoint.User("1005"),
                    new OriginateOptions
                        {
                            CallerIdNumber = "123456789", 
                            CallerIdName = "Dan Leg A", 
                            HangupAfterBridge = false,
                            TimeoutSeconds = 20
                        });

            if (!originate.Success)
            {
                using (Colour.Use(ConsoleColor.DarkRed))
                {
                    Console.WriteLine("Originate Failed {0}", originate.HangupCause);
                    client.Exit();
                }
            }
            else
            {
                Console.WriteLine("{0} {1} {2}", originate.ChannelData.EventName, originate.ChannelData.AnswerState, originate.ChannelData.ChannelState);
                var uuid = originate.ChannelData.UUID;
                await client.SetChannelVariable(uuid, "dtmf_verbose", "true");
                await client.StartDtmf(uuid);

                client.On(
                    uuid,
                    EventName.ChannelHangup,
                    e =>
                        {
                              using (Colour.Use(ConsoleColor.Red))
                              {
                                  Console.WriteLine("Hangup Detected on A-Leg {0} {1}",
                                                    e.UUID,
                                                    e.HangupCause);
                              }


                              client.Exit();
                          });

                client.On(
                    uuid,
                    EventName.Dtmf,
                    e =>
                        {
                            using (Colour.Use(ConsoleColor.DarkGreen))
                            {
                                Console.WriteLine(e.Headers[HeaderNames.DtmfDigit]);
                            } 
                        });

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

                using (Colour.Use(ConsoleColor.Green))
                    Console.WriteLine("Got digits: {0}", playGetDigitsResult.Digits);

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
                    await client.Play(uuid, "ivr/8000/ivr-you_may_exit_by_hanging_up.wav");
                }
            }
        }

        private static async void DtmfTest()
        {
            var client = await InboundSocket.Connect("10.10.10.36", 8021, "ClueCon");
            Console.WriteLine("Authenticated!");

            var originate =
                await
                client.Originate(
                    Endpoint.User("1005"), 
                    new OriginateOptions
                        {
                            CallerIdNumber = "123456789", 
                            CallerIdName = "Dan Leg A", 
                            HangupAfterBridge = false,
                            TimeoutSeconds = 20
                        });

            if (!originate.Success)
            {
                using (Colour.Use(ConsoleColor.DarkRed))
                {
                    Console.WriteLine("Originate Failed {0}", originate.HangupCause);
                    client.Exit();
                }
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
                await client.ExecuteAppAsync(uuid, "spandsp_start_dtmf");

                client.OnHangup(uuid,
                          e =>
                          {
                              using (Colour.Use(ConsoleColor.Red))
                              {
                                  Console.WriteLine("Hangup Detected on A-Leg {0} {1}",
                                                    e.Headers[HeaderNames.CallerUniqueId],
                                                    e.Headers[HeaderNames.HangupCause]);
                              }

                              client.Exit();
                          });

                client.Events.Where(x => x.UUID == uuid && x.EventName == EventName.Dtmf)
                      .Subscribe(e => Console.WriteLine(e.Headers[HeaderNames.DtmfDigit]));
            }
        }

        private static async void InboundSocketTest()
        {
            var client = await InboundSocket.Connect("10.10.10.36", 8021, "ClueCon");
            Console.WriteLine("Authenticated!");

            await client.SubscribeEvents(EventName.Dtmf);

            var originate =
                await
                client.Originate(
                    Endpoint.User("1000"), 
                    new OriginateOptions
                        {
                            CallerIdNumber = "123456789", 
                            CallerIdName = "Dan Leg A", 
                            HangupAfterBridge = false,
                            TimeoutSeconds = 20
                        });

            if (!originate.Success)
            {
                using (Colour.Use(ConsoleColor.DarkRed))
                {
                    Console.WriteLine("Originate Failed {0}", originate.HangupCause);
                    client.Exit();
                }
            }
            else
            {
                var uuid = originate.ChannelData.Headers[HeaderNames.CallerUniqueId];

                Console.WriteLine("Originate success {0}", originate.ChannelData.Headers[HeaderNames.AnswerState]);

                var recordingPath = "/usr/local/freeswitch/recordings/{0}.wav".Fmt(uuid); //"c:/temp/recording_{0}.wav".Fmt(uuid);

                client.OnHangup(uuid,
                          e =>
                              {
                                  using (Colour.Use(ConsoleColor.Red))
                                  {
                                      Console.WriteLine("Hangup Detected on A-Leg {0} {1}",
                                                        e.Headers[HeaderNames.CallerUniqueId],
                                                        e.Headers[HeaderNames.HangupCause]);
                                  }

                                  client.Exit();
                              });

                var playResult = await client.Play(uuid, "ivr/8000/ivr-call_being_transferred.wav");
                if (playResult.Success) Console.WriteLine("Played ok!");

                var bridgeUUID = Guid.NewGuid().ToString();

                var ringingHandler = client.Events.Where(x => x.UUID == bridgeUUID && x.EventName == EventName.ChannelProgress)
                      .Take(1)
                      .Subscribe(
                          e =>
                              {
                                  using (Colour.Use(ConsoleColor.Blue)) Console.WriteLine("Progress {0} on {1}", e.AnswerState, e.UUID); });

                var bridge =
                    await
                    client.Bridge(
                        uuid, 
                        Endpoint.User("1001"), 
                        new BridgeOptions()
                            {
                                UUID = bridgeUUID,
                                Timeout = 20, 
                                CallerIdName = "Dan B Leg", 
                                CallerIdNumber = "987654321", 
                                HangupAfterBridge = false, 
                                IgnoreEarlyMedia = true, 
                                ContinueOnFail = true, 
                                RingBack = "${uk-ring}"
                            });

                if (!bridge.Success)
                {
                    ringingHandler.Dispose();

                    using (Colour.Use(ConsoleColor.Red))
                    {
                        Console.WriteLine("Bridge failed {0}",  bridge.ResponseText);
                    }

                    await client.Play(uuid, "ivr/8000/ivr-call_rejected.wav");
                    await client.Hangup(uuid, HangupCause.CallRejected);
                }
                else
                {
                    using (Colour.Use(ConsoleColor.Green))
                    {
                        Console.WriteLine("Bridge succeeded from {0} to {1} - {2}", bridge.ChannelData.UUID, bridge.BridgeUUID, bridge.ResponseText);
                    }

                    await client.StartDtmf(uuid);

                    //when b-leg hangs up, play a notification to a-leg
                    client.OnHangup(bridge.BridgeUUID,
                                      async e =>
                                          {
                                              using (Colour.Use(ConsoleColor.Red))
                                              {
                                                  Console.WriteLine(
                                                      "Hangup Detected on B-Leg {0} {1}", 
                                                      e.Headers[HeaderNames.CallerUniqueId], 
                                                      e.Headers[HeaderNames.HangupCause]);
                                              }

                                              await client.Play(uuid, "ivr/8000/ivr-you_may_exit_by_hanging_up.wav");
                                              await client.Hangup(uuid, HangupCause.NormalClearing);
                                          });

                    await client.SetChannelVariable(uuid, "RECORD_ARTIST", "'Opex Hosting Ltd'");
                    await client.SetChannelVariable(uuid, "RECORD_MIN_SEC", 0);
                    await client.SetChannelVariable(uuid, "RECORD_STEREO", "true");

                    var recordingResult = await client.Api("uuid_record {0} start {1}".Fmt(uuid, recordingPath));
                    Console.WriteLine("Recording... " + recordingResult.Success);

                    if (recordingResult.Success)
                    {
                        client.Events.Where(x => x.UUID == uuid && x.EventName == EventName.Dtmf).Subscribe(
                            async (e) =>
                                {
                                    var dtmf = e.Headers[HeaderNames.DtmfDigit];
                                    switch (dtmf)
                                    {
                                        case "1":
                                            Console.WriteLine("Mask recording");
                                            await client.Api("uuid_record {0} mask {1}".Fmt(uuid, recordingPath));
                                            await
                                                client.ExecuteAppAsync(
                                                    uuid,
                                                    "displace_session",
                                                    appArg: "{0} m".Fmt("ivr/8000/ivr-recording_paused.wav"));
                                            break;
                                        case "2":
                                            Console.WriteLine("Unmask recording");
                                            await client.Api("uuid_record {0} unmask {1}".Fmt(uuid, recordingPath));
                                            await
                                                client.ExecuteAppAsync(
                                                    uuid,
                                                    "displace_session",
                                                    appArg: "{0} m".Fmt("ivr/8000/ivr-begin_recording.wav"));
                                            break;
                                        case "3":
                                            Console.WriteLine("Stop recording");
                                            await client.Api("uuid_record {0} stop {1}".Fmt(uuid, recordingPath));
                                            await
                                                client.ExecuteAppAsync(
                                                    uuid,
                                                    "displace_session",
                                                    appArg: "{0} m".Fmt("ivr/8000/ivr-recording_stopped.wav"));
                                            break;
                                    }
                                });
                    }
                }
            }
        }

        private static void OutboundSocketTest()
        {
            var listener = new OutboundListener(8084);

            listener.Connections.Subscribe(
                async connection =>
                    {
                        Console.WriteLine("New Socket connected");

                        connection.Events.Where(x => x.EventName == EventName.ChannelHangup).Take(1).Subscribe(
                            e =>
                                {
                                    using (Colour.Use(ConsoleColor.Red))
                                    {
                                        Console.WriteLine(
                                            "HANGUP DETECTED {0} {1}", 
                                            e.Headers[HeaderNames.CallerUniqueId], 
                                            e.Headers[HeaderNames.HangupCause]);
                                    }

                                    connection.Exit();
                                });

                        var uuid = connection.ChannelData.Headers[HeaderNames.UniqueId];
                        Console.WriteLine(uuid);

                        await
                            connection.SubscribeEvents(
                                EventName.Dtmf);

                        await connection.Linger();
                        await connection.SendMessage(uuid, "call-command: execute\nexecute-app-name: answer");

                        var result =
                            await
                            connection.Play(
                                uuid, 
                                "$${base_dir}/sounds/en/us/callie/misc/8000/misc-freeswitch_is_state_of_the_art.wav");
                        Console.WriteLine("Playback : {0}", result.Success);


                        await connection.ExecuteAppAsync(uuid, "conference", "test+1234");
                        //if (result.ChannelData.AnswerState != AnswerState.Hangup) await connection.Hangup(uuid, "NORMAL_CLEARING");
                    });

            listener.Start();
        }
    }
}