using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Demos;
using ConsoleMonster.Styling;
using SiteFrames.Capture;
using SiteFrames.Frames;
using SiteFrames.Replay;

namespace SiteFrames.Pages {
    /// <summary>Runs the copyable gallery programs and exports their frames, storyboards and metadata.</summary>
    internal static class GalleryPage {
        private static readonly HashSet<string> Categories = new HashSet<string>(StringComparer.Ordinal) {
            "Dashboards", "Wizards", "Terminal apps", "Everyday", "Welcome screens", "Reports"
        };

        /// <summary>Validated metadata paired with its three canonical source files.</summary>
        internal sealed class Demo {
            public Demo(string folder) {
                Folder = folder;
                Metadata = JsonNode.Parse(File.ReadAllText(Path.Combine(folder, "metadata.json")))!.AsObject();
                Slug = Metadata["slug"]!.GetValue<string>();
                Width = Metadata["terminal"]!["width"]!.GetValue<int>();
                Height = Metadata["terminal"]!["height"]!.GetValue<int>();
                Steps = Metadata["steps"]!.GetValue<int>();
                if (Slug != Path.GetFileName(folder) || Slug.Length == 0 || Slug.Any(c => !(c >= 'a' && c <= 'z') && c != '-')) {
                    throw new InvalidDataException("Gallery slug must match its folder: " + folder);
                }
                foreach (string key in new[] { "title", "pitch", "category", "mode", "tier", "useCase" }) {
                    if (string.IsNullOrWhiteSpace(Metadata[key]?.GetValue<string>())) throw new InvalidDataException(Slug + " needs " + key);
                }
                if (!Categories.Contains(Metadata["category"]!.GetValue<string>()) ||
                    (Tier != "starter" && Tier != "showcase") ||
                    !Metadata["useCase"]!.GetValue<string>().StartsWith("Use it when ", StringComparison.Ordinal) ||
                    Metadata["makeItYours"]!.AsArray().Count != 3 || Metadata["addons"]!.AsArray().Count != 0 ||
                    Metadata["tags"]!.AsArray().Count == 0 || Metadata["runtime"] is not JsonObject ||
                    Width < 40 || Width > 160 || Height < 4 || Height > 60 || Steps < 1 || Steps > 12 ||
                    Metadata["mode"]!.GetValue<string>() != (Steps == 1 ? "static" : "live") ||
                    Metadata["capture"]?["states"] is not JsonArray states || states.Count != Steps) {
                    throw new InvalidDataException("Invalid gallery metadata: " + Slug);
                }
                foreach (string path in new[] { PowerShell, CSharp, CSharpFluent }) {
                    // Counted after SourceLayout wraps at 100 columns: data rows take two or three lines each.
                    int limit = Tier == "showcase" ? 600 : 300;
                    if (!File.Exists(path) || File.ReadAllLines(path).Length > limit) {
                        throw new InvalidDataException(Slug + " needs a complete source of at most " + limit + " lines: " + path);
                    }
                }
            }

            public string Folder { get; }
            public JsonObject Metadata { get; }
            public string Slug { get; }
            public int Width { get; }
            public int Height { get; }
            public int Steps { get; }
            public string PowerShell => Path.Combine(Folder, Slug + ".ps1");
            public string CSharp => Path.Combine(Folder, "Program.cs");
            public string CSharpFluent => Path.Combine(Folder, "Program.Fluent.cs");
            public string Title => Metadata["title"]!.GetValue<string>();
            public string Tier => Metadata["tier"]!.GetValue<string>();
        }

        internal static Demo[] Read(Site site) => Directory.GetDirectories(Path.Combine(site.Website, "examples", "gallery"))
            .Select(path => new Demo(path))
            .OrderByDescending(demo => demo.Metadata["featured"]?.GetValue<bool>() ?? false)
            .ThenBy(demo => demo.Slug, StringComparer.Ordinal).ToArray();

        private static string CapturePath(Site site, Demo demo, int step) => Path.Combine(site.Captures, "gallery", demo.Slug + "-" + step.ToString(CultureInfo.InvariantCulture) + ".ps7.ansi");

        /// <summary>The verified final capture, reused for route thumbnails and social cards.</summary>
        internal static CellBuffer CapturedPoster(Site site, Demo demo) =>
            Parity.Transcript(File.ReadAllBytes(CapturePath(site, demo, demo.Steps - 1)), demo.Width, demo.Height);

        // Pin only portable inputs. Host versions and machine paths do not belong in deterministic data.
        private static string Stamp(Site site, Demo demo) {
            string[] parts = {
                Site.ReadText(demo.PowerShell), Site.ReadText(demo.CSharp), Site.ReadText(demo.CSharpFluent), Site.ReadText(Path.Combine(site.Website, "..", "ConsoleMonster.PowerShell", "Gallery", "DemoCapture.ps1")), demo.Width.ToString(CultureInfo.InvariantCulture), demo.Height.ToString(CultureInfo.InvariantCulture),
                demo.Metadata["capture"]!.ToJsonString(), "en-US/dark/truecolor/full/gallery-v3"
            };
            // Only clock recipes use this producer setting; ordinary gallery receipts retain their existing inputs.
            if (demo.Metadata["capture"]!["actionClock"] != null) {
                parts = parts.Append(Site.ReadText(Path.Combine(site.Website, "tools", "SiteFrames", "Capture", "GalleryCapture.ps1"))).ToArray();
            }
            return Replays.Fingerprint(parts);
        }

        /// <summary>Captures every snapshot on PowerShell 7, on macOS, Linux or Windows; no pseudo-console is required.</summary>
        public static int Capture(Site site) {
            string modules = PowerShellRunner.ModuleLinkRoot(site);
            string? selected = Environment.GetEnvironmentVariable("SITEFRAMES_GALLERY");
            foreach (Demo demo in Read(site).Where(demo => string.IsNullOrEmpty(selected) || selected.Split(',').Contains(demo.Slug, StringComparer.Ordinal))) {
                string output = Path.Combine(Path.GetTempPath(), "cm-gallery-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(output);
                try {
                    ProcessStartInfo info = new ProcessStartInfo("pwsh") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
                    foreach (string argument in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-File", Path.Combine(site.Tool, "Capture", "GalleryCapture.ps1"),
                        "-ModuleRoot", modules, "-Source", demo.PowerShell, "-Output", output,
                        "-Width", demo.Width.ToString(CultureInfo.InvariantCulture), "-Height", demo.Height.ToString(CultureInfo.InvariantCulture),
                        "-Steps", demo.Steps.ToString(CultureInfo.InvariantCulture) }) info.ArgumentList.Add(argument);
                    foreach (KeyValuePair<string, string?> setting in PowerShellRunner.Environment("dark")) info.Environment[setting.Key] = setting.Value;
                    // SiteFrames clears host overrides; captures use the Release module built for the website.
                    info.Environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Release";
                    using (Process process = Process.Start(info)!) {
                        var stdout = process.StandardOutput.ReadToEndAsync();
                        var stderr = process.StandardError.ReadToEndAsync();
                        if (!process.WaitForExit(60000)) {
                            process.Kill(true);
                            process.WaitForExit();
                            throw new TimeoutException(demo.Slug + " PowerShell capture exceeded one minute.");
                        }
                        if (process.ExitCode != 0) throw new InvalidOperationException(demo.Slug + " PowerShell capture failed:\n" + stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
                    }
                    for (int step = 0; step < demo.Steps; step++) {
                        string path = CapturePath(site, demo, step);
                        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                        File.Copy(Path.Combine(output, step.ToString(CultureInfo.InvariantCulture) + ".ansi"), path, true);
                        Stamps.Write(site, path, Stamp(site, demo));
                    }
                    Console.WriteLine("captured gallery " + demo.Slug + " (" + demo.Steps + " states)");
                } finally {
                    Site.RemoveTemporary(output);
                }
            }
            return 0;
        }

        /// <summary>Checks all three source programs cell by cell and by encoded frame bytes, then generates gallery data and downloadable sources.</summary>
        public static void Render(Site site, string? review = null) {
            Demo[] demos = Read(site);
            Site.Clean(site.Static("examples/gallery"));
            Site.Clean(site.Static("site/gallery"));
            JsonArray entries = new JsonArray();
            foreach (Demo demo in demos) {
                List<TimedFrame> timeline = new List<TimedFrame>();
                JsonArray parity = new JsonArray();
                // A scenario recipe shows complete outcomes, rather than successive live ticks.
                // Reuse the source's final demo pause so each outcome has time to be read on a slide.
                int holdMilliseconds = 900;
                if (demo.Metadata["capture"]!["states"]!.AsArray().Any(state => state?["arguments"] != null)) {
                    DemoStep? pause = DemoScript.Read(demo.PowerShell).Steps.LastOrDefault(item => item.Kind == DemoStepKind.Pause);
                    if (pause != null) holdMilliseconds = Math.Max(900, (int)(double.Parse(pause.Text, CultureInfo.InvariantCulture) * 1000));
                }
                for (int step = 0; step < demo.Steps; step++) {
                    CellBuffer frame = TripletFrame(site, demo, step);
                    timeline.Add(new TimedFrame(step * (long)holdMilliseconds, frame));
                    parity.Add(new JsonObject { ["step"] = step, ["identical"] = true, ["styles"] = 3, ["encoding"] = "UTF-8 SVG" });
                    if (review != null) {
                        ExportOptions options = new ExportOptions { Title = demo.Title, Theme = ExportTheme.Dark, WindowChrome = true };
                        site.Write(Path.Combine(review, demo.Slug + "-" + step + ".html"), ConsoleExporter.ToHtml(frame, options));
                        site.Write(Path.Combine(review, demo.Slug + "-" + step + ".txt"), ConsoleExporter.ToText(frame));
                    }
                }
                CellBuffer poster = timeline[demo.Steps == 1 ? 0 : demo.Steps - 1].Frame;
                string thumbnail = "site/gallery/" + demo.Slug + ".svg";
                site.Write(site.Static(thumbnail), FrameMinimap.Svg(poster, ColorDepth.TrueColor, Screen.Dark, demo.Width, poster.Height));
                JsonObject entry = (JsonObject)demo.Metadata.DeepClone();
                entry["id"] = demo.Slug;
                JsonObject powershell = Source(demo.PowerShell, demo.Slug);
                JsonObject csharp = Source(demo.CSharp, demo.Slug);
                JsonObject fluent = Source(demo.CSharpFluent, demo.Slug);
                entry["source"] = new JsonObject { ["powershell"] = powershell, ["csharp"] = csharp, ["csharpFluent"] = fluent };
                entry["powershell"] = powershell["text"]!.GetValue<string>();
                entry["csharp"] = csharp["text"]!.GetValue<string>();
                entry["csharpFluent"] = fluent["text"]!.GetValue<string>();
                entry["apiMap"] = ApiMappingNotes.ForPair(site, demo.CSharp, demo.CSharpFluent, demo.PowerShell);
                entry["powershellRepo"] = powershell["repo"]!.GetValue<string>();
                entry["csharpRepo"] = csharp["repo"]!.GetValue<string>();
                entry["csharpFluentRepo"] = fluent["repo"]!.GetValue<string>();
                entry["powershellFile"] = demo.Slug + ".ps1";
                entry["csharpFile"] = "Program.cs";
                entry["csharpFluentFile"] = "Program.Fluent.cs";
                entry["thumbnail"] = "/" + thumbnail;
                entry["frame"] = site.Frame(poster, null);
                entry["parity"] = parity;
                entry["lines"] = new JsonObject {
                    ["powershell"] = File.ReadAllLines(demo.PowerShell).Length,
                    ["csharp"] = File.ReadAllLines(demo.CSharp).Length,
                    ["csharpFluent"] = File.ReadAllLines(demo.CSharpFluent).Length
                };
                entry["featured"] = demo.Metadata["featured"]?.GetValue<bool>() ?? false;
                entry["replay"] = null;
                foreach (string file in new[] { demo.Slug + ".ps1", "Program.cs", "Program.Fluent.cs" }) {
                    string target = site.Static("examples/gallery/" + demo.Slug + "/" + file);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(Path.Combine(demo.Folder, file), target, true);
                }
                entry["powershellDownload"] = "/examples/gallery/" + demo.Slug + "/" + demo.Slug + ".ps1";
                entry["csharpDownload"] = "/examples/gallery/" + demo.Slug + "/Program.cs";
                entry["csharpFluentDownload"] = "/examples/gallery/" + demo.Slug + "/Program.Fluent.cs";
                string projectTarget = site.Static("examples/gallery/" + demo.Slug + "/GalleryDemo.csproj");
                File.Copy(Path.Combine(site.Website, "examples", "gallery", "GalleryDemo.csproj.template"), projectTarget, true);
                entry["projectDownload"] = "/examples/gallery/" + demo.Slug + "/GalleryDemo.csproj";
                entry["run"] = new JsonObject {
                    ["powershell"] = "Show-ConsoleDemo " + demo.Slug,
                    ["export"] = "Export-ConsoleDemo " + demo.Slug + " -Path ./" + demo.Slug + ".ps1",
                    ["csharp"] = "dotnet new consolemonster --demo " + demo.Slug,
                    ["csharpFluent"] = "dotnet new consolemonster --demo " + demo.Slug + " --DemoStyle Fluent"
                };
                if (demo.Steps > 1) {
                    string replay = PlayerData.Write(site, timeline, demo.Title, 10, Math.Max(1800, holdMilliseconds), out long duration, out int count);
                    site.Write(site.Static("replays/gallery-" + demo.Slug + ".json"), replay);
                    entry["url"] = "/replays/gallery-" + demo.Slug + ".json";
                    entry["capture"] = "scripted native program";
                    entry["duration"] = duration;
                    entry["frames"] = count;
                    entry["start"] = 0;
                    entry["fps"] = 10;
                    entry["replay"] = new JsonObject {
                        ["url"] = entry["url"]!.GetValue<string>(),
                        ["w"] = timeline.Max(frame => frame.Frame.Width),
                        ["h"] = timeline.Max(frame => frame.Frame.Height),
                        ["poster"] = site.Frame(poster, null),
                        ["start"] = timeline[timeline.Count - 1].Time,
                        ["hero"] = ReplayFit.Hero(timeline)
                    };
                }
                entries.Add(entry);
                Console.WriteLine("gallery " + demo.Slug + ": " + demo.Steps + " triplet states byte-identical");
            }
            site.WriteJson(site.Data("gallery"), new JsonObject { ["schemaVersion"] = 1, ["demos"] = entries });
            WriteDetailPages(site, demos);
        }

        /// <summary>The complete front matter for the page that reads this demo from gallery.json.</summary>
        internal static string DetailPage(Demo demo) {
            string pitch = JsonSerializer.Serialize(demo.Metadata["pitch"]!.GetValue<string>(),
                new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            return "---\n" +
                "title: " + demo.Title + "\n" +
                "description: " + pitch + "\n" +
                "slug: demos/" + demo.Slug + "\n" +
                "layout: demo\n" +
                "demo: " + demo.Slug + "\n" +
                "meta.seo_title: " + demo.Title + " demo | ConsoleMonster\n" +
                "---\n";
        }

        /// <summary>Generates routes from the same metadata as the catalogue; unrecognized pages are errors.</summary>
        internal static void WriteDetailPages(Site site, Demo[] demos) {
            string folder = Path.Combine(site.Website, "content", "pages", "demos");
            foreach (Demo demo in demos) site.Write(Path.Combine(folder, demo.Slug + ".md"), DetailPage(demo));
            ValidateDetailPages(site, demos);
        }

        /// <summary>Rejects missing, orphaned or stale pages before they can become broken detail links.</summary>
        internal static void ValidateDetailPages(Site site, Demo[] demos) {
            string folder = Path.Combine(site.Website, "content", "pages", "demos");
            string[] actual = Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*.md", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(folder, path)).ToArray()
                : Array.Empty<string>();
            string[] expected = demos.Select(demo => demo.Slug + ".md").ToArray();
            string[] missing = expected.Except(actual, StringComparer.Ordinal).ToArray();
            string[] orphaned = actual.Except(expected, StringComparer.Ordinal).ToArray();
            if (missing.Length > 0 || orphaned.Length > 0) {
                throw new InvalidDataException("Gallery detail pages do not match demos. Missing: " + string.Join(", ", missing) +
                    "; orphaned: " + string.Join(", ", orphaned));
            }
            foreach (Demo demo in demos) {
                if (File.ReadAllText(Path.Combine(folder, demo.Slug + ".md")).Replace("\r\n", "\n") != DetailPage(demo)) {
                    throw new InvalidDataException("Stale gallery detail page: " + demo.Slug + ". Run SiteFrames render.");
                }
            }
        }

        /// <summary>Regenerates every fluent program from its initializer source and checks the translated compilation.</summary>
        internal static void WriteFluentSources(Site site) {
            foreach (Demo demo in Read(site)) {
                string source = CSharpRunner.FluentSource(demo.CSharp);
                CSharpRunner.Check(demo.CSharp, source);
                CheckFluentConstruction(site, demo.CSharp, source);
                File.WriteAllText(demo.CSharpFluent, source, new UTF8Encoding(false));
                Console.WriteLine("gallery " + demo.Slug + ": fluent source regenerated");
            }
        }

        /// <summary>Compiles both C# styles independently, without needing terminal captures.</summary>
        internal static void CheckSources(Site site) {
            var failures = new List<string>();
            foreach (Demo demo in Read(site)) {
                foreach (string path in new[] { demo.CSharp, demo.CSharpFluent }) {
                    try { CSharpRunner.Compile(path); if (path == demo.CSharpFluent) { CheckFluentConstruction(site, path, Site.ReadText(path)); } }
                    catch (InvalidOperationException error) { failures.Add(demo.Slug + "/" + Path.GetFileName(path) + ": " + error.Message); }
                }
            }
            if (failures.Count != 0) { throw new InvalidDataException(string.Join("\n\n", failures)); }
            Console.WriteLine("All gallery initializer and fluent programs compile independently.");
        }

        private static void CheckFluentConstruction(Site site, string path, string source) {
            var map = JsonNode.Parse(File.ReadAllText(Path.Combine(site.Repository, "Tests", "Api", "round2-parity.json")))!;
            var allowed = map["FluentGrammar"]!["Entry"]!["DataConstructionExceptions"]!.AsArray().Select(row => row!["CSharpType"]!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
            string[] direct = CSharpRunner.ConstructedConsoleTypes(path, source).Where(type => !allowed.Contains(type)).ToArray();
            if (direct.Length > 0) { throw new InvalidOperationException("Fluent UI construction must use Ui and content builders: " + string.Join(", ", direct)); }
            if (!CSharpRunner.ConfiguredMembersFromSource(path, source).Any(member => member.StartsWith("M:ConsoleMonster.Ui.", StringComparison.Ordinal))) {
                throw new InvalidOperationException("A fluent gallery program must use the Ui entry point.");
            }
        }

        /// <summary>Checks recorded PowerShell against the ordinary C# programs without rewriting site assets.</summary>
        internal static void Verify(Site site, string? selected = null) {
            foreach (Demo demo in Read(site).Where(demo => selected == null || selected.Split(',').Contains(demo.Slug, StringComparer.Ordinal))) {
                for (int step = 0; step < demo.Steps; step++) TripletFrame(site, demo, step);
                Console.WriteLine(demo.Slug + ": " + demo.Steps + " triplet states byte-identical");
            }
        }

        private static CellBuffer TripletFrame(Site site, Demo demo, int step) {
            CellBuffer frame = Frame(demo, step);
            CellBuffer fluent = GalleryFrames.Run(demo, step, true);
            AssertSame(demo, step, frame, fluent, "C# fluent");
            string path = CapturePath(site, demo, step);
            if (!File.Exists(path)) throw new InvalidDataException("Missing " + demo.Slug + " capture; run SiteFrames capture Website gallery.");
            string? stale = Stamps.Check(site, path, Stamp(site, demo), "gallery");
            if (stale != null) throw new InvalidDataException(stale);
            CellBuffer twin = Parity.Transcript(File.ReadAllBytes(path), demo.Width, demo.Height);
            AssertSame(demo, step, frame, twin, "PowerShell");
            return frame;
        }

        // Compare cells (including styling and links), then the public export bytes for the same frame.
        // Capture recipes select deterministic states; raw terminal timings and control transcripts are not a stable format.
        private static void AssertSame(Demo demo, int step, CellBuffer expected, CellBuffer actual, string style) {
            ParityResult result = Parity.Compare(expected, actual);
            if (!result.Identical) { throw new InvalidDataException(demo.Slug + " step " + step + ": " + style + " " + result.Detail + "\nC#:\n" + Parity.Describe(expected) + style + ":\n" + Parity.Describe(actual)); }
            var options = new ExportOptions { Theme = ExportTheme.Dark };
            byte[] first = FrameExporter.Export(expected, "Svg", options);
            byte[] second = FrameExporter.Export(actual, "Svg", options);
            if (!first.SequenceEqual(second)) { throw new InvalidDataException(demo.Slug + " step " + step + ": " + style + " differs in encoded frame bytes."); }
        }

        // Display the whole UTF-8 program without the encoding marker; downloads retain the original bytes.
        // Site.ReadText is for normalized snippets and must not be used for copyable full sources.
        private static JsonObject Source(string path, string slug) => new JsonObject {
            ["file"] = Path.GetFileName(path),
            ["text"] = File.ReadAllText(path, new UTF8Encoding(false, true)),
            ["repo"] = "Website/examples/gallery/" + slug + "/" + Path.GetFileName(path)
        };

        private static CellBuffer Frame(Demo demo, int step) {
            return GalleryFrames.Run(demo, step);
        }
    }
}
