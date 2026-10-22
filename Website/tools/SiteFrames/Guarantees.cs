using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SiteFrames.Pages;

namespace SiteFrames {
    /// <summary>
    /// The concurrency guarantees table on /docs/threads/, generated on every render from the repository's
    /// <c>Docs/Concurrency-Guarantees.md</c> ("Guarantees and their tests") into <c>data/guarantees.json</c>, which the
    /// <c>cm-guarantees</c> shortcode draws. The render fails when the section or its table cannot be read, and CI rejects a
    /// committed <c>data/guarantees.json</c> that no longer matches the document, so the page and the document cannot drift.
    /// </summary>
    internal static class Guarantees {
        /// <summary>The document, relative to the repository root.</summary>
        internal const string Source = "Docs/Concurrency-Guarantees.md";

        private const string Heading = "## Guarantees and their tests";
        private const string Blob = "https://github.com/EvotecIT/ConsoleMonster/blob/master/";

        private static readonly Regex Link = new Regex(@"\[(?<text>[^\]]+)\]\((?<url>[^)\s]+)\)", RegexOptions.CultureInvariant);
        private static readonly Regex Code = new Regex("`(?<code>[^`]+)`", RegexOptions.CultureInvariant);
        private static readonly Regex PowerShellRuntimes = new Regex(@"PowerShell tests run on (?<runtimes>.+?)(?:\.\s|\.$|;|$)", RegexOptions.CultureInvariant);

        /// <summary>One table row: the guarantee, the tests that prove it (HTML) and where they run.</summary>
        internal sealed record Row(string Guarantee, string Evidence, string Runtimes);

        /// <summary>Reads the document and writes <c>data/guarantees.json</c>.</summary>
        public static void Render(Site site) {
            string path = Path.Combine(site.Repository, Source.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) {
                WidgetsPage.Report(new List<string> { Source + " is missing; the threads page draws its guarantees table from it." }, "guarantees");
                return;
            }
            List<string> problems = new List<string>();
            (List<string> notes, List<Row> rows) = Parse(Site.ReadText(path), file => Runtimes(site, file), problems);
            if (problems.Count > 0) {
                WidgetsPage.Report(problems, "guarantees");
                return;
            }
            JsonArray rowArray = new JsonArray();
            foreach (Row row in rows) {
                rowArray.Add(new JsonObject { ["guarantee"] = row.Guarantee, ["evidence"] = row.Evidence, ["runtimes"] = row.Runtimes });
            }
            JsonArray noteArray = new JsonArray();
            foreach (string note in notes) {
                noteArray.Add(note);
            }
            site.WriteJson(site.Data("guarantees"), new JsonObject {
                ["source"] = Source,
                ["url"] = Blob + Source + "#guarantees-and-their-tests",
                ["notes"] = noteArray,
                ["rows"] = rowArray
            });
        }

        /// <summary>
        /// The section's notes (the paragraph above the table, as HTML) and rows. <paramref name="runtimes"/> names where a
        /// referenced file's tests run; a row that links no file ("in the same file") uses the files of the row above.
        /// </summary>
        internal static (List<string> Notes, List<Row> Rows) Parse(string markdown, Func<string, string?> runtimes, List<string> problems) {
            string[] lines = markdown.Replace("\r\n", "\n").Split('\n');
            int start = Array.FindIndex(lines, l => l.Trim() == Heading);
            List<string> notes = new List<string>();
            List<Row> rows = new List<Row>();
            if (start < 0) {
                problems.Add(Source + ": no \"" + Heading.Substring(3) + "\" section.");
                return (notes, rows);
            }
            int i = start + 1;
            List<string> paragraph = new List<string>();
            for (; i < lines.Length && !lines[i].TrimStart().StartsWith("|", StringComparison.Ordinal) && !lines[i].StartsWith("#", StringComparison.Ordinal); i++) {
                if (lines[i].Trim().Length > 0) {
                    paragraph.Add(lines[i].Trim());
                }
            }
            string text = string.Join(" ", paragraph);
            string powerShell = PowerShellRuntimes.Match(text) is { Success: true } m ? m.Groups["runtimes"].Value.Trim() : "5.1 and 7";
            if (text.Length > 0) {
                notes.Add(Inline(text));
            }
            List<string> previousFiles = new List<string>();
            int index = 0;
            for (; i < lines.Length && lines[i].TrimStart().StartsWith("|", StringComparison.Ordinal); i++, index++) {
                string[] cells = Cells(lines[i]);
                if (index == 0 || cells.All(c => Regex.IsMatch(c, @"^:?-+:?$"))) {
                    continue;
                }
                if (cells.Length != 2) {
                    problems.Add(Source + ": a guarantees row has " + cells.Length + " cells instead of 2: " + lines[i].Trim());
                    continue;
                }
                List<string> files = Link.Matches(cells[1]).Select(l => l.Groups["url"].Value).Where(u => !u.Contains("://", StringComparison.Ordinal)).Select(Resolve).ToList();
                if (files.Count == 0 || cells[1].Contains("in the same file", StringComparison.Ordinal)) {
                    files = previousFiles.Concat(files).Distinct(StringComparer.Ordinal).ToList();
                }
                previousFiles = files;
                List<string> where = new List<string>();
                foreach (string file in files) {
                    string? runs = file.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) ? "PowerShell " + powerShell : runtimes(file);
                    if (runs == null) {
                        problems.Add(Source + ": no test project found for " + file + ".");
                    } else if (!where.Contains(runs)) {
                        where.Add(runs);
                    }
                }
                rows.Add(new Row(Inline(cells[0]), Inline(cells[1]), string.Join("; ", where)));
            }
            if (rows.Count == 0) {
                problems.Add(Source + ": the \"" + Heading.Substring(3) + "\" section has no table rows.");
            }
            return (notes, rows);
        }

        // A table row's cells, without the outer pipes; a pipe inside backticks stays in its cell.
        private static string[] Cells(string line) {
            List<string> cells = new List<string>();
            string trimmed = line.Trim();
            if (trimmed.StartsWith("|", StringComparison.Ordinal)) { trimmed = trimmed.Substring(1); }
            if (trimmed.EndsWith("|", StringComparison.Ordinal)) { trimmed = trimmed.Substring(0, trimmed.Length - 1); }
            bool code = false;
            int from = 0;
            for (int c = 0; c < trimmed.Length; c++) {
                if (trimmed[c] == '`') {
                    code = !code;
                } else if (trimmed[c] == '|' && !code) {
                    cells.Add(trimmed.Substring(from, c - from).Trim());
                    from = c + 1;
                }
            }
            cells.Add(trimmed.Substring(from).Trim());
            return cells.ToArray();
        }

        // A link relative to Docs/ as a repository path.
        private static string Resolve(string url) {
            string path = url.Split('#')[0];
            List<string> parts = new List<string> { "Docs" };
            foreach (string part in path.Split('/')) {
                if (part == "..") {
                    if (parts.Count > 0) { parts.RemoveAt(parts.Count - 1); }
                } else if (part.Length > 0 && part != ".") {
                    parts.Add(part);
                }
            }
            return string.Join("/", parts);
        }

        // Markdown inline text as HTML: escaped, links to repository files point at GitHub, `code` becomes <code>.
        internal static string Inline(string markdown) {
            string html = WebUtility.HtmlEncode(markdown).Replace("&#39;", "'");
            html = Link.Replace(html, l => {
                string url = WebUtility.HtmlDecode(l.Groups["url"].Value);
                string href = url.Contains("://", StringComparison.Ordinal) ? url : Blob + Resolve(url) + (url.Contains('#') ? "#" + url.Split('#')[1] : string.Empty);
                return "<a href=\"" + WebUtility.HtmlEncode(href) + "\">" + l.Groups["text"].Value + "</a>";
            });
            return Code.Replace(html, c => "<code>" + c.Groups["code"].Value + "</code>");
        }

        // Where a C# test file's tests run: its project's target frameworks, by name.
        private static string? Runtimes(Site site, string file) {
            string? folder = Path.GetDirectoryName(Path.Combine(site.Repository, file.Replace('/', Path.DirectorySeparatorChar)));
            while (folder != null && folder.Length >= site.Repository.Length) {
                string? project = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.csproj").FirstOrDefault() : null;
                if (project != null) {
                    Match frameworks = Regex.Match(File.ReadAllText(project), @"<TargetFrameworks?>(?<tf>[^<]+)</TargetFrameworks?>");
                    return frameworks.Success ? string.Join(", ", frameworks.Groups["tf"].Value.Split(';').Select(f => f.Trim()).Where(f => f.Length > 0).Select(Framework)) : null;
                }
                folder = Path.GetDirectoryName(folder);
            }
            return null;
        }

        private static string Framework(string moniker) {
            Match net = Regex.Match(moniker, @"^net(?<major>\d+)\.(?<minor>\d+)");
            if (net.Success) {
                return ".NET " + net.Groups["major"].Value + (net.Groups["minor"].Value == "0" ? string.Empty : "." + net.Groups["minor"].Value);
            }
            Match framework = Regex.Match(moniker, @"^net(?<v>\d{2,3})$");
            if (framework.Success) {
                return ".NET Framework " + string.Join(".", framework.Groups["v"].Value.ToCharArray());
            }
            return moniker;
        }
    }
}
