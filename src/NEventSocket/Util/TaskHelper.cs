// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TaskHelper.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Util
{
    using System;
    using System.Threading.Tasks;

    internal static class TaskHelper
    {
        public static Task Completed { get { return Task.FromResult(0); } }

        /// <summary>
        /// Completes a TaskCompletionSource based on the failed outcome of another Task.
        /// </summary>
        /// <typeparam name="TResult">The TaskCompletionSource return type.</typeparam>
        /// <param name="task">The Task</param>
        /// <param name="tcs">The TaskCompletionSource</param>
        /// <param name="onFailure">Failure callback to be invoked on failure, usually for cleanup.</param>
        /// <returns>The Task continuation.</returns>
        public static Task ContinueOnFaultedOrCancelled<TResult>(this Task task, TaskCompletionSource<TResult> tcs, Action onFailure)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            if (tcs == null)
            {
                throw new ArgumentNullException("tcs");
            }

            if (onFailure == null)
            {
                throw new ArgumentNullException("onFailure");
            }

            task.ContinueWith(
                t =>
                    {
                        if (t.IsFaulted && t.Exception != null)
                        {
                            tcs.TrySetException(t.Exception);
                            onFailure();
                        }
                        else if (t.IsCanceled)
                        {
                            tcs.TrySetCanceled();
                            onFailure();
                        }
                    });

            return task;
        }

        public static Task ContinueWithCompleted<TR1, TR2>(this Task<TR1> task, TaskCompletionSource<TR2> tcs, Func<TR1, TR2> convert)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            if (tcs == null)
            {
                throw new ArgumentNullException("tcs");
            }

            if (convert == null)
            {
                throw new ArgumentNullException("convert");
            }

            task.ContinueWith(t => tcs.SetResult(convert(t.Result)), TaskContinuationOptions.OnlyOnRanToCompletion);

            return task;
        }

        public static Task<TResult> Then<TResult>(this Task<TResult> task, Action onSuccess)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            if (onSuccess == null)
            {
                throw new ArgumentNullException("onSuccess");
            }

            var tcs = new TaskCompletionSource<TResult>();

            task.ContinueWith(
                previousTask =>
                    {
                        if (previousTask.IsFaulted && previousTask.Exception != null)
                        {
                            tcs.TrySetException(previousTask.Exception);
                        }
                        else if (previousTask.IsCanceled)
                        {
                            tcs.TrySetCanceled();
                        }
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

        public static Task<TResult> Then<TResult>(this Task<TResult> task, Action<TResult> onSuccess)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            if (onSuccess == null)
            {
                throw new ArgumentNullException("onSuccess");
            }

            var tcs = new TaskCompletionSource<TResult>();

            task.ContinueWith(
                previousTask =>
                    {
                        if (previousTask.IsFaulted && previousTask.Exception != null)
                        {
                            tcs.TrySetException(previousTask.Exception);
                        }
                        else if (previousTask.IsCanceled)
                        {
                            tcs.TrySetCanceled();
                        }
                        else
                        {
                            try
                            {
                                onSuccess(task.Result);
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