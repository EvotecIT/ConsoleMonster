using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    internal static partial class ApiMappingNotes {
        private static readonly Dictionary<string, Dictionary<string, HashSet<string>>> ScriptCommands = new(StringComparer.Ordinal);

        /// <summary>Prefers the cmdlet and parameter actually used in the displayed PowerShell source.</summary>
        public static HashSet<string> CommandsFor(Site site, string? script) {
            if (script == null || !File.Exists(script)) { return new HashSet<string>(StringComparer.OrdinalIgnoreCase); }
            Dictionary<string, HashSet<string>> files;
            lock (ScriptCommands) {
                if (!ScriptCommands.TryGetValue(site.Repository, out files!)) {
                    string[] paths = Directory.GetFiles(Path.Combine(site.Website, "examples"), "*.ps1", SearchOption.AllDirectories)
                        .Concat(Directory.GetFiles(Path.Combine(site.Repository, "Module", "Examples"), "*.ps1"))
                        .Append(script).Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal).ToArray();
                    IReadOnlyList<HashSet<string>> parsed = PowerShellSyntax.Commands(paths.Select(Site.ReadText).ToArray());
                    files = paths.Select((path, index) => (path, parsed[index])).ToDictionary(item => item.path, item => item.Item2, StringComparer.Ordinal);
                    ScriptCommands.Add(site.Repository, files);
                }
            }
            return files.TryGetValue(script, out HashSet<string>? commands) ? commands : PowerShellSyntax.Commands(new[] { Site.ReadText(script) })[0];
        }
    }
}
