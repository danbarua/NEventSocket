namespace NEventSocket.Examples
{
    using System;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading;
    using System.Threading.Tasks;

    class Util
    {
        public static Task WaitForEnterKeyPress(CancellationToken cancellation)
        {
            return
                Observable.Interval(TimeSpan.FromMilliseconds(100))
                    .Where(_ => Console.KeyAvailable)
                    .Select(_ => Console.ReadKey(false).Key)
                    .FirstAsync(x => x == ConsoleKey.Enter)
                    .ToTask(cancellation);
        }
    }
}