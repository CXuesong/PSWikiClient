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
            using (var syncContext = new QueueSynchronizationContext())
            {
                SynchronizationContext.SetSynchronizationContext(syncContext);
                try
                {
                    var task = ProcessRecordAsync(cts.Token);
                    task.ContinueWith(t => cts.Cancel());
                    syncContext.Run(cts.Token);
                    task.GetAwaiter().GetResult();
                }
                finally
                {
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
            cts = null;
        }

        protected abstract Task ProcessRecordAsync(CancellationToken cancellationToken);

    }
}
