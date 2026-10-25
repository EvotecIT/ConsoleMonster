using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Publishes a compiled fluent translation beside an example's maintained initializer source.</summary>
    internal static class SourceStyles {
        private static readonly Dictionary<string, string> Generated = new Dictionary<string, string>(StringComparer.Ordinal);

        public static JsonObject Add(Site site, JsonObject entry, string path, params string[] companions) {
            string key = path + "\n" + Site.ReadText(path) + "\n" + string.Join("\n", Array.ConvertAll(companions, Site.ReadText));
            if (!Generated.TryGetValue(key, out string? generated)) {
                generated = CSharpRunner.FluentSource(path, companions);
                CSharpRunner.Check(path, generated, companions);
                Generated.Add(key, generated);
            }
            (string shortForm, string full) snippet;
            try { snippet = SourceSnippets.Program(generated); }
            catch (InvalidOperationException error) { throw new InvalidDataException(path + ": " + error.Message, error); }
            var (shortForm, full) = snippet;
            entry["csharpFluent"] = shortForm;
            entry["csharpFluentFull"] = full;
            entry["csharpFluentFile"] = "Program.cs";
            entry["apiMap"] = ApiMappingNotes.FromSource(site, path, generated, companions);
            return entry;
        }
    }
}
