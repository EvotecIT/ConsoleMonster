using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SiteFrames.Pages;

namespace SiteFrames {
    /// <summary>
    /// Fails the render when code the site shows could not run in a reader's own folder: a script called or dot-sourced by a
    /// relative path into the repository (<c>&amp; ../../Module/Examples/X.ps1</c>), a path built from <c>$PSScriptRoot</c> and
    /// <c>..</c>, any <c>../</c> path, a folder of this repository (<c>Module/Examples</c>, <c>Website/</c>,
    /// <c>ConsoleMonster.Examples</c>, <c>tools/SiteFrames</c>), or the site's old shared data file <c>artifacts.ps1</c>.
    /// The code a page shows is the code that made its frame, complete, so it never depends on the repository.
    /// <para>
    /// Checked: every string in <c>data/*.json</c> except rendered frames (HTML) and links, and every fenced code block in
    /// <c>content/**/*.md</c>.
    /// </para>
    /// </summary>
    internal static class CodeGuard {
        // Keys whose values are rendered frames, links or player data, never code a reader copies.
        private static readonly HashSet<string> SkippedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "dark", "light", "lightOnly", "darkOnly", "poster", "scrollback", "url", "href", "github", "image", "thumb", "download"
        };

        // Rendered HTML (frames, posters) starts with a tag; a script that opens with a <# help block is still checked.
        private static readonly Regex Markup = new Regex(@"^\s*<[A-Za-z!/]", RegexOptions.CultureInvariant);

        // A using directive (not a using statement or declaration) as the first code line, after any comments, preprocessor
        // directives (#nullable) and blank lines: plain, static, global or an alias.
        private static readonly Regex UsingDirective = new Regex(@"^(?:[ \t]*(?://[^\n]*|#[^\n]*|/\*[\s\S]*?\*/[ \t]*)?\n)*[ \t]*(global\s+)?using\s+(static\s+)?(\w+\s*=\s*)?[\w.<>, ]+\s*;", RegexOptions.CultureInvariant);

        internal static readonly (Regex Pattern, string Why)[] Rules = {
            (new Regex(@"(?m)^\s*[&.]\s*['""]?\.\.[\\/]", RegexOptions.CultureInvariant), "runs a script by a path into the repository"),
            (new Regex(@"\$PSScriptRoot\b[^\n]*?(?<![.\w])\.\.(?![.\w])", RegexOptions.CultureInvariant), "builds a path from $PSScriptRoot and .."),
            (new Regex(@"Split-Path\b(?![^\n]*-(Leaf|LeafBase|Extension|Qualifier|NoQualifier|IsAbsolute)\b)[^\n]*\$(PSScriptRoot|PSCommandPath)\b|Join-Path\b[^\n]*[\s'""]\.\.(?=['""\s]|$)|(?m)^\s*(cd|sl|chdir|Set-Location|Push-Location)\b[^\n]*\s\.\.(?![.\w])", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline), "climbs out of the script's folder"),
            (new Regex(@"(?<![\w.])\.\.[\\/]\w", RegexOptions.CultureInvariant), "contains a ../ path"),
            (new Regex(@"\bartifacts\.ps1\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase), "depends on artifacts.ps1 instead of defining its data"),
            (new Regex(@"(?<![\w.-])(?<!\w/)(Module[\\/](Examples|Docs|Build|Tests|en-US|ConsoleMonster\.psd1)|Website[\\/]|ConsoleMonster\.Examples(\.Extensions)?[\\/]|ConsoleMonster\.PowerShell[\\/]|tools[\\/]SiteFrames)", RegexOptions.CultureInvariant), "names a folder of this repository"),
            (new Regex(@"[\\/]Support[\\/]GitHub[\\/]|[\\/]_worktrees[\\/]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase), "shows a developer checkout path")
        };

        public static void Check(Site site) {
            List<string> problems = new List<string>();
            string[] files = Directory.GetFiles(Path.Combine(site.Website, "data"), "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            foreach (string file in files) {
                JsonNode? root = JsonNode.Parse(File.ReadAllText(file));
                Walk(root, Path.GetFileName(file), problems);
            }
            string content = Path.Combine(site.Website, "content");
            string[] pages = Directory.GetFiles(content, "*.md", SearchOption.AllDirectories);
            Array.Sort(pages, StringComparer.Ordinal);
            foreach (string page in pages) {
                string name = Path.GetRelativePath(site.Website, page).Replace('\\', '/');
                foreach ((int line, string language, string code) in Blocks(Site.ReadText(page))) {
                    Inspect(code, name + ":" + line, problems);
                    // A C# block that starts with its using directives is a complete program: it must compile as a reader's would.
                    if ((language == "csharp" || language == "cs") && IsProgram(code)) {
                        try {
                            Capture.CSharpRunner.Check(name + ":" + line, code);
                        } catch (InvalidOperationException ex) {
                            problems.Add(ex.Message);
                        }
                    }
                }
            }
            WidgetsPage.Report(problems, "code");
        }

        /// <summary>Whether a C# block is a complete program: its first code line is a using directive.</summary>
        internal static bool IsProgram(string code) => UsingDirective.IsMatch(code.Replace("\r\n", "\n"));

        /// <summary>The fenced code blocks of a Markdown page, with the line each starts on.</summary>
        internal static IEnumerable<(int Line, string Code)> Fences(string markdown) {
            foreach ((int line, string _, string code) in Blocks(markdown)) {
                yield return (line, code);
            }
        }

        /// <summary>The fenced code blocks of a Markdown page, with the line each starts on and its language.</summary>
        internal static IEnumerable<(int Line, string Language, string Code)> Blocks(string markdown) {
            string[] lines = markdown.Split('\n');
            for (int i = 0; i < lines.Length; i++) {
                string trimmed = lines[i].TrimStart();
                if (!trimmed.StartsWith("```", StringComparison.Ordinal)) {
                    continue;
                }
                string language = trimmed.Substring(3).Trim().ToLowerInvariant();
                int start = i + 2;
                List<string> block = new List<string>();
                for (i++; i < lines.Length && !lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal); i++) {
                    block.Add(lines[i]);
                }
                yield return (start, language, string.Join("\n", block));
            }
        }

        private static void Walk(JsonNode? node, string where, List<string> problems) {
            switch (node) {
                case JsonObject obj:
                    foreach (KeyValuePair<string, JsonNode?> property in obj) {
                        // *Repo keys hold a file's path in the repository for the "View on GitHub" link, never shown as code.
                        if (SkippedKeys.Contains(property.Key) || property.Key.EndsWith("Repo", StringComparison.Ordinal)) {
                            continue;
                        }
                        string path = where + "." + property.Key;
                        if (property.Value is JsonValue value && value.TryGetValue(out string? text)) {
                
                            if (!Markup.IsMatch(text)) {
                                Inspect(text, path, problems);
                            }
                        } else {
                            Walk(property.Value, path, problems);
                        }
                    }
                    break;
                case JsonArray array:
                    for (int i = 0; i < array.Count; i++) {
                        Walk(array[i], where + "[" + i + "]", problems);
                    }
                    break;
            }
        }

        /// <summary>The problems in one piece of shown code, each with the line that has it.</summary>
        internal static IEnumerable<string> Find(string text) {
            foreach ((Regex pattern, string why) in Rules) {
                Match match = pattern.Match(text);
                if (match.Success) {
                    int start = text.LastIndexOf('\n', Math.Max(0, match.Index - 1)) + 1;
                    if (match.Index == 0) {
                        start = 0;
                    }
                    int end = text.IndexOf('\n', match.Index);
                    string line = text.Substring(start, (end < 0 ? text.Length : end) - start).Trim();
                    yield return why + ": " + line;
                }
            }
        }

        private static void Inspect(string text, string path, List<string> problems) {
            foreach (string problem in Find(text)) {
                problems.Add(path + " " + problem);
            }
        }
    }
}
