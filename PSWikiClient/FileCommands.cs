using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PSWikiClient.Infrastructures;
using WikiClientLibrary.Files;
using WikiClientLibrary.Sites;

namespace PSWikiClient
{

    [Cmdlet(VerbsData.Publish, NounsCommon.WikiFile)]
    public class PublishWikiFileCommand : AsyncCmdlet
    {

        /// <summary>
        /// The site where to upload the new file.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }
        
        /// <summary>
        /// The title where to upload the new file.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public string Title { get; set; }

        [Parameter(Position = 2)]
        [Alias("Summary", "Comment")]
        [ValidateNotNull]
        public string Comment { get; set; }

        /// <summary>
        /// Upload the from the local file.
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string File { get; set; }

        /// <summary>
        /// Upload file by 1MB chunks.
        /// </summary>
        public SwitchParameter Chunked { get; set; }

        /// <summary>
        /// Ignore upload warnings.
        /// </summary>
        public SwitchParameter Force { get; set; }

        [Parameter]
        [ValidateSet("Watch", "Unwatch", "None", "Default", IgnoreCase = true)]
        public string Watch { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            using (var fs = new FileStream(File, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 4,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var uploadSource = new StreamUploadSource(fs);
                var result = await WikiSite.UploadAsync(Title, uploadSource, Comment, Force, Utility.ParseAutoWatchBehavior(Watch), cancellationToken);
                WriteObject(result);
            }
        }
    }
}
