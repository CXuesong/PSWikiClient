using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace PSWikiClient
{
    public static class Utility
    {

        public static JsonSerializer JsonSerializer = new JsonSerializer();

        public static T LoadJson<T>(string content)
        {
            using (var reader = new StringReader(content))
            using (var jreader = new JsonTextReader(reader))
            {
                return JsonSerializer.Deserialize<T>(jreader);
            }
        }

        public static T LoadJsonFrom<T>(string path)
        {
            using (var reader = File.OpenText(path))
            using (var jreader = new JsonTextReader(reader))
            {
                return JsonSerializer.Deserialize<T>(jreader);
            }
        }

        public static string SaveJson(object value)
        {
            using (var writer = new StringWriter())
            {
                using (var jwriter = new JsonTextWriter(writer))
                {
                    JsonSerializer.Serialize(jwriter, value);
                }
                return writer.ToString();
            }
        }

        public static void SaveJsonTo(string path, object value)
        {
            using (var writer = File.CreateText(path))
            using (var jwriter = new JsonTextWriter(writer))
            {
                JsonSerializer.Serialize(jwriter, value);
            }
        }

    }
}
