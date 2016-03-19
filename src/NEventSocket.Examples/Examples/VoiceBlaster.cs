namespace NEventSocket.Examples.Examples
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Net.CommandLine;

    using NEventSocket.FreeSwitch;

    public class VoiceBlaster : ICommandLineTask, IDisposable
    {
        private const int MAX_CALLS = 2;

        private const string GATEWAY = "172.16.172.207:5080";

        private int currentCallCount = 0;

        public async Task Run(CancellationToken cancellationToken)
        { 

            //cancellationToken is cancelled when Ctrl+C is pressed
            //we'll use our own inner cancellationToken in our business logic
            //and link it to the outer one that is provided.
            var ourCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            using (var client = await InboundSocket.Connect())
            {
                Console.WriteLine("Authenticated!");

                await client.SubscribeEvents(EventName.ChannelHangup, EventName.BackgroundJob);

                client.Events.Where(x => x.EventName == EventName.ChannelHangup && x.HangupCause != HangupCause.NormalClearing)
                    .Subscribe(x => { Console.WriteLine("Hangup Detected : {0} {1}", x.GetVariable("mobile_no"), x.HangupCause); });

                using (var listener = new OutboundListener(8084))
                {
                    listener.Connections.Subscribe(
                        async socket =>
                            {
                                try
                                {
                                    await socket.Connect();
                                    await socket.Filter(HeaderNames.UniqueId, socket.ChannelData.UUID);

                                    var uuid = socket.ChannelData.Headers[HeaderNames.UniqueId];
                                    Console.WriteLine(
                                        "OutboundSocket connected for channel {0} {1}", 
                                        uuid, 
                                        socket.ChannelData.GetVariable("mobile_no"));

                                    await socket.Play(uuid, "misc/8000/misc-learn_more_about_freeswitch_solutions.wav");
                                    await socket.Play(uuid, "misc/8000/misc-freeswitch_is_state_of_the_art.wav");
                                    await socket.ExecuteApplication(uuid, "sleep", "1000"); //wait for audio to go out to the network
                                    await socket.Hangup(uuid, HangupCause.NormalClearing);
                                }
                                catch (OperationCanceledException)
                                {
                                    //hangup - freeswitch disconnected from us
                                }
                            });

                    listener.Start();

                    var checkCallCount = new Task(
                        async () =>
                            {
                                try
                                {
                                    while (!ourCancellationToken.IsCancellationRequested)
                                    {
                                        var res = await client.SendApi("show calls count");
                                        Console.WriteLine("Current Calls Count " + Convert.ToInt32(res.BodyText.Split(' ')[0]));
                                        currentCallCount = Convert.ToInt32(res.BodyText.Split(' ')[0]);
                                        await Task.Delay(2000);
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    //shutdown
                                }
                            });

                    checkCallCount.Start();

                    Task.Run(
                        async () =>
                            {
                                try
                                {
                                    await Dialler(client, ourCancellationToken.Token);
                                }
                                catch (OperationCanceledException)
                                {
                                    //shutdown
                                }
                            });

                    ColorConsole.WriteLine("Press [Enter] to exit.".Green());
                    await Util.WaitForEnterKeyPress(cancellationToken);
                    ourCancellationToken.Cancel();

                    listener.Dispose();
                }
            }
        }

        private async Task Dialler(InboundSocket client, CancellationToken ourCancellationToken)
        {
            while (!ourCancellationToken.IsCancellationRequested)
            {
                while (currentCallCount >= MAX_CALLS && !ourCancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(2000);
                }

                var mobileNos = new List<string>() { "8940703114", "8754006482" };

                await Task.WhenAll(
                    mobileNos.Take(MAX_CALLS).Select(
                        mobileNo => Task.Run(
                                        async () =>
                                        {
                                            Console.WriteLine("Call initiating : " + mobileNo);

                                            var originateOptions = new OriginateOptions
                                            {
                                                CallerIdName = "874561",
                                                CallerIdNumber = "874561",
                                                IgnoreEarlyMedia = true
                                            };

                                            originateOptions.ChannelVariables["mobile_no"] = mobileNo;

                                            var originateResult =
                                                                    await
                                                                    client.Originate(
                                                                        string.Format("sofia/internal/{0}@{1}", mobileNo, GATEWAY),
                                                                        originateOptions,
                                                                        "socket",
                                                                        "127.0.0.1:8084 async full");

                                            if (!originateResult.Success)
                                            {
                                                Console.WriteLine(
                                                                        "Call Failed to initiate : {0} {1}",
                                                                        mobileNo,
                                                                        originateResult.ResponseText);
                                            }
                                            else
                                            {
                                                Console.WriteLine("Call Successfully initiated {0}", mobileNo);
                                            }
                                        })));
            }
        }

        public void Dispose()
        {
        }
    }
}