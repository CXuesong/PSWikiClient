using PSWikiClient.Infrastructures;
using System;
using System.Management.Automation;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;

namespace PSWikiClient
{

    /// <summary>
    /// <para type="description">Creates a new <see cref="WikiSite"/> instance.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, NounsCommon.WikiSite)]
    [OutputType(typeof(WikiSite))]
    public class NewWikiSiteCommand : AsyncCmdlet
    {

        /// <summary>
        /// <para type="description">The <see cref="WikiClient"/> on which to issue the requests.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Default")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "WithLogin")]
        [ValidateNotNull]
        public WikiClient WikiClient { get; set; }

        /// <summary>
        /// <para type="description">API endpoint URL of the site.</para>
        /// </summary>
        /// <remarks>To search for an API endpoint from an arbitary URL taken from a MediaWiki site,
        /// <para type="description">use <see cref="SearchMediaWikiEndpointCommand"/>.</remarks></para>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true)]
        public string ApiEndpoint { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "WithLogin")]
        [ValidateNotNullOrEmpty]
        public string UserName { get; set; }

        [Parameter(Mandatory = true, Position = 3, ParameterSetName = "WithLogin")]
        [ValidateNotNullOrEmpty]
        public SecureString Password { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing()
        {
            base.EndProcessing();
            Password?.Dispose();
        }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            WikiSite site;
            if (UserName != null)
            {
                site = new WikiSite(WikiClient, new SiteOptions(ApiEndpoint), UserName, Utility.UnsafeSecureStringToString(Password));
            }
            else
            {
                site = new WikiSite(WikiClient, ApiEndpoint);
            }
            await site.Initialization;
            WriteObject(site);
        }
    }

    /// <summary>
    /// <para type="description">Logins into <see cref="WikiSite"/>.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, NounsCommon.WikiAccount)]
    public class SetWikiAccountCommand : AsyncCmdlet
    {

        /// <summary>
        /// <para type="description">The site to be logged-in.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }

        /// <summary>
        /// <para type="description">User name.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public string UserName { get; set; }

        /// <summary>
        /// <para type="description">Password.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 2)]
        [ValidateNotNullOrEmpty]
        public SecureString Password { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            await WikiSite.LoginAsync(UserName, Utility.UnsafeSecureStringToString(Password));
        }
    }

    /// <summary>
    /// <para type="description">Logouts from <see cref="WikiSite"/>.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, NounsCommon.WikiAccount)]
    public class RemoveWikiAccountCommand : AsyncCmdlet
    {

        /// <summary>
        /// <para type="description">The site to be logged-out.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            await WikiSite.LogoutAsync();
        }
    }

    /// <summary>
    /// <para type="description">Searches the MediaWiki API endpoint URL starting with an arbitrary given</para>
    /// <para type="description">URL from that site.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Search, NounsCommon.MediaWikiEndpoint)]
    public class SearchMediaWikiEndpointCommand : AsyncCmdlet
    {

        /// <summary>
        /// <para type="description">The <see cref="WikiClient"/> on which to issue the requests.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiClient WikiClient { get; set; }

        /// <summary>
        /// <para type="description">The URL from which to test and search for MediaWiki API endpoint.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string StartupPath { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var result = await WikiSite.SearchApiEndpointAsync(WikiClient, StartupPath);
            WriteObject(result);
        }
    }

}
