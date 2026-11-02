using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Changes;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Live.Tails;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Cross-cutting behaviour at the shared rendering and application boundaries.</summary>
    [Collection("Live sessions")]
    public sealed class BehaviourContractTests {
        private const string CallerText = "Łódź 東京 ✅ café";
        private static readonly Dictionary<string, Func<IWidget>> Scenes = new Dictionary<string, Func<IWidget>> {
            ["text"] = () => new TextBlock(CallerText),
            ["code"] = () => new CodeView(CallerText),
            ["report"] = TestScenes.Report,
            ["live dashboard"] = TestScenes.Live,
            ["table"] = () => new Table().AddColumn("Value").AddValues(CallerText).AddValues(null, ""),
            ["cards"] = () => new Table { AlwaysCards = true }.AddColumn("Value").AddValues(CallerText),
            ["record"] = () => new RecordView(CallerText).AddField("Value", CallerText),
            ["inspector"] = () => new InspectorView(new { Name = CallerText, Empty = "", Null = (string?)null }),
            ["key/value"] = () => new KeyValueList().AddValue("Name", CallerText),
            ["tree"] = () => { var tree = new Tree(CallerText) { WrapLabels = true }; tree.Add(CallerText).Value = TableCell.FromValue(CallerText); return tree; },
            ["calendar"] = () => new global::ConsoleMonster.Widgets.Calendar(2026, 11),
            ["exception"] = () => new ExceptionView(new InvalidOperationException(CallerText)),
            ["header"] = () => new HeaderBar(CallerText),
            ["footer"] = () => new FooterBar(new[] { new KeyHint("Enter", CallerText) }),
            ["view header"] = () => new ViewHeader(CallerText, "Ready", CallerText),
            ["legend"] = () => new Legend(CallerText, "Ready"),
            ["facts"] = () => new FactFlow(CallerText),
            ["gauge"] = () => new ScoreGauge(73),
            ["flow"] = () => new Flow(),
            ["cells"] = () => new CellMap().Resize(5),
            ["activity"] = () => new ActivityList(),
            ["summary"] = () => new SummaryBar().SetCounter(CallerText, 3),
            ["watch"] = () => new WatchView(),
            ["region"] = () => new RegionWidget("region", new TextBlock(CallerText)),
            ["dashboard"] = () => new Dashboard(CallerText, new TextBlock(CallerText)),
            ["live table"] = () => { var live = new LiveTable(new Table().AddColumn("Value")); live.Set("first", CallerText); return live; },
            ["grid"] = () => new DataGrid("grid", new Grid(CallerText).AddColumn("Value").AddRow("first", CallerText)),
            ["text input"] = () => new TextInput("input") { Value = CallerText },
            ["button"] = () => new Button("button", CallerText),
            ["toggle"] = () => new Toggle("toggle", true),
            ["select"] = () => new Select("select", new[] { new Choice(1, CallerText) }),
            ["multiselect"] = () => new MultiSelect("select", new[] { new Choice(1, CallerText) }),
            ["date picker"] = () => new DatePicker("date", new DateTime(2026, 11, 2)),
            ["tree select"] = () => new TreeSelect("tree", new ObjectExplorer(new { Name = CallerText })),
            ["log viewport"] = () => { var log = new LogViewport("log"); log.Write(CallerText); return log; },
            ["text area"] = () => new TextArea("area") { Value = CallerText },
            ["matrix"] = () => new Matrix(new[] { new MatrixLabel(CallerText) }, new[] { new MatrixLabel(CallerText) }).SetCell(0, 0, StatusKind.Pass, CallerText),
            ["paths"] = () => new PathSet(new PathHop("origin", CallerText)).Add(new PathHop("next", CallerText)),
            ["shell prompt"] = () => new ShellPrompt { Path = CallerText },
            ["help"] = () => new HelpView(new Help.HelpTopic("Get-Example") { Synopsis = CallerText }),
            ["tabs"] = () => new Tabs().Add(CallerText, new TextBlock(CallerText)),
            ["run dashboard"] = () => new RunDashboard(new RunTracker(1)),
            ["run summary"] = () => new RunSummary(new RunTracker(1)),
            ["run progress"] = () => new RunProgress(new RunTracker(1).Snapshot()),
            ["run view"] = () => new RunView(new RunTracker(1)),
            ["diff"] = () => new ChangeView(ObjectDiff.Compare(new { Name = "old" }, new { Name = CallerText }))
        };

        public static IEnumerable<object[]> RenderingCases() {
            foreach (string scene in Scenes.Keys)
                foreach (int width in new[] { 1, 2, 3, 8, 24, 80 })
                    foreach (GlyphTier tier in new[] { GlyphTier.Ascii, GlyphTier.Basic, GlyphTier.Full })
                        yield return new object[] { scene, width, tier };
        }

        [Theory]
        [MemberData(nameof(RenderingCases))]
        public void Rendering_RespectsWidthAndDestinationRepertoire(string scene, int width, GlyphTier tier) {
            IWidget widget = Scenes[scene]();
            try {
                CellBuffer block = TestTerminal.Render(widget, width, glyphs: tier);
                CellBuffer frame = TestTerminal.Render(widget, width, 12, tier);
                Assert.Equal(width, block.Width);
                Assert.Equal(12, frame.Height);
                foreach (CellBuffer output in new[] { block, frame }) {
                    for (int y = 0; y < output.Height; y++) {
                        string row = output.GetRowText(y);
                        Assert.True(DisplayWidth.Measure(row) <= width, scene + ": " + row);
                        if (tier != GlyphTier.Full) Assert.All(row, c => Assert.True(
                            tier == GlyphTier.Ascii ? c >= ' ' && c <= '~' : Wgl4Repertoire.Contains(c),
                            scene + ": U+" + ((int)c).ToString("X4")));
                    }
                }
            } finally { (widget as IDisposable)?.Dispose(); }
        }

        public static IEnumerable<object[]> ValueCases() {
            foreach (string renderer in new[] { "table", "cards", "key/value", "record", "tree", "diff" }) {
                yield return new object[] { renderer, "null", "(null)" };
                yield return new object[] { renderer, "empty", "(empty)" };
                yield return new object[] { renderer, "missing", "(missing)" };
            }
        }

        [Theory]
        [MemberData(nameof(ValueCases))]
        public void Values_KeepNullEmptyAndMissingDistinct(string renderer, string kind, string expected) {
            object? value = kind == "null" ? null : kind == "empty" ? "" : Missing.Value;
            IWidget widget;
            switch (renderer) {
                case "table": widget = new Table().AddColumn("Value").AddValues(new[] { value }); break;
                case "cards": widget = new Table { AlwaysCards = true }.AddColumn("Value").AddValues(new[] { value }); break;
                case "key/value": widget = new KeyValueList().AddValue("Value", value); break;
                case "record": widget = new RecordView().AddField("Value", value); break;
                case "diff": widget = new ChangeView(ObjectDiff.Compare(new Dictionary<string, object?> { ["Value"] = value }, new Dictionary<string, object?>())); break;
                default: var tree = new Tree("Root"); tree.Add("Value").Value = TableCell.FromValue(value); widget = tree; break;
            }
            Assert.Contains(expected, TestTerminal.RenderText(widget, 80, glyphs: GlyphTier.Ascii));
            string translated = kind == "missing" ? "(brak)" : kind == "empty" ? "(puste)" : "(null)";
            Assert.Contains(translated, TestTerminal.RenderText(widget, 80, glyphs: GlyphTier.Basic, culture: CultureInfo.GetCultureInfo("pl")));
        }

        public static IEnumerable<object[]> EntryCases() {
            foreach (string entry in new[] { "app", "app async", "menu", "menu async", "grid", "explorer" })
                foreach (string condition in new[] { "redirected", "CI", "noninteractive", "snapshot" })
                    yield return new object[] { entry, condition };
        }

        [Theory]
        [InlineData("watch", 0, 1)]
        [InlineData("watch", 2, 2)]
        [InlineData("tail", 0, 1)]
        [InlineData("tail", 2, 2)]
        public void PollingEntryPoints_DefaultToOneSnapshotAndHonourFiniteCount(string entry, int count, int expected) {
            var target = TestTerminal.Target(48);
            int reads = 0;
            using (Terminal.Use(target)) {
                if (entry == "watch") {
                    Assert.Equal(expected, ConsoleWatch.Run(_ => { reads++; return new TextBlock("State"); }, new WatchOptions { Count = count, Delay = (_, __) => { } }));
                } else {
                    ConsoleLogTail.Run(() => { reads++; return new[] { new TailedLogEntry("State") }; },
                        new LogTailOptions { Count = count, PollInterval = TimeSpan.FromMilliseconds(1) });
                }
            }
            Assert.Equal(expected, reads);
            Assert.Equal(expected, target.Text.Split(new[] { "State" }, StringSplitOptions.None).Length - 1);
        }

        [Theory]
        [InlineData("record")]
        [InlineData("inspector")]
        public void Options_ApplyWhenChangedAfterAnEarlierRender(string renderer) {
            if (renderer == "record") {
                var view = new RecordView().AddField("Optional", (object?)null);
                Assert.Contains("(null)", TestTerminal.RenderText(view));
                view.HideEmpty = true;
                Assert.DoesNotContain("Optional", TestTerminal.RenderText(view));
            } else {
                var view = new InspectorView(new { Value = 1 }, title: "Header");
                Assert.Contains("Header", TestTerminal.RenderText(view));
                view.ShowHeader = false;
                Assert.DoesNotContain("Header", TestTerminal.RenderText(view));
            }
        }

        [Theory]
        [MemberData(nameof(EntryCases))]
        public async Task EntryPoints_PrintOneStaticViewWithoutRunningActions(string entry, string condition) {
            var profile = new TerminalProfile(ColorDepth.None, GlyphTier.Ascii, OutputMode.Interactive, 48, 12,
                supportsVirtualTerminal: true, isOutputRedirected: condition == "redirected", isContinuousIntegration: condition == "CI");
            var target = new RecordingTarget(profile);
            var ambient = TestTerminal.Target(80);
            var options = new InteractiveOptions {
                Target = target, Profile = profile, NonInteractive = condition == "noninteractive",
                Mode = condition == "snapshot" ? InteractionMode.Snapshot : InteractionMode.Rich,
                SnapshotOutput = widget => Terminal.Write(widget)
            };
            int actions = 0;
            var menu = new Menu("State").Item("Deploy", item => item.Action = _ => actions++);
            using (Terminal.Use(ambient)) switch (entry) {
                case "app": ConsoleApp.Run(new TextBlock("State"), options: new ConsoleAppOptions { Interactive = options }); break;
                case "app async": await ConsoleApp.RunAsync(new TextBlock("State"), options: new ConsoleAppOptions { Interactive = options }); break;
                case "menu": menu.Run(new MenuOptions { Interactive = options }); break;
                case "menu async": await menu.RunAsync(new MenuOptions { Interactive = options }); break;
                case "grid": Assert.Empty(new Grid("State").AddColumn("Name").AddRow("first", "alpha").Show(new GridOptions { Interactive = options }).Items); break;
                default: Assert.Null(new ObjectExplorer(new { Name = "State" }).Show(options)); break;
            }
            Assert.Contains("State", target.Text);
            Assert.Equal(1, target.Text.Split(new[] { "State" }, StringSplitOptions.None).Length - 1);
            Assert.Equal(0, actions);
            Assert.Empty(ambient.Text);
        }

        [Fact]
        public void SnapshotHostCallback_UsesTheExplicitConsoleTarget() {
            var ambient = TestTerminal.Target(48);
            bool called = false;
            var options = new InteractiveOptions {
                Target = ConsoleTarget.Instance, Profile = TestTerminal.Profile(48),
                SnapshotOutput = _ => { Assert.Same(ConsoleTarget.Instance, Terminal.Target); called = true; }
            };
            using (Terminal.Use(ambient)) options.PrintSnapshot(new TextBlock("State"));
            Assert.True(called);
            Assert.Empty(ambient.Text);
        }

        [Theory]
        [InlineData("→")]
        [InlineData("…")]
        [InlineData("™")]
        public void RepertoireConversion_NeverExpandsPastAChildRegion(string glyph) {
            var buffer = new CellBuffer(3, 1) { GlyphTier = GlyphTier.Ascii };
            buffer.Set(1, 0, "X", Style.Plain);
            new Region(buffer).Sub(0, 0, 1, 1).Put(0, 0, glyph, Style.Plain);
            Assert.Equal("X", buffer[1, 0].Glyph);
            Assert.Equal(1, buffer[0, 0].Width);
        }

        [Theory]
        [InlineData(OutputMode.Plain, GlyphTier.Ascii)]
        [InlineData(OutputMode.Stream, GlyphTier.Ascii)]
        [InlineData(OutputMode.Plain, GlyphTier.Basic)]
        [InlineData(OutputMode.Stream, GlyphTier.Basic)]
        public void DirectStyledLines_EnforceTheSameDestinationRepertoire(OutputMode mode, GlyphTier tier) {
            var writer = new StringTerminalWriter();
            var profile = TestTerminal.Profile(glyphs: tier).WithMode(mode);
            Terminal.WriteLines(new[] { StyledLine.From(CallerText) }, profile: profile, writer: writer);
            Assert.All(writer.ToString().Where(c => c != '\r' && c != '\n'), c => Assert.True(
                tier == GlyphTier.Ascii ? c >= ' ' && c <= '~' : Wgl4Repertoire.Contains(c)));
            Assert.Contains("caf", writer.ToString());
        }

        [Theory]
        [InlineData("watch", "default")]
        [InlineData("tail", "default")]
        [InlineData("watch", "environment")]
        [InlineData("tail", "environment")]
        [InlineData("watch", "process")]
        [InlineData("tail", "process")]
        public void PollingEntryPoints_NoninteractiveFlagsPreventLiveInput(string entry, string flag) {
            InteractiveOptions previous = InteractiveOptions.Default;
            string? previousEnvironment = Environment.GetEnvironmentVariable("CONSOLEMONSTER_NONINTERACTIVE");
            var target = new RecordingTarget(TestTerminal.Profile(48, 12));
            int reads = 0;
            using (var input = new global::ConsoleMonster.Input.ScriptedInput("Escape"))
            using (var deadline = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2))) {
                try {
                    InteractiveOptions.Default = new InteractiveOptions { NonInteractive = flag == "default" };
                    Environment.SetEnvironmentVariable("CONSOLEMONSTER_NONINTERACTIVE", flag == "environment" ? "1" : null);
                    using (Terminal.Use(target)) {
                        if (entry == "watch") ConsoleWatch.Run(_ => { reads++; return new TextBlock("State"); }, new WatchOptions { NonInteractive = flag == "process" }, deadline.Token);
                        else ConsoleLogTail.Run(() => { reads++; return new[] { new TailedLogEntry("State") }; },
                            new LogTailOptions { Input = input, NonInteractive = flag == "process" }, deadline.Token);
                    }
                    Assert.Equal(1, reads);
                    Assert.Contains("State", target.Text);
                    Assert.Empty(target.Screens.Frames);
                    Assert.Equal(1, input.Remaining);
                } finally {
                    InteractiveOptions.Default = previous;
                    Environment.SetEnvironmentVariable("CONSOLEMONSTER_NONINTERACTIVE", previousEnvironment);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WatchFallback_PreservesAnExplicitTargetAndProfile(bool live) {
            var ambient = TestTerminal.Target(80);
            var destination = TestTerminal.Target(40);
            using (Terminal.Use(ambient)) {
                Assert.Equal(1, ConsoleWatch.Run(_ => new TextBlock("State 東京"), new WatchOptions {
                    Count = 1, Live = live,
                    Session = new LiveSessionOptions { Target = destination, Profile = TestTerminal.Profile(20, glyphs: GlyphTier.Ascii).WithMode(OutputMode.Plain), Theme = Theme.Light }
                }));
            }
            Assert.Empty(ambient.Text);
            Assert.Contains("State ??", destination.Text);
            Assert.All(destination.Text.Split('\n'), row => Assert.True(row.TrimEnd('\r').Length <= 20));
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("tr-TR")]
        public void SearchAndSort_UseOrdinalIgnoreCaseWithStableTies(string culture) {
            var grid = new Grid("Names") { Presentation = new Presenters.PresentationOptions { Culture = CultureInfo.GetCultureInfo(culture) } };
            grid.AddColumn("Name").AddRow("lower", "alpha").AddRow("upper", "ALPHA").AddRow("last", "zeta").AddRow("accent", "ä");
            using (var control = new DataGrid("grid", grid)) {
                control.Sort(0, false);
                Assert.Equal("lower", control.CurrentRow!.Item);
                control.Cursor = 2;
                Assert.Equal("last", control.CurrentRow!.Item);
                control.Filter = "AlPhA";
                Assert.Equal(2, control.VisibleCount);
            }
            var live = new LiveTable(new Table().AddColumn("Name")) { SortColumn = 0 };
            live.Set("lower", "alpha"); live.Set("upper", "ALPHA"); live.Set("last", "zeta"); live.Set("accent", "ä");
            Assert.Equal(new[] { "lower", "upper", "last", "accent" }, live.Keys);
            Assert.NotNull(new Menu("Tasks").Item("ALPHA").Find("alpha"));
            Assert.True(new LogFilter(tag: "API", text: "READY").Matches(new LogEntry(TimeSpan.Zero, "info", "ready", null, "api")));
        }

        [Theory]
        [InlineData("progress")]
        [InlineData("log")]
        [InlineData("table")]
        public void LiveWidgets_OutsideSessionPrintCurrentStateOnce(string kind) {
            IWidget widget;
            if (kind == "progress") widget = new ProgressBar("State") { Value = 50 };
            else if (kind == "log") { var log = new LogRegion(); log.Write("State"); widget = log; }
            else { var table = new LiveTable(new Table().AddColumn("Name")); table.Set("first", "State"); widget = table; }
            var target = TestTerminal.Target(80);
            using (Terminal.Use(target)) Terminal.Write(widget);
            Assert.Equal(1, target.Text.Split(new[] { "State" }, StringSplitOptions.None).Length - 1);
        }
    }
}
