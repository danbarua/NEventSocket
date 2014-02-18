using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NEventSocket.Example
{
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Common.Logging;
    using Common.Logging.Simple;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Sockets.Implementation;
    using NEventSocket.Sockets.Protocol;
    using NEventSocket.Util;

    public static class Colour
    {
        public static IDisposable Use(ConsoleColor color)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            return Disposable.Create(() => Console.ForegroundColor = prev);
        }
    }

    class Program
    {
        private static void Main(string[] args)
        {
            // set logger factory
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter();

            Console.WriteLine("Starting...");
            var client = new InboundSocket("localhost", 8021, "ClueCon");

            client.Connected += (sender, eventArgs) => Console.WriteLine("Connected...");
            
            client.Authenticated += async (sender, eventArgs) =>
                {
                    Console.WriteLine("Authenticated!");

                    var result = await client.Event(
                        EventType.BACKGROUND_JOB,
                        EventType.CHANNEL_EXECUTE_COMPLETE,
                        EventType.CHANNEL_HANGUP,
                        EventType.CHANNEL_HANGUP_COMPLETE,
                        EventType.DTMF,
                        EventType.CHANNEL_DESTROY,
                        EventType.CHANNEL_ANSWER);

                    Console.WriteLine(result.Success);

                    var channel =
                        await
                        client.Originate(
                            "{origination_caller_id_number=1234567}sofia/external/1000@172.16.50.128:5070 &park");

                    if (channel != null)
                    {
                        var uuid = channel.EventHeaders[HeaderNames.CallerUniqueId];
                        await client.MyEvents(Guid.Parse(uuid));
                        var evt = await client.ExecuteAppAsync(uuid, "playback", "$${base_dir}/sounds/en/us/callie/ivr/8000/ivr-all_your_call_are_belong_to_us.wav");
                        Console.WriteLine("Finished playback {0}", evt.EventHeaders[HeaderNames.ApplicationResponse]);
                    }
                };

            client.EventsReceived.Where(x => x.EventType == EventType.CHANNEL_EXECUTE_COMPLETE).Subscribe(
                x =>
                {
                    using (Colour.Use(ConsoleColor.DarkGreen))
                    {
                        Console.WriteLine("CHANNEL_EXECUTE_COMPLETE");
                        Console.WriteLine("Application: " + x.EventHeaders[HeaderNames.Application]);
                        Console.WriteLine("Args: " + x.EventHeaders[HeaderNames.ApplicationData]);
                        Console.WriteLine("Response: " + x.EventHeaders[HeaderNames.ApplicationResponse]);
                    }
                });

            client.Disconnected += (sender, eventArgs) =>
                {
                    using (Colour.Use(ConsoleColor.Blue))
                    {
                        Console.WriteLine("Disconnected!");
                    }
                };

            client.EventsReceived.Where(x => x.EventType == EventType.DTMF)
                  .Subscribe(e => Console.WriteLine("DTMF: {0}", e.EventHeaders["DTMF-Digit"]));

            client.EventsReceived.Where(x => x.EventType == EventType.CHANNEL_HANGUP || x.EventType == EventType.CHANNEL_HANGUP_COMPLETE).Subscribe(
                e =>
                    {
                        using (Colour.Use(ConsoleColor.Red))
                        {
                            Console.WriteLine("HANGUP DETECTED!");
                            e.EventHeaders.ForEach(x => Console.WriteLine(x));
                        }
                    });

            Console.ReadLine();

            client.Exit();

            Console.WriteLine("Finished...");
            Console.ReadLine();
        }
    }
}
