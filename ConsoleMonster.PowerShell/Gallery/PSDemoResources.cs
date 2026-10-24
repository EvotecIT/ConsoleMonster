using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ConsoleMonster.PowerShell {
    /// <summary>Reads the gallery's canonical scripts and metadata embedded at build time.</summary>
    internal static class PSDemoResources {
        private const string Prefix = "ConsoleMonster.PowerShell.Gallery.";
        private const string MetadataSuffix = ".metadata.json";
        private static readonly Assembly Assembly = typeof(PSDemoResources).Assembly;

        internal static string[] Slugs() {
            var slugs = new List<string>();
            foreach (string name in Assembly.GetManifestResourceNames()) {
                if (name.StartsWith(Prefix, StringComparison.Ordinal) && name.EndsWith(MetadataSuffix, StringComparison.Ordinal)) {
                    slugs.Add(name.Substring(Prefix.Length, name.Length - Prefix.Length - MetadataSuffix.Length));
                }
            }
            slugs.Sort(StringComparer.Ordinal);
            return slugs.ToArray();
        }

        internal static string? Find(string slug) {
            foreach (string candidate in Slugs()) {
                if (string.Equals(candidate, slug, StringComparison.OrdinalIgnoreCase)) { return candidate; }
            }
            return null;
        }

        internal static string Metadata(string slug) => Read(slug, "metadata.json");
        internal static string Script(string slug) => Read(slug, "demo.ps1");

        private static string Read(string slug, string file) {
            string name = Prefix + slug + "." + file;
            using (Stream stream = Assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException("The embedded gallery resource is missing: " + name))
            using (var reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }
    }
}
