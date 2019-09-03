using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;
using WikiClientLibrary.Client;

namespace PSWikiClient
{

    /// <summary>
    /// <para type="description">Instantiates a new <see cref="WikiClient"/> instance.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, NounsCommon.WikiClient)]
    [OutputType(typeof(WikiClient))]
    public class NewWikiClientCommand : Cmdlet
    {

        /// <summary>
        /// <para type="description">Load state content from string as persisted by <see cref="SaveWikiClientCommand"/>.</para>
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true, ParameterSetName = "FromState")]
        [ValidateNotNullOrEmpty]
        public string StateContent { get; set; }

        /// <summary>
        /// <para type="description">Load state content from file as persisted by <see cref="SaveWikiClientCommand"/>.</para>
        /// </summary>
        [Parameter(ParameterSetName = "FromStateFile")]
        [ValidateNotNullOrEmpty]
        public string StateFile { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var client = new WikiClient
            {
                ClientUserAgent = "PSWikiClient/0.1 (https://github.com/CXuesong/PSWikiClient)",
                Timeout = TimeSpan.FromMinutes(1),
            };
            WikiClientStateHolder state = null;
            if (StateContent != null)
                state = Utility.LoadJson<WikiClientStateHolder>(StateContent);
            else if (StateFile != null)
                state = Utility.LoadJsonFrom<WikiClientStateHolder>(StateFile);
            if (state != null)
            {
                client.CookieContainer = state.TryGetCookies();
            }
            WriteObject(client);
        }
    }

    /// <summary>
    /// <para type="description">Persists the state content of <see cref="WikiClient"/>.</para>
    /// </summary>
    [Cmdlet(VerbsData.Save, NounsCommon.WikiClient)]
    public class SaveWikiClientCommand : Cmdlet
    {
        /// <summary>
        /// <para type="description">The client instance to be persisted.</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNull]
        public WikiClient WikiClient { get; set; }

        /// <summary>
        /// <para type="description">The path of the persisted state file.</para>
        /// </summary>
        [Parameter(Position = 1)]
        public string StateFile { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var holder = new WikiClientStateHolder();
            holder.SetCookies(WikiClient.CookieContainer);
            if (StateFile == null)
                WriteObject(Utility.SaveJson(holder));
            else
                Utility.SaveJsonTo(StateFile, holder);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class WikiClientStateHolder
    {

        private static readonly BinaryFormatter formatter = new BinaryFormatter();

        [JsonProperty]
        public byte[] RawCookies { get; set; }

        public void SetCookies(CookieContainer cookieContainer)
        {
            if (cookieContainer == null)
            {
                RawCookies = null;
                return;
            }
            using (var s = new MemoryStream())
            {
                formatter.Serialize(s, cookieContainer);
                RawCookies = s.ToArray();
            }
        }

        public CookieContainer TryGetCookies()
        {
            if (RawCookies == null || RawCookies.Length == 0) return null;
            using (var s = new MemoryStream(RawCookies, false))
            {
                return (CookieContainer)formatter.Deserialize(s);
            }
        }

    }

}
