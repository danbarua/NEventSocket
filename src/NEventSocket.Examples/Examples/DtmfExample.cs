namespace NEventSocket.Examples.Examples
{
    using System;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Net.CommandLine;

    using NEventSocket.FreeSwitch;

    public class DtmfExample : ICommandLineTask, IDisposable
    {
        private InboundSocket client;

        public async Task Run(CancellationToken cancellationToken)
        {
            client = await InboundSocket.Connect("127.0.0.1", 8021, "ClueCon", TimeSpan.FromSeconds(20));

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
                ColorConsole.WriteLine("Originate Failed ".Red(), originate.HangupCause.ToString());
                await client.Exit();
            }
            else
            {
                var uuid = originate.ChannelData.UUID;
                await client.SubscribeEvents(EventName.Dtmf);

                //uncomment to play with mod_spandsp inband dtmf detection
                //note: i could not get this to work on windows, works fine on linux
                
                //await client.SetMultipleChannelVariables(uuid,
                //"min_dup_digit_spacing_ms=500",
                //"spandsp_dtmf_rx_threshold=-32");
                //"spandsp_dtmf_rx_twist=32",
                //"spandsp_dtmf_rx_reverse_twist=7");
                //await client.ExecuteApplication(uuid, "spandsp_start_dtmf");

                client.OnHangup(
                    uuid,
                    e =>
                    {
                        ColorConsole.WriteLine(
                            "Hangup Detected on A-Leg {0} {1}".Red(),
                            e.Headers[HeaderNames.CallerUniqueId],
                            e.Headers[HeaderNames.HangupCause]);

                        client.Exit();
                    });

                client.Events.Where(x => x.EventName == EventName.Dtmf).Subscribe(
                    e =>
                    {
                        Console.WriteLine("Got DTMF");
                        Console.WriteLine(e.UUID == uuid);
                        Console.WriteLine("UIIDS: event {0} ours {1}", e.UUID, uuid);
                        Console.WriteLine(e.Headers[HeaderNames.DtmfDigit]);
                    });

                ColorConsole.WriteLine("Press [Enter] to exit.".Green());
                await Util.WaitForEnterKeyPress(cancellationToken);
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