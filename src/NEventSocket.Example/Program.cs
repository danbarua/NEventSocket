using System;

namespace NEventSocket.Example
{
    using System.Reactive.Linq;

    using Common.Logging;
    using Common.Logging.Simple;

    using NEventSocket.FreeSwitch;

    class Program
    {
        private static void Main(string[] args)
        {
            // set logger factory
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(
                LogLevel.All, true, true, true, "yyyy-MM-dd hh:mm:ss");

            Console.WriteLine("Starting...");

            OutboundSocket();
            InboundSocket();

            Console.WriteLine("Press [Enter] to exit.");
            Console.ReadLine();
        }

        private static void OutboundSocket()
        {
            var listener = new OutboundListener(8084);

            listener.Connections
                    .Subscribe(
                        async connection =>
                            {
                                Console.WriteLine("New Socket connected");

                                connection.MessagesReceived.Subscribe(
                                    _ => { },
                                    () =>
                                        {
                                            using (Colour.Use(ConsoleColor.Yellow))
                                            {
                                                Console.WriteLine("MESSAGES_OBS_COMPLETE");
                                            }
                                        });

                                connection.EventsReceived.Where(x => x.EventType == EventType.CHANNEL_HANGUP)
                                          .Take(1)
                                          .Subscribe(
                                              e =>
                                                  {
                                                      using (Colour.Use(ConsoleColor.Red))
                                                      {
                                                          Console.WriteLine("HANGUP DETECTED {0} {1}", e.Headers[HeaderNames.CallerUniqueId], e.EventType);
                                                      }
                                                      connection.Exit();
                                                  });


                                connection.Disposed += (o, e) => Console.WriteLine("connection disposed");

                                var uuid = connection.ChannelData.Headers["Unique-Id"];
                                Console.WriteLine(uuid);

                                await connection.Events(
                                        EventType.BACKGROUND_JOB,
                                        EventType.CHANNEL_EXECUTE_COMPLETE,
                                        EventType.API,
                                        EventType.PLAYBACK_START,
                                        EventType.PLAYBACK_STOP,
                                        EventType.DTMF,
                                        EventType.CHANNEL_HANGUP_COMPLETE,
                                        EventType.CHANNEL_HANGUP);

                                await connection.Linger();
                                await connection.SendMessage(uuid, "call-command: execute\nexecute-app-name: answer");  //ExecuteAppAsync(uuid, "answer"); //

                                var result = await connection.ExecuteAppAsync(uuid, "playback", "$${base_dir}/sounds/en/us/callie/misc/8000/misc-freeswitch_is_state_of_the_art.wav");
                                Console.WriteLine("Finished playback {0}", result.Headers[HeaderNames.ApplicationResponse]);

                                if (result.AnswerState != "hangup")
                                {
                                    await connection.Hangup(uuid, "NORMAL_CLEARING");
                                }
                            });

            listener.Start();
        }

        private static void InboundSocket()
        {
            var client = new InboundSocket("localhost", 8021, "ClueCon");
            client.Disposed += (o, e) => Console.WriteLine("client disposed");

            client.MessagesReceived.Subscribe(
                                     _ => { },
                                     () =>
                                     {
                                         using (Colour.Use(ConsoleColor.Yellow))
                                         {
                                             Console.WriteLine("MESSAGES_OBS_COMPLETE");
                                         }
                                     });

            client.Authenticated += async (sender, eventArgs) =>
            {
                Console.WriteLine("Authenticated!");

                var result = await client.Events(
                    EventType.CHANNEL_HANGUP,
                    EventType.CHANNEL_HANGUP_COMPLETE,
                    EventType.DTMF,
                    EventType.CHANNEL_STATE,
                    EventType.CHANNEL_PROGRESS,
                    EventType.CHANNEL_DESTROY,
                    EventType.CHANNEL_ANSWER);


                var channel = await client.Originate("{origination_caller_id_number=123456789}sofia/external/1000@172.16.50.128:5070 &park");

                if (channel != null && channel.AnswerState == AnswerState.Answered)
                {
                    var uuid = channel.Headers[HeaderNames.CallerUniqueId];
                    await client.MyEvents(uuid);
                    await client.Linger();
                    var evt =
                        await
                        client.ExecuteAppAsync(
                            uuid,
                            "playback",
                            "$${base_dir}/sounds/en/us/callie/ivr/8000/ivr-all_your_call_are_belong_to_us.wav");

                    Console.WriteLine("Finished playback {0}", evt.Headers[HeaderNames.ApplicationResponse]);

                    if (evt.AnswerState != AnswerState.Hangup)
                    {
                        await client.Hangup(uuid, "NORMAL_CLEARING");
                    }
                }
                else
                {
                    using (Colour.Use(ConsoleColor.DarkRed))
                    {
                        Console.WriteLine("Call rejected");
                        client.Exit();
                    }
                }
            };

            client.EventsReceived.Where(x => x.EventType == EventType.DTMF)
                  .Subscribe(e => Console.WriteLine("DTMF: {0}", e.Headers["DTMF-Digit"]));

            client.EventsReceived.Where(x => x.EventType == EventType.CHANNEL_PROGRESS).Subscribe(
                e =>
                    {
                        using (Colour.Use(ConsoleColor.DarkGreen))
                        {
                            Console.WriteLine("Progress {0} {1}", e.Headers[HeaderNames.CallerUniqueId], e.AnswerState);
                        }
                    });

            client.EventsReceived.Where(x => x.EventType == EventType.CHANNEL_HANGUP).Subscribe(
                e =>
                {
                    using (Colour.Use(ConsoleColor.Red))
                    {
                        Console.WriteLine("HANGUP DETECTED {0} {1}", e.Headers[HeaderNames.CallerUniqueId], e.EventType);
                    }

                    client.Exit();
                });
        }
    }
}
