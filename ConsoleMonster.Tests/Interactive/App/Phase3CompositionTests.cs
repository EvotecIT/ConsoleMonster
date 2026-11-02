using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    public sealed class Phase3CompositionTests {
        [Fact]
        public void TabsMountOnlyActiveInputsAndRestoreFocusWithoutResettingValues() {
            var first = new TextInput("First"); var second = new TextInput("Second");
            var tabs = new Tabs().Add("First tab", first).Add("Second tab", second);
            ConsoleAppOptions options = AppTestHarness.Options("\"a\" Ctrl+PageDown \"b\" Ctrl+PageUp \"c\" Escape", out var capture);
            ConsoleApp.Run(tabs, options: options);
            Assert.Equal("ac", first.Value); Assert.Equal("b", second.Value);
            Assert.Equal(0, tabs.SelectedIndex); Assert.True(first.Focused);
            Assert.Contains(capture.Frames, frame => frame.ToPlainText().Contains("Second tab") && frame.ToPlainText().Contains("b"));
        }

        [Fact]
        public void NarrowRegionsFollowFocusedContentAndSwapWithoutLosingState() {
            var first = new TextInput("First", "alpha"); var second = new TextInput("Second", "beta");
            var regions = new LayoutRegions().Add("left", first).Add("right", second);
            var context = Terminal.CreateContext(TerminalProfile.Fixed(60, 20));
            var state = new AppState();
            using var app = new AppController(regions, state, context, 60);
            string left = app.PrepareFrame(60, 20).ToPlainText();
            Assert.Contains("alpha", left); Assert.DoesNotContain("beta", left);
            regions.Activate("right");
            string right = app.PrepareFrame(60, 20).ToPlainText();
            Assert.Contains("beta", right); Assert.DoesNotContain("alpha", right);
            regions.Swap("left", "right");
            Assert.Same(second, regions.Regions[0].Content);
            Assert.Equal("beta", second.Value);
            string wide = app.PrepareFrame(120, 20).ToPlainText();
            Assert.Contains("alpha", wide); Assert.Contains("beta", wide);
        }

        [Fact]
        public void TypedFormCommitsOnAppThreadAndRestoresUnderlyingFocus() {
            var body = new TextInput("Body"); var field = new TextInput("Destination", "old");
            var dialogs = new AppDialogs();
            var form = new FormDialog<string>("Copy", field, state => state.Get<string>("Destination")) { InitialFocus = "Destination" };
            DialogResult<string>? result = null;
            int thread = Thread.CurrentThread.ManagedThreadId, callbackThread = -1;
            ConsoleAppOptions options = AppTestHarness.Options("F5 End Ctrl+U \"target\" Ctrl+Enter \"body\" Escape", out _);
            options.Dialogs = dialogs;
            options.HandleInput = (_, input) => {
                if (!input.Is(ConsoleKey.F5)) { return false; }
                dialogs.Show(form, answer => { result = answer; callbackThread = Thread.CurrentThread.ManagedThreadId; }); return true;
            };
            ConsoleApp.Run(body, options: options);
            Assert.NotNull(result); Assert.True(result!.Accepted); Assert.Equal("target", result.Value);
            Assert.Equal(thread, callbackThread); Assert.True(body.Focused); Assert.Equal("body", body.Value);
        }

        [Fact]
        public void FormDefaultsToCancelAndValidationKeepsItOpen() {
            var dialogs = new AppDialogs();
            DialogResult<string>? result = null;
            var field = new TextInput("Destination", "bad");
            var form = new FormDialog<string>("Destination", field, state => state.Get<string>("Destination")) { Validate = _ => "Choose a writable destination" };
            var options = new ConsoleAppOptions { Dialogs = dialogs };
            options.HandleInput = (_, input) => { if (!input.Is(ConsoleKey.F5)) { return false; } dialogs.Show(form, answer => result = answer); return true; };
            var context = Terminal.CreateContext(TerminalProfile.Fixed(80, 24));
            var body = new TextInput("Body");
            using var app = new AppController(body, new AppState(), context, 60, options);
            app.PrepareFrame(80, 24);
            app.Handle(InputEvent.Press(ConsoleKey.F5), false);
            app.PrepareFrame(80, 24);
            app.Handle(InputEvent.Press(ConsoleKey.Enter, KeyModifiers.Control), false);
            Assert.Contains("Choose a writable destination", app.PrepareFrame(80, 24).ToPlainText());
            app.Handle(InputEvent.Press(ConsoleKey.Enter), false);
            Assert.NotNull(result); Assert.False(result!.Accepted);
            Assert.True(body.Focused);
        }

        [Fact]
        public void ContextFooterRetainsUnavailableSlotsAndChangesLabelsWithFocus() {
            bool enabled = false;
            var footer = new FooterBar { FixedSlots = true, HintsSource = () => new[] { new KeyHint("F1", enabled ? "Open" : "View", enabled), new KeyHint("Alt+8", "Delete", false) } };
            var context = Terminal.CreateContext(TerminalProfile.Fixed(60, 10));
            string first = FrameComposer.Compose(footer, 60, 1, context).ToPlainText();
            Assert.Contains("View", first); Assert.Contains("Alt+8", first); Assert.Contains("Delete", first);
            enabled = true;
            Assert.Contains("Open", FrameComposer.Compose(footer, 60, 1, context).ToPlainText());
        }

        [Fact]
        public void LogViewportCapsOutputAndHasKeyboardScrollback() {
            var log = new LogViewport("Log") { Capacity = 20, ShowElapsed = false };
            for (int i = 0; i < 1000; i++) { log.Write("entry " + i); }
            Assert.Equal(20, log.Entries.Count); Assert.Equal("entry 980", log.Entries.First().Text);
            var context = Terminal.CreateContext(TerminalProfile.Fixed(60, 12));
            log.HandleInput(InputEvent.Press(ConsoleKey.Home), context);
            Assert.False(log.Following); Assert.Contains("entry 980", FrameComposer.Compose(log, 60, 12, context).ToPlainText());
            log.HandleInput(InputEvent.Press(ConsoleKey.End), context); Assert.Equal(0, log.ScrollOffset); Assert.True(log.Following);
        }

        [Fact]
        public async Task BackgroundOperationReportsLatestProgressAndExplicitRetryWithoutAnotherRunner() {
            var retry = new AppCommand("retry", "Retry failures", (_, __) => Task.CompletedTask);
            using var session = new AppActionSession(default);
            var command = new AppCommand("copy", "Copy", (context, cancellation) => {
                context.Report(new AppProgress("file1", 10, 100, "1 MB/s"));
                context.Summarize(new AppOperationSummary("1 copied, 1 failed", new[] { "file2: access denied" }, retry));
                return Task.CompletedTask;
            }) { ProgressDialog = true };
            Assert.True(session.Start(command, Array.Empty<object?>()));
            DateTime deadline = DateTime.UtcNow.AddSeconds(5);
            while (session.IsBusy && DateTime.UtcNow < deadline) { await Task.Delay(10); }
            Assert.False(session.IsBusy);
            Assert.Equal("file1", session.Progress!.Message); Assert.Equal(10, session.Progress.Completed);
            Assert.Equal("1 copied, 1 failed", session.Summary!.Message); Assert.Same(retry, session.Summary.Retry);
            Assert.Single(session.Summary.Failures);
        }
    }
}
