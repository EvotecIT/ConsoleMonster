using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Examples.Showcases;
using SiteFrames.Capture;
using SiteFrames.Frames;
using SiteFrames.Replay;
using SiteFrames.Vt;

namespace SiteFrames.Pages {
    /// <summary>
    /// Live dashboards, each recorded from a real run of the program shown beside it, in a pseudo console: the C# programs
    /// under <c>Website/examples/live/</c> (built here exactly as written and run with <c>dotnet</c>) and the PowerShell
    /// build pipeline script on both hosts. The assessment program replays a recording file; that file is synthetic data
    /// the tool writes (and the page offers for download), the program only reads it.
    /// </summary>
    internal static class LivePage {
        public static string ProgramPath(Site site, string name) => Path.Combine(site.Website, "examples", "live", name + ".cs");

        private static string PipelineScript(Site site) => Path.Combine(site.Repository, "Module", "Examples", "Example-BuildPipeline.ps1");

        private static string CastPath(Site site, string id) => Path.Combine(site.Captures, "live", id + ".cast");

        // Where the programs are built and the recording file is written for one capture run: one folder per checkout and
        // run, removed afterwards, so two worktrees never run each other's builds.
        private static string? _work;

        private static string Work => _work ?? throw new InvalidOperationException("The live programs are not built.");

        /// <summary>The dotnet that hosts this tool, so a built program runs on the runtime it was compiled against.</summary>
        internal static string DotnetHost {
            get {
                string? host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
                if (!string.IsNullOrEmpty(host) && File.Exists(host)) {
                    return host!;
                }
                string root = Path.GetFullPath(Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", ".."));
                string candidate = Path.Combine(root, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
                return File.Exists(candidate) ? candidate : "dotnet";
            }
        }

        private static List<OutputChunk> Dotnet(string program, string arguments, int width, int height, Dictionary<string, string?> environment) =>
            PseudoConsole.Run("\"" + DotnetHost + "\" \"" + Path.Combine(Work, program + ".dll") + "\"" + (arguments.Length > 0 ? " " + arguments : string.Empty), width, height, environment, Work, TimeSpan.FromMinutes(3));

        private sealed class Run {
            public Run(string id, string title, Func<Site, List<OutputChunk>> record, int width, int height, Func<Site, string> source) {
                Id = id;
                Title = title;
                Record = record;
                Width = width;
                Height = height;
                Source = source;
            }

            public string Id { get; }
            public string Title { get; }
            public Func<Site, List<OutputChunk>> Record { get; }
            public int Width { get; }
            public int Height { get; }

            /// <summary>Everything the recording is made from, as text: its program, arguments, size, mode and data.</summary>
            public Func<Site, string> Source { get; }
        }

        private static string Program(Site site, string name, string arguments, int width, int height, string mode = "interactive") =>
            Site.ReadText(ProgramPath(site, name)) + "\n" + arguments + "\n" + width + "x" + height + " " + mode + (name == "replay" ? "\n" + AssessmentRecording() : string.Empty);

        /// <summary>The run recordings. The title is the command a reader types.</summary>
        private static IEnumerable<Run> Recordings() {
            yield return new Run("pipeline-csharp", "dotnet run", site => Dotnet("pipeline", string.Empty, 110, 32, PowerShellRunner.Environment("dark")), 110, 32, site => Program(site, "pipeline", string.Empty, 110, 32));
            yield return new Run("pipeline-ps7", "Example-BuildPipeline.ps1", site => PowerShellRunner.Run(site, PowerShellHost.Core, PipelineScript(site), 110, 32, PowerShellRunner.Environment("dark")), 110, 32, site => Site.ReadText(PipelineScript(site)) + "\nps7 110x32");
            yield return new Run("pipeline-ps5", "Example-BuildPipeline.ps1", site => PowerShellRunner.Run(site, PowerShellHost.Desktop, PipelineScript(site), 110, 32, PowerShellRunner.Environment("dark")), 110, 32, site => Site.ReadText(PipelineScript(site)) + "\nps5 110x32");
            yield return new Run("stress", "dotnet run -- 12", site => Dotnet("stress", "12", 120, 40, PowerShellRunner.Environment("dark")), 120, 40, site => Program(site, "stress", "12", 120, 40));
            yield return new Run("assessment", "dotnet run -- assessment-run.jsonl 2", site => Dotnet("replay", "assessment-run.jsonl 2", 120, 36, PowerShellRunner.Environment("dark")), 120, 36, site => Program(site, "replay", "assessment-run.jsonl 2", 120, 36));
            yield return new Run("assessment-compact", "dotnet run -- assessment-run.jsonl 2", site => Dotnet("replay", "assessment-run.jsonl 2", 64, 30, PowerShellRunner.Environment("dark")), 64, 30, site => Program(site, "replay", "assessment-run.jsonl 2", 64, 30));
            yield return new Run("assessment-stream", "dotnet run -- assessment-run.jsonl 6", site => Dotnet("replay", "assessment-run.jsonl 6", 100, 400, PowerShellRunner.Environment("dark", mode: "stream")), 100, 400, site => Program(site, "replay", "assessment-run.jsonl 6", 100, 400, "stream"));
            // The same dashboard in smaller windows, for the live sessions page.
            foreach ((int w, int h) in new[] { (100, 30), (64, 20), (50, 12) }) {
                yield return new Run("size-" + w + "x" + h, "dotnet run -- assessment-run.jsonl 2", site => Dotnet("replay", "assessment-run.jsonl 2", w, h, PowerShellRunner.Environment("dark")), w, h, site => Program(site, "replay", "assessment-run.jsonl 2", w, h));
            }
        }

        // Whether a recording was made from what its page shows now; a stale one is reported, not shown silently.
        private static void CheckFresh(Site site, string id, string cast) {
            foreach (Run run in Recordings()) {
                if (run.Id == id && Replays.Source(cast) != Replays.Fingerprint(run.Source(site))) {
                    WidgetsPage.Report(new List<string> { "recording " + id + " was made from another version of its program, arguments or data; run the capture command for the live area (SITEFRAMES_LIVE=" + id + ")." }, "live");
                }
            }
        }

        /// <summary>The synthetic recording the assessment program replays (also offered for download).</summary>
        public static string AssessmentRecording() => Replays.Record(ParallelRunRecording.Generate());

        /// <summary>Builds the programs, writes the recording file, and records every run (SITEFRAMES_LIVE=id,id for some).</summary>
        public static void Capture(Site site) {
            _work = Path.Combine(Path.GetTempPath(), "cmsite-live-" + Guid.NewGuid().ToString("N").Substring(0, 12));
            try {
                foreach (string program in new[] { "pipeline", "stress", "replay" }) {
                    CSharpRunner.Build(ProgramPath(site, program), Work);
                }
                File.WriteAllText(Path.Combine(Work, "assessment-run.jsonl"), AssessmentRecording(), new System.Text.UTF8Encoding(false));
                HashSet<string>? wanted = null;
                string? filter = Environment.GetEnvironmentVariable("SITEFRAMES_LIVE");
                if (!string.IsNullOrWhiteSpace(filter)) {
                    wanted = new HashSet<string>(filter!.Split(','), StringComparer.OrdinalIgnoreCase);
                }
                foreach (Run recording in Recordings()) {
                    if (wanted != null && !wanted.Contains(recording.Id)) {
                        continue;
                    }
                    Stopwatch watch = Stopwatch.StartNew();
                    List<OutputChunk> chunks = recording.Record(site);
                    string path = CastPath(site, recording.Id);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    // Window titles (OSC 0 and 2) carry the executable's path; nothing on the site shows them.
                    string cast = Regex.Replace(Replays.ToCast(chunks, recording.Width, recording.Height, recording.Title), @"\\u001B\][02];(?:[^""\\]|\\\\)*?(?:\\u0007|\\u001B\\\\)", string.Empty);
                    File.WriteAllText(path, Replays.Stamp(cast, Replays.Fingerprint(recording.Source(site))), new System.Text.UTF8Encoding(false));
                    Console.WriteLine("recorded " + recording.Id + " in " + watch.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
                }
            } finally {
                Site.RemoveTemporary(Work);
                _work = null;
            }
        }

        // The source of a program for the page: the short form, the whole program, and its file name.
        private static void Source(JsonObject entry, string code, string language, string file) {
            (string shortForm, string full) = SourceSnippets.Program(code);
            entry[language] = shortForm;
            entry[language + "Full"] = full;
            entry[language + "File"] = file;
        }

        public static void Render(Site site) {
            string replays = site.Static("replays");
            Site.Clean(replays);
            string downloads = site.Static("recordings");
            Site.Clean(downloads);
            JsonObject live = new JsonObject();

            // The assessment: the program replays the synthetic recording file, offered for download as it was replayed.
            string jsonLines = AssessmentRecording();
            site.Write(Path.Combine(downloads, "assessment-run.jsonl"), jsonLines);
            JsonObject assessment = FromCast(site, "assessment", "C#", "dotnet run -- assessment-run.jsonl 2", 5, 0.55);
            string[] lines = jsonLines.Split('\n');
            assessment["events"] = lines.Count(l => l.Trim().Length > 0);
            assessment["bytes"] = jsonLines.Length;
            assessment["excerpt"] = string.Join("\n", lines.Take(9));
            Source(assessment, Site.ReadText(ProgramPath(site, "replay")), "csharp", "Program.cs");
            SourceStyles.Add(site, assessment, ProgramPath(site, "replay"));
            live["assessment"] = assessment;
            JsonObject compact = FromCast(site, "assessment-compact", "C#", "dotnet run -- assessment-run.jsonl 2", 3, 0.55);
            // The same program as the assessment, in a narrower window: its code is the assessment's.
            compact["sourceOf"] = "assessment";
            live["assessmentCompact"] = compact;

            // Recorded runs.
            JsonObject pipeline = new JsonObject {
                ["csharp"] = FromCast(site, "pipeline-csharp", "C#", "dotnet run"),
                ["ps7"] = FromCast(site, "pipeline-ps7", "PowerShell 7", "Example-BuildPipeline.ps1"),
                ["ps5"] = FromCast(site, "pipeline-ps5", "Windows PowerShell 5.1", "Example-BuildPipeline.ps1")
            };
            // The recordings are keyed by run (csharp, ps7, ps5), so the two programs go under "source".
            JsonObject pipelineSource = new JsonObject();
            Source(pipelineSource, Site.ReadText(PipelineScript(site)), "powershell", "Example-BuildPipeline.ps1");
            Source(pipelineSource, Site.ReadText(ProgramPath(site, "pipeline")), "csharp", "Program.cs");
            SourceStyles.Add(site, pipelineSource, ProgramPath(site, "pipeline"));
            pipeline["source"] = pipelineSource;
            live["pipeline"] = pipeline;
            JsonObject stress = FromCast(site, "stress", "C#", "dotnet run -- 12", 3, 0.45);
            stress["result"] = StressResult(site);
            Source(stress, Site.ReadText(ProgramPath(site, "stress")), "csharp", "Program.cs");
            SourceStyles.Add(site, stress, ProgramPath(site, "stress"));
            live["stress"] = stress;
            JsonObject stream = Stream(site);
            Source(stream, Site.ReadText(ProgramPath(site, "replay")), "csharp", "Program.cs");
            SourceStyles.Add(site, stream, ProgramPath(site, "replay"));
            live["stream"] = stream;
            string query = ProgramPath(site, "query");
            JsonObject queryFigure = new JsonObject {
                ["title"] = "dotnet run",
                ["label"] = "DNS and mail-security query results printed once.",
                ["meta"] = new JsonArray("100 columns", "24-bit", "Full"),
                ["frame"] = site.Frame(CSharpRunner.Run(query, 100, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark, 120), CSharpRunner.Run(query, 100, ColorDepth.TrueColor, GlyphTier.Full, Theme.Light, 120))
            };
            Source(queryFigure, Site.ReadText(query), "csharp", "Program.cs");
            SourceStyles.Add(site, queryFigure, query);
            live["query"] = queryFigure;

            site.WriteJson(site.Data("live"), live);
        }

        /// <summary>The frame shown before a replay plays: the last frame at a share of the run, and its time.</summary>
        internal static long Poster(Site site, List<TimedFrame> frames, double share, out string html) => Poster(site, frames, share, out html, out _);

        /// <summary>The poster: the frame at a share of the run, as HTML and as the frame itself.</summary>
        internal static long Poster(Site site, List<TimedFrame> frames, double share, out string html, out CellBuffer frame) {
            long at = (long)Math.Round(frames[frames.Count - 1].Time * share);
            TimedFrame moment = frames.FindLast(f => f.Time <= at);
            html = Frames.FrameHtml.Body(Frames.FrameHtml.Rows(moment.Frame, ColorDepth.TrueColor, Screen.Dark, site.Styles), false);
            frame = moment.Frame;
            return moment.Time;
        }

        /// <summary>A frame from a recording at a share of its run (the frame the poster shows), or null.</summary>
        public static CellBuffer? FrameAt(Site site, string id, double share, int fps = 2) {
            string path = CastPath(site, id);
            if (!File.Exists(path)) {
                return null;
            }
            string cast = File.ReadAllText(path);
            CheckFresh(site, id, cast);
            List<TimedFrame> frames = Replays.FromCast(cast, fps, true, out _, out _, out _);
            Poster(site, frames, share, out _, out CellBuffer frame);
            return frame;
        }

        private static JsonObject FromCast(Site site, string id, string label, string command, double fps = 6, double share = 0.55) {
            string path = CastPath(site, id);
            if (!File.Exists(path)) {
                WidgetsPage.Report(new List<string> { "missing capture " + id + "; run the capture command for the live area." }, "live");
                return new JsonObject();
            }
            string cast = File.ReadAllText(path);
            CheckFresh(site, id, cast);
            List<TimedFrame> frames = Replays.FromCast(cast, fps, true, out CellBuffer scrollback, out int width, out int height);
            site.Write(site.Static("replays/" + id + ".json"), PlayerData.Write(site, frames, command, fps, 3000, out long duration, out int count));
            long posterTime = Poster(site, frames, share, out string poster);
            return new JsonObject {
                ["start"] = posterTime,
                ["id"] = id,
                ["label"] = label,
                ["command"] = command,
                ["url"] = "/replays/" + id + ".json",
                ["w"] = width,
                ["h"] = height,
                ["frames"] = count,
                ["duration"] = duration,
                ["fps"] = fps,
                ["poster"] = new JsonObject { ["w"] = width, ["h"] = height, ["dark"] = poster },
                ["scrollback"] = site.Frame(Site.Crop(scrollback), null)
            };
        }

        // The stress test prints one result line after the session; the page quotes it as measured on the capture machine.
        private static JsonObject? StressResult(Site site) {
            string path = CastPath(site, "stress");
            if (!File.Exists(path)) {
                return null;
            }
            // Wrapped rows are joined again: a row that fills the width continues on the next one.
            Replays.FromCast(File.ReadAllText(path), 3, true, out CellBuffer scrollback, out int width, out _);
            System.Text.StringBuilder joined = new System.Text.StringBuilder();
            for (int y = 0; y < scrollback.Height; y++) {
                string row = scrollback.GetRowText(y).TrimEnd();
                joined.Append(row);
                if (row.Length < width) {
                    joined.Append('\n');
                }
            }
            string text = joined.ToString();
            Match match = Regex.Match(text, @"STRESS ([^\r\n]*)");
            if (!match.Success) {
                return null;
            }
            JsonObject result = new JsonObject { ["line"] = match.Value.TrimEnd() };
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match pair in Regex.Matches(match.Groups[1].Value, @"(\w+)=(\S+)")) {
                values[pair.Groups[1].Value] = pair.Groups[2].Value;
                result[pair.Groups[1].Value] = pair.Groups[2].Value;
            }
            string N(string key) => long.TryParse(values[key].TrimEnd('/', 's'), NumberStyles.Integer, CultureInfo.InvariantCulture, out long n) ? n.ToString("N0", Site.Culture) : values[key];
            result["facts"] = new JsonArray(
                new JsonObject { ["label"] = "Updates posted in " + values["seconds"] + " s", ["value"] = N("posted") },
                new JsonObject { ["label"] = "Average update rate", ["value"] = N("avgRate") + "/s" },
                new JsonObject { ["label"] = "Frames drawn", ["value"] = N("frames") + " (" + values["fps"] + " fps)" },
                new JsonObject { ["label"] = "Dropped updates and errors", ["value"] = values["dropped"] + " / " + values["errors"] },
                new JsonObject { ["label"] = "CPU, share of one core", ["value"] = values["cpuOneCore"] });
            return result;
        }

        private static JsonObject Stream(Site site) {
            string path = CastPath(site, "assessment-stream");
            if (!File.Exists(path)) {
                return new JsonObject();
            }
            string cast = File.ReadAllText(path);
            CheckFresh(site, "assessment-stream", cast);
            Replays.FromCast(cast, 8, false, out CellBuffer scrollback, out int width, out _);
            return new JsonObject {
                ["title"] = "CONSOLEMONSTER_MODE=stream",
                ["frame"] = site.Frame(Site.Crop(scrollback), null),
                ["meta"] = new JsonArray(width + " columns", "stream mode")
            };
        }
    }
}
