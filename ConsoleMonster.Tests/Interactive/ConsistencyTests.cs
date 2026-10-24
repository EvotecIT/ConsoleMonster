using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>
    /// The design system holds across surfaces: the header bar, the footer, dialogs, the focus row and the status set are
    /// drawn by the same components and look the same on dashboards, the Shell and Classic layouts, full-screen forms, runs
    /// started from a menu and prompts, in every glyph tier.
    /// </summary>
    [Collection("Live sessions")]
    public sealed class ConsistencyTests {
        private const int Width = 90;
        private const int Height = 24;

        private static InteractiveOptions Options(ScreenCapture capture, string keys, GlyphTier tier, bool realTime = false) {
            return new InteractiveOptions {
                Mode = InteractionMode.Rich,
                Input = new ScriptedInput(keys) { Paced = true, RealTime = realTime },
                Capture = capture,
                Theme = Theme.Dark,
                Profile = TerminalProfile.Fixed(capture.Width, capture.Height, ColorDepth.TrueColor, tier),
                Labels = new PromptLabels()
            };
        }

        private static RenderContext Context(GlyphTier tier) => Terminal.CreateContext(TerminalProfile.Fixed(Width, Height, ColorDepth.TrueColor, tier), Theme.Dark);

        private static CellBuffer Bar(IWidget bar, GlyphTier tier, TimeSpan elapsed = default) {
            CellBuffer buffer = new CellBuffer(Width, 1);
            bar.Render(new Region(buffer), Context(tier).WithClock(0, elapsed));
            return buffer;
        }

        private static void SameRow(CellBuffer expected, CellBuffer actual, int row, string surface) {
            for (int x = 0; x < Width; x++) {
                Assert.True(expected[x, 0].Equals(actual[x, row]), surface + ": header differs at column " + x + " ('" + expected[x, 0].Glyph + "' vs '" + actual[x, row].Glyph + "')\n" + actual.GetRowText(row));
            }
        }

        // Every visible cell of a footer row is a key (Accent) or a label (Muted): one footer look everywhere.
        private static void FooterLook(CellBuffer frame, int row, GlyphTier tier, string surface) {
            RenderContext context = Context(tier);
            Style key = context.Style(ThemeToken.Accent);
            Style label = context.Style(ThemeToken.Muted);
            int cells = 0;
            for (int x = 0; x < frame.Width; x++) {
                Cell cell = frame[x, row];
                if (cell.IsContinuation || cell.Glyph == " ") {
                    continue;
                }
                cells++;
                Assert.True(cell.Style.Equals(key) || cell.Style.Equals(label), surface + ": footer cell '" + cell.Glyph + "' at " + x + " is not a key or a label\n" + frame.GetRowText(row));
            }
            Assert.True(cells > 4, surface + ": no footer on row " + row);
        }

        private static Menu Toolbox(List<string> log) {
            Menu menu = new Menu("Toolbox") { Subtitle = "lab" };
            MenuItem targets = new MenuItem("Targets").Hotkey('t').MultiSelect(new[] { "SRV01", "SRV02" }, s => s);
            menu.Add(targets);
            menu.Item("Patch", i => i.Hotkey('p').Action(ctx => ctx.Run(new Dashboard("Patching", new ProgressBar("Overall", "overall") { Total = 2 }), run => {
                Thread.Sleep(400);
                log.Add("ran");
                return new MenuRunResult("Done");
            })));
            menu.Exit("Quit");
            return menu;
        }

        public static IEnumerable<object[]> Tiers() {
            yield return new object[] { GlyphTier.Full };
            yield return new object[] { GlyphTier.Basic };
            yield return new object[] { GlyphTier.Ascii };
        }

        [Theory]
        [MemberData(nameof(Tiers))]
        public void HeaderBar_IsTheSameOnTheShellARunAndAForm(GlyphTier tier) {
            List<string> log = new List<string>();
            ScreenCapture shell = new ScreenCapture(Width, Height);
            Toolbox(log).Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = Options(shell, "t wait:50 Escape Down Enter wait:900 Escape Ctrl+Q", tier, true), StorageRoot = Path.Combine(Path.GetTempPath(), "cm-consistency-" + Guid.NewGuid().ToString("N").Substring(0, 6)) });
            Assert.Equal(new[] { "ran" }, log.ToArray());

            HeaderBar targets = new HeaderBar("Toolbox") { Right = "lab" };
            targets.Crumbs.Add("Targets");
            SameRow(Bar(targets, tier), shell.FindFrame("Targets")!, 0, "shell");

            // The breadcrumb says the app is running; the dashboard's title names the content panel instead.
            CellBuffer running = shell.Frames.First(f => f.GetRowText(0).Contains("Running") && f.GetRowText(Height - 1).Contains("ctrl+c"));
            HeaderBar run = new HeaderBar("Toolbox") { Right = "lab" };
            run.Crumbs.Add("Patch");
            run.Crumbs.Add("Running");
            SameRow(Bar(run, tier), running, 0, "run");
            FooterLook(running, Height - 1, tier, "run");

            ScreenCapture form = new ScreenCapture(Width, Height);
            Form f = new Form("Settings");
            f.Page("General").Text("Name", "Name");
            f.Page("Advanced").Toggle("Fast", "Fast mode");
            f.Show(new FormOptions { Interactive = Options(form, "Escape", tier) });
            HeaderBar formHeader = new HeaderBar("Settings");
            formHeader.Crumbs.Add("General");
            SameRow(Bar(formHeader, tier), form.Frames[0], 0, "form");
            FooterLook(form.Frames[0], Height - 1, tier, "form");
            FooterLook(shell.FindFrame("Targets")!, Height - 1, tier, "shell");
        }

        [Theory]
        [MemberData(nameof(Tiers))]
        public void Dashboard_UsesTheSharedHeaderAndFooter(GlyphTier tier) {
            Dashboard dashboard = new Dashboard("Patching", new ProgressBar("Overall", "overall")) { Subtitle = "5 servers", Detail = "running", Status = StatusKind.Running };
            CellBuffer frame = new CellBuffer(Width, Height);
            dashboard.Render(new Region(frame), Context(tier).WithClock(3, TimeSpan.FromSeconds(12)));
            SameRow(Bar(new HeaderBar("Patching") { Subtitle = "5 servers", Detail = "running", Status = StatusKind.Running, ShowElapsed = true }, tier, TimeSpan.FromSeconds(12)), frame, 0, "dashboard");
            FooterLook(frame, Height - 1, tier, "dashboard");
            Assert.Contains("ctrl+c stop", frame.GetRowText(Height - 1));
        }

        [Theory]
        [MemberData(nameof(Tiers))]
        public void Footer_IsTheSameOnClassicMenusAndPrompts(GlyphTier tier) {
            ScreenCapture classic = new ScreenCapture(Width, Height);
            Toolbox(new List<string>()).Run(new MenuOptions { Layout = MenuLayout.Classic, Interactive = Options(classic, "Ctrl+Q", tier), StorageRoot = Path.Combine(Path.GetTempPath(), "cm-consistency-" + Guid.NewGuid().ToString("N").Substring(0, 6)) });
            CellBuffer menu = classic.Frames[0];
            int footer = Enumerable.Range(0, Height).Last(y => menu.GetRowText(y).Contains("move"));
            FooterLook(menu, footer, tier, "classic");

            ScreenCapture prompt = new ScreenCapture(Width, 12, inline: true);
            new SelectPrompt<string>("Environment", new[] { new Choice("prod", "Production"), new Choice("lab", "Lab") }).Show(Options(prompt, "Enter", tier));
            CellBuffer select = prompt.Frames[0];
            int promptFooter = Enumerable.Range(0, select.Height).Last(y => select.GetRowText(y).Contains("move"));
            FooterLook(select, promptFooter, tier, "prompt");
        }

        [Theory]
        [MemberData(nameof(Tiers))]
        public void FocusRow_LooksTheSameInMenusFormsAndPrompts(GlyphTier tier) {
            Style focus = Context(tier).Style(ThemeToken.Focus);
            ScreenCapture shell = new ScreenCapture(Width, Height);
            Toolbox(new List<string>()).Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = Options(shell, "t wait:10 Ctrl+Q", tier), StorageRoot = Path.Combine(Path.GetTempPath(), "cm-consistency-" + Guid.NewGuid().ToString("N").Substring(0, 6)) });
            ScreenCapture prompt = new ScreenCapture(Width, 12, inline: true);
            new SelectPrompt<string>("Environment", new[] { new Choice("prod", "Production"), new Choice("lab", "Lab") }).Show(Options(prompt, "Enter", tier));
            ScreenCapture form = new ScreenCapture(Width, Height);
            Form f = new Form("Settings");
            f.Page("General").Text("Name", "Name");
            f.Show(new FormOptions { Interactive = Options(form, "Escape", tier) });
            foreach (KeyValuePair<string, CellBuffer> surface in new Dictionary<string, CellBuffer> { ["menu"] = shell.FindFrame("SRV01")!, ["prompt"] = prompt.Frames[0], ["form"] = form.Frames[0] }) {
                bool found = false;
                for (int y = 0; y < surface.Value.Height && !found; y++) {
                    for (int x = 0; x < surface.Value.Width; x++) {
                        Cell cell = surface.Value[x, y];
                        if (!cell.Style.BackgroundColor.IsDefault && cell.Style.BackgroundColor.Equals(focus.BackgroundColor) && cell.Glyph != " ") {
                            found = true;
                            break;
                        }
                    }
                }
                Assert.True(found, surface.Key + ": no focus row on the Focus background");
            }
        }

        [Theory]
        [MemberData(nameof(Tiers))]
        public void MenuIcons_FollowTheGlyphTier(GlyphTier tier) {
            ScreenCapture shell = new ScreenCapture(Width, Height);
            Menu menu = new Menu("Toolbox");
            menu.Item("Targets", i => i.Hotkey('t').WithIcon(GlyphIcon.Targets).Action(ctx => { }));
            menu.Item("Presets", i => i.Hotkey('p').WithIcon(GlyphIcon.Favorite).Action(ctx => { }));
            menu.Item("Custom", i => i.Hotkey('c').WithIcon("@").Action(ctx => { }));
            menu.Exit("Quit");
            menu.Run(new MenuOptions { Layout = MenuLayout.Shell, Interactive = Options(shell, "Ctrl+Q", tier), StorageRoot = Path.Combine(Path.GetTempPath(), "cm-consistency-" + Guid.NewGuid().ToString("N").Substring(0, 6)) });
            string text = shell.Frames[0].ToPlainText();
            IGlyphSet glyphs = GlyphSet.For(tier);
            Assert.Contains(glyphs.Icon(GlyphIcon.Targets) + " Targets", text);
            Assert.Contains(glyphs.Icon(GlyphIcon.Favorite) + " Presets", text);
            Assert.Contains("@ Custom", text);
            Assert.Equal(tier == GlyphTier.Full ? "★" : tier == GlyphTier.Basic ? "☼" : "*", glyphs.Icon(GlyphIcon.Favorite));
            if (tier == GlyphTier.Ascii) {
                foreach (GlyphIcon icon in (GlyphIcon[])Enum.GetValues(typeof(GlyphIcon))) {
                    Assert.True(glyphs.Icon(icon).Length == 1 && glyphs.Icon(icon)[0] < 128, icon + " is not ASCII");
                }
            }
        }

        [Theory]
        [MemberData(nameof(Tiers))]
        public void StatusSet_IsOneVocabularyPerTier(GlyphTier tier) {
            RenderContext context = Context(tier);
            StringBuilder sb = new StringBuilder();
            foreach (StatusKind kind in new[] { StatusKind.Pass, StatusKind.Warn, StatusKind.Fail, StatusKind.Skip, StatusKind.Running, StatusKind.Pending, StatusKind.Info }) {
                sb.Append(kind).Append(": symbol '").Append(context.Glyphs.StatusSymbol(kind)).Append("' marker '").Append(context.Glyphs.StatusMarker(kind)).Append("'\n");
            }
            StatusList list = new StatusList();
            list.Set("a", "Copy files", StatusKind.Pass);
            list.Set("b", "Register", StatusKind.Warn);
            list.Set("c", "Verify", StatusKind.Fail);
            list.Set("d", "Optional", StatusKind.Skip);
            list.Set("e", "Queued", StatusKind.Pending);
            sb.Append(ConsoleMonster.Testing.TestTerminal.RenderText(list, 40, glyphs: tier)).Append('\n');
            Golden.Match("design/status." + tier.ToString().ToLowerInvariant() + ".txt", sb.ToString());
        }
    }
}
