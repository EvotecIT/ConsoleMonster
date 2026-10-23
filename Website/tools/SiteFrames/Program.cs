using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using SiteFrames.Capture;
using SiteFrames.Pages;

namespace SiteFrames {
    /// <summary>
    /// The consolemonster.dev site tool.
    /// <list type="bullet">
    /// <item><c>render [Website]</c>: renders every frame, replay, thumbnail and page data file from the ConsoleMonster sources
    /// and the committed captures. Deterministic: rerun it after a renderer change and commit what changed.</item>
    /// <item><c>capture [Website]</c>: runs the PowerShell examples on Windows PowerShell 5.1 and PowerShell 7 and records the
    /// live showcases, each inside a Windows pseudo console, and saves the VT streams under <c>Website/captures/</c>.</item>
    /// </list>
    /// </summary>
    public static class Program {
        internal static int Failures;

        public static int Main(string[] args) {
            CultureInfo culture = Site.Culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            // Frames must not depend on the build machine (or a CI runner): the variables ConsoleMonster reads are cleared and the label
            // language is pinned (some widgets read it from the process, not from the profile).
            foreach (System.Collections.DictionaryEntry variable in Environment.GetEnvironmentVariables()) {
                string name = (string)variable.Key;
                if (name.StartsWith("CONSOLEMONSTER_", StringComparison.OrdinalIgnoreCase) || name == "NO_COLOR" || name == "COLORFGBG" || name == "CI" || name == "GITHUB_ACTIONS" || name == "TF_BUILD" || name == "BUILD_BUILDID" || name == "JENKINS_URL" || name == "GITLAB_CI" || name == "TEAMCITY_VERSION") {
                    Environment.SetEnvironmentVariable(name, null);
                }
            }
            Environment.SetEnvironmentVariable("CONSOLEMONSTER_LANGUAGE", "en");
            Environment.SetEnvironmentVariable("CONSOLEMONSTER_CULTURE", "en-US");
            string command = args.Length > 0 ? args[0] : "render";
            string website = args.Length > 1 ? args[1] : Path.Combine("..", "..");
            string? only = args.Length > 2 ? args[2] : null;
            if (command == "render" && only != null) {
                Console.Error.WriteLine("Render regenerates all areas together because they share styles and thumbnails. Use: SiteFrames render [WebsiteRoot]");
                return 2;
            }
            switch (command) {
                case "render":
                    return Render(new Site(website), only);
                case "capture":
                    return CaptureAll(new Site(website), only);
                case "readme":
                    return Readme.Sync(new Site(website));
                case "review-object":
                    if (only == null) {
                        Console.Error.WriteLine("Pass an output directory: SiteFrames review-object [WebsiteRoot] [OutputDirectory]");
                        return 2;
                    }
                    return ExplorerFigures.Review(new Site(website), only);
                case "review-chart":
                    if (only == null) {
                        Console.Error.WriteLine("Pass the review output directory as the third argument.");
                        return 2;
                    }
                    return ChartFigures.Review(new Site(website), only);
                case "review-text":
                    if (only == null) {
                        Console.Error.WriteLine("Pass an output directory: SiteFrames review-text [WebsiteRoot] [OutputDirectory]");
                        return 2;
                    }
                    return TextDiffFigures.Review(new Site(website), only);
                default:
                    Console.Error.WriteLine("Usage: SiteFrames render|capture|readme [WebsiteRoot] [area]");
                    Console.Error.WriteLine("       SiteFrames review-chart|review-text|review-object [WebsiteRoot] [OutputDirectory]");
                    return 2;
            }
        }

        // Pages fetch frame bundles, replays, recordings and Prism components by fixed URLs at run time, which asset
        // hashing cannot rename. Their combined content hash goes into data/assets.json; the pages carry it and the scripts
        // add it to those URLs (?v=), so a browser never pairs a new page with an old cached replay or frame bundle.
        // Line endings are normalised so every checkout computes the same value.
        private static void WriteRuntimeVersion(Site site) {
            using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create()) {
                List<string> files = new List<string>();
                foreach (string folder in new[] { "frames", "replays", "recordings", Path.Combine("assets", "prism") }) {
                    string root = site.Static(folder.Replace(Path.DirectorySeparatorChar, '/'));
                    if (Directory.Exists(root)) {
                        files.AddRange(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories));
                    }
                }
                string staticRoot = site.Static(string.Empty);
                files.Sort((x, y) => string.CompareOrdinal(Path.GetRelativePath(staticRoot, x).Replace('\\', '/'), Path.GetRelativePath(staticRoot, y).Replace('\\', '/')));
                foreach (string file in files) {
                    byte[] name = System.Text.Encoding.UTF8.GetBytes(Path.GetRelativePath(staticRoot, file).Replace('\\', '/') + "\n");
                    sha.TransformBlock(name, 0, name.Length, null, 0);
                    byte[] content = File.ReadAllBytes(file);
                    string extension = Path.GetExtension(file).ToLowerInvariant();
                    if (extension == ".json" || extension == ".jsonl" || extension == ".js" || extension == ".css" || extension == ".txt") {
                        content = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(content).Replace("\r\n", "\n"));
                    }
                    sha.TransformBlock(content, 0, content.Length, null, 0);
                }
                sha.TransformFinalBlock(new byte[0], 0, 0);
                string version = Convert.ToHexString(sha.Hash!).Substring(0, 12).ToLowerInvariant();
                site.WriteJson(site.Data("assets"), new JsonObject { ["runtime"] = version });
            }
        }
        private static bool Wants(string? only, string area) => only == null || string.Equals(only, area, StringComparison.OrdinalIgnoreCase);

        private static int CaptureAll(Site site, string? only) {
            JsonObject hosts = new JsonObject();
            foreach (PowerShellHost host in PowerShellHost.All) {
                hosts[host.Id] = PowerShellRunner.Version(host);
            }
            site.WriteJson(Path.Combine(site.Captures, "hosts.json"), hosts);
            if (Wants(only, "widgets")) {
                WidgetsPage.Capture(site);
            }
            if (Wants(only, "live")) {
                LivePage.Capture(site);
            }
            if (Wants(only, "home")) {
                HomePage.Capture(site);
            }
            if (Wants(only, "powershell")) {
                PowerShellPage.Capture(site);
            }
            if (Wants(only, "benchmarks")) {
                BenchmarksPage.Capture(site);
            }
            if (Wants(only, "docs")) {
                DocsPage.Capture(site);
            }
            if (Wants(only, "objects") || Wants(only, "docs")) {
                ExplorerFigures.Capture(site);
            }
            if (Wants(only, "charts") || Wants(only, "docs")) {
                ChartFigures.Capture(site);
            }
            if (Wants(only, "docs") || Wants(only, "text-diff")) {
                TextDiffFigures.Capture(site);
            }
            if (Wants(only, "interactive")) {
                InteractivePage.Capture(site);
            }
            if (Wants(only, "demos")) {
                DemosPage.Capture(site);
            }
            return 0;
        }

        private static int Render(Site site, string? only) {
            JsonObject hosts = (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(site.Captures, "hosts.json")))!;
            Figures.Render(site);
            ChartFigures.Render(site);
            TextDiffFigures.Render(site);
            WidgetsPage.Render(site, hosts);
            LivePage.Render(site);
            DemosPage.Render(site);
            TablesPage.Render(site);
            InteractivePage.Render(site);
            PowerShellPage.Render(site, hosts);
            BenchmarksPage.Render(site, (JsonObject)JsonNode.Parse(File.ReadAllText(site.Data("live")))!);
            DocsPage.Render(site, (JsonObject)JsonNode.Parse(File.ReadAllText(site.Data("powershell")))!);
            ExplorerFigures.Render(site);
            HomePage.Render(site, hosts);
            Visuals.Render(site);
            Figures.Write(site);
            site.Write(site.Static("css/frames.css"), site.Styles.Css());
            // The replay player is the engine's own (ConsoleExporter.PlayerScript and PlayerStyles), copied on every render.
            const string generated = "Generated by Website/tools/SiteFrames render from ConsoleMonster's ConsoleExporter.{0}; do not edit. The source is ConsoleMonster/Export/Assets/{1}.";
            site.Write(site.Static("assets/player/player.js"), "/* " + string.Format(System.Globalization.CultureInfo.InvariantCulture, generated, "PlayerScript", "player.js") + " */\n" + ConsoleMonster.Export.ConsoleExporter.PlayerScript);
            site.Write(site.Static("assets/player/player.css"), "/* " + string.Format(System.Globalization.CultureInfo.InvariantCulture, generated, "PlayerStyles", "player.css") + " */\n" + ConsoleMonster.Export.ConsoleExporter.PlayerStyles);
            SiteImages.Render(site);
            WriteRuntimeVersion(site);
            CodeGuard.Check(site);
            FrameSources.Check(site);
            Readme.Check(site);
            string glyphs = string.Join("\n", site.Styles.Glyphs.Where(g => !string.IsNullOrWhiteSpace(g)));
            site.Write(Path.Combine(site.Tool, "glyphs.txt"), glyphs.Length == 0 ? string.Empty : glyphs + "\n");
            Console.WriteLine("Wrote " + site.Written.Count + " files.");
            if (Failures > 0) {
                Console.Error.WriteLine(Failures + " problem(s); see above.");
                return 1;
            }
            return 0;
        }
    }
}
