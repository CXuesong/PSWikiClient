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

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Default")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "WithLogin")]
        [ValidateNotNull]
        public WikiClient WikiClient { get; set; }

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

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public string UserName { get; set; }

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

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNull]
        public WikiSite WikiSite { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            await WikiSite.LogoutAsync();
        }
    }

}
