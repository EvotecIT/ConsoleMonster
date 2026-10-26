using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
    /// the code that drew the recording. The ordinary demos use explicit invented data, fixed sizes and a pinned terminal profile.
    /// </summary>
    internal static partial class DemosPage {
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


            /// <summary>
            /// A project to build (Release) and record instead of a script, relative to the repository: its program runs with
            /// <c>dotnet</c> and <see cref="ProjectArguments"/>. Every file in its folder and <see cref="Sources"/> make the recording.
            /// </summary>
            public string? Project { get; set; }

            /// <summary>Arguments the recording passes to <see cref="Project"/>'s program.</summary>
            public string ProjectArguments { get; set; } = string.Empty;

            /// <summary>Files outside the project's folder that it compiles (relative to the repository).</summary>
            public string[] Sources { get; set; } = Array.Empty<string>();

            /// <summary>When the recording presses Ctrl+C, for a program that runs until it is stopped.</summary>
            public TimeSpan? Interrupt { get; set; }
            public string? InputReadyText { get; set; }
            public string? InputKeys { get; set; }
            public TimeSpan? InputDelay { get; set; }

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
            // recorded from fleet.cs and its two files, built as a reader's project and run with --demo, which drives it with
            // scripted keys on a fixed clock and seed. The demo goes on to
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
            yield return new Demo("parallel", "Invoke-ConsoleRun -Parallel", 100, 24) {
                Script = "parallel.ps1",
                AllowErrors = true,
                CSharpProgram = "parallel.cs",
                PosterShare = 0.45
            };
            yield return new Demo("watch", "Watch-Console -Differences", 96, 14) { CSharpProgram = "watch.cs", Script = "watch.ps1", PosterShare = 0.7 };
            yield return new Demo("pester", "Invoke-ConsolePester", 96, 24) { CSharpProgram = "pester.host.cs", Script = "pester.ps1", Companions = new[] { "fixtures/MailDomain.Tests.ps1" }, PosterShare = 0.75, AllowErrors = true };
            yield return new Demo("formatting", "Enable-ConsoleFormatting", 100, 40) { CSharpProgram = "formatting.host.cs",
                Script = "formatting.ps1",
                Session = true,

                PosterShare = 1,
                Fps = 5,
                Chapters = new[] { ("Before", "# PowerShell's own output"), ("After", "# The same objects now") }
            };
            yield return new Demo("shaping", "Show-ConsoleTable -GroupBy Site", 100, 40) { CSharpProgram = "shaping.host.cs", Script = "shaping.ps1", Static = true };
            // Format-Table, Format-List and Format-Wide before and after the separate FormatCommands opt-in.
            yield return new Demo("format-commands", "Enable-ConsoleFormatting -Feature FormatCommands", 100, 46) { CSharpProgram = "format-commands.host.cs",
                Script = "format-commands.ps1",
                Session = true,

                PosterShare = 1,
                Fps = 5,
                Chapters = new[] { ("Before", "# Format-Table, Format-List and Format-Wide as PowerShell draws them"), ("After", "# The same commands, drawn by ConsoleMonster") }
            };
            // A module that ships its own views as data, without depending on ConsoleMonster.
            yield return new Demo("module-views", "Contoso.Servers.consoleviews.psd1", 100, 40) { CSharpProgram = "module-views.host.cs",
                Script = "module-views.ps1",
                Companions = new[] { "Contoso.Servers/Contoso.Servers.consoleviews.psd1", "Contoso.Servers/Contoso.Servers.psm1", "Contoso.Servers/Contoso.Servers.psd1" },
                Session = true,
                Static = true
            };
            yield return new Demo("help", "Show-ConsoleHelp", 100, 50) { CSharpProgram = "help.host.cs", Script = "help.ps1", Session = true, PosterShare = 1 };
            yield return new Demo("progress", "Write-Progress, drawn by ConsoleMonster", 96, 12) { CSharpProgram = "progress.host.cs", Script = "progress.ps1", PosterShare = 0.5, Fps = 8, ShowProgress = true, EndsOn = "Inventory collected: 36 servers across 3 sites." };
            yield return new Demo("errors", "Show-Console -View Error", 100, 20) { CSharpProgram = "errors.cs", Script = "errors.ps1", Static = true, AllowErrors = true };
            // Threads and concurrency: the same parallel work written straight to the console, through a dashboard, and
            // through the same dashboard when CI is detected.
            yield return new Demo("threads-naive", "ForEach-Object -Parallel", 100, 24) { Script = "threads-naive.ps1", CSharpProgram = "threads-naive.cs", PosterShare = 0.6, Fps = 8 };
            yield return new Demo("threads-live", "Invoke-ConsoleDashboard -Parallel", 100, 22) { Script = "threads-dashboard.ps1", CSharpProgram = "threads-live.cs", PosterShare = 0.6, Fps = 8 };
            yield return new Demo("threads-ci", "CI=true", 100, 30) { CSharpProgram = "threads-live.cs", Script = "threads-dashboard.ps1", Setup = "$env:CI = 'true'", PosterShare = 0.55, Fps = 8 };
            yield return new Demo("session", "session.ps1", 100, 14) { Script = "session.ps1", CSharpProgram = "session.cs", PosterShare = 0.6, Fps = 6 };
            yield return new Demo("record", "record.ps1", 100, 18) { Script = "record.ps1", CSharpProgram = "record.cs", PosterShare = 0.4, Fps = 6 };
            yield return new Demo("login", "ssh deploy@app-web-03", 100, 30) { CSharpProgram = "login.cs",
                Script = Path.Combine("Module", "Examples", "Example-LoginOverview.ps1"),
                Arguments = "-Sample -Width 96",
                Prompt = "ssh deploy@app-web-03",
                PosterShare = 1
            };
            // The host runs until Ctrl+C, which the recording presses after twelve seconds, as a reader would.
            yield return new Demo("hosting", "dotnet run", 110, 30) {
                Project = Path.Combine("Website", "examples", "demos", "hosting", "Orders.csproj"),
                PowerShellTwin = "Website/examples/demos/hosting/Run.ps1",
                Interrupt = TimeSpan.FromSeconds(12),
                PosterShare = 0.75
            };
            yield return new Demo("logging", "dotnet run", 110, 30) {
                Project = Path.Combine("Website", "examples", "demos", "logging", "Shop.csproj"),
                PowerShellTwin = "Website/examples/demos/logging/Run.ps1",
                Static = true
            };
            // TemplateDemo compiles the template's own Program.cs and drives its menu with scripted keys.
            yield return new Demo("template", "dotnet run, then dotnet run -- serve", 100, 26) {
                Project = Path.Combine("Website", "tools", "TemplateDemo", "TemplateDemo.csproj"),
                InputReadyText = "Processed batch", InputKeys = "\u0003", InputDelay = TimeSpan.FromSeconds(9),
                PowerShellTwin = "Website/examples/demos/template.ps1",
                Sources = new[] { Path.Combine("Templates", "ConsoleMonster.Templates", "content", "ConsoleMonsterApp", "Program.cs") },
                CSharp = (Path.Combine("Templates", "ConsoleMonster.Templates", "content", "ConsoleMonsterApp", "Program.cs"), null, "public static async Task<int> Main(string[] args)"),
                PosterShare = 0.3
            };
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

        // Everything a demo's recording is made from, as text: its script or program, the files beside it, its arguments and size.
        private static string? Source(Site site, Demo demo) {
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            if (demo.Script != null) {
                text.Append(Site.ReadText(Script(site, demo.Script))).Append('\n');
                foreach (string companion in demo.Companions) {
                    text.Append(Site.ReadText(Path.Combine(Path.GetDirectoryName(Script(site, demo.Script))!, companion))).Append('\n');
                }
                text.Append(demo.Arguments).Append('\n').Append(demo.Prompt).Append('\n').Append(demo.Session).Append('\n').Append(demo.Setup);
                // Progress records and allowed errors change what the run prints.
                text.Append(demo.ShowProgress ? "\nprogress" : string.Empty).Append(demo.AllowErrors ? "\nerrors" : string.Empty);
            } else if (demo.ProgramArguments != null) {
                text.Append(Site.ReadText(Script(site, demo.CSharpProgram!))).Append('\n');
                foreach (string companion in Companions(site, demo)) {
                    text.Append(Site.ReadText(companion)).Append('\n');
                }
                text.Append(demo.ProgramArguments);
            } else if (demo.Project != null) {
                foreach (string file in ProjectSources(site, demo)) {
                    text.Append(Path.GetFileName(file)).Append('\n').Append(Site.ReadText(file)).Append('\n');
                }
                text.Append(demo.ProjectArguments).Append('\n').Append(demo.Interrupt?.TotalMilliseconds);
                text.Append('\n').Append(demo.InputReadyText).Append('\n').Append(demo.InputKeys).Append('\n').Append(demo.InputDelay?.TotalMilliseconds);
            } else {
                return null;
            }
            return text.Append('\n').Append(demo.Width).Append('x').Append(demo.Height).ToString();
        }

        // Everything a project demo compiles: the project file and sources in its folder, then the files it includes.
        private static List<string> ProjectSources(Site site, Demo demo) {
            string folder = Path.GetDirectoryName(Path.Combine(site.Repository, demo.Project!))!;
            List<string> files = new List<string>(Directory.GetFiles(folder, "*.csproj"));
            files.AddRange(Directory.GetFiles(folder, "*.cs"));
            files.Sort(StringComparer.Ordinal);
            foreach (string source in demo.Sources) {
                files.Add(Path.Combine(site.Repository, source));
            }
            return files;
        }


        // The shown program of a project demo that has no other C# to show: the Program.cs in its folder.
        private static string? ProjectProgramSource(Site site, Demo demo) {
            if (demo.Project == null || demo.CSharp != null) {
                return null;
            }
            string program = Path.Combine(Path.GetDirectoryName(Path.Combine(site.Repository, demo.Project))!, "Program.cs");
            return File.Exists(program) ? program : null;
        }

        private static string[] Companions(Site site, Demo demo) => Array.ConvertAll(demo.CSharpCompanions, c => Path.Combine(site.Repository, c));

        private static string Script(Site site, string name) => name.Contains(Path.DirectorySeparatorChar) || name.Contains('/') ? Path.Combine(site.Repository, name) : Path.Combine(site.Website, "examples", "demos", name);

        // The path a code bar names and the site links to on GitHub: relative to the repository, with forward slashes.
        private static string ScriptFile(Site site, string name) => Path.GetRelativePath(site.Repository, Script(site, name)).Replace('\\', '/');

        private static string CastPath(Site site, string id) => CapturePlatforms.PathFor(site, Path.Combine(site.Captures, "demos", id + ".cast"));

        public static void Capture(Site site) {
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
                        Site.RemoveTemporary(temp);
                    }
                } else if (demo.ProgramArguments != null) {
                    string work = Path.Combine(Path.GetTempPath(), "cmsite-demo-" + demo.Id + "-" + Guid.NewGuid().ToString("N").Substring(0, 12));
                    try {
                        string dll = CSharpRunner.Build(Script(site, demo.CSharpProgram!), work, demo.Id, Companions(site, demo));
                        chunks = CSharpRunner.Record(dll, demo.ProgramArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries), demo.Width, demo.Height, PowerShellRunner.Environment("dark"), work, TimeSpan.FromMinutes(3));
                    } finally {
                        Site.RemoveTemporary(work);
                    }
                } else {
                    // The project is built into a folder of this run's own, then run from there and removed.
                    string output = Path.Combine(Path.GetTempPath(), "cmsite-build-" + demo.Id + "-" + Guid.NewGuid().ToString("N").Substring(0, 12));
                    try {
                        Build(site, demo.Project!, output);
                        string dll = Path.Combine(output, Path.GetFileNameWithoutExtension(demo.Project!) + ".dll");
                        // The drive root as the working directory: a host logs its content root, which should not be this checkout.
                        chunks = CSharpRunner.Record(dll, demo.ProjectArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries), demo.Width, demo.Height, PowerShellRunner.Environment("dark"), Path.GetPathRoot(site.Repository) ?? site.Repository, TimeSpan.FromMinutes(2), demo.Interrupt,
                            demo.InputReadyText, demo.InputKeys, demo.InputDelay);
                    } finally {
                        Site.RemoveTemporary(output);
                    }
                }
                string path = CastPath(site, demo.Id);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                // Window titles (OSC 0 and 2) carry the executable's path; nothing on the site shows them.
                string cast = System.Text.RegularExpressions.Regex.Replace(Replays.ToCast(chunks, demo.Width, demo.Height, demo.Title), @"\\u001B\][02];(?:[^""\\]|\\\\)*?(?:\\u0007|\\u001B\\\\)", string.Empty);
                string? source = Source(site, demo);
                File.WriteAllText(path, source != null ? Replays.Stamp(cast, Replays.Fingerprint(source, CapturePins.Policy)) : cast, new System.Text.UTF8Encoding(false));
                CaptureRun.Record(path);
                Console.WriteLine("recorded demo " + demo.Id + " in " + watch.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
            }
        }


        private static void Build(Site site, string project, string output) {
            // ContinuousIntegrationBuild maps source paths to /_/, so stack traces in the recordings do not show this checkout.
            // The same dotnet that runs the recordings builds them.
            ProcessStartInfo info = new ProcessStartInfo(CSharpRunner.DotnetHost, "build \"" + Path.Combine(site.Repository, project) + "\" -c Release -nologo -v q -p:ContinuousIntegrationBuild=true -o \"" + output + "\"") { UseShellExecute = false };
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
                if (expected != null && Replays.Source(cast) != Replays.Fingerprint(expected, CapturePins.Policy)) {
                    problems.Add("demo " + demo.Id + ": the recording was made from another version of its script or program; run the capture command for the demos area (SITEFRAMES_DEMOS=" + demo.Id + ").");
                }
                List<TimedFrame> frames = Replays.FromCast(cast, demo.Fps, false, out CellBuffer scrollback, out int width, out int height);
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
                // Cut to what the frames that are played use, after the ones past the closing screen are gone.
                frames = ReplayFit.Trim(frames);
                if (frames.Count > 0) {
                    height = frames[0].Frame.Height;
                    width = frames[0].Frame.Width;
                    // A replay that ends on an empty console stops on nothing: a reader who lets it play sees a blank window.
                    if (!demo.Static && Blank(frames[frames.Count - 1].Frame) && frames.Exists(f => !Blank(f.Frame))) {
                        problems.Add("demo " + demo.Id + ": the replay ends on an empty screen; set EndsOn to the screen it should stop on.");
                    }
                }
                JsonObject entry = new JsonObject {
                    ["id"] = demo.Id,
                    ["title"] = demo.Title,
                    ["w"] = width,
                    ["h"] = height,
                    // The terminal the run had, for the window's chips; w and h are the part of it the frames use.
                    ["terminal"] = demo.Width.ToString(CultureInfo.InvariantCulture) + "×" + demo.Height.ToString(CultureInfo.InvariantCulture)
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
                    // A replay that ends on its result (or whose poster is its final screen) opens there, at its end, so the player shows that screen and Play
                    // starts again from the beginning.
                    if (demo.EndsOn != null || demo.PosterShare >= 1) {
                        posterTime = duration;
                    }
                    entry["url"] = "/replays/demo-" + demo.Id + ".json";
                    entry["frames"] = count;
                    entry["duration"] = duration;
                    entry["fps"] = demo.Fps;
                    entry["start"] = posterTime;
                    // A player that opens on the final screen plays from the first full screen, not from an empty prompt.
                    entry["hero"] = ReplayFit.Hero(frames);
                    entry["bytes"] = System.Text.Encoding.UTF8.GetByteCount(data);
                    entry["poster"] = new JsonObject { ["w"] = width, ["h"] = height, ["dark"] = poster };
                }
                AddPlatformVariants(site, demo, entry, problems);
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
                    entry["powershellNote"] = demo.Project == null ? "The recording ran the C# program. This script uses the same module capabilities."
                        : demo.Id == "template" ? "PowerShell launches the same .NET application after Program.cs is saved from either C# tab."
                        : "PowerShell launches the same .NET project after Program.cs is saved from either C# tab. The generic host and ILogger provider have no separate cmdlet implementation.";
                    if (demo.Project != null) {
                        entry["projectLauncher"] = true;
                        entry["powershellRun"] = "./" + Path.GetFileName(demo.PowerShellTwin);
                        entry["powershellFile"] = Path.GetFileName(demo.PowerShellTwin);
                    }
                }
                if (demo.CSharpProgram != null) {
                    string program = Script(site, demo.CSharpProgram);
                    if (program.EndsWith(".host.cs", StringComparison.Ordinal)) {
                        CSharpRunner.Check(program, Site.ReadText(program));
                    } else if (demo.CSharpCompanions.Length == 0) {
                        CSharpRunner.Compile(program);
                    } else {
                        string check = Path.Combine(Path.GetTempPath(), "cmsite-check-" + demo.Id + "-" + Guid.NewGuid().ToString("N").Substring(0, 12));
                        try {
                            CSharpRunner.Build(program, check, demo.Id, Companions(site, demo));
                        } finally {
                            Site.RemoveTemporary(check);
                        }
                    }
                    (string shortForm, string full) = SourceSnippets.Program(Site.ReadText(program));
                    entry["csharp"] = shortForm;
                    entry["csharpFull"] = full;
                    entry["csharpRepo"] = ScriptFile(site, demo.CSharpProgram);
                    entry["csharpFile"] = "Program.cs";
                    SourceStyles.Add(site, entry, program, Companions(site, demo));
                    if (program.EndsWith(".host.cs", StringComparison.Ordinal)) {
                        string projectTemplate = Path.Combine(site.Website, "examples", "demos", "HostExample.csproj.template");
                        string projectDownload = site.Static("examples/HostExample.csproj");
                        Directory.CreateDirectory(Path.GetDirectoryName(projectDownload)!);
                        File.Copy(projectTemplate, projectDownload, true);
                        site.Written.Add(Path.GetRelativePath(site.Website, projectDownload).Replace('\\', '/'));
                        entry["hostProject"] = "/examples/HostExample.csproj";
                        entry["hostBuild"] = "Import-Module ConsoleMonster\n$moduleBinary = Split-Path (Get-Command Show-Console).DLL\ndotnet build ./HostExample.csproj -c Release \"-p:ConsoleMonsterModuleDirectory=$moduleBinary\"";
                        entry["hostRun"] = "Import-Module ./bin/Release/net8.0/HostExample.dll\n" + CSharpRunner.HostCommand(program);
                        entry["csharpFile"] = Path.GetFileName(program);
                        entry["csharpFluentFile"] = Path.GetFileName(program);
                        entry["csharpNote"] = "This is a compiled PowerShell cmdlet class. Reference the installed module’s ConsoleMonster and ConsoleMonster.PowerShell assemblies in a PowerShell module project; ProcessRecord runs on the owning pipeline thread. Download the project beside the source file and build/run it from PowerShell 7.";
                        entry["csharpFluentNote"] = entry["csharpNote"]!.DeepClone();
                    }
                    // The recording ran the script; say so on the C# tab, whose program compiles here but did not draw it.
                    if (demo.Script != null && !program.EndsWith(".host.cs", StringComparison.Ordinal)) {
                        entry["csharpNote"] = "The recording ran the PowerShell script. This C# program does the same with the library and compiles as shown; its timings are its own.";
                        entry["csharpFluentNote"] = entry["csharpNote"]!.DeepClone();
                    }
                    // The other files of a multi-file program, each shown whole with its name.
                    JsonArray files = new JsonArray();
                    foreach (string companion in Companions(site, demo)) {
                        files.Add(new JsonObject { ["file"] = Path.GetFileName(companion), ["code"] = Site.ReadText(companion), ["codeRepo"] = Path.GetRelativePath(site.Repository, companion).Replace('\\', '/') });
                    }
                    if (files.Count > 0) {
                        entry["csharpFiles"] = files;
                    }
                }
                // A project demo shows its whole Program.cs: the capture built it (warnings are errors) and recorded that
                // build, and the fingerprint above proves the recording came from this text.
                string? projectProgram = ProjectProgramSource(site, demo);
                if (projectProgram != null) {
                    (string shortForm, string full) = SourceSnippets.Program(Site.ReadText(projectProgram));
                    entry["csharp"] = shortForm;
                    entry["csharpFull"] = full;
                    entry["csharpRepo"] = Path.GetRelativePath(site.Repository, projectProgram).Replace('\\', '/');
                    entry["csharpFile"] = "Program.cs";
                    SourceStyles.AddProject(site, entry, projectProgram, Path.Combine(site.Repository, demo.Project!));
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
                    SourceStyles.AddProject(site, entry, source, Path.Combine(site.Repository, demo.Project!));
                }
                demos[demo.Id] = entry;
            }
            site.WriteJson(site.Data("demos"), demos);
            WidgetsPage.Report(problems, "demos");
        }
    }
}
