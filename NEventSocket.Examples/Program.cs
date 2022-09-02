using System;
using NEventSocket.Logging;

namespace NEventSocket.Examples
{
    class Program
    {
        static int Main(string[] args)
        {
            using (var interactiveTaskRunner = new CommandLineTaskRunner())
            {
                try
                {
                    /*Logger.Configure(LoggerFactory.)
                    LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider(LogLevel.Info));*/

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
