namespace NEventSocket.Examples.Examples
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Net.CommandLine;

    public class ApiTest : ICommandLineTask, IDisposable
    {
        public async Task Run(CancellationToken cancellationToken)
        {
            using (var client = await InboundSocket.Connect("localhost", 8021, "ClueCon"))
            {
                ColorConsole.WriteLine((await client.SendApi("status")).BodyText.DarkBlue());
                ColorConsole.WriteLine((await client.SendApi("blah")).BodyText.DarkBlue());
                ColorConsole.WriteLine((await client.SendApi("status")).BodyText.DarkBlue());
            }
        }

        public void Dispose()
        {
        }
    }
}
