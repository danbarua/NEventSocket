// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncAutoResetEvent.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   An Asynchronous version of <see cref="System.Threading.AutoResetEvent" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Util
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// An Asynchronous version of <see cref="System.Threading.AutoResetEvent"/>.
    /// </summary>
    public class AsyncAutoResetEvent
    {
        private static readonly Task Completed = Task.FromResult(true);

        private readonly Queue<TaskCompletionSource<bool>> waits = new Queue<TaskCompletionSource<bool>>();

        private bool signalled;

        public AsyncAutoResetEvent(bool initialState = false)
        {
            this.signalled = initialState;
        }

        public Task WaitAsync()
        {
            lock (waits)
            {
                if (signalled)
                {
                    signalled = false;
                    return Completed;
                }
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    waits.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }

        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;

            lock (waits)
            {
                if (waits.Count > 0) toRelease = waits.Dequeue();
                else if (!signalled) signalled = true;
            }

            if (toRelease != null) toRelease.SetResult(true);
        }
    }
}