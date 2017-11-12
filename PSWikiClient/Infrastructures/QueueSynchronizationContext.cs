using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace PSWikiClient.Infrastructures
{
    internal class QueueSynchronizationContext : SynchronizationContext, IDisposable
    {

        private struct WorkItem
        {
            public readonly SendOrPostCallback Callback;

            public readonly object State;

            public readonly ManualResetEventSlim Completion;

            public Exception Exception;

            public WorkItem(SendOrPostCallback callback, object state, bool needCompletion)
            {
                Callback = callback;
                State = state;
                Completion = needCompletion ? new ManualResetEventSlim(false) : null;
                Exception = null;
            }

        }

        private readonly Queue<WorkItem> queue = new Queue<WorkItem>();
        private readonly SemaphoreSlim queueSemaphore = new SemaphoreSlim(0);

        public QueueSynchronizationContext()
        {
        }

        public void Run(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (!queueSemaphore.Wait(0))
                {
                    // We check for cancellationToken only when the queue has been exhausted.
                    if (cancellationToken.IsCancellationRequested) return;
                    try
                    {
                        queueSemaphore.Wait(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
                WorkItem item;
                lock (queue)
                    item = queue.Dequeue();
                try
                {
                    item.Callback(item.State);
                }
                catch (Exception e)
                {
                    item.Exception = e;
                }
                finally
                {
                    if (item.Completion != null) item.Completion.Set();
                }
            }
        }

        /// <inheritdoc />
        public override void Send(SendOrPostCallback d, object state)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            var item = new WorkItem(d, state, true);
            lock (queue) queue.Enqueue(item);
            queueSemaphore.Release();
            item.Completion.Wait();
            item.Completion.Dispose();
            if (item.Exception != null)
                ExceptionDispatchInfo.Capture(item.Exception).Throw();
        }

        /// <inheritdoc />
        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            lock (queue) queue.Enqueue(new WorkItem(d, state, false));
            queueSemaphore.Release();
        }

        /// <inheritdoc />
        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            queueSemaphore.Dispose();
            lock (queue)
            {
                foreach (var item in queue)
                    item.Completion?.Dispose();
                queue.Clear();
            }
        }
    }
}
