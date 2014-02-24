namespace NEventSocket.Util
{
    using System;
    using System.Threading.Tasks;

    internal static class TaskHelper
    {
        /// <summary>
        /// Completes a TaskCompletionSource based on the failed outcome of another Task.
        /// </summary>
        /// <typeparam name="T">The TaskCompletionSource return type.</typeparam>
        /// <param name="task">The Task</param>
        /// <param name="tcs">The TaskCompletionSource</param>
        /// <param name="onFailure">Failure callback to be invoked on failure, usually for cleanup.</param>
        /// <returns>The Task</returns>
        public static Task ContinueWithNotComplete<T>(this Task task, TaskCompletionSource<T> tcs, Action onFailure)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    tcs.SetException(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    tcs.SetCanceled();
                }

                onFailure();
            },
            TaskContinuationOptions.NotOnRanToCompletion);

            return task;
        }
    }
}