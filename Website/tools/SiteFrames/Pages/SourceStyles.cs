using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Publishes a compiled fluent translation beside an example's maintained initializer source.</summary>
    internal static partial class SourceStyles {
        private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> Generated = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

        public static JsonObject Add(Site site, JsonObject entry, string path, params string[] companions) => AddCore(site, entry, path, companions, null);

        public static JsonObject AddProject(Site site, JsonObject entry, string path, string project) => AddCore(site, entry, path, Array.Empty<string>(), project);

        private static JsonObject AddCore(Site site, JsonObject entry, string path, string[] companions, string? project) {
            string key = (project ?? "") + "\n" + path + "\n" + Site.ReadText(path) + "\n" + string.Join("\n", Array.ConvertAll(companions, Site.ReadText));
            if (!Generated.TryGetValue(key, out IReadOnlyDictionary<string, string>? generated)) {
                generated = project == null ? CSharpRunner.FluentSources(path, companions) : CSharpRunner.FluentProjectSources(path, project, companions);
                if (project == null) { CSharpRunner.CheckSources(path, generated, companions); }
                else { CSharpRunner.CheckProjectSources(path, project, generated, companions); }
                Generated.Add(key, generated);
            }
            (string shortForm, string full) snippet;
            try { snippet = SourceSnippets.Program(generated[path]); }
            catch (InvalidOperationException error) { throw new InvalidDataException(path + ": " + error.Message, error); }
            var (shortForm, full) = snippet;
            string script = Path.ChangeExtension(path, ".ps1");
            if (entry["powershell"] == null && entry["script"] == null && File.Exists(script)) {
                (string ps, string psFull) = SourceSnippets.Program(Site.ReadText(script));
                entry["powershell"] = ps;
                entry["powershellFull"] = psFull;
                entry["powershellFile"] = Path.GetFileName(script);
                // Figure shortcodes consume script, while the shared source partial consumes powershell.
                entry["script"] = ps;
                entry["scriptFull"] = psFull;
                entry["file"] = Path.GetFileName(script);
            }
            entry["csharpFluent"] = shortForm;
            entry["csharpFluentFull"] = full;
            entry["csharpFluentFile"] = "Program.cs";
            string? powerShell = entry["powershellRepo"]?.GetValue<string>();
            if (powerShell != null) { powerShell = Path.Combine(site.Repository, powerShell.Replace('/', Path.DirectorySeparatorChar)); }
            else if (File.Exists(script)) { powerShell = script; }
            HashSet<string> commands = ApiMappingNotes.CommandsFor(site, powerShell);
            entry["apiMap"] = project == null ? ApiMappingNotes.FromSource(site, path, generated[path], companions, commands) : ApiMappingNotes.ForProject(site, path, project, generated[path], commands);
            if (companions.Length > 0) {
                var files = new JsonArray();
                foreach (string companion in companions) {
                    files.Add(new JsonObject { ["file"] = Path.GetFileName(companion), ["code"] = SourceSnippets.Split(generated[companion]).Full });
                }
                entry["csharpFluentFiles"] = files;
            }
            return entry;
        }
    }
}
