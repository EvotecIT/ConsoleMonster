using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// Exports the showcases to HTML, SVG and text: key screens of the interactive apps (driven by scripted keys into a
    /// <see cref="ScreenCapture"/>), the parallel-run dashboard, the tables and the widget gallery, and the recorded run as
    /// an HTML player, an animated SVG and asciicast.
    /// </summary>
    public static class ExportShowcase {
        /// <summary>Writes every export into a folder.</summary>
        /// <param name="folder">The output folder.</param>
        /// <returns>The files written.</returns>
        public static List<string> Run(string folder) {
            Directory.CreateDirectory(folder);
            List<string> written = new List<string>();

            // Interactive screens: frames rendered with the dark theme, exported with the dark palette.
            ScreenCapture toolbox = Drive(ServerToolbox.Build(), MenuLayout.Shell, 140, 36,
                "wait:1600 Enter Space Down Space Down Down Down Space Escape Down Enter Down Down Down Down wait:300 Escape Ctrl+K \"cert\" wait:300 Escape Escape r wait:300 Ctrl+Q y");
            Frame(written, folder, "toolbox-tasks", Pick(toolbox, "3 of 11 selected", "Certificate expiry"), "Server toolbox · Tasks");
            Frame(written, folder, "toolbox-palette", Pick(toolbox, "Search items, actions and presets", "› cert"), "Server toolbox · Command palette");
            Frame(written, folder, "toolbox-review", Pick(toolbox, "Equivalent command", "Review & run"), "Server toolbox · Review & run");

            ScreenCapture blocked = Drive(ServerToolbox.Build(), MenuLayout.Shell, 140, 36, "wait:1600 r wait:300 Ctrl+Q y");
            Frame(written, folder, "toolbox-review-blocked", Pick(blocked, "Run · 1 problem"), "Server toolbox · Review with a problem");

            ScreenCapture narrow = Drive(ServerToolbox.Build(), MenuLayout.Shell, 76, 30, "wait:1600 k wait:300 Ctrl+Q y");
            Frame(written, folder, "toolbox-narrow", Pick(narrow, "Server toolbox › Tasks", "Disk cleanup"), "Server toolbox · narrow window");

            ScreenCapture classic = Drive(ServerToolbox.Build(), MenuLayout.Classic, 100, 30, "wait:600 Ctrl+Q y");
            Frame(written, folder, "toolbox-classic", classic.Frames.Count > 0 ? classic.Frames[classic.Frames.Count > 1 ? 1 : 0] : null, "Server toolbox · Classic");

            ScreenCapture installer = DriveForm(SetupWizard.Build(), 110, 32, "PageDown Enter Escape PageDown Right Down \"svc-toolbox\" Down \"Example-Pass1\" PageDown Right wait:400 Down wait:1800 Enter Enter PageDown wait:300 Escape Escape Escape y");
            Frame(written, folder, "installer-components", Pick(installer, "3 of 6 selected", "Agent service"), "Install Toolbox Agent · Components");
            Frame(written, folder, "installer-loading", installer.FindFrame("Loading…"), "Install Toolbox Agent · dependent field loading");
            Frame(written, folder, "installer-review", installer.FindFrame("Equivalent command"), "Install Toolbox Agent · Review");

            ScreenCapture prompt = new ScreenCapture(110, 20, inline: true);
            MultiSelectPrompt<ToolboxTask> tasks = new MultiSelectPrompt<ToolboxTask>("Tasks", ToolboxTaskChoices()) { Preview = ToolboxData.TaskPreview };
            try {
                tasks.Show(Options(prompt, "Down Space Down Down Down Escape"));
            } catch (PromptCancelledException) {
            }
            Frame(written, folder, "prompt-multiselect", prompt.FindFrame("Certificate expiry") != null ? Pick(prompt, "of 11 selected", "Certificate expiry") : prompt.LastFrame, "Multi-select with preview");

            // Static widgets: both themes, following the viewer's preference.
            Widget(written, folder, "dashboard", Dashboard(), new ExportOptions { Width = 120, Height = 36, WindowChrome = true, Title = "Parallel run", Elapsed = TimeSpan.FromMilliseconds(LastEventTime()) });
            Widget(written, folder, "tables", TableShowcases.All(), new ExportOptions { Width = 110, WindowChrome = true, Title = "Tables" });
            Widget(written, folder, "gallery", WidgetGallery.Build(), new ExportOptions { Width = 110, WindowChrome = true, Title = "Widget gallery" });

            // The recorded run.
            RecordingExportOptions run = new RecordingExportOptions { Width = 120, Height = 36, FramesPerSecond = 4, WindowChrome = true, Title = "Parallel run (recording)", Timestamp = new DateTimeOffset(2026, 10, 16, 14, 30, 0, TimeSpan.Zero) };
            List<LiveEvent> events = ParallelRunRecording.Generate();
            Save(written, Path.Combine(folder, "recording-player.html"), Recording.Export(ParallelRun.Build(out _), events, ExportFormat.Html, run));
            Save(written, Path.Combine(folder, "recording-animated.svg"), Recording.Export(ParallelRun.Build(out _), events, ExportFormat.Svg, run));
            RecordingExportOptions compact = new RecordingExportOptions { Width = 120, Height = 36, FramesPerSecond = 2, WindowChrome = true, Title = "Parallel run (recording, compact)" };
            Save(written, Path.Combine(folder, "recording-animated-compact.svg"), Recording.Export(ParallelRun.Build(out _), events, ExportFormat.Svg, compact));
            Save(written, Path.Combine(folder, "recording.cast"), Recording.Export(ParallelRun.Build(out _), events, ExportFormat.Asciicast, run));
            return written;
        }

        private static long LastEventTime() {
            long last = 0;
            foreach (LiveEvent e in ParallelRunRecording.Generate()) {
                last = Math.Max(last, e.Time);
            }
            return last;
        }

        private static IEnumerable<Choice> ToolboxTaskChoices() {
            foreach (ToolboxTask task in ToolboxData.Tasks) {
                yield return new Choice(task, task.Name) { Group = task.Category, Description = task.Description, Selected = task == ToolboxData.Tasks[3] };
            }
        }

        // The parallel-run dashboard after the whole recording was applied (a finished run).
        private static IWidget Dashboard() {
            Dashboard dashboard = ParallelRun.Build(out _);
            Dictionary<string, ILiveTarget> targets = new Dictionary<string, ILiveTarget>(StringComparer.Ordinal);
            Collect(dashboard, targets);
            foreach (LiveEvent e in ParallelRunRecording.Generate()) {
                if (targets.TryGetValue(e.Target, out ILiveTarget? target)) {
                    target.Apply(e);
                }
            }
            return dashboard;
        }

        private static void Collect(IWidget widget, Dictionary<string, ILiveTarget> targets) {
            if (widget is ILiveTarget target && !targets.ContainsKey(target.Name)) {
                targets.Add(target.Name, target);
            }
            if (widget is IContainer container) {
                foreach (IWidget child in container.Children) {
                    Collect(child, targets);
                }
            }
        }

        private static InteractiveOptions Options(ScreenCapture capture, string keys) {
            return new InteractiveOptions {
                Mode = InteractionMode.Rich,
                Input = new ScriptedInput(keys) { RealTime = true, Paced = true },
                Capture = capture,
                Theme = Theme.Dark,
                Profile = Capabilities.TerminalProfile.Fixed(capture.Width, capture.Height, Capabilities.ColorDepth.TrueColor, Capabilities.GlyphTier.Full)
            };
        }

        private static ScreenCapture Drive(Menu menu, MenuLayout layout, int width, int height, string keys) {
            ScreenCapture capture = new ScreenCapture(width, height);
            try {
                menu.Run(new MenuOptions { Layout = layout, Interactive = Options(capture, keys), StorageRoot = Path.Combine(Path.GetTempPath(), "cm-export-" + Guid.NewGuid().ToString("N").Substring(0, 8)) });
            } catch (Exception ex) when (ex is InputClosedException || ex is PromptCancelledException || ex is OperationCanceledException) {
            }
            return capture;
        }

        private static ScreenCapture DriveForm(Form form, int width, int height, string keys) {
            ScreenCapture capture = new ScreenCapture(width, height);
            try {
                form.Show(new FormOptions { Interactive = Options(capture, keys) });
            } catch (Exception ex) when (ex is InputClosedException || ex is PromptCancelledException || ex is OperationCanceledException) {
            }
            return capture;
        }

        // The last frame that shows every phrase (the screen once loading and typing settled).
        private static CellBuffer? Pick(ScreenCapture capture, params string[] phrases) {
            CellBuffer? found = null;
            foreach (CellBuffer frame in capture.Frames) {
                string text = frame.ToPlainText();
                bool all = true;
                foreach (string phrase in phrases) {
                    if (text.IndexOf(phrase, StringComparison.Ordinal) < 0) {
                        all = false;
                        break;
                    }
                }
                if (all) {
                    found = frame;
                }
            }
            return found;
        }

        private static void Frame(List<string> written, string folder, string name, CellBuffer? frame, string title) {
            if (frame == null) {
                Console.Error.WriteLine("No frame for " + name);
                return;
            }
            ExportOptions options = new ExportOptions { Theme = ExportTheme.Dark, WindowChrome = true, Title = title };
            Save(written, Path.Combine(folder, name + ".html"), ConsoleExporter.ToHtml(frame, options));
            Save(written, Path.Combine(folder, name + ".svg"), ConsoleExporter.ToSvg(frame, options));
            Save(written, Path.Combine(folder, name + ".txt"), ConsoleExporter.ToText(frame));
        }

        private static void Widget(List<string> written, string folder, string name, IWidget widget, ExportOptions options) {
            Save(written, Path.Combine(folder, name + ".html"), ConsoleExporter.ToHtml(widget, options));
            Save(written, Path.Combine(folder, name + ".svg"), ConsoleExporter.ToSvg(widget, options));
            Save(written, Path.Combine(folder, name + ".txt"), ConsoleExporter.ToText(widget, options));
        }

        private static void Save(List<string> written, string path, string content) {
            File.WriteAllText(path, content, new UTF8Encoding(false));
            written.Add(path);
        }
    }
}
