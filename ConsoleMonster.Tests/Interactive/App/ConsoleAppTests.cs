using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Testing;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Scripted public app runs, exit semantics and reusable cleanup after failures.</summary>
    [Collection("Live sessions")]
    public sealed class ConsoleAppTests {
        /// <summary>A run returns the supplied state with initialized controls and every committed edit.</summary>
        [Fact]
        public void Run_ReturnsFinalStateAndCommitsEachKeystroke() {
            TextInput query = new TextInput("query");
            Toggle active = new Toggle("active");
            AppState state = new AppState { ["owner"] = "test" };
            List<string> edits = new List<string>();
            state.Changed += (_, change) => { if (change.Name == "query") { edits.Add((string)change.Value!); } };
            ConsoleAppOptions options = AppTestHarness.Options("\"ab\" Tab Space Escape", out ScreenCapture capture);
            AppState result = ConsoleApp.Run(new Rows(query, active), state, options);
            Assert.Same(state, result);
            Assert.Equal(new[] { "", "a", "ab" }, edits);
            Assert.Equal("ab", result.Get<string>("query"));
            Assert.True(result.Get<bool>("active"));
            Assert.Equal("test", result.Get<string>("owner"));
            Assert.True(result.ExitRequested);
            Assert.NotEmpty(capture.Frames);
            Assert.Contains("ab", capture.LastFrame!.ToPlainText());
        }

        /// <summary>The async API returns committed values and dispatches input callbacks on its owning input thread.</summary>
        [Fact]
        public async Task RunAsync_ReturnsFinalStateWithInputThreadCallbacks() {
            TextInput query = new TextInput("query");
            int inputThread = 0;
            List<int> callbackThreads = new List<int>();
            query.Changed += (_, __) => callbackThreads.Add(Thread.CurrentThread.ManagedThreadId);
            ConsoleAppOptions options = AppTestHarness.Options("\"ok\" Escape", out _);
            options.ProcessEvents = _ => {
                if (inputThread == 0) { inputThread = Thread.CurrentThread.ManagedThreadId; }
                Assert.Equal(inputThread, Thread.CurrentThread.ManagedThreadId);
            };
            AppState result = await ConsoleApp.RunAsync(query, options: options);
            Assert.Equal("ok", result.Get<string>("query"));
            Assert.Equal(2, callbackThreads.Count);
            Assert.All(callbackThreads, thread => Assert.Equal(inputThread, thread));
        }

        /// <summary>Unhandled Enter advances focus while selector and button Enter commit without submitting the app.</summary>
        [Fact]
        public void Enter_AdvancesTextFocusButCommitsSelectorAndButton() {
            TextInput query = new TextInput("query");
            Select target = new Select("target", new[] { new Choice("alpha"), new Choice("beta") });
            Button apply = new Button("apply");
            List<string> focused = new List<string>();
            ConsoleAppOptions options = AppTestHarness.Options("Enter Down Enter Tab Enter Escape", out _);
            options.ProcessEvents = _ => focused.Add(query.Focused ? "query" : target.Focused ? "target" : "apply");
            AppState result = ConsoleApp.Run(new Rows(query, target, apply), options: options);
            Assert.Equal(new[] { "query", "target", "target", "target", "apply", "apply", "apply" }, focused);
            Assert.Equal("beta", result["target"]);
            Assert.Equal(1, result["apply"]);
        }

        /// <summary>Escape clears a selector filter first; a later unhandled Escape exits without consuming more keys.</summary>
        [Fact]
        public void Escape_ClearsSelectorFilterBeforeExitingApp() {
            Select target = new Select("target", new[] { new Choice("alpha"), new Choice("beta") });
            ConsoleAppOptions options = AppTestHarness.Options("\"beta\" Escape Home Enter Escape Down", out _);
            ScriptedInput input = (ScriptedInput)options.Interactive!.Input!;
            AppState result = ConsoleApp.Run(target, options: options);
            Assert.Equal("alpha", result["target"]);
            Assert.True(result.ExitRequested);
            Assert.Equal(1, input.Remaining);
        }

        /// <summary>Raw Ctrl+C exits before editor handling and leaves subsequent input unread.</summary>
        [Fact]
        public void CtrlC_QuitsWithCommittedResultAndLeavesRemainingInput() {
            ConsoleAppOptions options = AppTestHarness.Options("\"a\" Ctrl+C \"b\"", out _);
            ScriptedInput input = (ScriptedInput)options.Interactive!.Input!;
            AppState result = ConsoleApp.Run(new TextInput("query"), options: options);
            Assert.True(result.ExitRequested);
            Assert.Equal("a", result["query"]);
            Assert.Equal(1, input.Remaining);
        }

        /// <summary>Forward and backward tab navigation skip disabled controls through the actual app input loop.</summary>
        [Fact]
        public void TabAndShiftTab_SkipDisabledControls() {
            TextInput first = new TextInput("first");
            TextInput disabled = new TextInput("disabled") { Enabled = false };
            TextInput last = new TextInput("last");
            AppState result = ConsoleApp.Run(new Rows(first, disabled, last), options:
                AppTestHarness.Options("Tab \"z\" Shift+Tab \"a\" Escape", out _));
            Assert.Equal("a", result["first"]);
            Assert.Equal("", result["disabled"]);
            Assert.Equal("z", result["last"]);
        }

        /// <summary>A pressed primary mouse event focuses its rendered row while release events leave focus alone.</summary>
        [Fact]
        public void MousePress_FocusesControlAtFrameCoordinates() {
            TextInput first = new TextInput("first");
            TextInput second = new TextInput("second");
            ConsoleAppOptions options = AppTestHarness.Options("", out _);
            options.Interactive!.Input = new ScriptedInput(new[] {
                InputEvent.Mouse(2, 4, 1, true), InputEvent.Character('b'),
                InputEvent.Mouse(2, 1, 1, false), InputEvent.Character('c'), InputEvent.Press(ConsoleKey.Escape)
            });
            AppState result = ConsoleApp.Run(new Rows().Add(first, LayoutSize.Fixed(3)).Add(second, LayoutSize.Fixed(3)), options: options);
            Assert.Equal("", result["first"]);
            Assert.Equal("bc", result["second"]);
        }

        /// <summary>Exhausting a scripted source returns the final values and allows the same layout to run again.</summary>
        [Fact]
        public void InputCompletion_ReturnsAndLaterRunResetsRequestedExit() {
            TextInput query = new TextInput("query");
            AppState state = ConsoleApp.Run(query, options: AppTestHarness.Options("\"a\" Escape", out _));
            Assert.True(state.ExitRequested);
            AppState result = ConsoleApp.Run(query, state, AppTestHarness.Options("\"b\"", out _));
            Assert.Same(state, result);
            Assert.Equal("ab", result["query"]);
            Assert.False(result.ExitRequested);
            query.Value = "detached";
            Assert.Equal("ab", result["query"]);
        }

        /// <summary>Cancellation after a committed edit releases the global input lease and detaches state subscriptions.</summary>
        [Fact]
        public void Cancellation_CleansUpAndAllowsLaterRunAndInputLease() {
            TextInput query = new TextInput("query");
            AppState state = new AppState();
            using (CancellationTokenSource cancellation = new CancellationTokenSource()) {
                EventHandler<AppValueChangedEventArgs> cancel = (_, __) => cancellation.Cancel();
                query.Changed += cancel;
                try { WithGlobalInput("\"a\" Escape", () => {
                    ConsoleAppOptions options = AppTestHarness.Options("", out _);
                    options.Interactive!.Input = null;
                    Assert.ThrowsAny<OperationCanceledException>(() => ConsoleApp.Run(query, state, options, cancellation.Token));
                    Assert.False(ConsoleInput.IsAcquired);
                }); } finally { query.Changed -= cancel; }
            }
            Assert.Equal("a", state["query"]);
            query.Value = "detached";
            Assert.Equal("a", state["query"]);
            WithGlobalInput("Escape", () => {
                using (InputLease lease = ConsoleInput.Acquire()) { Assert.Same(ConsoleInput.Override, lease.Source); }
                Assert.False(ConsoleInput.IsAcquired);
                ConsoleAppOptions options = AppTestHarness.Options("", out _);
                options.Interactive!.Input = null;
                Assert.Equal("ready", ConsoleApp.Run(new TextInput("query", "ready"), options: options)["query"]);
                Assert.False(ConsoleInput.IsAcquired);
            });
        }

        /// <summary>A callback exception propagates and cleanup permits a later run using the same control and state.</summary>
        [Fact]
        public void CallbackFailure_CleansUpAndAllowsLaterRun() {
            TextInput query = new TextInput("query");
            AppState state = new AppState();
            InvalidOperationException expected = new InvalidOperationException("Synthetic callback failure.");
            EventHandler<AppValueChangedEventArgs> fail = (_, __) => throw expected;
            query.Changed += fail;
            try {
                WithGlobalInput("\"a\"", () => {
                    ConsoleAppOptions options = AppTestHarness.Options("", out _);
                    options.Interactive!.Input = null;
                    Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => ConsoleApp.Run(query, state, options)));
                    Assert.False(ConsoleInput.IsAcquired);
                });
            } finally { query.Changed -= fail; }
            query.Value = "next";
            Assert.Equal("a", state["query"]);
            Assert.Equal("next", ConsoleApp.Run(query, state, AppTestHarness.Options("Escape", out _))["query"]);
        }

        /// <summary>Static snapshots initialize all values and bindings without reading any input even in noninteractive mode.</summary>
        [Fact]
        public void StaticSnapshot_RendersInitializedStateWithoutInput() {
            TextInput query = new TextInput("query", "seed");
            AppBinding summary = new AppBinding(state => new ConsoleMonster.Widgets.TextBlock("Query: " + state.Get<string>("query")));
            ConsoleAppOptions options = AppTestHarness.Options("\"unused\"", out ScreenCapture capture);
            ScriptedInput input = (ScriptedInput)options.Interactive!.Input!;
            int remaining = input.Remaining;
            options.Interactive.Mode = InteractionMode.None;
            options.Interactive.NonInteractive = true;
            options.StaticSnapshot = true;
            AppState result = ConsoleApp.Run(new Rows(query, summary), options: options);
            Assert.Equal("seed", result["query"]);
            Assert.Equal(remaining, input.Remaining);
            Assert.Contains("Query: seed", Assert.Single(capture.Frames).ToPlainText());
            query.Value = "detached";
            Assert.Equal("seed", result["query"]);
        }

        /// <summary>Non-rich modes fail before initializing state, invoking bindings or reading input.</summary>
        [Theory]
        [InlineData(InteractionMode.None)]
        [InlineData(InteractionMode.Line)]
        [InlineData(InteractionMode.Accessible)]
        public void NoninteractiveRun_RefusesBeforeAnyAppWork(InteractionMode mode) {
            int builds = 0;
            AppState state = new AppState();
            ConsoleAppOptions options = AppTestHarness.Options("Escape", out ScreenCapture capture);
            ScriptedInput input = (ScriptedInput)options.Interactive!.Input!;
            options.Interactive.Mode = mode;
            AppBinding binding = new AppBinding(_ => { builds++; return new ConsoleMonster.Widgets.TextBlock("unused"); });
            Assert.Throws<PromptUnavailableException>(() => ConsoleApp.Run(new Rows(new TextInput("query"), binding), state, options));
            Assert.Empty(state);
            Assert.Empty(capture.Frames);
            Assert.Equal(0, builds);
            Assert.Equal(1, input.Remaining);
        }

        /// <summary>Inline output is rejected before callbacks or input can use a moving screen origin.</summary>
        [Fact]
        public void Run_RejectsInlineBeforeInitializingStateOrInput() {
            ConsoleAppOptions options = AppTestHarness.Options("Escape", out ScreenCapture capture);
            options.Live = new LiveSessionOptions { Mode = LiveMode.Inline };
            AppState state = new AppState();
            int callbacks = 0;
            AppBinding binding = new AppBinding(_ => { callbacks++; return new ConsoleMonster.Widgets.TextBlock("value"); });
            Assert.Throws<ArgumentException>(() => ConsoleApp.Run(new Rows(new TextInput("query"), binding), state, options));
            Assert.Empty(state);
            Assert.Empty(capture.Frames);
            Assert.Equal(0, callbacks);
            Assert.False(ConsoleInput.IsAcquired);
        }

        private static void WithGlobalInput(string script, Action run) {
            IInputSource? previous = ConsoleInput.Override;
            using (ScriptedInput input = new ScriptedInput(script)) {
                ConsoleInput.Override = input;
                try { run(); }
                finally { ConsoleInput.Override = previous; }
            }
        }
    }

}
