namespace NEventSocket.Tests.TestSupport
{
    using System;
    using System.Threading.Tasks;

    public static class Wait
    {
        public static async Task Until(Func<bool> predicate)
        {
            while (!predicate())
            {
                await Task.Delay(100);
            }
        }
    }
}