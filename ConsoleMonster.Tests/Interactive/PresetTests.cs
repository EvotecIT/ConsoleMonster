using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;
using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Interactive
{
    /// <summary>
    /// Built-in presets: declared by the app for a menu (selections and form values) or for a form, listed before the saved
    /// ones and marked as built in, applied the same way, and never overwritten or deleted.
    /// </summary>
    public sealed class PresetTests
    {
        private static string Root() => Path.Combine(Path.GetTempPath(), "cm-builtin-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        private static void Clean(string root)
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }

        private static RecordingSurface Run(Menu menu, string keys, string root, int width = 100, int height = 26)
        {
            RecordingSurface surface = new RecordingSurface(width, height, false);
            PromptHarness.Guard(surface, () =>
            {
                menu.Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = PromptHarness.Options(keys, surface), StorageRoot = root });
                return 0;
            });
            return surface;
        }

        // A menu with a selection, a form, the presets list and a review that can save a preset, plus one built-in preset.
        private static Menu Toolbox(out MenuItem targets, out Form options)
        {
            Menu menu = new Menu("Toolbox");
            MenuItem selection = new MenuItem("Targets").Hotkey('t').MultiSelect(new[] { "SRV01", "SRV02", "WEB01" }, s => s);
            menu.Add(selection);
            Form form = new Form("Options")
            {
                Review = false
            };
            TextField folder = form.Text("Folder", "Report folder");
            folder.Default = @"C:\Reports";
            NumberField depth = form.Number("Depth", "Depth");
            depth.Default = 1m;
            menu.Item("Options", i => i.Hotkey('o').Form(form));
            menu.Item("Presets", i => i.Hotkey('p').Presets());
            menu.Item("Review", i =>
            {
                i.Hotkey = 'r';
                i.Review = ctx =>
                {
                    ReviewModel review = new ReviewModel();
                    review.Row("Targets", string.Join(", ", selection.GetSelection<string>()));
                    review.Run = c =>
                    {
                    };
                    return review;
                };
            });
            menu.Exit("Quit");
            menu.Preset("Nightly", p => p.Description("Every web server at depth 3.").Selection(selection, "SRV02", "WEB01").Form(form, new Dictionary<string, object?> { ["Folder"] = @"D:\Nightly", ["Depth"] = 3 }));
            targets = selection;
            options = form;
            return menu;
        }

        [Fact]
        public void MenuPresets_BuiltInFirstAndMarked_ApplyLikeSavedOnes()
        {
            string root = Root();
            try
            {
                new ChoiceStore("Toolbox", root).SavePreset("menu", "Alpha", new Dictionary<string, object?> { ["selections"] = new Dictionary<string, object?> { ["Targets"] = new List<object?> { "SRV01" } } });
                Menu menu = Toolbox(out MenuItem targets, out Form options);
                RecordingSurface surface = Run(menu, "p Enter Ctrl+Q", root);
                string list = surface.Frames.Select(f => f.ToPlainText()).First(f => f.Contains("Alpha"));
                Assert.True(list.IndexOf("Nightly", StringComparison.Ordinal) < list.IndexOf("Alpha", StringComparison.Ordinal), "The built-in preset is not listed first:\n" + list);
                string nightly = list.Split('\n').First(l => l.Contains("Nightly") && !l.Contains("Every web server"));
                Assert.Contains("built in", nightly);
                Assert.DoesNotContain("built in", list.Split('\n').First(l => l.Contains("Alpha")));
                Assert.Contains("Every web server at depth 3.", list);
                Assert.Equal(new[] { "SRV02", "WEB01" }, targets.GetSelection<string>().ToArray());
                Assert.Equal(@"D:\Nightly", options.Find("Folder")!.Value);
                Assert.Equal(3m, options.Find("Depth")!.Value);
                Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Preset 'Nightly' applied"));
            }
            finally
            {
                Clean(root);
            }
        }

        [Fact]
        public void MenuPresets_InlineLayout_ListsAndAppliesThemLikeTheShell()
        {
            string root = Root();
            try
            {
                new ChoiceStore("Toolbox", root).SavePreset("menu", "Alpha", new Dictionary<string, object?> { ["selections"] = new Dictionary<string, object?> { ["Targets"] = new List<object?> { "SRV01" } } });
                Menu menu = Toolbox(out MenuItem targets, out Form options);
                RecordingSurface surface = new RecordingSurface(100, 26, true);
                PromptHarness.Guard(surface, () =>
                {
                    menu.Run(new MenuOptions { Layout = MenuLayout.Inline, Interactive = PromptHarness.Options("p Enter End Enter", surface), StorageRoot = root });
                    return 0;
                });
                string list = surface.Frames.Select(f => f.ToPlainText()).First(f => f.Contains("Alpha"));
                Assert.True(list.IndexOf("Nightly", StringComparison.Ordinal) < list.IndexOf("Alpha", StringComparison.Ordinal), "The built-in preset is not listed first:\n" + list);
                Assert.Contains("built in", list.Split('\n').First(l => l.Contains("Nightly")));
                Assert.DoesNotContain("built in", list.Split('\n').First(l => l.Contains("Alpha")));
                Assert.Equal(new[] { "SRV02", "WEB01" }, targets.GetSelection<string>().ToArray());
                Assert.Equal(@"D:\Nightly", options.Find("Folder")!.Value);
                Assert.Equal(3m, options.Find("Depth")!.Value);
            }
            finally
            {
                Clean(root);
            }
        }

        [Fact]
        public void MenuPresets_InlineLayoutWithoutPresets_SaysSoAndReturnsToTheMenu()
        {
            string root = Root();
            try
            {
                Menu menu = new Menu("Toolbox");
                menu.Item("Presets", i => i.Hotkey('p').Presets());
                menu.Exit("Quit");
                RecordingSurface surface = new RecordingSurface(100, 26, true);
                RecordingTarget printed = ConsoleMonster.Testing.TestTerminal.Target(100);
                using (Terminal.Use(printed))
                {
                    PromptHarness.Guard(surface, () =>
                    {
                        menu.Run(new MenuOptions { Layout = MenuLayout.Inline, Interactive = PromptHarness.Options("p End Enter", surface), StorageRoot = root });
                        return 0;
                    });
                }

                Assert.Contains(new PromptLabels().NoPresets, printed.Text);
            }
            finally
            {
                Clean(root);
            }
        }

        [Fact]
        public void MenuPresets_WithPreview_ShowsItWhileFocused()
        {
            string root = Root();
            try
            {
                Menu menu = Toolbox(out _, out _);
                menu.Presets[0].Preview(new ConsoleMonster.Widgets.TextBlock("Depth 3 on SRV02 and WEB01"));
                RecordingSurface surface = Run(menu, "p Enter Ctrl+Q", root);
                Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Nightly") && f.ToPlainText().Contains("Depth 3 on SRV02 and WEB01"));
            }
            finally
            {
                Clean(root);
            }
        }

        [Fact]
        public void MenuPresets_BuiltInToAnOpenedForm_UpdatesItsShownText()
        {
            string root = Root();
            try
            {
                Menu menu = Toolbox(out _, out _);
                // Open the form, go back to the sidebar, apply the preset, then look at the form again.
                RecordingSurface surface = Run(menu, "o Escape p Enter Escape Up Ctrl+Q y", root);
                string form = surface.Frames.Select(f => f.ToPlainText()).Last(f => f.Contains("Report folder"));
                Assert.Contains(@"D:\Nightly", form);
                Assert.Contains("3", form.Split('\n').First(l => l.Contains("Depth")));
            }
            finally
            {
                Clean(root);
            }
        }

        [Fact]
        public void MenuPresets_BuiltInBeforeTheFormOpens_WinsOverRememberedChoices()
        {
            string root = Root();
            try
            {
                new ChoiceStore("Toolbox", root).SaveLast("Options", new Dictionary<string, object?> { ["Folder"] = @"C:\Remembered" });
                Menu menu = Toolbox(out _, out Form options);
                RecordingSurface surface = new RecordingSurface(100, 26, false);
                // Apply the preset first, then open the form for the first time (it loads the remembered choices then).
                PromptHarness.Guard(surface, () =>
                {
                    menu.Run(new MenuOptions { Layout = MenuLayout.Shell, RememberLastChoices = true, Interactive = PromptHarness.Options("p Enter Escape Up Ctrl+Q", surface), StorageRoot = root });
                    return 0;
                });
                Assert.Contains(@"D:\Nightly", surface.Frames.Select(f => f.ToPlainText()).Last(f => f.Contains("Report folder")));
                Assert.Equal(@"D:\Nightly", options.Find("Folder")!.Value);
            }
            finally
            {
                Clean(root);
            }
        }

        [Fact]
        public void FormPresets_ConfigureOverload_SetsValuesDescriptionAndPreview()
        {
            Form form = new Form("Install agent");
            form.Text("Path", "Install folder");
            ConsoleMonster.Widgets.TextBlock preview = new ConsoleMonster.Widgets.TextBlock("Installs to D:\\Weekly");
            form.Preset("Weekly", p => p.Value("path", @"D:\Weekly").Description("The weekly install.").Preview(preview));
            FormPreset preset = Assert.Single(form.Presets);
            Assert.Equal(@"D:\Weekly", preset.Values["Path"]);
            Assert.Equal("The weekly install.", preset.Description);
            Assert.Same(preview, preset.Preview);
            RecordingSurface surface = new RecordingSurface(80, 24, false);
            FormResult result = PromptHarness.Guard(surface, () => form.Show(new FormOptions { Preset = "weekly", Interactive = PromptHarness.Options("PageDown Enter", surface) }));
            Assert.Equal(@"D:\Weekly", result.Values["Path"]);
            Assert.Equal("Weekly", result.Preset);
        }

        [Fact]
        public void MenuPresets_BuiltIn_CannotBeDeletedOrSavedOver()
        {
            string root = Root();
            try
            {
                Menu menu = Toolbox(out _, out _);
                RecordingSurface surface = Run(menu, "p Delete Escape t Space Escape r Right Enter \"nightly\" Enter Ctrl+Q", root);
                string presets = surface.Frames.Select(f => f.ToPlainText()).First(f => f.Contains("Nightly"));
                Assert.DoesNotContain("del delete", presets);
                Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("'Nightly' is built in and can't be changed or deleted"));
                Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("'nightly' is built in and can't be changed or deleted"));
                Assert.Empty(new ChoiceStore("Toolbox", root).PresetNames("menu"));
                Assert.Single(menu.Presets);
            }
            finally
            {
                Clean(root);
            }
        }

        private static Form Agent()
        {
            Form form = new Form("Install agent")
            {
                Command = "Install-Agent"
            };
            FormPage page = form.Page("Location");
            TextField path = page.Text("Path", "Install folder");
            path.Default = @"C:\Agent";
            path.ParameterName = "Path";
            ChoiceField site = page.MultiSelect("Site", "Sites");
            site.ParameterName = "Site";
            site.LoadChoices = (values, token) => Task.FromResult<IEnumerable<Choice>>(new[] { new Choice("waw", "Warsaw"), new Choice("ber", "Berlin") });
            form.Preset("Weekly", new Dictionary<string, object?> { ["Path"] = @"D:\Weekly", ["Site"] = new[] { "ber" } }, "The weekly install.");
            return form;
        }

        [Fact]
        public void FormPresets_BuiltInByName_AppliesIncludingLoadedOptions()
        {
            RecordingSurface surface = new RecordingSurface(80, 24, false);
            Form form = Agent();
            FormResult result = PromptHarness.Guard(surface, () => form.Show(new FormOptions { Preset = "Weekly", Interactive = PromptHarness.Options("PageDown Enter", surface) }));
            Assert.True(result.Submitted);
            Assert.Equal(@"D:\Weekly", result.Values["Path"]);
            Assert.Equal(new[] { "ber" }, result.Values.GetList<string>("Site").ToArray());
            Assert.Equal("Weekly", result.Preset);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Preset 'Weekly' applied"));
        }

        [Fact]
        public void FormPresets_SavingUnderABuiltInName_IsRefused()
        {
            string root = Root();
            try
            {
                RecordingSurface surface = new RecordingSurface(80, 24, false);
                PromptHarness.Guard(surface, () => Agent().Show(new FormOptions { AppName = "Agent app", StorageRoot = root, Interactive = PromptHarness.Options("PageDown s \"Weekly\" Enter Ctrl+C", surface) }));
                Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("'Weekly' is built in and can't be changed or deleted"));
                Assert.Empty(new ChoiceStore("Agent app", root).PresetNames("Install agent"));
            }
            finally
            {
                Clean(root);
            }
        }

        [Fact]
        public void FormPresets_Palette_ListsBuiltInOnesAndAppliesThem()
        {
            string root = Root();
            try
            {
                Form form = Agent();
                Menu menu = new Menu("Toolbox");
                menu.Item("Install", i => i.Hotkey('i').Form(form));
                menu.Exit("Quit");
                RecordingSurface surface = Run(menu, "Ctrl+K \"Weekly\" Enter Ctrl+Q y", root);
                string palette = surface.Frames.Select(f => f.ToPlainText()).Last(f => f.Contains("Commands") && f.Contains("Weekly"));
                Assert.Contains("built in", palette);
                Assert.Equal(@"D:\Weekly", form.Find("Path")!.Value);
            }
            finally
            {
                Clean(root);
            }
        }
    }
}
