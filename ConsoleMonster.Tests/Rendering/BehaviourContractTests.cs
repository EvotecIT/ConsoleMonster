using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Sessions;
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
        public static IEnumerable<object[]> TerminalExitCases() {
            foreach (string entry in new[] { "app", "app async", "menu", "menu async", "prompt" })
                foreach (string exit in new[] { "normal", "Esc", "Ctrl+C", "cancel", "exception" })
                    yield return new object[] { entry, exit };
            foreach (string entry in new[] { "live full", "live inline" })
                foreach (string exit in new[] { "normal", "cancel", "exception" })
                    yield return new object[] { entry, exit };
            foreach (string entry in new[] { "empty full", "empty inline", "empty prompt", "empty full no colour", "empty inline no colour", "empty prompt no colour" })
                yield return new object[] { entry, "exception" };
        }

        [Theory]
        [MemberData(nameof(TerminalExitCases))]
        public async Task TerminalExit_ReturnsACleanLineToTheNextPrompt(string entry, string exit) {
            var profile = TerminalProfile.Fixed(48, 12, entry.EndsWith("no colour", StringComparison.Ordinal) ? ColorDepth.None : ColorDepth.TrueColor, GlyphTier.Full).WithHyperlinks(true);
            using var text = new StringWriter(CultureInfo.InvariantCulture);
            var target = new WriterTarget(text, profile);
            // A caller can have written a coloured partial line before invoking an app.
            text.Write("\u001b]8;;https://example.test/\u001b\\\u001b[31mcaller partial");
            using var cancellation = new CancellationTokenSource();
            using var input = new ExitContractInput(exit, cancellation,
                entry.StartsWith("app", StringComparison.Ordinal) ? "Escape" : "Enter");
            var interactive = new InteractiveOptions { Target = target, Profile = profile, Mode = InteractionMode.Rich };
            Exception? failure = null;
            IInputSource? previousInput = ConsoleInput.Override;
            ConsoleInput.Override = input;
            try {
                if (entry.StartsWith("app", StringComparison.Ordinal)) {
                    var state = new AppState();
                    var options = new ConsoleAppOptions { Interactive = interactive,
                        Live = new LiveSessionOptions { CaptureConsole = false, HandleCancelKey = false, TaskbarProgress = false } };
                    if (exit == "normal") options.ProcessEvents = values => values.RequestExit();
                    if (entry == "app async") await ConsoleApp.RunAsync(new TextBlock("State"), state, options, cancellation.Token);
                    else ConsoleApp.Run(new TextBlock("State"), state, options, cancellation.Token);
                } else if (entry.StartsWith("menu", StringComparison.Ordinal)) {
                    var menu = new Menu("State").Exit("Quit");
                    if (entry == "menu async") await menu.RunAsync(new MenuOptions { Interactive = interactive }, cancellation.Token);
                    else menu.Run(new MenuOptions { Interactive = interactive }, cancellation.Token);
                } else if (entry == "prompt") {
                    new ConfirmPrompt("State", true).Show(interactive, cancellation.Token);
                } else if (entry.StartsWith("live", StringComparison.Ordinal)) {
                    using var session = new LiveSession(new TextBlock("State"), new LiveSessionOptions {
                        Target = target, Profile = profile, CaptureConsole = false, HandleCancelKey = false,
                        TaskbarProgress = false, PrintFinalState = false,
                        Mode = entry == "live inline" ? LiveMode.Inline : LiveMode.FullScreen
                    });
                    session.Start();
                    Assert.True(session.Flush(TimeSpan.FromSeconds(10)));
                    if (exit == "cancel") { cancellation.Cancel(); cancellation.Token.ThrowIfCancellationRequested(); }
                    if (exit == "exception") throw new InvalidOperationException("exit contract");
                } else {
                    // Setup can fail after acquiring the screen, before the first frame exists.
                    if (entry.StartsWith("empty prompt", StringComparison.Ordinal)) { using var surface = new InlineSurface(profile, target.Writer); throw new InvalidOperationException("exit contract"); }
                    using var output = entry.StartsWith("empty full", StringComparison.Ordinal) ? Terminal.CreateOutput(profile, target.Writer) : Terminal.CreateInlineOutput(profile, target.Writer);
                    output.BeginLive();
                    throw new InvalidOperationException("exit contract");
                }
            } catch (Exception ex) { failure = ex; }
            finally { ConsoleInput.Override = previousInput; }
            if (exit == "exception") Assert.IsType<InvalidOperationException>(failure);
            else if (exit == "cancel") Assert.IsAssignableFrom<OperationCanceledException>(failure);
            else if (exit == "Ctrl+C" && entry == "prompt") Assert.IsType<PromptCanceledException>(failure);
            else Assert.Null(failure);
            Assert.True(input.Reads > 0 || entry.StartsWith("live", StringComparison.Ordinal) || entry.StartsWith("empty", StringComparison.Ordinal) || (exit == "normal" && entry.StartsWith("app", StringComparison.Ordinal)));

            AssertPromptReady(text.ToString());
            // A different thread must be able to take the global lease after every exit.
            try {
                ConsoleInput.Override = input;
                await Task.Run(() => { using InputLease lease = ConsoleInput.Acquire(); Assert.Same(input, lease.Source); });
            } finally { ConsoleInput.Override = previousInput; }
        }

        [Theory]
        [InlineData("nested prompt")]
        [InlineData("overlay setup failure")]
        [InlineData("owning session cancelled")]
        [InlineData("full cleanup write failure")]
        [InlineData("inline cleanup write failure")]
        [InlineData("prompt cleanup write failure")]
        [InlineData("full cleanup flush failure")]
        [InlineData("inline cleanup flush failure")]
        [InlineData("prompt cleanup flush failure")]
        public void TerminalRecovery_PreservesOwnershipAndReturnsACleanLine(string scenario) {
            var profile = TerminalProfile.Fixed(48, 12, ColorDepth.TrueColor, GlyphTier.Full);
            using var text = new ExitContractWriter();
            var target = new WriterTarget(text, profile);
            text.Write("\u001b[31mcaller partial");
            if (scenario.Contains("cleanup ")) {
                if (scenario.StartsWith("prompt", StringComparison.Ordinal)) {
                    var surface = new InlineSurface(profile, target.Writer);
                    surface.Present(TestTerminal.Render(new TextBlock("State\nSecond"), 40), new Caret(2, 1));
                    text.FailNextWrite = scenario.Contains("write failure");
                    text.FailNextFlush = scenario.Contains("flush failure");
                    Assert.Throws<IOException>(() => surface.Complete(null));
                    surface.Dispose();
                } else {
                    var output = scenario.StartsWith("full", StringComparison.Ordinal)
                        ? Terminal.CreateOutput(profile, target.Writer) : Terminal.CreateInlineOutput(profile, target.Writer);
                    output.BeginLive();
                    output.RenderFrame(TestTerminal.Render(new TextBlock("State\nSecond"), 40));
                    text.FailNextWrite = scenario.Contains("write failure");
                    text.FailNextFlush = scenario.Contains("flush failure");
                    Assert.Throws<IOException>(() => output.EndLive());
                    output.Dispose();
                }
            } else {
                int failWindow = 0;
                int promptThread = Thread.CurrentThread.ManagedThreadId;
                using var session = new LiveSession(new TextBlock("State"), new LiveSessionOptions {
                    Target = target, Profile = profile, CaptureConsole = false, HandleCancelKey = false,
                    TaskbarProgress = false, PrintFinalState = false,
                    WindowSource = () => Thread.CurrentThread.ManagedThreadId == promptThread && Volatile.Read(ref failWindow) != 0
                        ? throw new InvalidOperationException("overlay window failure") : new[] { 0, 0, 48, 12 }
                });
                session.Start();
                Assert.True(session.Flush(TimeSpan.FromSeconds(10)));
                using LivePause pause = session.Pause();
                if (scenario == "overlay setup failure") {
                    Volatile.Write(ref failWindow, 1);
                    Assert.Throws<InvalidOperationException>(() => new ConfirmPrompt("Nested", true).Show(new InteractiveOptions {
                        Target = target, Profile = profile, Input = new ScriptedInput("Enter"), Mode = InteractionMode.Rich
                    }));
                    Volatile.Write(ref failWindow, 0);
                } else if (scenario == "owning session cancelled") {
                    Assert.ThrowsAny<OperationCanceledException>(() => new ConfirmPrompt("Nested", true).Show(new InteractiveOptions {
                        Target = target, Profile = profile, Input = new OwnerCancellationInput(session.Cancel), Mode = InteractionMode.Rich
                    }));
                } else {
                    Assert.True(new ConfirmPrompt("Nested", true).Show(new InteractiveOptions {
                        Target = target, Profile = profile, Input = new ScriptedInput("Enter"), Mode = InteractionMode.Rich
                    }));
                }
                // The nested lifetime did not leave the borrowed screen or retain an extra pause.
                var active = new VtScreen(48, 12, keepScrollback: false);
                active.Feed(text.ToString());
                if (scenario != "owning session cancelled") { Assert.True(active.OnAlternate); }
                Assert.True(session.IsPaused);
                pause.Dispose();
                Assert.False(session.IsPaused);
                session.Dispose();
            }
            AssertPromptReady(text.ToString());
            var cleaned = new VtScreen(48, 12, keepScrollback: false);
            cleaned.Feed(text.ToString());
            Assert.DoesNotContain("State", cleaned.Snapshot().ToPlainText());
            Assert.DoesNotContain("Second", cleaned.Snapshot().ToPlainText());
            text.Write("writer remains open");
            Assert.EndsWith("writer remains open", text.ToString());
        }

        [Theory]
        [InlineData("Run")]
        [InlineData("RunAsync")]
        [InlineData("Dispose")]
        public async Task TerminalLifetime_WaitsForDeferredRestorationBeforeReturning(string lifetime) {
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            using var text = new StringWriter(CultureInfo.InvariantCulture);
            var profile = TerminalProfile.Fixed(48, 12, ColorDepth.TrueColor, GlyphTier.Full);
            var target = new WriterTarget(text, profile);
            var widget = new global::ConsoleMonster.Tests.Live.FakeTarget {
                Rendering = _ => { entered.Set(); Assert.True(release.Wait(TimeSpan.FromSeconds(10))); }
            };
            using var session = new LiveSession(widget, new LiveSessionOptions {
                Target = target, Profile = profile, CaptureConsole = false, HandleCancelKey = false,
                TaskbarProgress = false, PrintFinalState = false, StopTimeout = TimeSpan.Zero
            });
            Task operation = Task.Run(async () => {
                void Work(LiveSession _) { Assert.True(entered.Wait(TimeSpan.FromSeconds(10))); }
                if (lifetime == "Run") session.Run(Work);
                else if (lifetime == "RunAsync") await session.RunAsync((current, _) => { Work(current); return Task.CompletedTask; });
                else { session.Start(); Work(session); session.Dispose(); }
            });
            try {
                Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
                Assert.True(SpinWait.SpinUntil(() => session.LastError is TimeoutException, TimeSpan.FromSeconds(10)));
                Assert.False(operation.IsCompleted);
            } finally { release.Set(); }
            await operation;
            Assert.True(session.OutputStopped.IsCompleted);
            AssertPromptReady(text.ToString());
        }

        private static void AssertPromptReady(string output) {
            var terminal = new VtScreen(48, 12, keepScrollback: false);
            terminal.Feed(output);
            Assert.False(terminal.OnAlternate);
            Assert.True(terminal.CursorVisible);
            Assert.Equal(0, terminal.Cursor.X);
            Assert.False(terminal.Cursor.PendingWrap);
            Assert.False(terminal.SynchronizedOutput);
            Assert.False(terminal.ButtonMotionMouse);
            Assert.False(terminal.SgrMouse);
            Assert.False(terminal.HasIncompleteSequence);
            Assert.Equal(Style.Plain, terminal.ActiveStyle);
            Assert.True(string.IsNullOrWhiteSpace(terminal.Snapshot().GetRowText(terminal.Cursor.Y)));
            // The next shell prompt starts on the empty row, with ordinary colours and no hidden suffix.
            terminal.Feed("next prompt> ");
            Assert.Equal("next prompt> ", terminal.Snapshot().GetRowText(terminal.Cursor.Y).TrimEnd() + " ");
        }

        private sealed class OwnerCancellationInput : IInputSource {
            private readonly Action _cancel;
            internal OwnerCancellationInput(Action cancel) { _cancel = cancel; }
            public string Name => "owner cancellation input";
            public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
                _cancel();
                cancellationToken.ThrowIfCancellationRequested();
                throw new InvalidOperationException("The prompt did not observe its owner's cancellation.");
            }
            public void Dispose() { }
        }

        private sealed class ExitContractWriter : StringWriter {
            internal bool FailNextWrite;
            internal bool FailNextFlush;
            public override void Flush() {
                if (FailNextFlush) { FailNextFlush = false; throw new IOException("contract cleanup flush failure"); }
                base.Flush();
            }
            public override void Write(string? value) {
                if (FailNextWrite) { FailNextWrite = false; throw new IOException("contract cleanup write failure"); }
                base.Write(value);
            }
        }

        private sealed class ExitContractInput : IInputSource {
            private readonly string _exit;
            private readonly CancellationTokenSource _cancellation;
            private readonly ScriptedInput _keys;
            internal int Reads;
            internal ExitContractInput(string exit, CancellationTokenSource cancellation, string normal) {
                _exit = exit; _cancellation = cancellation;
                _keys = new ScriptedInput(exit == "Esc" ? "Escape" : exit == "Ctrl+C" ? "Ctrl+C" : normal);
            }
            public string Name => "contract exit input";
            public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
                Reads++;
                if (_exit == "exception") throw new InvalidOperationException("exit contract");
                if (_exit == "cancel") { _cancellation.Cancel(); cancellationToken.ThrowIfCancellationRequested(); }
                return _keys.Read(timeout, cancellationToken);
            }
            public void Dispose() => _keys.Dispose();
        }

        private const string CallerText = "Łódź 東京 ✅ café";
        private static readonly Dictionary<string, Func<IWidget>> Scenes = new Dictionary<string, Func<IWidget>> {
            ["text"] = () => new TextBlock(CallerText),
            ["diagram"] = () => new Diagram().Node(CallerText).Node("Target").Link(CallerText, "Target"),
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
                yield return new object[] { renderer, "null", renderer == "diff" ? "(null)" : "—" };
                yield return new object[] { renderer, "empty", renderer == "diff" ? "(empty)" : "" };
                yield return new object[] { renderer, "missing", renderer == "diff" ? "(missing)" : "—" };
            }
        }

        [Theory]
        [MemberData(nameof(ValueCases))]
        public void Values_UseQuietPlaceholdersExceptInComparisons(string renderer, string kind, string expected) {
            object? value = kind == "null" ? null : kind == "empty" ? "" : Missing.Value;
            IWidget widget;
            switch (renderer) {
                case "table": widget = new Table().AddColumn("Value").AddValues(new[] { value }); break;
                case "cards": widget = new Table { AlwaysCards = true }.AddColumn("Value").AddValues(new[] { value }); break;
                case "key/value": widget = new KeyValueList().AddValue("Value", value); break;
                case "record": widget = new RecordView { HideEmpty = false }.AddField("Value", value); break;
                case "diff": widget = new ChangeView(ObjectDiff.Compare(new Dictionary<string, object?> { ["Value"] = value }, new Dictionary<string, object?>())); break;
                default: var tree = new Tree("Root"); tree.Add("Value").Value = TableCell.FromValue(value); widget = tree; break;
            }
            string ascii = TestTerminal.RenderText(widget, 80, glyphs: GlyphTier.Ascii);
            string basic = TestTerminal.RenderText(widget, 80, glyphs: GlyphTier.Basic, culture: CultureInfo.GetCultureInfo("pl"));
            if (renderer == "diff") {
                Assert.Contains(expected, ascii);
                string translated = kind == "missing" ? "(brak)" : kind == "empty" ? "(puste)" : "(null)";
                Assert.Contains(translated, basic);
            } else {
                Assert.Equal(expected, global::ConsoleMonster.Values.ValueShaper.Text(value));
                Assert.Contains("Value", basic);
                foreach (string marker in new[] { "(null)", "(empty)", "(missing)", "(brak)", "(puste)" }) {
                    Assert.DoesNotContain(marker, ascii); Assert.DoesNotContain(marker, basic);
                }
                if (expected.Length > 0) { Assert.Contains(expected, basic); Assert.Contains("-", ascii); }
                else { Assert.DoesNotContain("—", basic); }
            }
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
                Assert.DoesNotContain("Optional", TestTerminal.RenderText(view));
                view.HideEmpty = false;
                Assert.Contains("Optional", TestTerminal.RenderText(view));
                Assert.Contains("—", TestTerminal.RenderText(view));
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
            Assert.DoesNotContain("tab next", target.Text.ToLowerInvariant());
            Assert.DoesNotContain("esc quit", target.Text.ToLowerInvariant());
            Assert.DoesNotContain("enter select", target.Text.ToLowerInvariant());
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
        [InlineData(OutputMode.Interactive, GlyphTier.Ascii)]
        [InlineData(OutputMode.Interactive, GlyphTier.Basic)]
        [InlineData(OutputMode.Interactive, GlyphTier.Full)]
        public void DirectStyledLines_EnforceTheSameDestinationRepertoire(OutputMode mode, GlyphTier tier) {
            var writer = new StringTerminalWriter();
            var profile = TestTerminal.Profile(glyphs: tier).WithMode(mode).WithHyperlinks(true);
            var line = new StyledLine(new[] { new TextSpan(CallerText, Style.Plain.WithLink("https://example.test/Łódź/東京")) });
            var domain = new StyledLine(new[] { new TextSpan("IDN", Style.Plain.WithLink("https://例え.テスト/报告")) });
            Terminal.WriteLines(new[] { line, domain }, profile: profile, writer: writer);
            if (tier != GlyphTier.Full) Assert.All(writer.ToString().Where(c => c != '\r' && c != '\n' && c != '\u001b'), c => Assert.True(
                tier == GlyphTier.Ascii ? c >= ' ' && c <= '~' : Wgl4Repertoire.Contains(c)));
            Assert.Contains("caf", writer.ToString());
            if (tier == GlyphTier.Ascii) {
                Assert.Contains("Lodz ?? [OK] cafe", writer.ToString());
                string latin = SymbolFallback.Constrain("ł Ł đ Đ ø Ø ß æ Æ œ Œ þ ð e\u0301", tier);
                Assert.Equal("l L d D o O ss ae AE oe OE th d e", latin);
            }
            if (mode == OutputMode.Interactive) {
                Assert.Contains(tier == GlyphTier.Full ? "https://example.test/Łódź/東京" : "https://example.test/%C5%81%C3%B3d%C5%BA/%E6%9D%B1%E4%BA%AC", writer.ToString());
                Assert.Contains(tier == GlyphTier.Full ? "https://例え.テスト/报告" : "https://xn--r8jz45g.xn--zckzah/%E6%8A%A5%E5%91%8A", writer.ToString());
            }
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
            string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cm-behaviour-" + Guid.NewGuid().ToString("N"));
            CultureInfo previous = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var store = new ChoiceStore("Presets", root);
                foreach (string name in new[] { "alpha", "ALPHA", "zeta", "ä" }) store.SavePreset("form", name, new Dictionary<string, object?>());
                Assert.Equal(new[] { "ALPHA", "alpha", "zeta", "ä" }, store.PresetNames("form"));
                Assert.Null(store.LoadPreset("form", "Alpha"));
                string files = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(root, "paths")).FullName;
                foreach (string name in new[] { "alpha", "zeta", "ä" }) System.IO.File.WriteAllText(System.IO.Path.Combine(files, name), "");
                var path = new PathPrompt("Path") { BasePath = files };
                Assert.Equal(new[] { "alpha", "zeta", "ä" }, path.Complete(""));
                Assert.Equal(new[] { "alpha" }, path.Complete("AL"));
            } finally {
                CultureInfo.CurrentCulture = previous;
                if (System.IO.Directory.Exists(root)) System.IO.Directory.Delete(root, true);
            }
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
