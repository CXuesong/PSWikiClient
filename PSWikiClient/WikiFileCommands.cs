using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PSWikiClient.Infrastructures;
using WikiClientLibrary;
using WikiClientLibrary.Files;
using WikiClientLibrary.Sites;

namespace PSWikiClient
{

    /// <summary>
    /// <para type="description">Uploads a file to MediaWiki site.</para>
    /// </summary>
    [Cmdlet(VerbsData.Publish, NounsCommon.WikiFile, SupportsShouldProcess = true)]
    [OutputType(typeof(UploadResult))]
    public class PublishWikiFileCommand : AsyncCmdlet
    {

        /// <summary>
        /// <para type="description">Upload from the local file.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public FileInfo File { get; set; }

        /// <summary>
        /// <para type="description">The site where to upload the new file.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }
        
        /// <summary>
        /// <para type="description">The title where to upload the new file.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 2)]
        [ValidateNotNullOrEmpty]
        public string Title { get; set; }

        [Parameter(Position = 3)]
        [Alias("Summary")]
        [ValidateNotNull]
        public string Comment { get; set; }

        /// <summary>
        /// <para type="description">Upload file by 500KB chunks.</para>
        /// </summary>
        [Parameter]
        public SwitchParameter Chunked { get; set; }

        /// <summary>
        /// <para type="description">Ignore upload warnings.</para>
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        [ValidateSet("Watch", "Unwatch", "None", "Default", IgnoreCase = true)]
        public string Watch { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            using (var fs = new FileStream(File.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 4,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var titleLink = WikiLink.Parse(WikiSite, Title, BuiltInNamespaces.File);
                WikiUploadSource uploadSource;
                if (Chunked)
                {
                    if (!ShouldProcess($"{File}", "Chunked stash")) return;
                    var src = new ChunkedUploadSource(WikiSite, fs, File.Name)
                    {
                        DefaultChunkSize = 512 * 1024
                    };
                    var progress = new ProgressRecord(0, $"Stash {File}", null);
                    WriteProgress(progress);
                    do
                    {
                        var r = await src.StashNextChunkAsync(cancellationToken);
                        if (r.ResultCode == UploadResultCode.Warning)
                        {
                            WriteWarning(r.Warnings.ToString());
                        }
                        progress.PercentComplete = (int) (100.0 * src.UploadedSize / src.TotalSize);
                    } while (!src.IsStashed);
                    progress.RecordType = ProgressRecordType.Completed;
                    uploadSource = src;
                }
                else
                {
                    uploadSource = new StreamUploadSource(fs);
                }
                if (!ShouldProcess($"{File} -> {titleLink}", "Upload")) return;
                var result = await WikiSite.UploadAsync(Title, uploadSource, Comment, Force, Utility.ParseAutoWatchBehavior(Watch), cancellationToken);
                if (result.ResultCode == UploadResultCode.Warning)
                {
                    WriteWarning(result.Warnings.ToString());
                }
                WriteObject(result);
            }
        }
    }
}
