using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PSWikiClient.Infrastructures;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace PSWikiClient
{

    /// <summary>
    /// Creates a new <see cref="WikiPage"/> instance.
    /// </summary>
    [Cmdlet(VerbsCommon.New, NounsCommon.WikiPage)]
    public class NewWikiPageCommand : Cmdlet
    {

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }

        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Title { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var page = WikiPage.FromTitle(WikiSite, Title);
            WriteObject(page);
        }
    }

    [Cmdlet(VerbsCommunications.Read, NounsCommon.WikiPage)]
    [Alias("Refresh-WikiPage")]
    public class ReadWikiPageCommand : AsyncCmdlet
    {

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public WikiPage[] Pages { get; set; }

        [Parameter]
        public SwitchParameter Content { get; set; }

        [Parameter]
        public SwitchParameter ResolveRedirects { get; set; }

        [Parameter]
        public SwitchParameter Extract { get; set; }

        [Parameter]
        public SwitchParameter GeoCoordinate { get; set; }

        /// <inheritdoc />
        protected override Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var options = PageQueryOptions.None;
            if (Content) options |= PageQueryOptions.FetchContent;
            if (ResolveRedirects) options |= PageQueryOptions.ResolveRedirects;
            if (Extract) options |= PageQueryOptions.FetchExtract;
            if (GeoCoordinate) options |= PageQueryOptions.FetchGeoCoordinate;
            return Pages.RefreshAsync(options, cancellationToken);
        }
    }

    [Cmdlet(VerbsCommunications.Write, NounsCommon.WikiPage)]
    [Alias("Update-WikiPage")]
    public class WriteWikiPageCommand : AsyncCmdlet
    {

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNull]
        public WikiPage Page { get; set; }

        [Parameter(Position = 1)]
        [Alias("Comment")]
        [ValidateNotNull]
        public string Summary { get; set; }

        [Parameter]
        public SwitchParameter Bot { get; set; }

        [Parameter]
        public SwitchParameter Minor { get; set; }

        [Parameter]
        [ValidateSet("Watch", "Unwatch", "None", "Default", IgnoreCase = true)]
        public string Watch { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var result = await Page.UpdateContentAsync(Summary, Minor, Bot,
                Utility.ParseAutoWatchBehavior(Watch), cancellationToken);
            WriteObject(result);
        }

    }

    [Cmdlet(VerbsCommon.Move, NounsCommon.WikiPage)]
    public class MoveWikiPageCommand : AsyncCmdlet
    {

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNull]
        public WikiPage Page { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public string NewTitle { get; set; }

        [Parameter(Position = 2)]
        [Alias("Summary", "Comment")]
        [ValidateNotNull]
        public string Reason { get; set; }

        [Parameter]
        [ValidateSet("Watch", "Unwatch", "None", "Default", IgnoreCase = true)]
        public string Watch { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public SwitchParameter LeaveTalk { get; set; }

        [Parameter]
        public SwitchParameter NoRedirect { get; set; }

        /// <inheritdoc />
        protected override Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var options = PageMovingOptions.None;
            if (Force) options |= PageMovingOptions.IgnoreWarnings;
            if (Recurse) options |= PageMovingOptions.MoveSubpages;
            if (LeaveTalk) options |= PageMovingOptions.LeaveTalk;
            if (NoRedirect) options |= PageMovingOptions.NoRedirect;
            return Page.MoveAsync(NewTitle, Reason, options,
                Utility.ParseAutoWatchBehavior(Watch), cancellationToken);
        }

    }

    [Cmdlet(VerbsCommon.Remove, NounsCommon.WikiPage)]
    [Alias("Delete-WikiPage")]
    public class DeleteWikiPageCommand : AsyncCmdlet
    {

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNull]
        public WikiPage Page { get; set; }

        [Parameter(Position = 1)]
        [Alias("Summary", "Comment")]
        [ValidateNotNull]
        public string Reason { get; set; }

        [Parameter]
        [ValidateSet("Watch", "Unwatch", "None", "Default", IgnoreCase = true)]
        public string Watch { get; set; }

        /// <inheritdoc />
        protected override Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            return Page.DeleteAsync(Reason, Utility.ParseAutoWatchBehavior(Watch), cancellationToken);
        }

    }

}
