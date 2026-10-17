using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    // The app's thread rules and action output.
    //
    // Everything that touches the screen or the keyboard runs on the menu's own thread (the one that owns the input).
    // Async actions run on a worker; when they ask a question, start a run or show a toast, the call is queued for the
    // menu's thread, which carries it out on its next tick while the worker waits for the answer. Calls from any other
    // thread are refused with a clear error instead of racing the menu for the console.
    //
    // While an action runs, what it writes to Console.Out/Console.Error (and, from PowerShell, Write-Host, warnings and
    // errors) is collected instead of drawn over the app, then shown as a toast (one line) or an output dialog (more).
    internal sealed partial class MenuApp {
        private readonly int _ownerThread = Thread.CurrentThread.ManagedThreadId;
        private readonly ConcurrentQueue<PendingCall> _calls = new ConcurrentQueue<PendingCall>();
        private ActionOutput? _output;
        private ConsoleCapture? _capture;
        private string? _outputTitle;

        private bool OnOwnerThread => Thread.CurrentThread.ManagedThreadId == _ownerThread;

        MenuLayout IMenuHost.Layout => Layout;

        void IMenuHost.ShowToast(Toast toast) => Post(() => ShowToast(toast));

        T IMenuHost.Ask<T>(Prompt<T> prompt, CancellationToken cancellationToken) => OnLoop(() => Ask(prompt, cancellationToken), cancellationToken);

        RunResult IMenuHost.Run(MenuContext context, IWidget dashboard, Func<RunContext, RunResult?> work, LiveSessionOptions? options) {
            return OnLoop(() => Run(context, dashboard, work, options), context.CancellationToken);
        }

        void IMenuHost.ShowResult(RunResult result) => Post(() => ShowResult(result));

        void IMenuHost.Open(MenuItem item) => Post(() => Open(item));

        void IMenuHost.Exit() => Post(Exit);

        void IMenuHost.Log(string text, StatusKind status) {
            ActionOutput? output = _output;
            if (output != null) {
                output.Add(text, status);
                return;
            }
            Post(() => ShowToast(new Toast(text, status)));
        }

        /// <summary>Runs a call on the menu's thread: directly when already there, queued from an async action's worker.</summary>
        private T OnLoop<T>(Func<T> call, CancellationToken cancellationToken) {
            if (OnOwnerThread) {
                return call();
            }
            EnsureAsyncCaller();
            PendingCall pending = new PendingCall(() => call());
            _calls.Enqueue(pending);
            pending.Wait(cancellationToken);
            return (T)pending.Result!;
        }

        private void Post(Action call) {
            if (OnOwnerThread) {
                call();
                return;
            }
            EnsureAsyncCaller();
            _calls.Enqueue(new PendingCall(() => {
                call();
                return null;
            }));
        }

        private void EnsureAsyncCaller() {
            if (_asyncAction == null) {
                throw new InvalidOperationException("Menu services (prompts, runs, toasts, navigation) can only be used from the menu's own thread or from an async action while it runs.");
            }
        }

        /// <summary>Carries out calls queued by an async action; returns whether anything ran.</summary>
        private bool DrainCalls() {
            bool ran = false;
            while (_calls.TryDequeue(out PendingCall? pending)) {
                ran = true;
                pending.Execute(this);
            }
            return ran;
        }

        /// <summary>
        /// Ends the app's background work when the loop finishes: cancels a running async action and fails the calls it
        /// queued, so its worker never waits for a menu that is gone.
        /// </summary>
        internal void Shutdown() {
            try {
                _asyncCancel?.Cancel();
            } catch (ObjectDisposedException) {
            }
            while (_calls.TryDequeue(out PendingCall? pending)) {
                pending.Fail(new OperationCanceledException("The menu has closed."));
            }
            EndOutputSilently();
        }

        private void EndOutputSilently() {
            _capture?.Dispose();
            _capture = null;
            _output = null;
        }

        // ---- action output ---------------------------------------------------------------------------------------

        /// <summary>Starts collecting what an action writes; returns false when an outer action already collects.</summary>
        private bool BeginOutput(string title) {
            if (_output != null) {
                return false;
            }
            ActionOutput output = new ActionOutput();
            _output = output;
            _outputTitle = title;
            try {
                _capture = new ConsoleCapture((line, stream) => output.Add(line, stream == "err" ? StatusKind.Fail : StatusKind.Info));
            } catch (Exception ex) when (ex is System.Security.SecurityException || ex is InvalidOperationException) {
                _capture = null;
            }
            return true;
        }

        /// <summary>Stops collecting and shows what was written.</summary>
        private void EndOutput() {
            ActionOutput? output = _output;
            _capture?.Dispose();
            _capture = null;
            _output = null;
            if (output == null) {
                return;
            }
            List<OutputLine> lines = output.Take();
            if (lines.Count == 0) {
                return;
            }
            if (lines.Count == 1 && output.Dropped == 0) {
                OutputLine line = lines[0];
                ShowToast(new Toast(line.Text, line.Status == StatusKind.Info ? StatusKind.Info : line.Status, TimeSpan.FromSeconds(4)));
                return;
            }
            ShowModal(new OutputDialog((_outputTitle ?? _root.Title) + " " + InteractiveGlyphs.For(_context).Separator + " " + Labels.Output, lines, output.Dropped, Labels));
        }

        private sealed class PendingCall {
            private readonly Func<object?> _call;
            private readonly ManualResetEventSlim _done = new ManualResetEventSlim(false);
            private Exception? _error;

            public PendingCall(Func<object?> call) {
                _call = call;
            }

            public object? Result { get; private set; }

            public void Execute(MenuApp app) {
                Func<Canvas.RenderContext, IPromptSurface?>? previous = PromptHost.Current;
                PromptHost.Current = app.ResolvePromptSurface;
                try {
                    Result = _call();
                } catch (Exception ex) {
                    _error = ex;
                } finally {
                    PromptHost.Current = previous;
                    _done.Set();
                }
            }

            public void Fail(Exception error) {
                _error = error;
                _done.Set();
            }

            public void Wait(CancellationToken cancellationToken) {
                _done.Wait(cancellationToken);
                if (_error != null) {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_error).Throw();
                }
            }
        }
    }
}
