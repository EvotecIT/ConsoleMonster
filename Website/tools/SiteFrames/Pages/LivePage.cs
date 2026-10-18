using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using ConsoleMonster.Examples.Showcases;
using SiteFrames.Capture;
using SiteFrames.Frames;
using SiteFrames.Replay;
using SiteFrames.Vt;

namespace SiteFrames.Pages {
    /// <summary>
    /// Live dashboards: the TestimoX-style assessment replayed from its JSON-lines recording, and the build pipeline, the
    /// stress test and the CI narration recorded from real runs in a pseudo console.
    /// </summary>
    internal static class LivePage {
        private static string Examples(Site site) => Path.Combine(site.Repository, "ConsoleMonster.Examples", "bin", "Release", "net8.0", "ConsoleMonster.Examples.exe");

        private static string CastPath(Site site, string id) => Path.Combine(site.Captures, "live", id + ".cast");

        /// <summary>The run recordings: (id, title, command, width, height, environment).</summary>
        private static IEnumerable<(string Id, string Title, Func<Site, List<OutputChunk>> Run, int Width, int Height)> Recordings() {
            yield return ("pipeline-csharp", "ConsoleMonster.Examples pipeline", site => PseudoConsole.Run("\"" + Examples(site) + "\" pipeline", 110, 32, PowerShellRunner.Environment("dark"), site.Repository, TimeSpan.FromMinutes(2)), 110, 32);
            yield return ("pipeline-ps7", "Example-BuildPipeline.ps1", site => PowerShellRunner.Run(site, PowerShellHost.Core, Path.Combine(site.Repository, "Module", "Examples", "Example-BuildPipeline.ps1"), 110, 32, PowerShellRunner.Environment("dark")), 110, 32);
            yield return ("pipeline-ps5", "Example-BuildPipeline.ps1", site => PowerShellRunner.Run(site, PowerShellHost.Desktop, Path.Combine(site.Repository, "Module", "Examples", "Example-BuildPipeline.ps1"), 110, 32, PowerShellRunner.Environment("dark")), 110, 32);
            yield return ("stress", "ConsoleMonster.Examples stress 12", site => PseudoConsole.Run("\"" + Examples(site) + "\" stress 12", 120, 40, PowerShellRunner.Environment("dark"), site.Repository, TimeSpan.FromMinutes(2)), 120, 40);
            yield return ("testimox-stream", "ConsoleMonster.Examples testimox --speed 6", site => PseudoConsole.Run("\"" + Examples(site) + "\" testimox --speed 6", 100, 400, PowerShellRunner.Environment("dark", mode: "stream"), site.Repository, TimeSpan.FromMinutes(2)), 100, 400);
        }

        public static void Capture(Site site) {
            Build(site);
            foreach (var recording in Recordings()) {
                Stopwatch watch = Stopwatch.StartNew();
                List<OutputChunk> chunks = recording.Run(site);
                string path = CastPath(site, recording.Id);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, Replays.ToCast(chunks, recording.Width, recording.Height, recording.Title), new System.Text.UTF8Encoding(false));
                Console.WriteLine("recorded " + recording.Id + " in " + watch.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
            }
        }

        private static void Build(Site site) {
            ProcessStartInfo info = new ProcessStartInfo("dotnet", "build \"" + Path.Combine(site.Repository, "ConsoleMonster.Examples", "ConsoleMonster.Examples.csproj") + "\" -c Release -f net8.0 -nologo -v q") { UseShellExecute = false };
            using (Process process = Process.Start(info)!) {
                process.WaitForExit();
                if (process.ExitCode != 0) {
                    throw new InvalidOperationException("Building ConsoleMonster.Examples failed.");
                }
            }
        }

        public static void Render(Site site) {
            string replays = site.Static("replays");
            Site.Clean(replays);
            string downloads = site.Static("recordings");
            Site.Clean(downloads);
            JsonObject live = new JsonObject();

            // The assessment: synthetic events from the examples, written with LiveRecorder and read back with LiveRecordingReader.
            string jsonLines = Replays.Record(ParallelRunRecording.Generate());
            site.Write(Path.Combine(downloads, "testimox-run.jsonl"), jsonLines);
            List<LiveEvent> events = Replays.Read(jsonLines);
            live["testimox"] = Assessment(site, events, jsonLines, 120, 36, "testimox", 5, 2);
            live["testimoxCompact"] = Assessment(site, events, jsonLines, 64, 30, "testimox-compact", 3, 2.5);

            // Recorded runs.
            live["pipeline"] = new JsonObject {
                ["csharp"] = FromCast(site, "pipeline-csharp", "C#", "ConsoleMonster.Examples pipeline"),
                ["ps7"] = FromCast(site, "pipeline-ps7", "PowerShell 7", "Example-BuildPipeline.ps1"),
                ["ps5"] = FromCast(site, "pipeline-ps5", "Windows PowerShell 5.1", "Example-BuildPipeline.ps1")
            };
            JsonObject stress = FromCast(site, "stress", "C#", "ConsoleMonster.Examples stress 12", 3, 0.45);
            stress["result"] = StressResult(site);
            live["stress"] = stress;
            live["stream"] = Stream(site);
            live["query"] = Figures.Figure(site, "ConsoleMonster.Examples query", "DnsClientX and DomainDetective style query results printed once.", QueryResults.Build(), 100, rebuild: QueryResults.Build);

            live["testimoxSource"] = SourceSnippets.Method(Path.Combine(site.Repository, "ConsoleMonster.Examples", "Showcases", "ParallelRun.cs"), "public static Dashboard Build(out Lanes lanes)");
            live["pipelineScript"] = Site.ReadText(Path.Combine(site.Repository, "Module", "Examples", "Example-BuildPipeline.ps1"));
            site.WriteJson(site.Data("live"), live);
        }

        private static JsonObject Assessment(Site site, List<LiveEvent> events, string jsonLines, int width, int height, string id, double fps, double speed) {
            // The replay is the engine's own player data (Recording.ExportPlayerData), played by the engine's player.
            string data = ConsoleMonster.Export.Recording.ExportPlayerData(ParallelRun.Build(out _), events, new ConsoleMonster.Export.RecordingExportOptions {
                Width = width,
                Height = height,
                // FramesPerSecond counts recorded time; the site shows fps frames per second of the sped-up replay.
                FramesPerSecond = fps / speed,
                Speed = speed,
                Theme = ConsoleMonster.Export.ExportTheme.Dark,
                Glyphs = GlyphTier.Full,
                Culture = Site.Culture,
                HoldLastFrame = TimeSpan.FromSeconds(4),
                Title = "ConsoleMonster.Examples testimox"
            });
            site.Write(site.Static("replays/" + id + ".json"), data);
            (long duration, int count) = Summary(data);
            // The poster (and where the replay starts) is the run in full swing, not its empty first second or its end. The
            // site samples the same events at the same rate to pick that moment and draw it for readers without JavaScript.
            List<TimedFrame> frames = Replays.FromEvents(ParallelRun.Build(out _), events, width, height, fps, Theme.Dark, speed);
            long posterTime = Poster(site, frames, 0.55, out string poster);
            string[] lines = jsonLines.Split('\n');
            JsonArray excerpt = new JsonArray();
            for (int i = 0; i < Math.Min(9, lines.Length); i++) {
                excerpt.Add(lines[i]);
            }
            return new JsonObject {
                ["url"] = "/replays/" + id + ".json",
                ["w"] = width,
                ["h"] = height,
                ["frames"] = count,
                ["duration"] = duration,
                ["events"] = events.Count,
                ["fps"] = fps,
                ["speed"] = speed,
                ["start"] = posterTime,
                ["bytes"] = jsonLines.Length,
                ["excerpt"] = string.Join("\n", ToStrings(excerpt)),
                ["poster"] = new JsonObject { ["w"] = width, ["h"] = height, ["dark"] = poster, ["start"] = posterTime }
            };
        }

        // The length and frame count of player data, for the page's labels.
        private static (long Duration, int Count) Summary(string data) {
            using (System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(data)) {
                return (doc.RootElement.GetProperty("duration").GetInt64(), doc.RootElement.GetProperty("frames").GetArrayLength());
            }
        }


        private static IEnumerable<string> ToStrings(JsonArray array) {
            foreach (JsonNode? node in array) {
                yield return node!.GetValue<string>();
            }
        }

        /// <summary>The frame shown before a replay plays: the last frame at a share of the run, and its time.</summary>
        internal static long Poster(Site site, List<TimedFrame> frames, double share, out string html) {
            long at = (long)Math.Round(frames[frames.Count - 1].Time * share);
            TimedFrame moment = frames.FindLast(f => f.Time <= at);
            html = Frames.FrameHtml.Body(Frames.FrameHtml.Rows(moment.Frame, ColorDepth.TrueColor, Screen.Dark, site.Styles), false);
            return moment.Time;
        }

        private static JsonObject FromCast(Site site, string id, string label, string command, double fps = 6, double share = 0.55) {
            string path = CastPath(site, id);
            if (!File.Exists(path)) {
                WidgetsPage.Report(new List<string> { "missing capture " + id + "; run the capture command." }, "live");
                return new JsonObject();
            }
            List<TimedFrame> frames = Replays.FromCast(File.ReadAllText(path), fps, true, out CellBuffer scrollback, out int width, out int height);
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
            string path = CastPath(site, "testimox-stream");
            if (!File.Exists(path)) {
                return new JsonObject();
            }
            Replays.FromCast(File.ReadAllText(path), 8, false, out CellBuffer scrollback, out int width, out _);
            return new JsonObject {
                ["title"] = "CONSOLEMONSTER_MODE=stream",
                ["frame"] = site.Frame(Site.Crop(scrollback), null),
                ["meta"] = new JsonArray(width + " columns", "stream mode")
            };
        }
    }
}
