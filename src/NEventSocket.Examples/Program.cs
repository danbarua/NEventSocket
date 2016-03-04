using System;

namespace NEventSocket.Examples
{
    using NEventSocket.Logging;

    class Program
    {
        static int Main(string[] args)
        {
            using (var interactiveTaskRunner = new CommandLineTaskRunner())
            {
                try
                {
                    LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider(LogLevel.Debug));

                    var main = interactiveTaskRunner.Run();
                    Console.ReadLine();
                    return main;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.ReadLine();
                    return 1;
                }
            }
        }
    }
}
