namespace NEventSocket.Example
{
    using System;
    using System.Reactive.Disposables;

    public static class Colour
    {
        public static IDisposable Use(ConsoleColor color)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            return Disposable.Create(() => Console.ForegroundColor = prev);
        }
    }
}