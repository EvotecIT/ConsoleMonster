using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Interactive
{
    /// <summary>
    /// Menus and the app shell: Shell (wide and narrow), Classic and Inline layouts, navigation with the breadcrumb, the
    /// palette, the key map, review items with problems, presets, actions and exit confirmation.
    /// </summary>
    public sealed class MenuTests
    {
        internal static Menu Toolbox(List<string> log)
        {
            Menu menu = new Menu("Toolbox")
            {
                Subtitle = "lab"
            };
            menu.Item("Targets", i => i.Hotkey('t').MultiSelect(new[] { "SRV01", "SRV02", "WEB01" }, s => s, s => s.StartsWith("SRV", StringComparison.Ordinal) ? "Servers" : "Web").Description("Where tasks run."));
            menu.Item("Mode", i => i.Hotkey('m').Select(new[] { "Check", "Fix" }));
            Form options = new Form("Options")
            {
                Review = false
            };
            TextField folder = options.Text("Folder", "Report folder");
            folder.Default = @"C:\Reports";
            menu.Item("Options", i => i.Hotkey('o').Form(options, (ctx, result) => log.Add("saved " + result.Values["Folder"])));
            menu.Item("Presets", i => i.Hotkey('p').Presets());
            menu.Separator("Run");
            menu.Item("Review", i =>
            {
                i.Hotkey = 'r';
                i.Review = ctx =>
                {
                    MenuItem targets = ctx.Menu.Find("Targets")!;
                    ReviewModel review = new ReviewModel();
                    review.Row("Targets", string.Join(", ", targets.GetSelection<string>()));
                    review.Command = CommandLine.Join("Invoke-Toolbox", new[] { CommandLine.Argument("Target", targets.GetSelection<string>().ToList(), CommandStyle.PowerShell) });
                    if (targets.Selection.Count == 0)
                    {
                        review.Problem("Pick at least one target");
                    }

                    review.Run = c => log.Add("run " + targets.Selection.Count);
                    return review;
                };
            });
            menu.Item("Clean", i =>
            {
                i.Hotkey = 'c';
                i.Action = ctx => log.Add("clean");
                i.DisabledReason = () => log.Contains("lock") ? "locked" : null;
            });
            menu.Exit("Quit");
            return menu;
        }

        private static RecordingSurface Run(Menu menu, string keys, MenuLayout layout, int width = 100, int height = 26, string? root = null)
        {
            RecordingSurface surface = new RecordingSurface(width, height, false);
            PromptHarness.Guard(surface, () =>
            {
                menu.Run(new MenuOptions { Layout = layout, Interactive = PromptHarness.Options(keys, surface), StorageRoot = root ?? Path.Combine(Path.GetTempPath(), "cm-menu-none-" + Guid.NewGuid().ToString("N").Substring(0, 6)) });
                return 0;
            });
            return surface;
        }

        [Fact]
        public void Shell_SidebarFollowsFocusAndContentOpens()
        {
            List<string> log = new List<string>();
            RecordingSurface surface = Run(Toolbox(log), "Enter Space Down Space Escape Down Ctrl+Q", MenuLayout.Shell);
            Golden.Match("interactive/menu.shell.initial.txt", surface.Frames[0].ToPlainText());
            Golden.Match("interactive/menu.shell.selection.txt", surface.Frames[4].ToPlainText());
            string last = surface.LastText();
            Assert.Contains("Toolbox › Mode", last);
            Assert.Contains("2/3", last);
        }

        [Fact]
        public void Shell_NarrowWindow_FoldsTheSidebarIntoTheBreadcrumb()
        {
            RecordingSurface surface = Run(Toolbox(new List<string>()), "Enter Ctrl+Q", MenuLayout.Shell, 70, 20);
            Golden.Match("interactive/menu.narrow.root.txt", surface.Frames[0].ToPlainText());
            Assert.Contains("Toolbox › Targets", surface.LastText());
        }

        [Fact]
        public void Classic_CentredCardWithHotkeys()
        {
            RecordingSurface surface = Run(Toolbox(new List<string>()), "o Escape Ctrl+Q", MenuLayout.Classic, 90, 24);
            Golden.Match("interactive/menu.classic.root.txt", surface.Frames[0].ToPlainText().TrimEnd('\n') + "\n");
            Assert.Contains("Toolbox › Options", surface.Frames[1].ToPlainText());
        }

        [Fact]
        public void Palette_FindsNestedItemsAndOpensThem()
        {
            RecordingSurface surface = Run(Toolbox(new List<string>()), "Ctrl+K \"rev\" Enter Ctrl+Q", MenuLayout.Shell);
            string palette = surface.Frames.Select(f => f.ToPlainText()).First(f => f.Contains("Commands"));
            Golden.Match("interactive/menu.palette.txt", surface.Frames[4].ToPlainText());
            Assert.Contains("Go to", palette);
            Assert.Contains("Toolbox › Review", surface.LastText());
        }

        [Fact]
        public void KeyMap_ListsTheActiveBindings()
        {
            RecordingSurface surface = Run(Toolbox(new List<string>()), "? Escape Ctrl+Q", MenuLayout.Shell);
            string map = surface.Frames[1].ToPlainText();
            Assert.Contains("Keys", map);
            Assert.Contains("commands", map);
            Assert.Contains("quit", map);
        }

        [Fact]
        public void Review_ProblemsBlockRunUntilFixed()
        {
            List<string> log = new List<string>();
            RecordingSurface surface = Run(Toolbox(log), "r Enter Escape t Space Escape r Enter Ctrl+Q", MenuLayout.Shell);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("✗ Pick at least one target"));
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Invoke-Toolbox -Target SRV01"));
            Assert.Equal(new[] { "run 1" }, log.ToArray());
            Golden.Match("interactive/menu.review.txt", surface.Frames.First(f => f.ToPlainText().Contains("Invoke-Toolbox -Target SRV01")).ToPlainText());
        }

        [Fact]
        public void Presets_SaveFromReviewAndApplyLater()
        {
            string root = Path.Combine(Path.GetTempPath(), "cm-menu-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try
            {
                Run(Toolbox(new List<string>()), "Enter Space Escape r Right Right Enter \"Two\" Enter Ctrl+Q", MenuLayout.Shell, root: root);
                Assert.Equal(new[] { "Two" }, new ChoiceStore("Toolbox", root).PresetNames("menu").ToArray());
                Menu fresh = Toolbox(new List<string>());
                RecordingSurface surface = Run(fresh, "p Enter Enter Ctrl+Q", MenuLayout.Shell, root: root);
                Assert.Equal(new[] { "SRV01" }, fresh.Find("Targets")!.GetSelection<string>().ToArray());
                Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Preset 'Two' applied"));
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [Fact]
        public void Action_RunsAndDisabledItemsExplainWhy()
        {
            List<string> log = new List<string>();
            Run(Toolbox(log), "c Ctrl+Q", MenuLayout.Classic);
            Assert.Equal(new[] { "clean" }, log.ToArray());
            List<string> locked = new List<string>
            {
                "lock"
            };
            RecordingSurface surface = Run(Toolbox(locked), "c Ctrl+Q", MenuLayout.Classic);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Not available: locked"));
        }

        [Fact]
        public void Exit_WithUnsavedFormChanges_AsksFirst()
        {
            RecordingSurface surface = Run(Toolbox(new List<string>()), "o \"x\" Escape Ctrl+Q n Ctrl+Q y", MenuLayout.Shell);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Discard unsaved changes and exit?"));
        }

        [Fact]
        public void Inline_StepsThroughSelectionsAndActions()
        {
            List<string> log = new List<string>();
            Menu menu = Toolbox(log);
            RecordingSurface surface = new RecordingSurface(80, 20, true);
            PromptHarness.Guard(surface, () =>
            {
                menu.Run(new MenuOptions { Layout = MenuLayout.Inline, Interactive = PromptHarness.Options("t Space Enter c End Enter", surface) });
                return 0;
            });
            Assert.Equal(new[] { "SRV01" }, menu.Find("Targets")!.GetSelection<string>().ToArray());
            Assert.Equal(new[] { "clean" }, log.ToArray());
        }

        [Fact]
        public void Run_NonInteractive_FailsClearly()
        {
            PromptUnavailableException error = Assert.Throws<PromptUnavailableException>(() => Toolbox(new List<string>()).Run(new MenuOptions { Interactive = new InteractiveOptions { Mode = InteractionMode.None } }));
            Assert.Equal("Menu 'Toolbox' needs an interactive session.", error.Message);
        }
    }
}
