namespace NEventSocket.Util
{
    using System.Text;

    /// <summary>
    /// Lifted from Roslyn, http://mattwarren.org/2014/06/10/roslyn-code-base-performance-lessons-part-2/
    /// </summary>
    internal static class StringBuilderPool
    {
        public static StringBuilder Allocate()
        {
            return SharedPools.Default<StringBuilder>().AllocateAndClear();
        }

        public static void Free(StringBuilder builder)
        {
            SharedPools.Default<StringBuilder>().ClearAndFree(builder);
        }

        public static string ReturnAndFree(StringBuilder builder)
        {
            return SharedPools.Default<StringBuilder>().ReturnAndFree(builder);
        }
    }
}