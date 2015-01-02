namespace Channels
{
    using System;

    using ColoredConsole;

    using NEventSocket.Logging;
    using NEventSocket.Logging.LogProviders;

    class Program
    {
        static void Main(string[] args)
        {
            LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider());

            ColorConsole.WriteLine("Starting...".OnGreen());

            new ChannelExample().Run();

            ColorConsole.WriteLine("Press [Enter] to exit.".OnGreen());
            Console.ReadLine();
        }
    }
}
