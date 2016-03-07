namespace NEventSocket.Examples.Examples
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Net.CommandLine;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Util;

    public class LoadTest : ICommandLineTask, IDisposable
    {
        private readonly CommandLineReader commandLineReader;

        public LoadTest(CommandLineReader commandLineReader)
        {
            this.commandLineReader = commandLineReader;
        }

        public Task Run(CancellationToken cancellationToken)
        {
            int authFailures = 0;
            int heartbeatsReceived = 0;
            
            var settings = commandLineReader.ReadObject<LoadTestSettings>(cancellationToken);
            
            ColorConsole.WriteLine("Spinning up ".DarkGreen(), settings.MaxClients.ToString().Green(), " InboundSockets".DarkGreen());
            ColorConsole.WriteLine("They will connect and subscribe to HeartBeat events then disconnect when the first Heartbeat has been received.".DarkGreen());
            Parallel.For(0, settings.MaxClients,
                async (_) =>
                {
                    long clientId = 0;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        using (
                            InboundSocket client =
                                await
                                    InboundSocket.Connect(
                                        "127.0.0.1",
                                        8021,
                                        "ClueCon",
                                        TimeSpan.FromSeconds(settings.ConnectionTimeoutSeconds)))
                        {
                            clientId = client.Id;
                            await client.SubscribeEvents(EventName.Heartbeat);

                            EventMessage heartbeat =
                                await client.Events.FirstOrDefaultAsync(x => x.EventName == EventName.Heartbeat).ToTask(cancellationToken);
                            if (heartbeat != null)
                            {
                                Interlocked.Increment(ref heartbeatsReceived);
                                ColorConsole.WriteLine("Client ".DarkCyan(), clientId.ToString(), " reporting in ".DarkCyan());
                            }
                        }
                    }
                    catch (InboundSocketConnectionFailedException ex)
                    {
                        if (ex.InnerException != null && ex.InnerException is TimeoutException)
                        {
                            ColorConsole.WriteLine("Auth Timeout".OnDarkRed());
                        }
                        else
                        {
                            ColorConsole.WriteLine("Connection failure ".OnDarkRed(), ex.Message.DarkRed());
                        }
                        Interlocked.Increment(ref authFailures);
                    }
                    catch (TaskCanceledException)
                    {

                    }
                });

            ColorConsole.WriteLine("Press [Enter] to exit.".Green());
            Console.ReadLine();

            ColorConsole.WriteLine("THere were {0} heartbeats".Fmt(heartbeatsReceived).Green());
            ColorConsole.WriteLine("THere were {0} auth timeout failures".Fmt(authFailures).Red());

            return Task.FromResult(0);
        }

        public class LoadTestSettings
        {
            public LoadTestSettings()
            {
                MaxClients = 512;
                ConnectionTimeoutSeconds = 30;
            }

            public int MaxClients { get; set; }
            public int ConnectionTimeoutSeconds { get; set; }
        }

        public void Dispose()
        {
        }
    }
}