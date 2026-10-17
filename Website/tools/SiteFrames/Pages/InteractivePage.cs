using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using ConsoleMonster.Examples.Showcases;
using SiteFrames.Frames;
using SiteFrames.Replay;

namespace SiteFrames.Pages {
    /// <summary>
    /// Prompts, menus, forms and the grid, driven by scripted keys into a <see cref="ScreenCapture"/> (ConsoleMonster's
    /// in-memory surface for documentation captures). Each storyboard keeps the distinct screens the keys produced. The
    /// lines between the example markers in each method are what the site shows as its C# source; <c>options</c> carries
    /// the scripted keys and the capture.
    /// </summary>
    internal static class InteractivePage {
        private sealed class Story {
            public Story(string id, string title, string summary, string keys, int width, int height, bool inline, bool realTime, Action<InteractiveOptions> run, string method, string powershell, string[]? pick = null) {
                Id = id;
                Title = title;
                Summary = summary;
                Keys = keys;
                Width = width;
                Height = height;
                Inline = inline;
                RealTime = realTime;
                Run = run;
                Method = method;
                PowerShell = powershell;
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
            public Action<InteractiveOptions> Run { get; }
            public string Method { get; }
            public string PowerShell { get; }
            public string[]? Pick { get; }
        }

        private static IEnumerable<Story> Stories() {
            yield return new Story("select", "Selection", "A list that filters as you type, with groups, badges, and choices that are unavailable with their reason. The answer comes back as the chosen object, not a string.",
                "Down Down \"we\" Down Enter", 100, 16, true, false, SelectServer, nameof(SelectServer),
                "$server = Read-ConsoleSelection 'Target server' -Choices $servers -Display Name -GroupBy Site");
            yield return new Story("multi", "Multi-selection with a preview", "Tri-state group checkboxes, a live count and a preview pane beside the list. Ctrl+A selects all, Ctrl+N none and Ctrl+T inverts.",
                "Down Space Down Down Space Down Down Down Space Enter", 110, 20, true, false, SelectTasks, nameof(SelectTasks),
                "$tasks = $allTasks | Read-ConsoleMultiSelection 'Tasks' -Display Name -GroupBy Category -Minimum 1");
            yield return new Story("text", "Text with validation", "A pattern, a length or a scriptblock validates the answer, and the message stays inline until the input is valid.",
                "\"9reports\" Enter Home Delete Enter", 90, 10, true, false, AskShareName, nameof(AskShareName),
                "$name = Read-ConsoleText 'Share name' -Pattern '[A-Za-z][A-Za-z0-9_-]{1,23}' -PatternMessage 'Start with a letter; letters, digits, - and _ only'");
            yield return new Story("typed", "Typed confirmation", "For destructive actions the name has to be typed; anything else keeps the prompt open.",
                "\"FS0\" Enter \"1\" Enter", 90, 10, true, false, ConfirmRemoval, nameof(ConfirmRemoval),
                "if (Read-ConsoleConfirm 'Remove FS01 from the inventory?' -TypedConfirmation 'FS01') { Remove-Server FS01 }");
            yield return new Story("shell", "A menu as an app", "The Shell layout: a breadcrumb title bar, a sidebar, the content pane and a footer that lists the keys that work right now. Ctrl+K opens the command palette, and the review shows the equivalent command line.",
                "wait:1600 Enter Space Down Space Down Down Down Space Escape Down Enter Down Down Down Down wait:300 Escape Ctrl+K \"cert\" wait:300 Escape Escape r wait:300 Ctrl+Q y", 120, 34, false, true, RunToolbox, nameof(RunToolbox),
                "Show-ConsoleMenu $menu -Layout Shell", new[] { "SQL01", "3 of 11 selected", "› cert", "Equivalent command" });
            yield return new Story("wizard", "A form as a wizard", "Fields with pages become a wizard with a progress strip. Dependent fields reload their choices with a spinner, and the review page shows every answer and the equivalent command.",
                "PageDown Enter Escape PageDown Right Down \"svc-toolbox\" Down \"Example-Pass1\" PageDown Right wait:400 Down wait:1800 Enter Enter PageDown wait:300 Escape Escape Escape y", 110, 30, false, true, RunInstaller, nameof(RunInstaller),
                "$answers = Read-ConsoleForm $form -AppName 'Agent setup'", new[] { "3 of 6 selected", "Loading…", "Equivalent command" });
            yield return new Story("grid", "The grid", "An Out-GridView for the console on 5.1 and 7: typing filters, F3 sorts by column, Space marks rows, Enter shows every column of a row and Ctrl+Enter returns the original objects.",
                "\"sql\" wait:200 Backspace Backspace Backspace F3 Down Space Down Space Enter wait:200 Escape Ctrl+Enter", 110, 22, false, false, BrowseServices, nameof(BrowseServices),
                "Get-Service | Show-ConsoleGrid -Property Name, Status, StartType -Title 'Services' | Restart-Service -WhatIf");
        }

        private static void SelectServer(InteractiveOptions options) {
            // <example>
            SelectPrompt<ToolboxServer> prompt = new SelectPrompt<ToolboxServer>("Target server", ToolboxData.Servers, s => s.Name, s => s.Site) {
                Description = "Type to filter; offline servers show why they cannot be picked."
            };
            foreach (Choice choice in prompt.Choices) {
                ToolboxServer server = (ToolboxServer)choice.Value!;
                choice.Badge = RichText.Plain(server.Role, new RichStyle(ThemeToken.Muted));
                choice.DisabledReason = server.Offline;
            }
            ToolboxServer target = prompt.Show(options);
            // </example>
        }

        private static void SelectTasks(InteractiveOptions options) {
            // <example>
            IReadOnlyList<ToolboxTask> tasks = new MultiSelectPrompt<ToolboxTask>("Tasks", ToolboxData.Tasks, t => t.Name, t => t.Category) {
                Minimum = 1,
                Preview = ToolboxData.TaskPreview
            }.Show(options);
            // </example>
        }

        private static void AskShareName(InteractiveOptions options) {
            // <example>
            string name = new TextPrompt("Share name") {
                Default = "Reports",
                Pattern = "[A-Za-z][A-Za-z0-9_-]{1,23}",
                PatternMessage = "Start with a letter; letters, digits, - and _ only",
                Description = "Shown to users when they browse the server."
            }.Show(options);
            // </example>
        }

        private static void ConfirmRemoval(InteractiveOptions options) {
            // <example>
            bool remove = new TypedConfirmPrompt("Remove FS01 from the inventory?", "FS01") {
                Warning = "Its history and reports are deleted too. This cannot be undone."
            }.Show(options);
            // </example>
        }

        private static void RunToolbox(InteractiveOptions options) {
            // <example>
            Menu menu = ServerToolbox.Build();
            menu.Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = options, StorageRoot = Path.Combine(Path.GetTempPath(), "cmsite-toolbox") });
            // </example>
        }

        private static void RunInstaller(InteractiveOptions options) {
            // <example>
            FormResult result = SetupWizard.Build().Show(new FormOptions { Interactive = options });
            // </example>
        }

        private static void BrowseServices(InteractiveOptions options) {
            // <example>
            ConsoleMonster.Interactive.Grid grid = new ConsoleMonster.Interactive.Grid("Services on FS01");
            grid.AddColumn(new TableColumn("Name") { IsKey = true });
            grid.AddColumn(new TableColumn("Status") { Presenter = CellPresenters.Status });
            grid.AddColumn("Start type");
            grid.AddColumn(new TableColumn("Memory") { Presenter = CellPresenters.Bytes });
            grid.AddRow("Spooler", "Spooler", StatusKind.Pass, "Automatic", 14_221_312L);
            grid.AddRow("MSSQLSERVER", "MSSQLSERVER", StatusKind.Pass, "Automatic", 812_466_176L);
            grid.AddRow("SQLSERVERAGENT", "SQLSERVERAGENT", StatusKind.Warn, "Manual", 42_004_480L);
            grid.AddRow("W32Time", "W32Time", StatusKind.Pass, "Automatic", 3_276_800L);
            grid.AddRow("WinRM", "WinRM", StatusKind.Pass, "Automatic", 9_830_400L);
            grid.AddRow("DFSR", "DFSR", StatusKind.Fail, "Automatic", 0L);
            grid.AddRow("LanmanServer", "LanmanServer", StatusKind.Pass, "Automatic", 6_553_600L);
            GridResult picked = grid.Show(new GridOptions { Interactive = options });
            // </example>
        }

        private static ScreenCapture Capture(Story story) {
            ScreenCapture capture = new ScreenCapture(story.Width, story.Height, story.Inline);
            InteractiveOptions options = new InteractiveOptions {
                Mode = InteractionMode.Rich,
                Input = new ScriptedInput(story.Keys) { Paced = true, RealTime = story.RealTime },
                Capture = capture,
                Theme = Theme.Dark,
                Labels = new PromptLabels(),
                Profile = TerminalProfile.Fixed(story.Width, story.Height, ColorDepth.TrueColor, GlyphTier.Full).WithCulture(Site.Culture)
            };
            try {
                story.Run(options);
            } catch (Exception ex) when (ex is InputClosedException || ex is PromptCancelledException || ex is OperationCanceledException) {
                // The script ends the prompt; what was drawn is kept in the capture.
            }
            return capture;
        }

        public static void Render(Site site) {
            string source = Path.Combine(site.Tool, "Pages", "InteractivePage.cs");
            JsonArray list = new JsonArray();
            foreach (Story story in Stories()) {
                List<CellBuffer> screens = Screens(story, Capture(story));
                if (screens.Count == 0) {
                    WidgetsPage.Report(new List<string> { story.Id + " drew no screens" }, "interactive");
                    continue;
                }
                List<TimedFrame> frames = new List<TimedFrame>();
                long step = story.Pick != null ? 2600 : 900;
                for (int i = 0; i < screens.Count; i++) {
                    frames.Add(new TimedFrame(i * step, screens[i]));
                }
                JsonObject timeline = Timeline.Encode(site, frames, ColorDepth.TrueColor, Screen.Dark, 2400, out string poster);
                site.WriteJson(site.Static("replays/interactive-" + story.Id + ".json"), timeline);
                // A prompt ends by leaving its one-line answer; the page shows the screen before that without JavaScript.
                if (story.Inline && screens.Count > 1) {
                    poster = FrameHtml.Body(FrameHtml.Rows(screens[screens.Count - 2], ColorDepth.TrueColor, Screen.Dark, site.Styles), false);
                }
                list.Add(new JsonObject {
                    ["id"] = story.Id,
                    ["title"] = story.Title,
                    ["summary"] = story.Summary,
                    ["keys"] = story.Keys,
                    ["csharp"] = SourceSnippets.Get(source, story.Method),
                    ["powershell"] = story.PowerShell,
                    ["player"] = new JsonObject {
                        ["url"] = "/replays/interactive-" + story.Id + ".json",
                        ["w"] = story.Width,
                        ["h"] = story.Height,
                        ["duration"] = timeline["duration"]!.GetValue<long>(),
                        ["frames"] = timeline["count"]!.GetValue<int>(),
                        ["poster"] = new JsonObject { ["w"] = story.Width, ["h"] = timeline["h"]!.GetValue<int>(), ["dark"] = poster }
                    }
                });
            }
            site.WriteJson(site.Data("interactive"), new JsonObject { ["stories"] = list });
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
                    if (found != null) {
                        screens.Add(found);
                    }
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

        private static CellBuffer? _thumbnail;

        /// <summary>The menu shell's task screen, for the navigation thumbnail.</summary>
        public static CellBuffer? Thumbnail(Site site, ITheme theme) {
            if (_thumbnail == null) {
                foreach (Story story in Stories()) {
                    if (story.Id == "shell") {
                        List<CellBuffer> screens = Screens(story, Capture(story));
                        _thumbnail = screens.Count > 1 ? screens[1] : screens.Count > 0 ? screens[0] : null;
                    }
                }
            }
            return _thumbnail;
        }
    }
}
