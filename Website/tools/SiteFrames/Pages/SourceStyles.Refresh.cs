using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SiteFrames.Pages {
    internal static partial class SourceStyles {
        /// <summary>Source-only publication results; unresolved entries retain all their stored metadata.</summary>
        internal sealed class SourceRefreshResult {
            public List<string> Refreshed { get; } = new List<string>();
            public List<string> Unmatched { get; } = new List<string>();
            public List<string> Ambiguous { get; } = new List<string>();
        }

        /// <summary>
        /// Refreshes non-gallery C# tabs and mapping notes through the normal source publishers, without rendering,
        /// running examples, or changing stored frames, parity results or capture stamps. Published repository paths
        /// identify maintained programs; entries without a path require an exact token match. Unresolved or ambiguous
        /// sources are reported and left untouched.
        /// Project-backed examples require the same built project references as a normal source publication.
        /// </summary>
        internal static SourceRefreshResult RefreshStoredSources(Site site) {
            var result = new SourceRefreshResult();
            var programs = SourceFiles(Path.Combine(site.Website, "examples"))
                .Where(path => !Path.GetRelativePath(Path.Combine(site.Website, "examples"), path)
                    .StartsWith("gallery" + Path.DirectorySeparatorChar, StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
            var projects = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (string project in Directory.EnumerateFiles(Path.Combine(site.Website, "examples"), "*.csproj", SearchOption.AllDirectories)
                .Append(Path.Combine(site.Website, "tools", "TemplateDemo", "TemplateDemo.csproj")).OrderBy(path => path, StringComparer.Ordinal)) {
                string directory = Path.GetDirectoryName(project)!;
                foreach (string source in SourceFiles(directory).Concat(XDocument.Load(project).Descendants("Compile")
                    .Select(element => element.Attribute("Include")?.Value).OfType<string>()
                    .Select(include => Path.GetFullPath(Path.Combine(directory, include.Replace('\\', Path.DirectorySeparatorChar))))).Distinct(StringComparer.Ordinal)) {
                    programs.Add(source);
                    if (!projects.TryGetValue(source, out List<string>? owners)) { projects[source] = owners = new List<string>(); }
                    owners.Add(project);
                }
            }
            var byTokens = programs.OrderBy(path => path, StringComparer.Ordinal)
                .ToLookup(path => SourceIdentity(SourceSnippets.Program(Site.ReadText(path)).Full), StringComparer.Ordinal);
            foreach (string path in Directory.EnumerateFiles(Path.Combine(site.Website, "data"), "*.json").OrderBy(path => path, StringComparer.Ordinal)) {
                if (Path.GetFileName(path) == "gallery.json") { continue; }
                JsonNode data = JsonNode.Parse(File.ReadAllText(path))!;
                bool changed = false;
                foreach ((string location, JsonObject entry) in StoredPrograms(data, Path.GetFileName(path))) {
                    string? repositoryPath = entry["csharpRepo"]?.GetValue<string>();
                    string[] matches = repositoryPath == null
                        ? byTokens[SourceIdentity(entry["csharpFull"]!.GetValue<string>())].ToArray()
                        : programs.Where(program => program == Path.GetFullPath(Path.Combine(site.Repository, repositoryPath.Replace('/', Path.DirectorySeparatorChar)))).ToArray();
                    if (matches.Length == 0) { result.Unmatched.Add(location); continue; }
                    if (matches.Length != 1) {
                        result.Ambiguous.Add(location + ": " + string.Join(", ", matches.Select(file => Path.GetRelativePath(site.Repository, file))));
                        continue;
                    }
                    string program = matches[0];
                    if (projects.TryGetValue(program, out List<string>? owners) && owners.Count != 1) {
                        result.Ambiguous.Add(location + ": projects " + string.Join(", ", owners));
                        continue;
                    }
                    var companions = new List<string>();
                    var files = new JsonArray();
                    bool resolved = true;
                    foreach (JsonObject file in (entry["csharpFiles"]?.AsArray() ?? new JsonArray()).Select(node => node!.AsObject())) {
                        string? relative = file["codeRepo"]?.GetValue<string>();
                        string? companion = relative == null ? null : Path.GetFullPath(Path.Combine(site.Repository, relative.Replace('/', Path.DirectorySeparatorChar)));
                        if (companion == null || !File.Exists(companion)
                            || SourceIdentity(file["code"]!.GetValue<string>()) != SourceIdentity(SourceSnippets.Split(Site.ReadText(companion)).Full)) {
                            result.Unmatched.Add(location + "/csharpFiles/" + file["file"]!.GetValue<string>());
                            resolved = false;
                            break;
                        }
                        companions.Add(companion);
                        JsonObject refreshed = file.DeepClone().AsObject();
                        refreshed["code"] = SourceSnippets.Split(Site.ReadText(companion)).Full;
                        files.Add(refreshed);
                    }
                    if (!resolved) { continue; }
                    JsonObject updated = entry.DeepClone().AsObject();
                    (string shortForm, string full) = SourceSnippets.Program(Site.ReadText(program));
                    // Some project demos lead with one method, while their full tab contains the whole program.
                    // Keep that selection only when its own tokens also identify a unique maintained method.
                    string storedShort = SourceIdentity(entry["csharp"]!.GetValue<string>());
                    if (storedShort != SourceIdentity(shortForm)) {
                        string[] methods = CSharpSyntaxTree.ParseText(Site.ReadText(program)).GetRoot().DescendantNodes()
                            .OfType<MethodDeclarationSyntax>()
                            .Select(method => SourceSnippets.Split(SourceSnippets.Method(program, method.ToString().Split('\n')[0])).Short)
                            .Where(method => SourceIdentity(method) == storedShort).ToArray();
                        if (methods.Length > 1) { result.Ambiguous.Add(location + "/csharp: multiple method snippets"); continue; }
                        if (methods.Length == 1) { shortForm = methods[0]; }
                    }
                    updated["csharp"] = shortForm;
                    updated["csharpFull"] = full;
                    if (companions.Count > 0) { updated["csharpFiles"] = files; }
                    if (owners == null) { Add(site, updated, program, companions.ToArray()); }
                    else { AddProject(site, updated, program, owners[0]); }
                    // Copy only C# publication fields. Add can also discover a PowerShell sibling on a normal render.
                    foreach (string field in new[] { "csharp", "csharpFull", "csharpRepo", "csharpFiles", "csharpFluent", "csharpFluentFull", "csharpFluentFiles", "apiMap" }) {
                        if (updated[field] != null) { entry[field] = updated[field]!.DeepClone(); }
                    }
                    result.Refreshed.Add(location);
                    changed = true;
                }
                if (changed) { site.WriteJson(path, data); }
            }
            return result;
        }

        private static IEnumerable<string> SourceFiles(string directory) => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(directory, path).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"));

        private static IEnumerable<(string Location, JsonObject Entry)> StoredPrograms(JsonNode node, string location) {
            if (node is JsonObject entry) {
                if (entry["csharpFull"] is JsonValue) { yield return (location, entry); }
                foreach (var field in entry.ToArray()) {
                    if (field.Value == null) { continue; }
                    foreach (var found in StoredPrograms(field.Value, location + "/" + field.Key)) { yield return found; }
                }
            } else if (node is JsonArray array) {
                for (int index = 0; index < array.Count; index++) {
                    if (array[index] == null) { continue; }
                    foreach (var found in StoredPrograms(array[index]!, location + "/" + index)) { yield return found; }
                }
            }
        }

        // Token text, rather than ValueText or whitespace removal, preserves literal contents, escapes and token boundaries.
        private static string SourceIdentity(string code) {
            var identity = new StringBuilder();
            foreach (var token in CSharpSyntaxTree.ParseText(code).GetRoot().DescendantTokens()) {
                identity.Append(token.RawKind).Append(':').Append(token.Text.Length).Append(':').Append(token.Text).Append(';');
            }
            return identity.ToString();
        }
    }
}
