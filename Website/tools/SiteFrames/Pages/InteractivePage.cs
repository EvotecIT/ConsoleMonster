using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using SiteFrames.Capture;
using SiteFrames.Frames;
using SiteFrames.Replay;

namespace SiteFrames.Pages {
    /// <summary>
    /// Prompts, menus, forms and the grid. Each story is a complete C# program (<c>Website/examples/interactive/&lt;id&gt;.cs</c>)
    /// and, where one exists, its PowerShell twin (<c>&lt;id&gt;.ps1</c>). Neither mentions the recording: the tool is the
    /// driver, setting <see cref="InteractiveOptions.Default"/> to scripted keys and an in-memory <see cref="ScreenCapture"/>
    /// before it runs the program, exactly as the module's example test does. The storyboard keeps the distinct screens the
    /// keys produced; the capture runs the twin the same way on PowerShell 7 and the render compares its screens' text with
    /// the program's.
    /// </summary>
    internal static class InteractivePage {
        internal sealed class Story {
            public Story(string id, string title, string summary, string keys, int width, int height, bool inline, bool realTime, string[]? pick = null) {
                Id = id;
                ExampleId = id;
                Title = title;
                Summary = summary;
                Keys = keys;
                Width = width;
                Height = height;
                Inline = inline;
                RealTime = realTime;
                Pick = pick;
            }

            public string Id { get; }
            public string Title { get; }
            public string Summary { get; }
            public string Keys { get; }
            public int Width { get; }
            public int Height { get; }
            public bool Inline { get; }
            public bool RealTime { get; }
            public string[]? Pick { get; }
            public string Area { get; set; } = "interactive";
            public string ExampleId { get; set; }
            public ITheme? Theme { get; set; }
            public ColorDepth Depth { get; set; } = ColorDepth.TrueColor;
            public GlyphTier Glyphs { get; set; } = GlyphTier.Full;
        }

        private static IEnumerable<Story> Stories() {
            yield return new Story("select", "Selection", "A list that filters as you type, with groups, badges, and choices that are unavailable with their reason. The answer comes back as the chosen object, not a string.",
                "Down Down \"we\" Down Enter", 100, 16, true, false);
            yield return new Story("multi", "Multi-selection with a preview", "Tri-state group checkboxes, a live count and a preview pane beside the list. Ctrl+A selects all, Ctrl+N none and Ctrl+T inverts.",
                "Down Space Down Down Space Down Down Down Space Enter", 110, 20, true, false);
            yield return new Story("text", "Text with validation", "A pattern, a length or a scriptblock validates the answer, and the message stays inline until the input is valid.",
                "\"9reports\" Enter Home Delete Enter", 90, 10, true, false);
            yield return new Story("typed", "Typed confirmation", "For destructive actions the name has to be typed; anything else keeps the prompt open.",
                "\"FS0\" Enter \"1\" Enter", 90, 10, true, false);
            yield return new Story("shell", "A menu as an app", "The Shell layout: a breadcrumb title bar, a sidebar, the content pane and a footer that lists the keys that work right now. Ctrl+K opens the command palette, and the review shows the equivalent command line.",
                "wait:1600 Enter Space Down Space Down Down Down Space Escape Down Enter Down Down Down Down wait:300 Escape Ctrl+K \"cert\" wait:300 Escape Escape r wait:300 Ctrl+Q", 120, 34, false, true, new[] { "SQL01", "3 of 11 selected", "› cert", "Equivalent command" });
            yield return new Story("wizard", "A form as a wizard", "Fields with pages become a wizard with a progress strip. Dependent fields reload their choices with a spinner, and the review page shows every answer and the equivalent command.",
                "PageDown Enter Escape PageDown Right Down \"svc-toolbox\" Down \"Example-Pass1\" PageDown Right wait:400 Down wait:1800 Enter Enter PageDown wait:300 Escape Escape Escape Escape Escape y", 110, 30, false, true, new[] { "3 of 6 selected", "eu-central-1", "Equivalent command" });
            yield return new Story("grid", "The grid", "An Out-GridView for the console on 5.1 and 7: typing filters, F3 sorts by column, Space marks rows, Enter shows every column of a row and Ctrl+Enter returns the original objects.",
                "\"sql\" wait:200 Backspace Backspace Backspace F3 Down Space Down Space Enter wait:200 Escape Ctrl+Enter", 110, 22, false, false);
        }

        private static string ProgramPath(Site site, Story story) => Path.Combine(site.Website, "examples", story.Area, story.ExampleId + ".cs");

        private static string ScriptPath(Site site, Story story) => Path.Combine(site.Website, "examples", story.Area, story.ExampleId + ".ps1");

        private static string CapturePath(Site site, Story story, PowerShellHost? host = null) => Path.Combine(site.Captures, "interactive", story.Id + "." + (host ?? PowerShellHost.Core).Id + ".txt");

        private static TerminalProfile Profile(Story story) =>
            TerminalProfile.Fixed(story.Width, story.Height, story.Depth, story.Glyphs).WithCulture(Site.Culture).WithLanguage(Site.Culture);

        // The driver: the scripted keys and the capture, set as the process defaults the shown program's prompts pick up. The
        // run gets a fresh, empty temporary folder (a program may keep state there, such as the menu's presets), and every key
        // must be used: a program that ends early, or waits for more keys, fails the render.
        private static ScreenCapture Capture(Site site, Story story, ITheme? theme = null) {
            ScreenCapture capture = new ScreenCapture(story.Width, story.Height, story.Inline);
            ScriptedInput keys = new ScriptedInput(story.Keys) { Paced = true, RealTime = story.RealTime };
            InteractiveOptions options = new InteractiveOptions {
                Mode = InteractionMode.Rich,
                Input = keys,
                Capture = capture,
                Theme = theme ?? story.Theme ?? Theme.Dark,
                Labels = new PromptLabels(),
                Profile = Profile(story)
            };
            string? tmp = Environment.GetEnvironmentVariable("TMP");
            string? temp = Environment.GetEnvironmentVariable("TEMP");
            string fresh = Path.Combine(Path.GetTempPath(), "cmsite-story-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(fresh);
            try {
                Environment.SetEnvironmentVariable("TMP", fresh);
                Environment.SetEnvironmentVariable("TEMP", fresh);
                Environment.SetEnvironmentVariable("TMPDIR", fresh);
                CSharpRunner.Execute(ProgramPath(site, story), Profile(story), theme ?? story.Theme ?? Theme.Dark, options);
            } finally {
                Environment.SetEnvironmentVariable("TMP", tmp);
                Environment.SetEnvironmentVariable("TEMP", temp);
                Environment.SetEnvironmentVariable("TMPDIR", null);
                Site.RemoveTemporary(fresh);
            }
            if (keys.Remaining > 0) {
                throw new InvalidOperationException(story.Id + ".cs ended with " + keys.Remaining + " scripted keys unused.");
            }
            return capture;
        }

        // What a story's capture must have been made from: the twin script, the keys and the screen.
        private static string Fingerprint(Site site, Story story) {
            string source = Site.ReadText(ScriptPath(site, story)) + "\n" + story.Keys + "\n" + story.Width + "x" + story.Height + (story.Inline ? " inline" : " screen") + (story.RealTime ? " realtime" : string.Empty);
            if (story.Area != "interactive") { source += "\n" + ThemeName(story) + " " + story.Depth + " " + story.Glyphs + " Release"; }
            return Hash(source);
        }

        private static string ThemeName(Story story) => ReferenceEquals(story.Theme, Theme.Light) ? "Light" : "Dark";

        /// <summary>Captures a shown interactive example using the same scripted driver as the interactive gallery.</summary>
        internal static void CaptureExample(Site site, Story story) {
            foreach (PowerShellHost host in PowerShellHost.All) { CaptureTwin(site, story, host); }
        }

        /// <summary>Requires the shown example's current source and every styled screen to match its C# twin.</summary>
        internal static bool CheckExample(Site site, Story story, List<string> problems) {
            ScreenCapture capture = Capture(site, story);
            bool same = true;
            foreach (PowerShellHost host in PowerShellHost.All) { same = Parity(site, story, capture, problems, host) && same; }
            return same;
        }

        private static string Hash(string text) {
            using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create()) {
                return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Substring(0, 16).ToLowerInvariant();
            }
        }

        // One captured screen: a hash of its cells with their styles (ConsoleMonster's own HTML export), then its text, so a
        // difference in colour fails the comparison as surely as a difference in words.
        private static string Describe(CellBuffer frame) => Hash(ConsoleMonster.Export.ConsoleExporter.ToHtml(frame)) + "\n" + frame.ToPlainText().Replace("\r\n", "\n").TrimEnd();

        /// <summary>Runs each PowerShell twin on PowerShell 7 with the same keys and saves the text of the screens it drew.</summary>
        public static void Capture(Site site) {
            foreach (Story story in Stories()) { CaptureTwin(site, story); }
        }

        private static void CaptureTwin(Site site, Story story, PowerShellHost? host = null) {
            string script = ScriptPath(site, story);
            if (!File.Exists(script)) {
                return;
            }
            string output = CapturePath(site, story, host);
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            if (File.Exists(output)) {
                File.Delete(output);
            }
            string q(string text) => "'" + text.Replace("'", "''") + "'";
            string driver =
                "$cmCapture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(" + story.Width + ", " + story.Height + ", $" + (story.Inline ? "true" : "false") + ")\n" +
                "$cmKeys = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new(" + q(story.Keys) + ")\n" +
                "$cmKeys.Paced = $true\n$cmKeys.RealTime = $" + (story.RealTime ? "true" : "false") + "\n" +
                "$cmOptions = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default\n" +
                "$cmOptions.Mode = 'Rich'\n$cmOptions.Input = $cmKeys\n$cmOptions.Capture = $cmCapture\n" +
                "$cmOptions.Theme = [ConsoleMonster.Styling.Theme]::" + ThemeName(story) + "\n$cmOptions.Labels = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.PromptLabels'))::new()\n" +
                "$cmCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')\n" +
                "$cmOptions.Profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed(" + story.Width + ", " + story.Height + ", '" + story.Depth + "', '" + story.Glyphs + "').WithCulture($cmCulture).WithLanguage($cmCulture)\n" +
                ". " + q(script) + "\n" +
                "if ($cmKeys.Remaining -gt 0) { throw ('The script ended with ' + $cmKeys.Remaining + ' scripted keys unused.') }\n" +
                "$cmSha = [System.Security.Cryptography.SHA256]::Create()\n" +
                "$cmScreens = foreach ($cmFrame in $cmCapture.Frames) {\n" +
                "    $cmHtml = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Export.ConsoleExporter'))::ToHtml($cmFrame)\n" +
                "    $cmHash = ([BitConverter]::ToString($cmSha.ComputeHash([Text.Encoding]::UTF8.GetBytes($cmHtml))) -replace '-', '').Substring(0, 16).ToLowerInvariant()\n" +
                "    $cmHash + \"`n\" + ($cmFrame.ToPlainText() -replace \"`r`n\", \"`n\").TrimEnd()\n" +
                "}\n" +
                "[IO.File]::WriteAllText(" + q(output) + ", ('source " + Fingerprint(site, story) + "' + [string][char]12 + (@($cmScreens) -join [string][char]12)), [Text.UTF8Encoding]::new($false))\n";
            string wrapper = Path.Combine(Path.GetTempPath(), "cmsite-interactive-" + story.Id + "-" + (host ?? PowerShellHost.Core).Id + ".ps1");
            File.WriteAllText(wrapper, driver, new System.Text.UTF8Encoding(true));
            try {
                Dictionary<string, string?> environment = PowerShellRunner.Environment(ThemeName(story).ToLowerInvariant(),
                    story.Depth == ColorDepth.None ? "none" : story.Depth == ColorDepth.Ansi16 ? "16" : "truecolor", story.Glyphs.ToString().ToLowerInvariant());
                if (story.Area != "interactive") { environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Release"; }
                PowerShellRunner.Run(site, host ?? PowerShellHost.Core, wrapper, story.Width, story.Height, environment, Path.GetDirectoryName(script), interactive: true);
            } finally {
                File.Delete(wrapper);
            }
            if (!File.Exists(output)) {
                throw new InvalidOperationException(story.Id + ".ps1 did not finish; nothing was captured.");
            }
            Console.WriteLine("captured " + Path.GetRelativePath(site.Website, output));
        }

        // Each distinct screen, in order (a screen repeated by the next frame counts once).
        private static List<string> Distinct(IEnumerable<string> screens) {
            List<string> list = new List<string>();
            foreach (string screen in screens) {
                if (list.Count == 0 || list[list.Count - 1] != screen) {
                    list.Add(screen);
                }
            }
            return list;
        }

        public static void Render(Site site) {
            JsonArray list = new JsonArray();
            List<string> problems = new List<string>();
            foreach (Story story in Stories()) {
                ScreenCapture capture = Capture(site, story);
                List<CellBuffer> screens = Screens(story, capture);
                if (screens.Count == 0) {
                    WidgetsPage.Report(new List<string> { story.Id + " drew no screens" }, "interactive");
                    continue;
                }
                List<TimedFrame> frames = new List<TimedFrame>();
                long step = story.Pick != null ? 2600 : 900;
                for (int i = 0; i < screens.Count; i++) {
                    frames.Add(new TimedFrame(i * step, screens[i]));
                }
                string data = PlayerData.Write(site, frames, story.Title, 1000.0 / step, 2400, out long duration, out int count);
                site.Write(site.Static("replays/interactive-" + story.Id + ".json"), data);
                // Before it plays, a storyboard shows its fullest screen (a prompt ends by collapsing to a one-line answer).
                int fullest = Fullest(screens);
                string poster = FrameHtml.Body(FrameHtml.Rows(screens[fullest], ColorDepth.TrueColor, Screen.Dark, site.Styles), false);
                (string csharp, string csharpFull) = SourceSnippets.Program(Site.ReadText(ProgramPath(site, story)));
                JsonObject entry = new JsonObject {
                    ["id"] = story.Id,
                    ["title"] = story.Title,
                    ["summary"] = story.Summary,
                    ["keys"] = story.Keys,
                    ["csharp"] = csharp,
                    ["csharpFull"] = csharpFull,
                    ["csharpFile"] = "Program.cs",
                    ["player"] = new JsonObject {
                        ["url"] = "/replays/interactive-" + story.Id + ".json",
                        ["w"] = story.Width,
                        ["h"] = story.Height,
                        ["duration"] = duration,
                        ["frames"] = count,
                        ["start"] = frames[fullest].Time,
                        ["poster"] = new JsonObject { ["w"] = story.Width, ["h"] = Height(screens), ["dark"] = poster }
                    }
                };
                if (File.Exists(ScriptPath(site, story))) {
                    (string powershell, string powershellFull) = SourceSnippets.Program(Site.ReadText(ScriptPath(site, story)));
                    entry["powershell"] = powershell;
                    entry["powershellFull"] = powershellFull;
                    entry["powershellFile"] = story.Id + ".ps1";
                    entry["parity"] = Parity(site, story, capture, problems);
                }
                list.Add(entry);
            }
            site.WriteJson(site.Data("interactive"), new JsonObject { ["stories"] = list });
            WidgetsPage.Report(problems, "interactive");
        }

        // Whether the PowerShell twin drew the same screens (by their text) as the C# program for the same keys.
        private static bool Parity(Site site, Story story, ScreenCapture capture, List<string> problems, PowerShellHost? host = null) {
            string path = CapturePath(site, story, host);
            if (!File.Exists(path)) {
                problems.Add(story.Id + " has no PowerShell capture; run the capture command for the interactive area.");
                return false;
            }
            string[] parts = File.ReadAllText(path).Split('\f');
            if (parts[0] != "source " + Fingerprint(site, story)) {
                problems.Add(story.Id + ": the PowerShell capture was made from another version of " + story.Id + ".ps1, its keys or its screen; run the capture command for the interactive area.");
                return false;
            }
            List<string> csharp = Distinct(System.Linq.Enumerable.Select(capture.Frames, Describe));
            List<string> powershell = Distinct(System.Linq.Enumerable.Skip(parts, 1));
            if (csharp.Count != powershell.Count) {
                problems.Add(story.Id + ": PowerShell drew " + powershell.Count + " distinct screens, C# " + csharp.Count + ".");
                return false;
            }
            for (int i = 0; i < csharp.Count; i++) {
                if (csharp[i] != powershell[i]) {
                    problems.Add(story.Id + ": screen " + (i + 1) + " differs between PowerShell and C#:\n--- C#\n" + csharp[i] + "\n--- PowerShell\n" + powershell[i]);
                    return false;
                }
            }
            return true;
        }

        // The screens a storyboard shows: every distinct frame, or for apps the frame that settles on each phrase.
        private static List<CellBuffer> Screens(Story story, ScreenCapture capture) {
            List<CellBuffer> screens = new List<CellBuffer>();
            if (story.Pick != null) {
                foreach (string phrase in story.Pick) {
                    CellBuffer? found = null;
                    foreach (CellBuffer frame in capture.Frames) {
                        if (frame.ToPlainText().IndexOf(phrase, StringComparison.Ordinal) >= 0) {
                            found = frame;
                            if (phrase == story.Pick[0]) {
                                break;
                            }
                        }
                    }
                    if (found == null) {
                        throw new InvalidOperationException(story.Id + ": no screen shows \"" + phrase + "\"; the keys or the program changed.");
                    }
                    screens.Add(found);
                }
                return screens;
            }
            CellBuffer? previous = null;
            foreach (CellBuffer frame in capture.Frames) {
                if (previous == null || !Same(frame, previous)) {
                    screens.Add(frame);
                    previous = frame;
                }
            }
            return screens;
        }

        private static int Height(List<CellBuffer> screens) {
            int height = 0;
            foreach (CellBuffer screen in screens) {
                height = Math.Max(height, screen.Height);
            }
            return height;
        }

        private static int Fullest(List<CellBuffer> screens) {
            int best = 0;
            int most = -1;
            for (int i = 0; i < screens.Count; i++) {
                CellBuffer screen = screens[i];
                int count = 0;
                for (int y = 0; y < screen.Height; y++) {
                    for (int x = 0; x < screen.Width; x++) {
                        string g = screen[x, y].Glyph ?? " ";
                        if (g != " " && g.Length > 0) {
                            count++;
                        }
                    }
                }
                if (count > most) {
                    most = count;
                    best = i;
                }
            }
            return best;
        }

        private static bool Same(CellBuffer a, CellBuffer b) {
            if (a.Width != b.Width || a.Height != b.Height) {
                return false;
            }
            for (int y = 0; y < a.Height; y++) {
                for (int x = 0; x < a.Width; x++) {
                    if (!a[x, y].Equals(b[x, y])) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>The menu shell's task screen, for the navigation thumbnail.</summary>
        public static CellBuffer? Thumbnail(Site site, ITheme theme) {
            foreach (Story story in Stories()) {
                if (story.Id == "shell") {
                    List<CellBuffer> screens = Screens(story, Capture(site, story, theme));
                    return screens.Count > 1 ? screens[1] : screens.Count > 0 ? screens[0] : null;
                }
            }
            return null;
        }
    }
}
