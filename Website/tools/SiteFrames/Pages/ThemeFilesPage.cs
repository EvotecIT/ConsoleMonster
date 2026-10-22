using System;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Theme-file figures and offline exports made by the complete program the website shows.</summary>
    internal static class ThemeFilesPage {
        private static string Sources(Site site) => Path.Combine(site.Website, "examples", "themes");

        /// <summary>Registers the before/after report and both complete native sources in the normal site render.</summary>
        public static void Render(Site site) {
            string sources = Sources(site);
            (string script, string scriptFull) = SourceSnippets.Program(Site.ReadText(Path.Combine(sources, "theme-files.ps1")));
            foreach (int width in new[] { 120, 60 }) {
                foreach (bool after in new[] { false, true }) {
                    string stage = after ? "after" : "before";
                    Figures.Add("theme-files-" + stage + "-" + width, Figures.WithProgram(new JsonObject {
                        ["title"] = after ? "After: team files" : "Before: built-in themes",
                        ["label"] = "The same build report with " + (after ? "team theme files" : "the built-in themes"),
                        ["meta"] = new JsonArray(width + " columns", "24-bit", "Full"),
                        ["file"] = "theme-files.ps1",
                        ["script"] = script,
                        ["scriptFull"] = scriptFull,
                        ["powershellRepo"] = "Website/examples/themes/theme-files.ps1",
                        ["csharpRepo"] = "Website/examples/themes/report.cs",
                        ["frame"] = site.Frame(Frame(site, width, after, false), Frame(site, width, after, true))
                    }, Path.Combine(sources, "report.cs")));
                }
            }
        }

        /// <summary>Renders the compiled example before and after loading team files, at 120 and 60 columns.</summary>
        /// <param name="site">The source website, including its team JSON examples.</param>
        /// <param name="outputDirectory">An explicit review directory outside the repository; no existing directory is cleaned.</param>
        /// <param name="powershell">Also run the public PowerShell example through the site's existing source-module capture runner.</param>
        /// <returns>Zero after writing the text, HTML, SVG, site frame bundle and four preset exports.</returns>
        /// <remarks>HTML comes from ConsoleExporter; site fragments and their stylesheet come from Site.Frame and FrameStyles.</remarks>
        public static int Review(Site site, string outputDirectory, bool powershell = false) {
            string output = Path.GetFullPath(outputDirectory);
            string relative = Path.GetRelativePath(site.Repository, output);
            if (relative == "." || (!Path.IsPathRooted(relative) && relative != ".."
                && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))) {
                throw new ArgumentException("Theme review output must be outside the repository.", nameof(outputDirectory));
            }

            string sources = Sources(site);
            JsonObject frames = new JsonObject();
            JsonArray files = new JsonArray();
            foreach (int width in new[] { 120, 60 }) {
                foreach (bool after in new[] { false, true }) {
                    string stage = after ? "after" : "before";
                    CellBuffer darkFrame = Frame(site, width, after, false);
                    CellBuffer lightFrame = Frame(site, width, after, true);
                    frames[stage + "-" + width] = new JsonObject {
                        ["title"] = after ? "After: team files" : "Before: built-in themes",
                        ["label"] = "Compiled .NET build report; " + width + " columns",
                        ["csharpRepo"] = "Website/examples/themes/report.cs",
                        ["frame"] = site.Frame(darkFrame, lightFrame)
                    };
                    WriteFrame(site, output, files, stage + "-dark-" + width, darkFrame, ExportTheme.Dark);
                    WriteFrame(site, output, files, stage + "-light-" + width, lightFrame, ExportTheme.Light);
                }
            }

            JsonArray presets = new JsonArray();
            foreach (Theme preset in new[] { Theme.Dark, Theme.Light, Theme.HighContrast, Theme.ColorBlindSafe }) {
                string name = Path.Combine("presets", preset.Name + ".json");
                site.Write(Path.Combine(output, name), ThemeFile.FromTheme(preset).ToJson());
                presets.Add(name.Replace('\\', '/'));
            }
            site.WriteJson(Path.Combine(output, "site-frames.json"), frames);
            site.Write(Path.Combine(output, "frames.css"), site.Styles.Css());
            JsonObject review = new JsonObject {
                ["producer"] = "SiteFrames.Capture.CSharpRunner.Execute",
                ["source"] = "Website/examples/themes/report.cs",
                ["powershellExample"] = "Website/examples/themes/theme-files.ps1",
                ["scope"] = "Offline exports; browser appearance is not established by this run.",
                ["powershellParity"] = powershell ? "Eight text, eight HTML and eight SVG exports match the compiled .NET example." : "Not requested.",
                ["files"] = files,
                ["presets"] = presets
            };
            Console.WriteLine("Theme review: " + output + " (8 frames, HTML, SVG and text; 4 JSON presets).");
            if (powershell) {
                string quote(string value) => "'" + value.Replace("'", "''") + "'";
                foreach (PowerShellHost host in OperatingSystem.IsWindows() ? PowerShellHost.All : new[] { PowerShellHost.Core }) {
                    string nativeOutput = Path.Combine(output, host == PowerShellHost.Core ? "powershell" : "powershell5");
                    string receipt = Path.Combine(nativeOutput, "review-complete.json");
                    if (File.Exists(receipt)) {
                        File.Delete(receipt);
                    }
                    var capture = PowerShellRunner.Run(site, host,
                        Path.Combine(site.Tool, "Capture", "ThemeFilesReview.ps1"), 120, 80,
                        PowerShellRunner.Environment("dark"),
                        arguments: "-ExamplePath " + quote(Path.Combine(sources, "theme-files.ps1"))
                            + " -OutputDirectory " + quote(nativeOutput));
                    // The ConPTY transport reports its stream, so require the harness's completion receipt as well.
                    if (!File.Exists(receipt)) {
                        throw new InvalidOperationException("The PowerShell review did not finish; no completion receipt was written.");
                    }
                    using (FileStream stream = File.Create(Path.Combine(nativeOutput, "capture.ansi"))) {
                        foreach (var chunk in capture) {
                            stream.Write(chunk.Data, 0, chunk.Data.Length);
                        }
                    }
                    foreach (JsonNode? file in files) {
                        string name = file!.GetValue<string>();
                        if (Site.ReadText(Path.Combine(output, name)) != Site.ReadText(Path.Combine(nativeOutput, name))) {
                            throw new InvalidOperationException("PowerShell/.NET theme review differs: " + name);
                        }
                    }
                    foreach (JsonNode? preset in presets) {
                        string name = preset!.GetValue<string>();
                        if (Site.ReadText(Path.Combine(output, name)) != Site.ReadText(Path.Combine(nativeOutput, name))) {
                            throw new InvalidOperationException(host.Label + "/.NET preset export differs: " + name);
                        }
                    }
                    Console.WriteLine(host.Label + " theme review: " + nativeOutput);
                }
            }
            site.WriteJson(Path.Combine(output, "review.json"), review);
            return 0;
        }

        /// <summary>Renders a fresh composition with fixed culture, full glyphs and true colour.</summary>
        /// <param name="site">The website containing the complete program and team files.</param>
        /// <param name="width">Terminal columns.</param>
        /// <param name="after">Pass the team file as the program's command-line argument.</param>
        /// <param name="light">Use a light recording target and the matching team file.</param>
        /// <returns>The real cells produced by the compiled example.</returns>
        private static CellBuffer Frame(Site site, int width, bool after, bool light) {
            string[] arguments = after ? new[] { Path.Combine(Sources(site), light ? "team-light.json" : "team-dark.json") } : Array.Empty<string>();
            var target = CSharpRunner.Execute(Path.Combine(Sources(site), "report.cs"),
                CSharpRunner.Profile(width, 80, ColorDepth.TrueColor, GlyphTier.Full), light ? Theme.Light : Theme.Dark, null, arguments);
            return Parity.Transcript(System.Text.Encoding.UTF8.GetBytes(target.Output), width, 80);
        }

        /// <summary>Writes a cell buffer through the engine's existing standalone HTML, SVG and text exporters.</summary>
        /// <param name="site">The deterministic file writer.</param>
        /// <param name="output">The review directory.</param>
        /// <param name="files">Receives the relative artifact names.</param>
        /// <param name="name">The stage, background and width.</param>
        /// <param name="frame">The already rendered cells, including the selected theme.</param>
        /// <param name="background">The terminal default background for those cells.</param>
        private static void WriteFrame(Site site, string output, JsonArray files, string name, CellBuffer frame, ExportTheme background) {
            ExportOptions options = new ExportOptions {
                Theme = background,
                WindowChrome = true,
                Title = name + " | Build queue (sample data)"
            };
            site.Write(Path.Combine(output, name + ".html"), ConsoleExporter.ToHtml(frame, options));
            site.Write(Path.Combine(output, name + ".svg"), ConsoleExporter.ToSvg(frame, options));
            site.Write(Path.Combine(output, name + ".txt"), ConsoleExporter.ToText(frame));
            files.Add(name + ".html");
            files.Add(name + ".svg");
            files.Add(name + ".txt");
        }
    }
}
