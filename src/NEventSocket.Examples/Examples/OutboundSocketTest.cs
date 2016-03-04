namespace NEventSocket.Examples.Examples
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Net.CommandLine;

    using NEventSocket.FreeSwitch;

    public class OutboundSocketTest : ICommandLineTask, IDisposable
    {
        private OutboundListener listener;

        public Task Run(CancellationToken cancellationToken)
        {
            listener = new OutboundListener(8084);

            listener.Connections.Subscribe(
                async connection =>
                {
                    await connection.Connect();
                    Console.WriteLine("New Socket connected");

                    connection.Events.Where(x => x.UUID == connection.ChannelData.UUID && x.EventName == EventName.ChannelHangup)
                        .Take(1)
                        .Subscribe(
                            e =>
                            {
                                ColorConsole.WriteLine(
                                    "Hangup Detected on A-Leg ".Red(),
                                    e.Headers[HeaderNames.CallerUniqueId],
                                    " ",
                                    e.Headers[HeaderNames.HangupCause]);

                                connection.Exit();
                            });

                    var uuid = connection.ChannelData.Headers[HeaderNames.UniqueId];

                    await connection.SubscribeEvents(EventName.Dtmf, EventName.ChannelHangup);

                    await connection.Linger();
                    await connection.ExecuteApplication(uuid, "answer", null, true, false);

                    var result =
                        await connection.Play(uuid, "$${base_dir}/sounds/en/us/callie/misc/8000/misc-freeswitch_is_state_of_the_art.wav");
                });

            listener.Start();

            Console.WriteLine("Listener started on 8084. Press [Enter] to exit");
            Console.ReadLine();

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            this.listener.Dispose();
        }
    }
}