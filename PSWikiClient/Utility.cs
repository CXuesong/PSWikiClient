using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Newtonsoft.Json;
using WikiClientLibrary.Pages;

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

        public static string UnsafeSecureStringToString(SecureString ss)
        {
            var passwordPtr = IntPtr.Zero;
            try
            {
                passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(ss);
                return Marshal.PtrToStringUni(passwordPtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
            }
        }

        public static AutoWatchBehavior ParseAutoWatchBehavior(string expr)
        {
            if (expr == null) return AutoWatchBehavior.Default;
            if ("Watch".Equals(expr, StringComparison.OrdinalIgnoreCase))
                return AutoWatchBehavior.Watch;
            if ("Unwatch".Equals(expr, StringComparison.OrdinalIgnoreCase))
                return AutoWatchBehavior.Unwatch;
            if ("None".Equals(expr, StringComparison.OrdinalIgnoreCase))
                return AutoWatchBehavior.None;
            if ("Default".Equals(expr, StringComparison.OrdinalIgnoreCase))
                return AutoWatchBehavior.Default;
            throw new ArgumentException("Invalid AutoWatchBehavior value.", nameof(expr));
        }

        public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (source is List<T> l)
            {
                l.AddRange(items);
                return;
            }
            foreach (var i in items) source.Add(i);
        }

    }
}
