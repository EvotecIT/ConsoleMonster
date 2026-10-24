using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
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

        /// <summary>Validated metadata paired with its two canonical source files.</summary>
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
                foreach (string key in new[] { "title", "pitch", "category", "mode" }) {
                    if (string.IsNullOrWhiteSpace(Metadata[key]?.GetValue<string>())) throw new InvalidDataException(Slug + " needs " + key);
                }
                if (!Categories.Contains(Metadata["category"]!.GetValue<string>()) ||
                    Metadata["makeItYours"]!.AsArray().Count != 3 || Metadata["addons"]!.AsArray().Count != 0 ||
                    Metadata["tags"]!.AsArray().Count == 0 || Metadata["runtime"] is not JsonObject ||
                    Width < 40 || Width > 160 || Height < 4 || Height > 60 || Steps < 1 || Steps > 12 ||
                    Metadata["mode"]!.GetValue<string>() != (Steps == 1 ? "static" : "live")) {
                    throw new InvalidDataException("Invalid gallery metadata: " + Slug);
                }
                foreach (string path in new[] { PowerShell, CSharp }) {
                    if (!File.Exists(path) || File.ReadAllLines(path).Length > 250) {
                        throw new InvalidDataException(Slug + " needs a complete source of at most 250 lines: " + path);
                    }
                }
            }

            public string Folder { get; }
            public JsonObject Metadata { get; }
            public string Slug { get; }
            public int Width { get; }
            public int Height { get; }
            public int Steps { get; }
            public string PowerShell => Path.Combine(Folder, "demo.ps1");
            public string CSharp => Path.Combine(Folder, "Program.cs");
            public string Title => Metadata["title"]!.GetValue<string>();
        }

        internal static Demo[] Read(Site site) => Directory.GetDirectories(Path.Combine(site.Website, "examples", "gallery"))
            .OrderBy(path => path, StringComparer.Ordinal).Select(path => new Demo(path)).ToArray();

        private static string CapturePath(Site site, Demo demo, int step) => Path.Combine(site.Captures, "gallery", demo.Slug + "-" + step.ToString(CultureInfo.InvariantCulture) + ".ps7.ansi");

        // Pin only portable inputs. Host versions and machine paths do not belong in deterministic data.
        private static string Stamp(Demo demo) => Replays.Fingerprint(
            Site.ReadText(demo.PowerShell), demo.Width.ToString(CultureInfo.InvariantCulture), demo.Height.ToString(CultureInfo.InvariantCulture),
            demo.Steps.ToString(CultureInfo.InvariantCulture), "en-US/dark/truecolor/full/gallery-v1");

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
                        Stamps.Write(site, path, Stamp(demo));
                    }
                    Console.WriteLine("captured gallery " + demo.Slug + " (" + demo.Steps + " states)");
                } finally {
                    Site.RemoveTemporary(output);
                }
            }
            return 0;
        }

        /// <summary>Checks both source programs cell by cell, then generates gallery data and downloadable sources.</summary>
        public static void Render(Site site, string? review = null) {
            JsonArray entries = new JsonArray();
            foreach (Demo demo in Read(site)) {
                List<TimedFrame> timeline = new List<TimedFrame>();
                JsonArray parity = new JsonArray();
                for (int step = 0; step < demo.Steps; step++) {
                    CellBuffer frame = Frame(demo, step);
                    string path = CapturePath(site, demo, step);
                    if (!File.Exists(path)) throw new InvalidDataException("Missing " + demo.Slug + " capture; run SiteFrames capture Website gallery.");
                    string? stale = Stamps.Check(site, path, Stamp(demo), "gallery");
                    if (stale != null) throw new InvalidDataException(stale);
                    CellBuffer twin = Parity.Transcript(File.ReadAllBytes(path), demo.Width, demo.Height);
                    ParityResult result = Parity.Compare(frame, twin);
                    if (!result.Identical) throw new InvalidDataException(demo.Slug + " step " + step + ": " + result.Detail + "\nC#:\n" + Parity.Describe(frame) + "PowerShell:\n" + Parity.Describe(twin));
                    timeline.Add(new TimedFrame(step * 900, frame));
                    parity.Add(new JsonObject { ["step"] = step, ["identical"] = true });
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
                entry["powershell"] = Site.ReadText(demo.PowerShell);
                entry["csharp"] = Site.ReadText(demo.CSharp);
                entry["powershellRepo"] = "Website/examples/gallery/" + demo.Slug + "/demo.ps1";
                entry["csharpRepo"] = "Website/examples/gallery/" + demo.Slug + "/Program.cs";
                entry["powershellFile"] = demo.Slug + ".ps1";
                entry["csharpFile"] = "Program.cs";
                entry["thumbnail"] = "/" + thumbnail;
                entry["frame"] = site.Frame(poster, null);
                entry["parity"] = parity;
                foreach (string file in new[] { "demo.ps1", "Program.cs" }) {
                    string target = site.Static("examples/gallery/" + demo.Slug + "/" + file);
                    site.Write(target, File.ReadAllText(Path.Combine(demo.Folder, file), Encoding.UTF8));
                }
                entry["powershellDownload"] = "/examples/gallery/" + demo.Slug + "/demo.ps1";
                entry["csharpDownload"] = "/examples/gallery/" + demo.Slug + "/Program.cs";
                entry["run"] = new JsonObject {
                    ["powershell"] = "Show-ConsoleDemo " + demo.Slug,
                    ["export"] = "Get-ConsoleDemo " + demo.Slug + " -Path ./" + demo.Slug + ".ps1",
                    ["csharp"] = "dotnet new consolemonster --demo " + demo.Slug
                };
                if (demo.Steps > 1) {
                    string replay = PlayerData.Write(site, timeline, demo.Title, 10, 1800, out long duration, out int count);
                    site.Write(site.Static("replays/gallery-" + demo.Slug + ".json"), replay);
                    entry["url"] = "/replays/gallery-" + demo.Slug + ".json";
                    entry["capture"] = "deterministic snapshot storyboard";
                    entry["duration"] = duration;
                    entry["frames"] = count;
                    entry["start"] = 0;
                    entry["fps"] = 10;
                }
                entries.Add(entry);
                Console.WriteLine("gallery " + demo.Slug + ": " + demo.Steps + " twin states identical");
            }
            site.WriteJson(site.Data("gallery"), new JsonObject { ["schemaVersion"] = 1, ["demos"] = entries });
        }

        private static CellBuffer Frame(Demo demo, int step) {
            var target = CSharpRunner.Execute(demo.CSharp, CSharpRunner.Profile(demo.Width, demo.Height, ColorDepth.TrueColor, GlyphTier.Full),
                Theme.Dark, null, new[] { "--snapshot", "--step", step.ToString(CultureInfo.InvariantCulture) });
            CellBuffer frame = Parity.Transcript(Encoding.UTF8.GetBytes(target.Output), demo.Width, demo.Height);
            if (frame.Height >= demo.Height - 1) throw new InvalidDataException(demo.Slug + " needs a taller declared terminal to avoid clipping.");
            return frame;
        }
    }
}
