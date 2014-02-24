namespace NEventSocket.Util
{
    using System;
    using System.Threading.Tasks;

    internal static class TaskHelper
    {
        /// <summary>
        /// Completes a TaskCompletionSource based on the failed outcome of another Task.
        /// </summary>
        /// <typeparam name="TResult">The TaskCompletionSource return type.</typeparam>
        /// <param name="task">The Task</param>
        /// <param name="tcs">The TaskCompletionSource</param>
        /// <param name="onFailure">Failure callback to be invoked on failure, usually for cleanup.</param>
        /// <returns>The Task</returns>
        public static Task ContinueWithNotComplete<TResult>(this Task task, TaskCompletionSource<TResult> tcs, Action onFailure)
        {
            if (task == null) throw new ArgumentNullException("task");
            if (tcs == null) throw new ArgumentNullException("tcs");
            if (onFailure == null) throw new ArgumentNullException("onFailure");

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

        public static Task ContinueWithCompleted<TR1, TR2>(this Task<TR1> task, TaskCompletionSource<TR2> tcs, Func<TR1, TR2> convert)
        {
            if (task == null) throw new ArgumentNullException("task");
            if (tcs == null) throw new ArgumentNullException("tcs");
            if (convert == null) throw new ArgumentNullException("convert");

            task.ContinueWith(t => tcs.SetResult(convert(t.Result)), TaskContinuationOptions.OnlyOnRanToCompletion);

            return task;
        }

        public static Task<TResult> Then<TResult>(this Task<TResult> task, Action onSuccess)
        {
            if (task == null) throw new ArgumentNullException("task");
            if (onSuccess == null) throw new ArgumentNullException("onSuccess");

            var tcs = new TaskCompletionSource<TResult>();

            task.ContinueWith(previousTask =>
                {
                    if (previousTask.IsFaulted && previousTask.Exception != null) tcs.TrySetException(previousTask.Exception);
                    else if (previousTask.IsCanceled) tcs.TrySetCanceled();
                    else
                    {
                        try
                        {
                            onSuccess();
                            tcs.TrySetResult(previousTask.Result);
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                    }
                });

            return tcs.Task;
        }
    }

}