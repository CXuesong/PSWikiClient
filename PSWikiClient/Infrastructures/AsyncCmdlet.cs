using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSWikiClient.Infrastructures
{
    public abstract class AsyncCmdlet : Cmdlet
    {

        private CancellationTokenSource cts;

        /// <inheritdoc />
        protected override void BeginProcessing()
        {
            cts = new CancellationTokenSource();
        }

        /// <inheritdoc />
        protected sealed override void ProcessRecord()
        {
            QueueSynchronizationContext syncContext = null;
            if (SynchronizationContext.Current == null)
            {
                SynchronizationContext.SetSynchronizationContext(syncContext = new QueueSynchronizationContext());
            }
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            try
            {
                var task = Task.Factory.StartNew(() => ProcessRecordAsync(cts.Token), cts.Token,
                    TaskCreationOptions.None, scheduler);
                if (syncContext == null)
                {
                    task.GetAwaiter().GetResult();
                }
                else
                {
                    task.ContinueWith(t => cts.Cancel());
                    syncContext.Run(cts.Token);
                }
            }
            finally
            {
                if (syncContext != null)
                {
                    syncContext.Dispose();
                    SynchronizationContext.SetSynchronizationContext(null);
                }
            }
        }

        /// <inheritdoc />
        protected sealed override void StopProcessing()
        {
            cts.Cancel();
        }

        /// <inheritdoc />
        protected override void EndProcessing()
        {
            cts.Dispose();
        }

        protected abstract Task ProcessRecordAsync(CancellationToken cancellationToken);

    }
}
