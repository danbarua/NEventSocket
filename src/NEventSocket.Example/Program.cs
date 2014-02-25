// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   The program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.Example
{
    using System;
    using System.Reactive.Linq;
    using System.Security;

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

            OutboundSocketTest();
            InboundSocketTest();

            Console.WriteLine("Press [Enter] to exit.");
            Console.ReadLine();
        }

        private static void OutboundSocketTest()
        {
            var listener = new OutboundListener(8084);

            listener.Connections.Subscribe(
                async connection =>
                    {
                        Console.WriteLine("New Socket connected");

                        connection.Events.Where(x => x.EventType == EventType.CHANNEL_HANGUP)
                                  .Take(1)
                                  .Subscribe(
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

                        var uuid = connection.ChannelData.Headers["Unique-Id"];
                        Console.WriteLine(uuid);

                        await
                            connection.SubscribeEvents(
                                EventType.PLAYBACK_START, 
                                EventType.PLAYBACK_STOP, 
                                EventType.DTMF, 
                                EventType.CHANNEL_HANGUP_COMPLETE, 
                                EventType.CHANNEL_HANGUP);

                        await connection.Linger();
                        await connection.SendMessage(uuid, "call-command: execute\nexecute-app-name: answer");

                        var result =
                            await connection.Play(uuid, "$${base_dir}/sounds/en/us/callie/misc/8000/misc-freeswitch_is_state_of_the_art.wav");
                        Console.WriteLine("Playback : {0}", result.Success);

                        if (result.ChannelData.AnswerState != AnswerState.Hangup) await connection.Hangup(uuid, "NORMAL_CLEARING");
                    });

            listener.Start();
        }

        private static async void InboundSocketTest()
        {
            try
            {
                var client = await InboundSocket.Connect("127.0.0.1", 8021, "ClueCon");
                Console.WriteLine("Authenticated!");

                var result =
                    await
                    client.SubscribeEvents(
                        EventType.CHANNEL_HANGUP,
                        EventType.CHANNEL_HANGUP_COMPLETE,
                        EventType.DTMF,
                        EventType.CHANNEL_STATE,
                        EventType.CHANNEL_PROGRESS,
                        EventType.CHANNEL_DESTROY,
                        EventType.CHANNEL_ANSWER);

                var originateResult =
                    await
                    client.Originate(
                        Sofia.External("1000@172.16.50.128:5070"),
                        new OriginateOptions
                        {
                            CallerIdNumber = "123456789",
                            CallerIdName = "Dan B",
                            HangupAfterBridge = false,
                            //ReturnRingReady = true,
                            Timeout = 20
                        });

                if (!originateResult.Success)
                {
                    using (Colour.Use(ConsoleColor.DarkRed))
                    {
                        Console.WriteLine("Originate Failed {0}", originateResult.HangupCause);
                        client.Exit();
                    }
                }
                else
                {
                    var uuid = originateResult.ChannelData.Headers[HeaderNames.CallerUniqueId];
                    Console.WriteLine(
                        "Originate success {0}", originateResult.ChannelData.Headers[HeaderNames.AnswerState]);

                    var recordingPath = "c:/temp/recording_{0}.wav".Fmt(uuid);
                    await client.MyEvents(uuid);
                    await client.Linger();

                    client.Events.Where(x => x.EventType == EventType.CHANNEL_HANGUP).Take(1).Subscribe(
                        e =>
                        {
                            using (Colour.Use(ConsoleColor.Red))
                            {
                                Console.WriteLine(
                                    "HANGUP DETECTED {0} {1}",
                                    e.Headers[HeaderNames.CallerUniqueId],
                                    e.Headers[HeaderNames.HangupCause]);
                            }

                            //client.Exit();
                        });

                    var playResult = await client.Play(
                        uuid, "$${base_dir}/sounds/en/us/callie/ivr/8000/ivr-call_being_transferred.wav");
                    if (playResult.Success)
                    {
                        Console.WriteLine("Played ok!");
                    }

                    await client.SetChannelVariable(uuid, "continue_on_fail", "true");
                    var bridge =
                        await
                        client.Bridge(
                            uuid,
                            Sofia.Extension("internal", "1000@172.16.50.128"),
                            new BridgeOptions() { CallerIdName = "Click2Dial", CallerIdNumber = "Click2Dial", HangupAfterBridge = false, IgnoreEarlyMedia = false });

                    if (bridge.Success)
                    {
                        await client.SetChannelVariable(uuid, "RECORD_ARTIST", "'Opex Hosting Ltd'");
                        await client.SetChannelVariable(uuid, "RECORD_MIN_SEC", 0); //freeswitch discards recordings under 3 seconds

                        var recordingResult = await client.Api("uuid_record {0} start {1}".Fmt(uuid, recordingPath));
                        Console.WriteLine("Recording... " + recordingResult.Success);

                        if (recordingResult.Success)
                        {
                            client.Events.Where(x => x.EventType == EventType.DTMF)
                              .Subscribe(async (e) =>
                              {
                                  var dtmf = e.Headers["DTMF-Digit"];
                                  switch (dtmf)
                                  {
                                      case "1":
                                          Console.WriteLine("Mask recording");
                                          await client.Api("uuid_record {0} mask {1}".Fmt(uuid, recordingPath));
                                          await client.ExecuteAppAsync(uuid,"displace_session","{0}".Fmt("ivr/8000/ivr-recording_paused.wav"));
                                          // await client.Play(uuid, "$${base_dir}/sounds/en/us/callie/ivr/8000/ivr-recording_paused.wav");
                                          break;
                                      case "2":
                                          Console.WriteLine("Unmask recording");
                                          await client.Api("uuid_record {0} unmask {1}".Fmt(uuid, recordingPath));
                                          await client.ExecuteAppAsync(uuid, "displace_session", "{0}".Fmt("ivr/8000/ivr-begin_recording.wav"));
                                          // await client.Api("uuid_displace {0} start {1}  mux".Fmt(uuid, "$${base_dir}/sounds/en/us/callie/ivr/8000/ivr-begin_recording.wav"));
                                          // await client.Play(uuid, "$${base_dir}/sounds/en/us/callie/ivr/8000/ivr-begin_recording.wav");
                                          break;
                                      case "3":
                                          Console.WriteLine("stop recording");
                                          await client.Api("uuid_record {0} stop {1}".Fmt(uuid, recordingPath));
                                          await client.ExecuteAppAsync(uuid, "displace_session", "{0}".Fmt("ivr/8000/ivr-recording_stopped.wav"));
                                          // await client.Api("uuid_displace {0} start {1}  mux".Fmt(uuid, "$${base_dir}/sounds/en/us/callie/ivr/8000/ivr-recording_stopped.wav"));
                                          // await client.Play(uuid, "$${base_dir}/sounds/en/us/callie/ivr/8000/ivr-recording_stopped.wav");
                                          break;
                                  }
                              });
                        }
                    }
                    // if (evt.AnswerState != AnswerState.Hangup) await client.Hangup(uuid, "NORMAL_CLEARING");
                }
            }
            catch (SecurityException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}