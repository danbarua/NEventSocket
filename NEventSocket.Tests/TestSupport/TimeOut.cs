namespace NEventSocket.Tests.TestSupport
{
    using NEventSocket.Util;

    public static class TimeOut
    {
        public const int TestTimeOutMs = 10000;

    }
    
    public static class PreventThreadPoolStarvation
    {
        private static readonly InterlockedBoolean Initialized = new InterlockedBoolean(false);

        public static void Init()
        {
            PreventThreadPoolStarvationWhenRunningTestsInParallel();
        }

        private static void PreventThreadPoolStarvationWhenRunningTestsInParallel()
        {
            if (Initialized.EnsureCalledOnce())
            {
                return;
            }

            // doing this to avoid thread starvation when running tests in parallel eg. nCrunch, AppVeyor
            int maxThreads;
            int maxIoPorts;
            System.Threading.ThreadPool.GetMaxThreads(out maxThreads, out maxIoPorts);
            System.Threading.ThreadPool.SetMaxThreads(maxThreads * 2, maxIoPorts * 2);
        }
    }
}