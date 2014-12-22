namespace NEventSocket.Tests.TestSupport
{
    using System;
    using System.Threading;

    public static class ThreadUtils
    {
        /// <summary>
        /// Sleeps the thread until the given predicate passes.
        /// Note: Remember to apply the Timeout attribute on your unit tests to prevent this
        /// from blocking forever in case the predicate never becomes true.
        /// </summary>
        public static void WaitUntil(Func<bool> predicate)
        {
            while (!predicate())
            {
                Thread.Sleep(100);
            }
        }
    }
}