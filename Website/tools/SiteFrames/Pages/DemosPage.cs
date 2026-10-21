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

            /// <summary>
            /// The PowerShell script the demo records (on PowerShell 7) and shows: a file under Website/examples/demos, or a path
            /// relative to the repository (a published example script). Null for program demos.
            /// </summary>
            public string? Script { get; set; }

            /// <summary>Arguments the recording passes to <see cref="Script"/>: only the script's own public parameters.</summary>
            public string? Arguments { get; set; }

            /// <summary>A command line the recording shows typed before the script runs; it is never part of the shown code.</summary>
            public string? Prompt { get; set; }

            /// <summary>Files the script reads, shown under it so a reader can save them beside it (relative to its folder).</summary>
            public string[] Companions { get; set; } = Array.Empty<string>();

            /// <summary>Whether the recording plays the script as typed commands, each after a prompt (see PowerShellRunner).</summary>
            public bool Session { get; set; }

            /// <summary>PowerShell the recording runs before the script (for example the folder to start in); never shown.</summary>
            public string? Setup { get; set; }

            /// <summary>
            /// The PowerShell twin of a recorded program: a script relative to the repository, shown short (its <c>#region more</c>
            /// and <c>#region hidden</c> parts folded) with the whole file as the full program. Null for script demos.
            /// </summary>
            public string? PowerShellTwin { get; set; }


            /// <summary>A program to run instead of a script: (executable relative to the repository, arguments).</summary>
            public (string Executable, string Arguments)? Program { get; set; }

            /// <summary>
            /// The C# source shown beside the recording: the file relative to the repository, and either the method whose
            /// <c>// &lt;example&gt;</c> lines are the snippet (Example) or the signature of a method shown whole (Signature).
            /// The whole file and any <see cref="CSharpCompanions"/> form the full program.
            /// </summary>
            public (string File, string? Example, string? Signature)? CSharp { get; set; }

            /// <summary>
            /// The C# twin of a PowerShell demo as a complete program under Website/examples/demos (a Program.cs). It is
            /// compiled on every render exactly as a reader's project would compile it, so the C# tab is always runnable.
            /// </summary>
            public string? CSharpProgram { get; set; }

            /// <summary>
            /// Arguments for recording <see cref="CSharpProgram"/> itself (with no <see cref="Script"/>): the program and its
            /// companions are built as a reader's project would build them and run with <c>dotnet</c>.
            /// </summary>
            public string? ProgramArguments { get; set; }

            /// <summary>Additional source files needed by the full C# program, relative to the repository.</summary>
            public string[] CSharpCompanions { get; set; } = Array.Empty<string>();

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
                CSharpProgram = "fleet.cs",
                CSharpCompanions = new[] { Path.Combine("ConsoleMonster.Examples", "Showcases", "FleetHealth.cs"), Path.Combine("ConsoleMonster.Examples", "Showcases", "FleetData.cs") },
                ProgramArguments = "--demo",
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
            yield return new Demo("pester", "Invoke-ConsolePester", 96, 24) { Script = "pester.ps1", Companions = new[] { "fixtures/MailDomain.Tests.ps1" }, PosterShare = 0.75, AllowErrors = true };
            yield return new Demo("formatting", "Enable-ConsoleFormatting", 100, 40) {
                Script = "formatting.ps1",
                Session = true,
                Setup = "Set-Location -LiteralPath (Join-Path $PSHOME 'Modules')",
                PosterShare = 1,
                Fps = 5,
                Chapters = new[] { ("Before", "# PowerShell's own output"), ("After", "# The same commands now") }
            };
            yield return new Demo("shaping", "Format-ConsoleTable -GroupBy Site", 100, 40) { Script = "shaping.ps1", Static = true };
            yield return new Demo("help", "Show-ConsoleHelp", 100, 50) { Script = "help.ps1", Session = true, PosterShare = 1 };
            yield return new Demo("progress", "Write-Progress, drawn by ConsoleMonster", 96, 12) { Script = "progress.ps1", PosterShare = 0.5, Fps = 8, ShowProgress = true };
            yield return new Demo("errors", "Format-ConsoleError", 100, 20) { Script = "errors.ps1", Static = true, AllowErrors = true };
            yield return new Demo("session", "session.ps1", 100, 14) { Script = "session.ps1", CSharpProgram = "session.cs", PosterShare = 0.6, Fps = 6 };
            yield return new Demo("record", "record.ps1", 100, 18) { Script = "record.ps1", CSharpProgram = "record.cs", PosterShare = 0.4, Fps = 6 };
            yield return new Demo("login", "ssh deploy@app-web-03", 100, 30) {
                Script = Path.Combine("Module", "Examples", "Example-LoginOverview.ps1"),
                Arguments = "-Sample -Width 96",
                Prompt = "ssh deploy@app-web-03",
                PosterShare = 1
            };
            yield return new Demo("hosting", "dotnet run (ConsoleMonster.Hosting)", 110, 30) {
                Program = (Path.Combine("ConsoleMonster.Examples.Extensions", "bin", "Release", "net8.0", "ConsoleMonster.Examples.Extensions.exe"), "hosting 12"),
                CSharp = (Path.Combine("ConsoleMonster.Examples.Extensions", "Program.cs"), null, "private static async Task HostingAsync(int seconds)"),
                CSharpCompanions = new[] { Path.Combine("ConsoleMonster.Examples.Extensions", "CommandLineExample.cs") },
                PosterShare = 0.75
            };
            yield return new Demo("logging", "dotnet run (ConsoleMonster.Extensions.Logging)", 110, 30) {
                Program = (Path.Combine("ConsoleMonster.Examples.Extensions", "bin", "Release", "net8.0", "ConsoleMonster.Examples.Extensions.exe"), "logging"),
                CSharp = (Path.Combine("ConsoleMonster.Examples.Extensions", "Program.cs"), null, "private static void Logging()"),
                CSharpCompanions = new[] { Path.Combine("ConsoleMonster.Examples.Extensions", "CommandLineExample.cs") },
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

                // Everything a demo's recording is made from, as text, or null for demos recorded from a prebuilt program.
        private static string? Source(Site site, Demo demo) {
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            if (demo.Script != null) {
                text.Append(Site.ReadText(Script(site, demo.Script))).Append('\n');
                foreach (string companion in demo.Companions) {
                    text.Append(Site.ReadText(Path.Combine(Path.GetDirectoryName(Script(site, demo.Script))!, companion))).Append('\n');
                }
                text.Append(demo.Arguments).Append('\n').Append(demo.Prompt).Append('\n').Append(demo.Session).Append('\n').Append(demo.Setup);
            } else if (demo.ProgramArguments != null) {
                text.Append(Site.ReadText(Script(site, demo.CSharpProgram!))).Append('\n');
                foreach (string companion in Companions(site, demo)) {
                    text.Append(Site.ReadText(companion)).Append('\n');
                }
                text.Append(demo.ProgramArguments);
            } else {
                return null;
            }
            return text.Append('\n').Append(demo.Width).Append('x').Append(demo.Height).ToString();
        }

        private static string[] Companions(Site site, Demo demo) => Array.ConvertAll(demo.CSharpCompanions, c => Path.Combine(site.Repository, c));

        private static string Script(Site site, string name) => name.Contains(Path.DirectorySeparatorChar) || name.Contains('/') ? Path.Combine(site.Repository, name) : Path.Combine(site.Website, "examples", "demos", name);

        // The path a code bar names and the site links to on GitHub: relative to the repository, with forward slashes.
        private static string ScriptFile(Site site, string name) => Path.GetRelativePath(site.Repository, Script(site, name)).Replace('\\', '/');

        private static string CastPath(Site site, string id) => Path.Combine(site.Captures, "demos", id + ".cast");

        public static void Capture(Site site) {
            BuildExamples(site);
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
                    // A fresh temporary folder per run: a script may write there (the record demo does), and nothing is left.
                    string temp = Path.Combine(Path.GetTempPath(), "cmsite-demo-temp-" + Guid.NewGuid().ToString("N").Substring(0, 12));
                    Directory.CreateDirectory(temp);
                    Dictionary<string, string?> environment = PowerShellRunner.Environment("dark");
                    environment["TEMP"] = temp;
                    environment["TMP"] = temp;
                    try {
                        chunks = PowerShellRunner.Run(site, PowerShellHost.Core, Script(site, demo.Script), demo.Width, demo.Height, environment, allowErrors: demo.AllowErrors, showProgress: demo.ShowProgress, arguments: demo.Arguments, prompt: demo.Prompt, session: demo.Session, setup: demo.Setup);
                    } finally {
                        Directory.Delete(temp, true);
                    }
                } else if (demo.ProgramArguments != null) {
                    string work = Path.Combine(Path.GetTempPath(), "cmsite-demo-" + demo.Id + "-" + Guid.NewGuid().ToString("N").Substring(0, 12));
                    if (Directory.Exists(work)) {
                        Directory.Delete(work, true);
                    }
                    string dll = CSharpRunner.Build(Script(site, demo.CSharpProgram!), work, demo.Id, Companions(site, demo));
                    try {
                        chunks = PseudoConsole.Run("\"" + LivePage.DotnetHost + "\" \"" + dll + "\" " + demo.ProgramArguments, demo.Width, demo.Height, PowerShellRunner.Environment("dark"), work, TimeSpan.FromMinutes(3));
                    } finally {
                        Directory.Delete(work, true);
                    }
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
                string? source = Source(site, demo);
                File.WriteAllText(path, source != null ? Replays.Stamp(cast, Replays.Fingerprint(source)) : cast, new System.Text.UTF8Encoding(false));
                Console.WriteLine("recorded demo " + demo.Id + " in " + watch.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
            }
        }

        /// <summary>Builds ConsoleMonster.Examples (the fleet app the Monster demo records, and the profile command).</summary>
        internal static void BuildExamples(Site site) => Build(site, Path.Combine("ConsoleMonster.Examples", "ConsoleMonster.Examples.csproj"));

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
                string? expected = Source(site, demo);
                if (expected != null && Replays.Source(cast) != Replays.Fingerprint(expected)) {
                    problems.Add("demo " + demo.Id + ": the recording was made from another version of its script or program; run the capture command for the demos area (SITEFRAMES_DEMOS=" + demo.Id + ").");
                }
                List<TimedFrame> frames = TrimRows(Replays.FromCast(cast, demo.Fps, false, out CellBuffer scrollback, out int width, out int height));
                if (!demo.Static && frames.Count == 0) {
                    problems.Add("demo " + demo.Id + ": the capture has no replay frames; record a non-empty run.");
                }
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
                    (string shortForm, string full) = SourceSnippets.Program(Site.ReadText(Script(site, demo.Script)));
                    entry["powershell"] = shortForm;
                    entry["powershellRepo"] = ScriptFile(site, demo.Script);
                    entry["powershellFull"] = WidgetsPage.Longer((shortForm, full));
                    if (demo.Companions.Length > 0) {
                        JsonArray companions = new JsonArray();
                        foreach (string companion in demo.Companions) {
                            string file = Path.Combine(Path.GetDirectoryName(Script(site, demo.Script))!, companion.Replace('/', Path.DirectorySeparatorChar));
                            companions.Add(new JsonObject { ["file"] = companion, ["code"] = Site.ReadText(file) });
                        }
                        entry["companions"] = companions;
                    }
                    // A published example script is also served from the site under /examples/, so the "Use it" steps download it from
                    // here (copy.json demos.<id>.use).
                    if (demo.Script.Contains(Path.DirectorySeparatorChar) || demo.Script.Contains('/')) {
                        string name = Path.GetFileName(demo.Script);
                        // Byte for byte, so the file keeps its UTF-8 byte order mark (Windows PowerShell 5.1 needs it).
                        string target = site.Static("examples/" + name);
                        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                        File.Copy(Script(site, demo.Script), target, true);
                        site.Written.Add(Path.GetRelativePath(site.Website, target).Replace('\\', '/'));
                    }
                }
                if (demo.Script == null && demo.PowerShellTwin != null) {
                    (string shortForm, string full) = SourceSnippets.Program(Site.ReadText(Path.Combine(site.Repository, demo.PowerShellTwin)));
                    entry["powershell"] = shortForm;
                    entry["powershellRepo"] = demo.PowerShellTwin.Replace('\\', '/');
                    entry["powershellFull"] = full.TrimEnd('\n');
                }
                if (demo.CSharpProgram != null) {
                    string program = Script(site, demo.CSharpProgram);
                    if (demo.CSharpCompanions.Length == 0) {
                        CSharpRunner.Compile(program);
                    } else {
                        string check = Path.Combine(Path.GetTempPath(), "cmsite-check-" + demo.Id + "-" + Guid.NewGuid().ToString("N").Substring(0, 12));
                        try {
                            CSharpRunner.Build(program, check, demo.Id, Companions(site, demo));
                        } finally {
                            Directory.Delete(check, true);
                        }
                    }
                    (string shortForm, string full) = SourceSnippets.Program(Site.ReadText(program));
                    entry["csharp"] = shortForm;
                    entry["csharpFull"] = full;
                    entry["csharpRepo"] = ScriptFile(site, demo.CSharpProgram);
                    entry["csharpFile"] = "Program.cs";
                    // The other files of a multi-file program, each shown whole with its name.
                    JsonArray files = new JsonArray();
                    foreach (string companion in Companions(site, demo)) {
                        files.Add(new JsonObject { ["file"] = Path.GetFileName(companion), ["code"] = Site.ReadText(companion), ["codeRepo"] = Path.GetRelativePath(site.Repository, companion).Replace('\\', '/') });
                    }
                    if (files.Count > 0) {
                        entry["csharpFiles"] = files;
                    }
                }
                if (demo.CSharp != null) {
                    (string file, string? example, string? signature) = demo.CSharp.Value;
                    string source = Path.Combine(site.Repository, file);
                    entry["csharp"] = SourceSnippets.Split(example != null ? SourceSnippets.Get(source, example) : SourceSnippets.Method(source, signature!)).Short;
                    string fullProgram = Site.ReadText(source);
                    foreach (string companion in demo.CSharpCompanions) {
                        fullProgram += "\n\n" + Site.ReadText(Path.Combine(site.Repository, companion));
                    }
                    entry["csharpFull"] = SourceSnippets.Split(fullProgram).Full.TrimEnd('\n');
                    entry["csharpRepo"] = file.Replace('\\', '/');
                }
                demos[demo.Id] = entry;
            }
            site.WriteJson(site.Data("demos"), demos);
            WidgetsPage.Report(problems, "demos");
        }
    }
}
