using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PSWikiClient.Infrastructures;
using WikiClientLibrary.Infrastructures;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Pages.Queries;
using WikiClientLibrary.Pages.Queries.Properties;
using WikiClientLibrary.Sites;

namespace PSWikiClient
{

    /// <summary>
    /// <para type="description">Creates a new <see cref="WikiPage"/> instance.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, NounsCommon.WikiPage)]
    [OutputType(typeof(WikiPage))]
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
            var page = new WikiPage(WikiSite, Title);
            WriteObject(page);
        }
    }

    /// <summary>
    /// <para type="description">Gets the information and/or content of a sequence of <see cref="WikiPage"/>.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, NounsCommon.WikiPage)]
    [OutputType(typeof(WikiPage))]
    public class GetWikiPageCommand : AsyncCmdlet
    {

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Titles")]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = "Pages")]
        [ValidateNotNullOrEmpty]
        public WikiPage[] Pages { get; set; }

        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true, ParameterSetName = "Titles")]
        [ValidateNotNullOrEmpty]
        public string[] Titles { get; set; }

        [Parameter]
        public SwitchParameter Content { get; set; }

        [Parameter]
        public SwitchParameter ResolveRedirects { get; set; }

        [Parameter]
        public SwitchParameter Extract { get; set; }

        [Parameter]
        public SwitchParameter GeoCoordinate { get; set; }

        private WikiPage[] GetPages()
        {
            if (Pages != null) return Pages;
            if (Titles != null) return Titles.Select(t => new WikiPage(WikiSite, t)).ToArray();
            throw new InvalidOperationException();
        }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var pages = GetPages();
            var options = PageQueryOptions.None;
            if (Content) options |= PageQueryOptions.FetchContent;
            if (ResolveRedirects) options |= PageQueryOptions.ResolveRedirects;
            var provider = MediaWikiHelper.QueryProviderFromOptions(options);
            if (Extract | GeoCoordinate)
            {
                var provider1 = WikiPageQueryProvider.FromOptions(options);
                if (Extract)
                {
                    provider1.Properties.Add(new ExtractsPropertyProvider
                    {
                        AsPlainText = true,
                        IntroductionOnly = true,
                        MaxSentences = 1,
                    });
                }
                if (GeoCoordinate)
                {
                    provider1.Properties.Add(new GeoCoordinatesPropertyProvider());
                }
                provider = provider1;
            }
            await pages.RefreshAsync(provider, cancellationToken);
            WriteObject(pages, true);
        }
    }

    [Cmdlet(VerbsData.Publish, NounsCommon.WikiPage, SupportsShouldProcess = true)]
    [OutputType(typeof(bool))]
    public class PublishWikiPageCommand : AsyncCmdlet
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
            if (!ShouldProcess(Page.ToString()))
            {
                WriteObject(false);
                return;
            }
            var result = await Page.UpdateContentAsync(Summary, Minor, Bot,
                Utility.ParseAutoWatchBehavior(Watch), cancellationToken);
            WriteObject(result);
        }

    }

    [Cmdlet(VerbsCommon.Move, NounsCommon.WikiPage, SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
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
            if (!ShouldProcess(Page.ToString())) return Task.CompletedTask;
            return Page.MoveAsync(NewTitle, Reason, options,
                Utility.ParseAutoWatchBehavior(Watch), cancellationToken);
        }

    }

    [Cmdlet(VerbsCommon.Remove, NounsCommon.WikiPage, SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
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
            if (!ShouldProcess(Page.ToString())) return Task.CompletedTask;
            return Page.DeleteAsync(Reason, Utility.ParseAutoWatchBehavior(Watch), cancellationToken);
        }

    }

}
