using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>
    /// The app parts docs page: one small app per part (<c>Website/examples/docs/app-&lt;part&gt;.cs</c> and its PowerShell
    /// twin), driven by scripted keys and clicks that live here, never in the shown programs. Each figure is the screen
    /// the keys leave, in both themes, and the PowerShell 5.1 and 7 captures must draw the same screens as the program.
    /// </summary>
    internal static class AppPartFigures {
        private sealed record Part(string Id, string Title, string Label, string Keys, int Width, int Height, string? Pick = null, bool RealTime = false, string Area = "docs") {
            internal (string Ready, string Keys)[]? Steps { get; init; }
        }

        private static readonly Part[] Parts = {
            new("app-splitter", "Splitter", "Two panes after dragging the divider to the right", "drag:24,2,36,2 Escape", 80, 6),
            new("app-accordion", "Accordion", "Tab and Enter open Parameters; the accordion folds Synopsis", "Tab Enter Escape", 70, 8),
            new("app-outline", "Outline and breadcrumb", "Down twice and Enter: Linux is current and the breadcrumb follows", "Down Down Enter Escape", 60, 8),
            new("app-tabs", "Tabs", "A click on the Disks tab", "click:14,0 Escape", 60, 5),
            new("app-chartview", "ChartView with LinkKey", "Down in the grid moves the chart's crosshair to the same sample", "Down*4 Escape", 90, 12),
            new("app-find", "Find bar", "Slash, a query and Enter: the second of two matches", "/ \"timeout\" Enter Escape Escape Escape", 70, 12, "2 of 2") {
                Steps = new[] { ("Deploy complete", "/ \"timeout\" Enter"), ("2 of 2", "Escape Escape Escape") }
            },
            new("tree", "TreeSelect", "Right expands, typing filters, a double-click opens SQL11 and Down picks Patched", "Right Down*4 Right \"sql\" dclick:8,3 Down Down Escape", 90, 16, Area: "interactive") {
                Steps = InteractivePage.TreeSteps
            }
        };

        private static string ProgramPath(Site site, Part part) => Path.Combine(site.Website, "examples", part.Area, part.Id + ".cs");

        private static string ScriptPath(Site site, Part part) => Path.Combine(site.Website, "examples", part.Area, part.Id + ".ps1");

        private static InteractivePage.Story Story(Part part, string theme) =>
            new InteractivePage.Story(part.Id + "-" + theme, part.Title, string.Empty, part.Keys, part.Width, part.Height, false, part.RealTime) {
                Area = part.Area, ExampleId = part.Id, Theme = theme == "light" ? Theme.Light : Theme.Dark, Steps = part.Steps
            };

        /// <summary>Records each part's PowerShell twin, dark and light, on every available host.</summary>
        public static void Capture(Site site) {
            foreach (Part part in Parts) {
                foreach (string theme in new[] { "dark", "light" }) { InteractivePage.CaptureExample(site, Story(part, theme)); }
            }
        }

        private static ScreenCapture Run(Site site, Part part, ITheme theme, bool fluent = false) {
            ScreenCapture capture = new ScreenCapture(part.Width, part.Height);
            TerminalProfile profile = CSharpRunner.Profile(part.Width, part.Height, ColorDepth.TrueColor, GlyphTier.Full);
            ScriptedInput input = new ScriptedInput(part.Steps == null ? part.Keys : string.Empty) { Paced = true, RealTime = part.RealTime };
            foreach ((string ready, string keys) in part.Steps ?? Array.Empty<(string, string)>()) {
                input.WaitForOutput(capture, ready, TimeSpan.FromSeconds(10));
                input.Append(keys);
            }
            CSharpRunner.Execute(ProgramPath(site, part), profile, theme, new InteractiveOptions {
                Mode = InteractionMode.Rich, Input = input, Capture = capture, Profile = profile, Theme = theme, Labels = new PromptLabels()
            }, fluent: fluent);
            if (input.Remaining > 0) { throw new InvalidOperationException(part.Id + ".cs ended with " + input.Remaining + " scripted keys unused."); }
            return capture;
        }

        // The screen a figure shows: the last one the keys left, or the last one showing a phrase.
        private static CellBuffer Frame(Site site, Part part, ITheme theme, bool fluent = false) {
            ScreenCapture capture = Run(site, part, theme, fluent);
            CellBuffer? frame = part.Pick == null ? capture.LastFrame : capture.Frames.LastOrDefault(f => f.ToPlainText().Contains(part.Pick, StringComparison.Ordinal));
            return frame ?? throw new InvalidOperationException(part.Id + ": no screen" + (part.Pick == null ? string.Empty : " shows \"" + part.Pick + "\"") + "; the keys or the program changed.");
        }

        private static bool Check(Site site, Part part, string theme) {
            List<string> problems = new List<string>();
            bool same = InteractivePage.CheckExample(site, Story(part, theme), problems);
            WidgetsPage.Report(problems, part.Id + "/" + theme);
            return same;
        }

        // The generated fluent program, given the same keys, must leave the same screen as the initializer program.
        private static bool SameFluent(Site site, Part part, CellBuffer expected) {
            ParityResult result = Parity.Compare(expected, Frame(site, part, Theme.Dark, fluent: true));
            if (!result.Identical) { WidgetsPage.Report(new List<string> { part.Id + ": C# fluent " + result.Detail }, part.Id + "/fluent"); }
            return result.Identical;
        }

        public static void Render(Site site) {
            foreach (Part part in Parts) {
                CellBuffer dark = Frame(site, part, Theme.Dark);
                CellBuffer light = Frame(site, part, Theme.Light);
                JsonObject figure = Figures.WithProgram(site, new JsonObject {
                    ["title"] = part.Title,
                    ["file"] = part.Id + ".ps1",
                    ["label"] = part.Label,
                    ["meta"] = new JsonArray(part.Width + "×" + dark.Height, "24-bit", "Full"),
                    ["frame"] = site.Frame(dark, light),
                    ["keys"] = part.Keys
                }, ProgramPath(site, part));
                (string script, string scriptFull) = SourceSnippets.Program(Site.ReadText(ScriptPath(site, part)));
                figure["script"] = script;
                figure["scriptFull"] = scriptFull;
                figure["parity"] = new JsonObject { ["dark"] = Check(site, part, "dark"), ["light"] = Check(site, part, "light"), ["fluent"] = SameFluent(site, part, dark) };
                DocCards.Offer(part.Id, dark, Frames.Screen.Dark);
                Figures.Add(part.Id, figure);
            }
        }

        /// <summary>Writes every distinct screen of each part as text, for placing keys and clicks.</summary>
        public static int Review(Site site, string output) {
            Directory.CreateDirectory(output);
            foreach (Part part in Parts) {
                ScreenCapture capture = Run(site, part, Theme.Dark);
                List<string> screens = new List<string>();
                foreach (CellBuffer frame in capture.Frames) {
                    string text = frame.ToPlainText().Replace("\r\n", "\n").TrimEnd();
                    if (screens.Count == 0 || screens[screens.Count - 1] != text) { screens.Add(text); }
                }
                File.WriteAllText(Path.Combine(output, part.Id + ".txt"), string.Join("\n\f\n", screens) + "\n");
                Console.WriteLine(part.Id + ": " + screens.Count + " screens");
            }
            return 0;
        }
    }
}
