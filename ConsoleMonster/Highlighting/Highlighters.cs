using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Highlighting {
    /// <summary>
    /// The highlighter registry: the built-in languages (json, xml, yaml, powershell, csharp, sql, ini, csv, diff, log,
    /// markdown), the smart value highlighter for plain text, lookup by name, alias or file extension, and detection
    /// from a path and a light look at the content. Register your own languages with <see cref="Register"/>.
    /// </summary>
    public static class Highlighters {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, ITextHighlighter> ByName = new Dictionary<string, ITextHighlighter>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ITextHighlighter> ByExtension = new Dictionary<string, ITextHighlighter>(StringComparer.OrdinalIgnoreCase);
        private static readonly Regex LogStart = new Regex(@"^\[?(?:\d{4}-\d{2}-\d{2}[T ])?\d{2}:\d{2}:\d{2}", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        static Highlighters() {
            Register(new JsonHighlighter(), "json", "jsonc", "geojson");
            Register(new XmlHighlighter(), "xml", "html", "htm", "xaml", "csproj", "props", "targets", "config", "svg", "ps1xml", "resx", "nuspec");
            Register(new YamlHighlighter(), "yaml", "yml");
            Register(new PowerShellHighlighter(), "ps1", "psm1", "psd1");
            Register(new LogHighlighter(), "log");
            Register(new DiffHighlighter(), "diff", "patch");
            Register(new CsvHighlighter(), "csv", "tsv");
            Register(BuiltInRules.CSharp(), "cs", "csx");
            Register(BuiltInRules.Sql(), "sql");
            Register(BuiltInRules.Ini(), "ini", "toml", "cfg", "conf", "editorconfig", "gitconfig");
            Register(BuiltInRules.Markdown(), "md", "markdown");
            Alias("ps", "powershell");
            Alias("pwsh", "powershell");
            Alias("cs", "csharp");
            Alias("c#", "csharp");
            Alias("yml", "yaml");
            Alias("html", "xml");
            Alias("toml", "ini");
            Alias("tsv", "csv");
            Alias("patch", "diff");
            Alias("md", "markdown");
            Alias("smart", Smart);
        }

        /// <summary>The smart value highlighter for plain text (numbers, booleans, GUIDs, IP addresses, URLs, paths, dates, DNs, quoted strings).</summary>
        public static ITextHighlighter Smart { get; } = new SmartHighlighter();

        /// <summary>The registered language names.</summary>
        public static IReadOnlyList<string> Names {
            get {
                lock (Gate) {
                    return ByName.Values.Select(h => h.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n, StringComparer.Ordinal).ToList();
                }
            }
        }

        /// <summary>Registers a highlighter under its name and file extensions (replacing earlier registrations).</summary>
        /// <param name="highlighter">The highlighter.</param>
        /// <param name="extensions">File extensions without the dot.</param>
        public static void Register(ITextHighlighter highlighter, params string[] extensions) {
            if (highlighter == null) {
                throw new ArgumentNullException(nameof(highlighter));
            }
            lock (Gate) {
                ByName[highlighter.Name] = highlighter;
                foreach (string extension in extensions ?? new string[0]) {
                    ByExtension[extension.TrimStart('.')] = highlighter;
                }
            }
        }

        /// <summary>A highlighter by name or alias ("json", "ps1", "c#", "smart"), or null.</summary>
        /// <param name="name">The name.</param>
        /// <returns>The highlighter, or null.</returns>
        public static ITextHighlighter? Get(string? name) {
            if (string.IsNullOrWhiteSpace(name)) {
                return null;
            }
            lock (Gate) {
                if (ByName.TryGetValue(name!.Trim(), out ITextHighlighter? found)) {
                    return found;
                }
                return ByExtension.TryGetValue(name.Trim().TrimStart('.'), out found) ? found : null;
            }
        }

        /// <summary>The highlighter for a file by its extension, or null.</summary>
        /// <param name="path">The path or file name.</param>
        /// <returns>The highlighter, or null.</returns>
        public static ITextHighlighter? ForPath(string? path) {
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            string name = Path.GetFileName(path!);
            string extension = name.StartsWith(".", StringComparison.Ordinal) && name.IndexOf('.', 1) < 0 ? name.Substring(1) : Path.GetExtension(name).TrimStart('.');
            lock (Gate) {
                return extension.Length > 0 && ByExtension.TryGetValue(extension, out ITextHighlighter? found) ? found : null;
            }
        }

        /// <summary>The highlighter for a text: by the path's extension, then by a look at the content; null for plain text.</summary>
        /// <param name="text">The text.</param>
        /// <param name="path">The file it came from, or null.</param>
        /// <returns>The highlighter, or null.</returns>
        public static ITextHighlighter? Detect(string? text, string? path = null) {
            ITextHighlighter? byPath = ForPath(path);
            if (byPath != null || string.IsNullOrEmpty(text)) {
                return byPath;
            }
            string sample = text!.Length > 4000 ? text.Substring(0, 4000) : text;
            string trimmed = sample.TrimStart('﻿', ' ', '\t', '\r', '\n');
            if (trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[{", StringComparison.Ordinal) || trimmed.StartsWith("[\"", StringComparison.Ordinal) || trimmed.StartsWith("[\n", StringComparison.Ordinal) || trimmed.StartsWith("[\r", StringComparison.Ordinal) || trimmed == "[]") {
                return Get("json");
            }
            if (trimmed.StartsWith("<", StringComparison.Ordinal)) {
                return Get("xml");
            }
            if (trimmed.StartsWith("diff --git", StringComparison.Ordinal) || trimmed.StartsWith("--- ", StringComparison.Ordinal) || sample.IndexOf("\n@@ ", StringComparison.Ordinal) >= 0) {
                return Get("diff");
            }
            try {
                if (LogStart.IsMatch(trimmed)) {
                    return Get("log");
                }
            } catch (RegexMatchTimeoutException) {
            }
            if (trimmed.StartsWith("#Requires", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("param(", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("function ", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("<#", StringComparison.Ordinal) || sample.IndexOf("$_", StringComparison.Ordinal) >= 0 && sample.IndexOf(" -", StringComparison.Ordinal) >= 0) {
                return Get("powershell");
            }
            if (trimmed.StartsWith("---", StringComparison.Ordinal) || LooksLikeYaml(trimmed)) {
                return Get("yaml");
            }
            return null;
        }

        private static void Alias(string alias, string name) {
            lock (Gate) {
                if (ByName.TryGetValue(name, out ITextHighlighter? target)) {
                    ByName[alias] = target;
                }
            }
        }

        private static void Alias(string alias, ITextHighlighter target) {
            lock (Gate) {
                ByName[alias] = target;
            }
        }

        // Most non-empty, non-comment lines are "key: value" or "- item".
        private static bool LooksLikeYaml(string text) {
            int lines = 0;
            int yaml = 0;
            foreach (string raw in text.Split('\n').Take(30)) {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) {
                    continue;
                }
                lines++;
                int colon = line.IndexOf(':');
                if (line.StartsWith("- ", StringComparison.Ordinal) || (colon > 0 && (colon == line.Length - 1 || line[colon + 1] == ' ') && (line.IndexOf(' ') < 0 || line.IndexOf(' ') > colon))) {
                    yaml++;
                }
            }
            return lines >= 2 && yaml * 10 >= lines * 8;
        }
    }

    /// <summary>Collects tokens for the hand-written lexers.</summary>
    internal sealed class TokenList {
        private readonly List<HighlightToken> _tokens = new List<HighlightToken>();

        public List<HighlightToken> Tokens => _tokens;

        public void Add(int start, int length, HighlightRole role, string? link = null) {
            if (length > 0 && role != HighlightRole.Text) {
                _tokens.Add(new HighlightToken(start, length, role, link));
            }
        }
    }
}
