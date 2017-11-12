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

    [Cmdlet(VerbsCommon.New, NounsCommon.WikiSite)]
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
                var passwordPtr = IntPtr.Zero;
                string unsafePassword;
                try
                {
                    passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(Password);
                    unsafePassword = Marshal.PtrToStringUni(passwordPtr);
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
                }
                site = new WikiSite(WikiClient, new SiteOptions(ApiEndpoint), UserName, unsafePassword);
            }
            else
            {
                site = new WikiSite(WikiClient, ApiEndpoint);
            }
            await site.Initialization;
            WriteObject(site);
        }
    }

}
