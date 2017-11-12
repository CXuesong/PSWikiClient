using PSWikiClient.Infrastructures;
using System;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;

namespace PSWikiClient
{

    /// <summary>
    /// Creates a new <see cref="WikiSite"/> instance.
    /// </summary>
    [Cmdlet(VerbsCommon.New, NounsCommon.WikiSite)]
    [OutputType(typeof(WikiSite))]
    public class NewWikiSiteCommand : AsyncCmdlet
    {

        /// <summary>
        /// The <see cref="WikiClient"/> on which to issue the requests.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Default")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "WithLogin")]
        [ValidateNotNull]
        public WikiClient WikiClient { get; set; }

        /// <summary>
        /// API endpoint URL of the site.
        /// </summary>
        /// <remarks>To search for an API endpoint from an arbitary URL taken from a MediaWiki site,
        /// use <see cref="SearchMediaWikiEndpointCommand"/>.</remarks>
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
    /// Logins into <see cref="WikiSite"/>.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, NounsCommon.WikiAccount)]
    [Alias("Login-WikiAccount", "Login-WikiSite")]
    public class AddWikiAccountCommand : AsyncCmdlet
    {

        /// <summary>
        /// The site to be logged-in.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }

        /// <summary>
        /// User name.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public string UserName { get; set; }

        /// <summary>
        /// Password.
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
    /// Logouts from <see cref="WikiSite"/>.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, NounsCommon.WikiAccount)]
    [Alias("Logout-WikiAccount", "Logout-WikiSite")]
    public class RemoveWikiAccountCommand : AsyncCmdlet
    {

        /// <summary>
        /// The site to be logged-out.
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
    /// Searches the MediaWiki API endpoint URL starting with an arbitary given
    /// URL from that site.
    /// </summary>
    [Cmdlet(VerbsCommon.Search, NounsCommon.MediaWikiEndpoint)]
    [Alias("Search-MWEndpoint")]
    public class SearchMediaWikiEndpointCommand : AsyncCmdlet
    {

        /// <summary>
        /// The <see cref="WikiClient"/> on which to issue the requests.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiClient WikiClient { get; set; }

        /// <summary>
        /// The URL from which to test and search for MediaWiki API endpoint.
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
