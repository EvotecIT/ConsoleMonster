using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using SiteFrames.Capture;
using SiteFrames.Replay;
using SiteFrames.Vt;

namespace SiteFrames.Pages {
    /// <summary>
    /// The demos: one recording per feature, played by the engine's own player next to the exact source that recorded it.
    /// <c>capture</c> runs each demo script or program once in a pseudo console of a fixed size and saves the VT stream
    /// (asciicast) under <c>captures/demos/</c>; <c>render</c> turns the committed streams into player data, a poster frame
    /// and <c>data/demos.json</c>, reading the sources from the same files the capture ran, so the code shown on the site is
    /// the code that drew the recording. Every demo uses invented or generic data (svchost, the PowerShell install folder, W* services), fixed sizes and a pinned terminal profile.
    /// </summary>
    internal static class DemosPage {
        private sealed class Demo {
            public Demo(string id, string title, int width, int height) {
                Id = id;
                Title = title;
                Width = width;
                Height = height;
            }

            public string Id { get; }

            /// <summary>The window title: the command a reader would type, or an app's own name.</summary>
            public string Title { get; }

            public int Width { get; }

            public int Height { get; }

            /// <summary>A PowerShell script under Website/examples/demos (recorded on PowerShell 7), or null.</summary>
            public string? Script { get; set; }

            /// <summary>
            /// The PowerShell twin of a recorded program: a script relative to the repository, shown short (its <c>#region more</c>
            /// and <c>#region hidden</c> parts folded) with the whole file as the full program. Null for script demos.
            /// </summary>
            public string? PowerShellTwin { get; set; }

            /// <summary>A script shown as the full program instead of the demo script (a wrapper that calls it), or null.</summary>
            public string? FullScript { get; set; }

            /// <summary>A program to run instead of a script: (executable relative to the repository, arguments).</summary>
            public (string Executable, string Arguments)? Program { get; set; }

            /// <summary>
            /// The C# source shown beside the recording: the file relative to the repository, and either the method whose
            /// <c>// &lt;example&gt;</c> lines are the snippet (Example) or the signature of a method shown whole (Signature).
            /// The whole file is the full program.
            /// </summary>
            public (string File, string? Example, string? Signature)? CSharp { get; set; }

            /// <summary>The share of the run the poster shows (default 0.6).</summary>
            public double PosterShare { get; set; } = 0.6;

            /// <summary>Frames per second sampled from the stream (default 6).</summary>
            public double Fps { get; set; } = 6;

            /// <summary>Whether progress records are kept (they are hidden for the other captures).</summary>
            public bool ShowProgress { get; set; }

            /// <summary>Whether the demo shows errors on purpose (the run goes on and ends normally).</summary>
            public bool AllowErrors { get; set; }

            /// <summary>Chapter marks: a label and the text on screen that starts its chapter (the first frame showing it).</summary>
            public (string Label, string Text)[]? Chapters { get; set; }

            /// <summary>A static figure: the transcript after the run instead of a replay.</summary>
            public bool Static { get; set; }

            /// <summary>
            /// Text on the screen the replay ends on, or null for the whole run. Frames after the last one showing it (an app's
            /// later screens and its exit, which leaves an empty console) are left out, so the replay stops on that screen and
            /// the player holds it. The poster and the player's opening position are that last frame: a reader sees the result,
            /// and Play runs the replay from the beginning.
            /// </summary>
            public string? EndsOn { get; set; }
        }

        private static IEnumerable<Demo> All() {
            // The home page's Monster hero: a whole application (a Shell menu, a form, a live dashboard and the results),
            // recorded from the Release build driving itself with scripted keys on a fixed clock and seed. The demo goes on to
            // the history and quits, which clears the console; the replay ends on the results instead.
            yield return new Demo("app", "Fleet health check", 140, 40) {
                Program = (Path.Combine("ConsoleMonster.Examples", "bin", "Release", "net8.0", "ConsoleMonster.Examples.exe"), "fleet --demo"),
                CSharp = (Path.Combine("ConsoleMonster.Examples", "Showcases", "FleetHealth.cs"), "Build", null),
                PowerShellTwin = Path.Combine("Module", "Examples", "Example-FleetHealth.ps1"),
                Fps = 8,
                Chapters = new[] { ("Menu", "Fleet health check"), ("Form", "New check › Scope"), ("Dashboard", "Check map"), ("Results", "Fleet check finished") },
                EndsOn = "Fleet check finished"
            };
            yield return new Demo("parallel", "ForEach-ConsoleItem -Parallel", 100, 24) {
                Script = "parallel.ps1",
                AllowErrors = true,
                CSharp = (Path.Combine("ConsoleMonster.Examples", "Showcases", "ParallelPatch.cs"), "RunAsync", null),
                PosterShare = 0.45
            };
            yield return new Demo("watch", "Watch-Console -Differences", 96, 14) { Script = "watch.ps1", PosterShare = 0.7 };
            yield return new Demo("pester", "Invoke-ConsolePester", 96, 24) { Script = "pester.ps1", PosterShare = 0.75, AllowErrors = true };
            yield return new Demo("formatting", "Enable-ConsoleFormatting", 100, 40) {
                Script = "formatting.ps1",
                PosterShare = 1,
                Fps = 5,
                Chapters = new[] { ("Before", "# PowerShell's own output"), ("After", "# The same commands now") }
            };
            yield return new Demo("shaping", "Format-ConsoleTable -GroupBy Site", 100, 40) { Script = "shaping.ps1", Static = true };
            yield return new Demo("help", "Show-ConsoleHelp", 100, 50) { Script = "help.ps1", PosterShare = 1 };
            yield return new Demo("progress", "Write-Progress, drawn by ConsoleMonster", 96, 12) { Script = "progress.ps1", PosterShare = 0.5, Fps = 8, ShowProgress = true };
            yield return new Demo("errors", "Format-ConsoleError", 100, 20) { Script = "errors.ps1", Static = true, AllowErrors = true };
            yield return new Demo("login", "ssh deploy@app-web-03", 100, 30) {
                Script = "login.ps1",
                FullScript = Path.Combine("Module", "Examples", "Example-LoginOverview.ps1"),
                PosterShare = 1
            };
            yield return new Demo("hosting", "dotnet run (ConsoleMonster.Hosting)", 110, 30) {
                Program = (Path.Combine("ConsoleMonster.Examples.Extensions", "bin", "Release", "net8.0", "ConsoleMonster.Examples.Extensions.exe"), "hosting 12"),
                CSharp = (Path.Combine("ConsoleMonster.Examples.Extensions", "Program.cs"), null, "private static async Task HostingAsync(int seconds)"),
                PosterShare = 0.75
            };
            yield return new Demo("logging", "dotnet run (ConsoleMonster.Extensions.Logging)", 110, 30) {
                Program = (Path.Combine("ConsoleMonster.Examples.Extensions", "bin", "Release", "net8.0", "ConsoleMonster.Examples.Extensions.exe"), "logging"),
                CSharp = (Path.Combine("ConsoleMonster.Examples.Extensions", "Program.cs"), null, "private static void Logging()"),
                Static = true
            };
            yield return new Demo("template", "dotnet run, then dotnet run -- serve", 100, 26) {
                Program = (Path.Combine("Website", "tools", "TemplateDemo", "bin", "Release", "net8.0", "TemplateDemo.exe"), string.Empty),
                CSharp = (Path.Combine("Templates", "ConsoleMonster.Templates", "content", "ConsoleMonsterApp", "Program.cs"), null, "public static async Task<int> Main(string[] args)"),
                PosterShare = 0.3
            };
        }

        // Inline views draw at the top of the pseudo console and leave the rows below empty: every frame is cut to the
        // lowest row any frame uses, so the player is as tall as the run, not the console.
        private static List<TimedFrame> TrimRows(List<TimedFrame> frames) {
            int used = 1;
            foreach (TimedFrame frame in frames) {
                for (int y = frame.Frame.Height - 1; y >= used; y--) {
                    if (frame.Frame.GetRowText(y).Trim().Length > 0) {
                        used = y + 1;
                        break;
                    }
                }
            }
            List<TimedFrame> trimmed = new List<TimedFrame>(frames.Count);
            foreach (TimedFrame frame in frames) {
                if (frame.Frame.Height <= used) {
                    trimmed.Add(frame);
                    continue;
                }
                CellBuffer cut = new CellBuffer(frame.Frame.Width, used);
                cut.Blit(frame.Frame, 0, 0, frame.Frame.Width, used, 0, 0);
                trimmed.Add(new TimedFrame(frame.Time, cut));
            }
            return trimmed;
        }

        private static bool Shows(CellBuffer frame, string text) {
            for (int y = 0; y < frame.Height; y++) {
                if (frame.GetRowText(y).Contains(text, StringComparison.Ordinal)) {
                    return true;
                }
            }
            return false;
        }

        private static bool Blank(CellBuffer frame) {
            for (int y = 0; y < frame.Height; y++) {
                if (frame.GetRowText(y).Trim().Length > 0) {
                    return false;
                }
            }
            return true;
        }

        private static string Script(Site site, string name) => Path.Combine(site.Website, "examples", "demos", name);

        private static string CastPath(Site site, string id) => Path.Combine(site.Captures, "demos", id + ".cast");

        public static void Capture(Site site) {
            LivePage.Build(site);
            Build(site, Path.Combine("ConsoleMonster.Examples.Extensions", "ConsoleMonster.Examples.Extensions.csproj"));
            Build(site, Path.Combine("Website", "tools", "TemplateDemo", "TemplateDemo.csproj"));
            // SITEFRAMES_DEMOS (comma-separated ids) records only those demos, leaving the other recordings as they are.
            HashSet<string>? wanted = null;
            string? filter = Environment.GetEnvironmentVariable("SITEFRAMES_DEMOS");
            if (!string.IsNullOrWhiteSpace(filter)) {
                wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string id in filter!.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                    wanted.Add(id.Trim());
                }
            }
            foreach (Demo demo in All()) {
                if (wanted != null && !wanted.Contains(demo.Id)) {
                    continue;
                }
                Stopwatch watch = Stopwatch.StartNew();
                List<OutputChunk> chunks;
                if (demo.Script != null) {
                    chunks = PowerShellRunner.Run(site, PowerShellHost.Core, Script(site, demo.Script), demo.Width, demo.Height, PowerShellRunner.Environment("dark"), allowErrors: demo.AllowErrors, showProgress: demo.ShowProgress);
                } else {
                    (string executable, string arguments) = demo.Program!.Value;
                    string command = "\"" + Path.Combine(site.Repository, executable) + "\"" + (arguments.Length > 0 ? " " + arguments : string.Empty);
                    // The drive root as the working directory: a host logs its content root, which should not be this checkout.
                    chunks = PseudoConsole.Run(command, demo.Width, demo.Height, PowerShellRunner.Environment("dark"), Path.GetPathRoot(site.Repository) ?? site.Repository, TimeSpan.FromMinutes(2));
                }
                string path = CastPath(site, demo.Id);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                // Window titles (OSC 0 and 2) carry the executable's path; nothing on the site shows them.
                string cast = System.Text.RegularExpressions.Regex.Replace(Replays.ToCast(chunks, demo.Width, demo.Height, demo.Title), @"\\u001B\][02];(?:[^""\\]|\\\\)*?(?:\\u0007|\\u001B\\\\)", string.Empty);
                File.WriteAllText(path, cast, new System.Text.UTF8Encoding(false));
                Console.WriteLine("recorded demo " + demo.Id + " in " + watch.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
            }
        }

        private static void Build(Site site, string project) {
            // ContinuousIntegrationBuild maps source paths to /_/, so stack traces in the recordings do not show this checkout.
            ProcessStartInfo info = new ProcessStartInfo("dotnet", "build \"" + Path.Combine(site.Repository, project) + "\" -c Release -nologo -v q -p:ContinuousIntegrationBuild=true") { UseShellExecute = false };
            using (Process process = Process.Start(info)!) {
                process.WaitForExit();
                if (process.ExitCode != 0) {
                    throw new InvalidOperationException("Building " + project + " failed.");
                }
            }
        }

        public static void Render(Site site) {
            List<string> problems = new List<string>();
            JsonObject demos = new JsonObject();
            foreach (Demo demo in All()) {
                string path = CastPath(site, demo.Id);
                if (!File.Exists(path)) {
                    problems.Add("missing capture demos/" + demo.Id + "; run the capture command for the demos area.");
                    continue;
                }
                string cast = File.ReadAllText(path);
                List<TimedFrame> frames = TrimRows(Replays.FromCast(cast, demo.Fps, false, out CellBuffer scrollback, out int width, out int height));
                if (demo.EndsOn != null) {
                    int last = frames.FindLastIndex(f => Shows(f.Frame, demo.EndsOn));
                    if (last < 0) {
                        problems.Add("demo " + demo.Id + ": no frame shows the closing text \"" + demo.EndsOn + "\".");
                    } else {
                        frames.RemoveRange(last + 1, frames.Count - last - 1);
                    }
                }
                if (frames.Count > 0) {
                    height = frames[0].Frame.Height;
                    // A replay that ends on an empty console stops on nothing: a reader who lets it play sees a blank window.
                    if (!demo.Static && Blank(frames[frames.Count - 1].Frame) && frames.Exists(f => !Blank(f.Frame))) {
                        problems.Add("demo " + demo.Id + ": the replay ends on an empty screen; set EndsOn to the screen it should stop on.");
                    }
                }
                JsonObject entry = new JsonObject {
                    ["id"] = demo.Id,
                    ["title"] = demo.Title,
                    ["w"] = width,
                    ["h"] = height
                };
                if (demo.Static || frames.Count == 0) {
                    CellBuffer still = Site.Crop(scrollback);
                    DocCards.Offer(demo.Id, still, Frames.Screen.Dark);
                    entry["frame"] = site.Frame(still, null);
                } else {
                    List<(long Time, string Label)>? markers = null;
                    if (demo.Chapters != null) {
                        markers = new List<(long Time, string Label)>();
                        foreach ((string label, string text) in demo.Chapters) {
                            int at = frames.FindIndex(f => Shows(f.Frame, text));
                            if (at < 0) {
                                problems.Add("demo " + demo.Id + ": no frame shows the chapter text \"" + text + "\".");
                                continue;
                            }
                            // The first chapter starts the recording.
                            markers.Add((markers.Count == 0 ? 0 : frames[at].Time, label));
                        }
                    }
                    string data = PlayerData.Write(site, frames, demo.Title, demo.Fps, 3000, out long duration, out int count, markers);
                    site.Write(site.Static("replays/demo-" + demo.Id + ".json"), data);
                    long posterTime = LivePage.Poster(site, frames, demo.EndsOn != null ? 1 : demo.PosterShare, out string poster, out CellBuffer posterFrame);
                    DocCards.Offer(demo.Id, posterFrame, Frames.Screen.Dark);
                    // A replay that ends on its result opens there, at its end, so the player shows that screen and Play
                    // starts again from the beginning.
                    if (demo.EndsOn != null) {
                        posterTime = duration;
                    }
                    entry["url"] = "/replays/demo-" + demo.Id + ".json";
                    entry["frames"] = count;
                    entry["duration"] = duration;
                    entry["fps"] = demo.Fps;
                    entry["start"] = posterTime;
                    entry["bytes"] = System.Text.Encoding.UTF8.GetByteCount(data);
                    entry["poster"] = new JsonObject { ["w"] = width, ["h"] = height, ["dark"] = poster };
                }
                if (demo.Script != null) {
                    (string shortForm, string full) = SourceSnippets.Split(Site.ReadText(Script(site, demo.Script)));
                    entry["powershell"] = shortForm;
                    entry["powershellFile"] = "Website/examples/demos/" + demo.Script;
                    if (demo.FullScript != null) {
                        entry["powershellFull"] = Site.ReadText(Path.Combine(site.Repository, demo.FullScript));
                        entry["powershellFullFile"] = demo.FullScript.Replace('\\', '/');
                    } else {
                        entry["powershellFull"] = WidgetsPage.Longer((shortForm, full));
                    }
                }
                if (demo.Script == null && demo.PowerShellTwin != null) {
                    (string shortForm, string full) = SourceSnippets.Split(Site.ReadText(Path.Combine(site.Repository, demo.PowerShellTwin)));
                    entry["powershell"] = shortForm;
                    entry["powershellFile"] = demo.PowerShellTwin.Replace('\\', '/');
                    entry["powershellFull"] = full.TrimEnd('\n');
                }
                if (demo.CSharp != null) {
                    (string file, string? example, string? signature) = demo.CSharp.Value;
                    string source = Path.Combine(site.Repository, file);
                    entry["csharp"] = SourceSnippets.Split(example != null ? SourceSnippets.Get(source, example) : SourceSnippets.Method(source, signature!)).Short;
                    entry["csharpFull"] = SourceSnippets.Split(Site.ReadText(source)).Full.TrimEnd('\n');
                    entry["csharpFile"] = file.Replace('\\', '/');
                }
                demos[demo.Id] = entry;
            }
            site.WriteJson(site.Data("demos"), demos);
            WidgetsPage.Report(problems, "demos");
        }
    }
}
