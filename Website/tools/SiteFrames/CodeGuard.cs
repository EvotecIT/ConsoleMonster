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
    /// The code a page shows is the code that made its frame, complete, so it never depends on the repository. It also fails
    /// a code block that holds only a run command or labels dotnet commands as anything but Terminal (<see cref="Presentation"/>).
    /// <para>
    /// Checked: every string in <c>data/*.json</c> except rendered frames (HTML) and links, and every fenced code block in
    /// <c>content/**/*.md</c>.
    /// </para>
    /// </summary>
    internal static class CodeGuard {
        // Keys whose values are rendered frames, links or player data, never code a reader copies.
        private static readonly HashSet<string> SkippedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "dark", "light", "lightOnly", "darkOnly", "poster", "scrollback", "url", "href", "github", "repo", "image", "thumb", "download"
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
            // PowerShell a reader copies: code blocks on the pages and the steps in copy.json, parsed by PowerShell itself.
            List<(string Where, int Line, string Code)> powershell = new List<(string, int, string)>();
            CopySnippets(JsonNode.Parse(File.ReadAllText(site.Data("copy"))), "data/copy.json", powershell);
            string content = Path.Combine(site.Website, "content");
            string[] pages = Directory.GetFiles(content, "*.md", SearchOption.AllDirectories);
            Array.Sort(pages, StringComparer.Ordinal);
            foreach (string page in pages) {
                string name = Path.GetRelativePath(site.Website, page).Replace('\\', '/');
                foreach ((int line, string language, string code) in Blocks(Site.ReadText(page))) {
                    Inspect(code, name + ":" + line, problems);
                    // Docs and guides walk a reader through a program, so a run command stands on its own line under it.
                    bool walkthrough = name.StartsWith("content/docs/", StringComparison.Ordinal) || name.StartsWith("content/guides/", StringComparison.Ordinal);
                    foreach (string problem in Presentation(language, code, walkthrough)) {
                        problems.Add(name + ":" + line + " " + problem);
                    }
                    if (PowerShellLanguages.Contains(language)) {
                        powershell.Add((name, line, code));
                    }
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
            problems.AddRange(Capture.PowerShellSyntax.Check(powershell));
            CopyBlocks(JsonNode.Parse(File.ReadAllText(site.Data("copy")))?["demos"], "data/copy.json.demos", problems);
            WidgetsPage.Report(problems, "code");
        }

        private static readonly HashSet<string> PowerShellLanguages = new HashSet<string>(StringComparer.Ordinal) { "powershell", "pwsh", "ps1", "ps" };

        // A line that only starts a program: dotnet run, a script by its path (run or dot-sourced), or pwsh/powershell with
        // a script file. It belongs on the Run line under the program (cm-try), never in a code block of its own.
        private static readonly Regex RunCommand = new Regex(@"^(?:dotnet\s+run\b.*|(?:pwsh|powershell)(?:\.exe)?\s+(?:-\w+\s+)*(?:-File\s+)?(?:\.{1,2}[\\/])?[\w./\\~-]+\.ps1\b.*|(?:[&.]\s+)?(?:\.{1,2}|~)[\\/][\w./\\-]+\.ps1\b.*)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex DotnetCommand = new Regex(@"(?m)^\s*dotnet\s+\w", RegexOptions.CultureInvariant);

        /// <summary>
        /// How a block is presented: one that holds only a run command (<c>dotnet run</c>, <c>./script.ps1</c>) gives a
        /// one-line step the weight of a program, and <c>dotnet</c> commands are fenced as <c>terminal</c> (labelled
        /// Terminal), not as a shell, Text or PowerShell.
        /// </summary>
        /// <param name="language">The fence's language, or the block's label for page data.</param>
        /// <param name="code">The code.</param>
        /// <param name="runOnly">Whether a block holding only a run command is a problem (docs, guides and demo steps).</param>
        internal static IEnumerable<string> Presentation(string language, string code, bool runOnly = true) {
            List<string> lines = new List<string>();
            foreach (string raw in code.Replace("\r\n", "\n").Split('\n')) {
                string line = raw.Trim();
                if (line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal) && !line.StartsWith("//", StringComparison.Ordinal)) {
                    lines.Add(line);
                }
            }
            if (runOnly && lines.Count > 0 && lines.TrueForAll(l => RunCommand.IsMatch(l))) {
                yield return "holds only a run command (" + lines[0] + "): put it on the Run line under its program (cm-try), or in the step it belongs to.";
            }
            if (DotnetCommand.IsMatch(code) && !string.Equals(language, "terminal", StringComparison.OrdinalIgnoreCase)) {
                yield return "has dotnet commands labelled \"" + (language.Length == 0 ? "text" : language) + "\": fence them as terminal, labelled Terminal.";
            }
        }

        // Code blocks in copy.json demos (install blocks and step code): the same presentation rules, with the label as shown.
        private static void CopyBlocks(JsonNode? node, string where, List<string> problems) {
            switch (node) {
                case JsonObject obj:
                    string? label = obj["label"] is JsonValue l && l.TryGetValue(out string? shown) ? shown : null;
                    foreach (KeyValuePair<string, JsonNode?> property in obj) {
                        string path = where + "." + property.Key;
                        if ((property.Key == "install" || property.Key == "text" && obj.ContainsKey("lang")) && property.Value is JsonValue value && value.TryGetValue(out string? code)) {
                            foreach (string problem in Presentation(label ?? string.Empty, code!)) {
                                problems.Add(path + " " + problem);
                            }
                        } else if (property.Value is not JsonValue) {
                            CopyBlocks(property.Value, path, problems);
                        }
                    }
                    break;
                case JsonArray array:
                    for (int i = 0; i < array.Count; i++) {
                        CopyBlocks(array[i], where + "[" + i + "]", problems);
                    }
                    break;
            }
        }

        /// <summary>
        /// The PowerShell in copy.json that pages show as code: install blocks and step code whose language is PowerShell (the
        /// default when none is named), and the console variants' run lines.
        /// </summary>
        internal static void CopySnippets(JsonNode? node, string where, List<(string Where, int Line, string Code)> found) {
            switch (node) {
                case JsonObject obj:
                    string? language = obj["lang"] is JsonValue lang && lang.TryGetValue(out string? named) ? named : null;
                    bool isPowerShell = language == null || PowerShellLanguages.Contains(language);
                    foreach (KeyValuePair<string, JsonNode?> property in obj) {
                        string path = where + "." + property.Key;
                        if (property.Value is JsonValue value && value.TryGetValue(out string? text)) {
                            if ((property.Key == "install" || property.Key == "text" && obj.ContainsKey("lang") || property.Key == "run") && isPowerShell) {
                                found.Add((path, 1, text!));
                            }
                        } else {
                            CopySnippets(property.Value, path, found);
                        }
                    }
                    break;
                case JsonArray array:
                    for (int i = 0; i < array.Count; i++) {
                        CopySnippets(array[i], where + "[" + i + "]", found);
                    }
                    break;
            }
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
                // A block inside a list item is indented like its fence; the code is what is left after that indent.
                int indent = lines[i].Length - trimmed.Length;
                int start = i + 2;
                List<string> block = new List<string>();
                for (i++; i < lines.Length && !lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal); i++) {
                    string line = lines[i];
                    int strip = 0;
                    while (strip < indent && strip < line.Length && line[strip] == ' ') {
                        strip++;
                    }
                    block.Add(line.Substring(strip));
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
