using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using WikiClientLibrary.Wikibase;

namespace PSWikiClient.Wikibase
{
    internal class WikibaseUtility
    {

        private static IList<string> localLanguagesCache;

        private static readonly IDictionary<string, string> fallbackLanguages = new Dictionary<string, string>
        {
            {"zh-cn", "zh-hans"},
            {"zh-sg", "zh-hans"},
            {"zh-hk", "zh-hant"},
            {"zh-mo", "zh-hant"},
            {"zh-tw", "zh-hant"},
            {"zh-hant", "zh-hans"},
        };

        public static IList<string> GetLocalLanguages()
        {
            if (localLanguagesCache != null) return localLanguagesCache;
            var langs = new List<string>(EnumFallbackLanguages(CultureInfo.CurrentUICulture.IetfLanguageTag));
            if (CultureInfo.CurrentCulture.IetfLanguageTag != CultureInfo.CurrentUICulture.IetfLanguageTag)
            {
                langs.AddRange(EnumFallbackLanguages(CultureInfo.CurrentCulture.IetfLanguageTag));
            }
            langs.Add("en");
            var languages = langs.AsReadOnly();
            Volatile.Write(ref localLanguagesCache, languages);
            return languages;
        }

        private static IEnumerable<string> EnumFallbackLanguages(string lang)
        {
            lang = lang.ToLowerInvariant();
            while (lang != null)
            {
                yield return lang;
                if (!fallbackLanguages.TryGetValue(lang, out var fb))
                {
                    var sep = lang.LastIndexOf('-');
                    if (sep > 0) fb = lang.Substring(0, sep);
                    else break;
                }
                lang = fb;
            }
        }

        public static EntityEditOptions MakeEntityEditOptions(bool bulk = false, bool bot = false)
        {
            var opt = EntityEditOptions.None;
            if (bulk) opt |= EntityEditOptions.Bulk;
            if (bot) opt |= EntityEditOptions.Bot;
            return opt;
        }

    }
}
