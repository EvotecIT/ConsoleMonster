using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SiteFrames.Pages;

namespace SiteFrames {
    /// <summary>
    /// The repository README follows the website. Its code blocks are the site's own verified example sources, and its
    /// images are the site's own frames.
    /// <list type="bullet">
    /// <item>A block is marked <c>&lt;!-- example: Website/examples/…/x.ps1 --&gt;</c> (the whole program, as the site shows
    /// it under "Full program") or <c>&lt;!-- example: Website/content/…/page.md#N --&gt;</c> (the Nth code block of that page).
    /// <c>SiteFrames readme</c> writes the blocks from their sources, and the render fails when one has drifted.</item>
    /// <item>Images are pages written to <c>_temp/images/readme-*.html</c> from the frames the site shows (recorded posters,
    /// figures, widget frames), listed in <c>_temp/images/readme.json</c>; <c>build/Render-SiteImages.ps1</c> screenshots
    /// them into <c>static/readme/</c>. The README points at them with absolute raw GitHub URLs, which also render in the
    /// NuGet package readme.</item>
    /// </list>
    /// </summary>
    internal static class Readme {
        /// <summary>Where the README's images are served from: absolute, so the NuGet package readme can show them.</summary>
        public const string ImageBase = "https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/";

        // The closing fence is a line of exactly ``` (a block that shows a fence inside, indented or longer, stays whole).
        private static readonly Regex Marker = new Regex(@"<!--\s*example:\s*(?<source>[^\s>]+)\s*-->\n```(?<lang>[\w-]*)\n(?<code>[\s\S]*?)\n```[ \t]*(?=\n|$)", RegexOptions.CultureInvariant);

        private static readonly Regex Image = new Regex(Regex.Escape(ImageBase) + @"(?<name>[\w.-]+\.png)", RegexOptions.CultureInvariant);

        // Cell geometry of the images: JetBrains Mono at 14 px, a cell 0.6 em by 1.2 em, the screen's padding, the window bar.
        private const double CellWidth = 8.4;
        private const double CellHeight = 16.8;
        private const int PadX = 28;
        private const int PadY = 26;
        private const int Bar = 34;

        /// <summary>One README image: a frame the site shows, with its light rendering when the frame has one.</summary>
        internal sealed record ImageSpec(string Id, string Title, int Columns, int Rows, string Dark, string? Light);

        private static string ReadmePath(Site site) => Path.Combine(site.Repository, "README.md");

        /// <summary>The images the README uses, each from data the render just wrote.</summary>
        internal static List<ImageSpec> Images(Site site) {
            JsonObject demos = Load(site, "demos");
            JsonObject figures = Load(site, "figures");
            JsonObject widgets = Load(site, "widgets");
            JsonObject home = Load(site, "home");
            List<ImageSpec> images = new List<ImageSpec>();
            void Demo(string id, string demo, string title) {
                JsonNode? still = demos[demo]?["poster"] ?? demos[demo]?["frame"];
                Add(id, title, still);
            }
            void Figure(string id, string figure, string title) => Add(id, title, figures[figure]?["frame"]);
            void Widget(string id, string widget, string title) {
                JsonNode? example = widgets["examples"]?.AsArray().FirstOrDefault(e => e?["id"]?.GetValue<string>() == widget);
                Add(id, title, example?["frame"]);
            }
            void Add(string id, string title, JsonNode? frame) {
                string? dark = frame?["dark"]?.GetValue<string>() ?? frame?["darkOnly"]?.GetValue<string>();
                if (frame == null || dark == null) {
                    WidgetsPage.Report(new List<string> { "README image " + id + " has no frame to draw from." }, "readme");
                    return;
                }
                string? light = frame["light"]?.GetValue<string>() ?? frame["lightOnly"]?.GetValue<string>();
                int columns = frame["w"]!.GetValue<int>();
                int rows = frame["h"]?.GetValue<int>() ?? dark.Split('\n').Length;
                images.Add(new ImageSpec(id, title, columns, rows, dark, light));
            }
            Demo("hero", "app", "Fleet health check");
            Demo("formatting", "formatting", "Enable-ConsoleFormatting");
            Figure("format-consoletable", "format-consoletable", "Format-ConsoleTable");
            Widget("tree", "tree", "New-ConsoleTree");
            Demo("parallel", "parallel", "ForEach-ConsoleItem -Parallel");
            Add("csharp", "dotnet run", home["controllers"]?["frame"]);
            Demo("record", "record", "record.ps1");
            Demo("threads", "threads-live", "Start-ConsoleDashboard -Parallel");
            return images;
        }

        private static JsonObject Load(Site site, string name) => JsonNode.Parse(File.ReadAllText(site.Data(name)))!.AsObject();

        /// <summary>Writes a page per image and theme, and the list Render-SiteImages.ps1 screenshots.</summary>
        public static void WritePages(Site site, string folder, string css) {
            JsonArray list = new JsonArray();
            foreach (ImageSpec image in Images(site)) {
                int width = (int)Math.Ceiling(image.Columns * CellWidth) + PadX;
                int height = Bar + (int)Math.Ceiling(image.Rows * CellHeight) + PadY;
                foreach (string scheme in new[] { "dark", "light" }) {
                    // A frame recorded in the dark theme stays dark on the light page: only its window follows the page.
                    bool lightScreen = scheme == "light" && image.Light != null;
                    string rows = lightScreen ? image.Light! : image.Dark;
                    string page = "readme-" + image.Id + "-" + scheme + ".html";
                    StringBuilder html = new StringBuilder();
                    html.Append("<!doctype html><html data-theme=\"").Append(scheme).Append("\"><head><meta charset=\"utf-8\"><style>").Append(css).Append(@"
html,body{margin:0;width:").Append(width).Append("px;height:").Append(height).Append(@"px;overflow:hidden;background:transparent}
.win{box-sizing:border-box;width:100%;height:100%;border-radius:10px;overflow:hidden;border:1px solid ").Append(scheme == "dark" ? "#273041" : "#d0d7de").Append(@";background:").Append(lightScreen ? "#ffffff" : "#0c0c0c").Append(@"}
.bar{box-sizing:border-box;height:").Append(Bar).Append(@"px;display:flex;align-items:center;gap:8px;padding:0 14px;background:").Append(scheme == "dark" ? "#161b26" : "#eef1f5").Append(";color:").Append(scheme == "dark" ? "#c3cad6" : "#3c4555").Append(@";font:600 13px/1 var(--cm-mono);border-bottom:1px solid ").Append(scheme == "dark" ? "#273041" : "#d0d7de").Append(@"}
.bar i{width:10px;height:10px;border-radius:50%;background:").Append(scheme == "dark" ? "#3c4555" : "#c3cad6").Append(@";display:inline-block}
.bar b{margin-left:8px;font-weight:600}
.win .cm-screen{font-size:14px!important;min-width:0;width:auto}
</style></head><body><div class=""win""><div class=""bar""><i></i><i></i><i></i><b>").Append(System.Net.WebUtility.HtmlEncode(image.Title)).Append("</b></div><pre class=\"cm-screen ")
                        .Append(lightScreen ? "cm-screen--light" : "cm-screen--dark").Append("\" style=\"--cols:").Append(image.Columns).Append("\">").Append(rows).Append("</pre></div></body></html>");
                    File.WriteAllText(Path.Combine(folder, page), html.ToString(), new UTF8Encoding(false));
                    list.Add(new JsonObject { ["page"] = page, ["out"] = "static/readme/" + image.Id + "-" + scheme + ".png", ["width"] = width, ["height"] = height });
                }
            }
            File.WriteAllText(Path.Combine(folder, "readme.json"), list.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        }

        /// <summary>What a marked block must contain, and the fence language it is shown with.</summary>
        internal static (string Code, string Language) Expected(Site site, string source) {
            int hash = source.IndexOf('#');
            string path = Path.Combine(site.Repository, (hash < 0 ? source : source.Substring(0, hash)).Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) {
                throw new InvalidOperationException("README example source " + source + " does not exist.");
            }
            if (hash >= 0) {
                if (!int.TryParse(source.Substring(hash + 1), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int index)) {
                    throw new InvalidOperationException("README example source " + source + " does not name a code block by number (#1, #2, ...).");
                }
                List<(int Line, string Language, string Code)> blocks = CodeGuard.Blocks(Site.ReadText(path)).ToList();
                if (index < 1 || index > blocks.Count) {
                    throw new InvalidOperationException("README example source " + source + " names code block " + index + " of " + blocks.Count + ".");
                }
                return (blocks[index - 1].Code.TrimEnd('\n'), blocks[index - 1].Language);
            }
            string language = Path.GetExtension(path) == ".cs" ? "csharp" : "powershell";
            return (SourceSnippets.Program(Site.ReadText(path)).Full.TrimEnd('\n'), language);
        }

        /// <summary>
        /// The README's problems: blocks that differ from their source, code blocks with no source, markers with no block,
        /// images that were never rendered, and a package readme that no longer matches the README.
        /// </summary>
        internal static List<string> Find(Site site, string readme, string? packageReadme = null) {
            readme = readme.Replace("\r\n", "\n");
            List<string> problems = new List<string>();
            HashSet<int> marked = new HashSet<int>();
            foreach (Match match in Marker.Matches(readme)) {
                marked.Add(match.Groups["lang"].Index);
                string source = match.Groups["source"].Value;
                try {
                    (string code, string language) = Expected(site, source);
                    if (match.Groups["code"].Value.TrimEnd('\n') != code) {
                        problems.Add("README.md: the block from " + source + " differs from its source; run SiteFrames readme.");
                    } else if (match.Groups["lang"].Value != language) {
                        problems.Add("README.md: the block from " + source + " should be fenced as " + language + ".");
                    }
                } catch (InvalidOperationException ex) {
                    problems.Add("README.md: " + ex.Message);
                }
            }
            // Every code block is the code that ran somewhere on the site; one without a marker has no source to match.
            foreach (Match fence in OpeningFence.Matches(readme)) {
                if (!marked.Contains(fence.Groups["lang"].Index)) {
                    int line = readme.Take(fence.Index).Count(c => c == '\n') + 1;
                    problems.Add("README.md:" + line + ": a code block without an <!-- example: --> marker naming its source.");
                }
            }
            int markers = Regex.Matches(readme, @"<!--\s*example:").Count;
            if (markers != marked.Count) {
                problems.Add("README.md: " + (markers - marked.Count) + " <!-- example: --> marker(s) are not followed directly by a code block.");
            }
            foreach (Match image in Image.Matches(readme)) {
                if (!File.Exists(site.Static("readme/" + image.Groups["name"].Value))) {
                    problems.Add("README.md: image " + image.Groups["name"].Value + " is not in Website/static/readme; run build/Render-SiteImages.ps1.");
                }
            }
            if (packageReadme != null && packageReadme.Replace("\r\n", "\n") != PackageReadme(readme)) {
                problems.Add(Path.GetFileName(PackageReadmePath(site)) + " differs from README.md; run SiteFrames readme.");
            }
            return problems;
        }

        // An opening fence: a line that starts with ``` and names a language, or a bare ``` that opens (outside a block).
        private static readonly Regex OpeningFence = new Regex(@"(?m)^```(?<lang>[\w-]+)[ \t]*$", RegexOptions.CultureInvariant);

        // A README image: <picture> with the dark source and the light <img>, optionally inside a link.
        private static readonly Regex Picture = new Regex(@"(?:<a href=""(?<href>[^""]+)"">)?<picture>\s*<source[^>]*>\s*<img alt=""(?<alt>[^""]*)""(?:\s+width=""\d+"")?\s+src=""(?<src>[^""]+)"">\s*</picture>(?:</a>)?", RegexOptions.CultureInvariant);

        /// <summary>
        /// The README as the NuGet package shows it. nuget.org renders Markdown without HTML, so each &lt;picture&gt; becomes a
        /// Markdown image of its light rendering (linked when the picture was), and the marker comments go (nuget.org would
        /// drop them too).
        /// </summary>
        internal static string PackageReadme(string readme) {
            string text = Picture.Replace(readme.Replace("\r\n", "\n"), m => {
                string image = "![" + m.Groups["alt"].Value + "](" + m.Groups["src"].Value + ")";
                return m.Groups["href"].Success ? "[" + image + "](" + m.Groups["href"].Value + ")" : image;
            });
            return Regex.Replace(text, @"<!--\s*example:[^>]*-->\n", string.Empty);
        }

        private static string PackageReadmePath(Site site) => Path.Combine(site.Repository, "ConsoleMonster", "README.nuget.md");

        /// <summary>Fails the render when the README or the package readme has drifted from the site.</summary>
        public static void Check(Site site) {
            string path = ReadmePath(site);
            if (File.Exists(path)) {
                string package = PackageReadmePath(site);
                WidgetsPage.Report(Find(site, File.ReadAllText(path), File.Exists(package) ? File.ReadAllText(package) : string.Empty), "readme");
            }
        }

        /// <summary>Writes every marked block from its source, then the package readme (the SiteFrames readme command).</summary>
        public static int Sync(Site site) {
            string path = ReadmePath(site);
            string readme = File.ReadAllText(path).Replace("\r\n", "\n");
            List<string> refused = new List<string>();
            string updated = Marker.Replace(readme, match => {
                (string code, string language) = Expected(site, match.Groups["source"].Value);
                // A docs page's numbered block that changed language is probably a different block now: say so.
                if (match.Groups["lang"].Value.Length > 0 && match.Groups["lang"].Value != language) {
                    refused.Add(match.Groups["source"].Value + " is now a " + language + " block, not " + match.Groups["lang"].Value + "; check the number.");
                    return match.Value;
                }
                return "<!-- example: " + match.Groups["source"].Value + " -->\n```" + language + "\n" + code + "\n```";
            });
            if (refused.Count > 0) {
                Console.Error.WriteLine(string.Join("\n", refused));
                return 1;
            }
            if (updated != readme) {
                File.WriteAllText(path, updated, new UTF8Encoding(false));
                Console.WriteLine("Updated README.md from its example sources.");
            } else {
                Console.WriteLine("README.md matches its example sources.");
            }
            string package = PackageReadmePath(site);
            if (Directory.Exists(Path.GetDirectoryName(package))) {
                File.WriteAllText(package, PackageReadme(updated), new UTF8Encoding(false));
            }
            return 0;
        }
    }
}
