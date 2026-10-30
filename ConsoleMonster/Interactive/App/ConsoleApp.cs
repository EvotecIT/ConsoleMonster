using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;

namespace ConsoleMonster.Interactive {
    /// <summary>Runs reactive input widgets in an ordinary layout, with callbacks on the input thread and live cell diffing.</summary>
    public static partial class ConsoleApp {
        /// <summary>Runs until Escape, input completion, cancellation or a requested exit, and returns the final state.</summary>
        /// <param name="layout">A layout containing named inputs and dependent display bindings.</param>
        /// <param name="state">Shared values, or null to create an empty state.</param>
        /// <param name="options">Input and rendering configuration.</param>
        /// <param name="cancellationToken">Cancels input and restores the terminal before returning control.</param>
        /// <returns>The final state, including every named input's committed value.</returns>
        /// <exception cref="PromptUnavailableException">Interactive input is unavailable and a static snapshot was not requested.</exception>
        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        /// <exception cref="ArgumentException">Inline live mode was requested; interactive apps require a stable full-screen origin.</exception>
        public static AppState Run(IWidget layout, AppState? state = null, ConsoleAppOptions? options = null, CancellationToken cancellationToken = default) {
            if (layout == null) { throw new ArgumentNullException(nameof(layout)); }
            ConsoleAppOptions settings = options ?? new ConsoleAppOptions();
            if (settings.ReflowBelow < 1) { throw new ArgumentOutOfRangeException(nameof(options), AppMessages.Get("ReflowWidth")); }
            if (settings.Live?.Mode == LiveMode.Inline) { throw new ArgumentException(AppMessages.Get("FullScreen"), nameof(options)); }
            // Capability detection may enable Windows VT on Terminal's very first use. The session's
            // own guard starts later, so retain the entry mode across initialization and all exit paths.
            using TerminalGuard outputMode = new TerminalGuard(() => { }, false);
            InteractiveOptions interactive = (settings.Interactive ?? InteractiveOptions.Default).Clone();
            if (interactive.Capture == null && interactive.ResolveTarget() is RecordingTarget recording) {
                interactive.Capture = recording.Screens;
            }
            TerminalProfile profile = interactive.ResolveProfile();
            bool snapshot = interactive.ResolveMode() == InteractionMode.Snapshot || (SnapshotRequested() && interactive.Input == null && ConsoleInput.Override == null);
            if (!snapshot && interactive.ResolveMode() != InteractionMode.Rich) { throw new PromptUnavailableException(AppMessages.Get("Unavailable", profile.Language)); }
            cancellationToken.ThrowIfCancellationRequested();
            AppState values = state ?? new AppState();
            values.BeginRun();
            using InputLease? lease = snapshot ? null : ConsoleInput.Acquire(interactive.Input);
            if (settings.SignalKeys) {
                if (lease?.Source is WindowsConsoleInput windows) { windows.EnableCancelKeyProcessing(); }
                else if (lease?.Source is ConsoleKeyInput console) { console.EnableCancelKeyProcessing(); }
            } else if (lease?.Source is ConsoleKeyInput console) {
                // The initial frame and ProcessEvents callback can run before the first Read.
                console.Activate();
            }
            RenderContext context = Terminal.CreateContext(profile, interactive.ResolveTheme());
            using (AppController app = new AppController(layout, values, context, settings.ReflowBelow, settings, cancellationToken, interactive.ActionClock)) {
                if (snapshot) {
                    CellBuffer frame = FrameComposer.RenderBlock(app.Root, profile.Width, context);
                    if (interactive.Capture != null) { interactive.Capture.Surface.Present(frame, null); }
                    else if (settings.Live?.Output != null) { settings.Live.Output.WriteBlock(frame); }
                    else {
                        using (ITerminalOutput output = Terminal.CreateOutput(profile, interactive.ResolveTarget().Writer)) { output.WriteBlock(frame); }
                    }
                    return values;
                }
                CellBuffer initial = app.PrepareFrame(profile.Width, profile.Height);
                if (interactive.Capture != null) { interactive.Capture.Surface.Present(initial, null); }
                LiveSessionOptions live = settings.Live ?? new LiveSessionOptions {
                    CaptureConsole = interactive.Capture == null, TaskbarProgress = interactive.Capture == null
                };
                live.Profile ??= profile;
                live.Theme ??= context.Theme;
                live.Target ??= interactive.ResolveTarget();
                live.PrintFinalState = false;
                int[] captureWindow = new[] { 0, 0, profile.Width, profile.Height };
                if (interactive.Capture != null) {
                    live.Output = new AppCaptureOutput(interactive.Capture, profile);
                    live.WindowSource = () => Volatile.Read(ref captureWindow);
                }
                using (MouseReporting? mouse = MouseReporting.Open(settings.Mouse, interactive, profile, lease!.Source, live.Output != null))
                using (LiveSession session = new LiveSession(app.Root, live)) {
                    session.Start();
                    using IDisposable? handoff = settings.TerminalHandoff?.Attach(lease.Source, session, profile, cancellationToken);
                    bool eventsProcessed = false;
                    try {
                        AppEventLoop.Run(lease.Source, () => values.ExitRequested || !session.IsRunning, () => {
                            if (!eventsProcessed) { settings.ProcessEvents?.Invoke(values); }
                            eventsProcessed = false;
                            app.Refresh(session);
                        }, input => {

                                if (interactive.Capture != null && input.Kind == InputEventKind.Resize && input.Width > 0 && input.Height > 0) {
                                    Interlocked.Exchange(ref captureWindow, new[] { 0, 0, input.Width, input.Height });
                                }
                                app.Handle(input, settings.Mouse);
                                // Drain pipeline-owned callbacks before another event can capture an action's selection.
                                settings.ProcessEvents?.Invoke(values);
                                app.Refresh(session);
                                eventsProcessed = true;
                        }, cancellationToken, endOnInputClosed: true);
                        if (session.Failure == null) {
                            if (!eventsProcessed) { settings.ProcessEvents?.Invoke(values); }
                            app.Refresh(session);
                        }
                    } finally {
                        session.Stop();
                        if (session.Failure == null && interactive.Capture != null) {
                            int[] finalWindow = Volatile.Read(ref captureWindow);
                            interactive.Capture.Surface.Present(app.PrepareFrame(finalWindow[2], finalWindow[3]), null);
                        }
                    }
                    if (session.Failure is Exception failure) { ExceptionDispatchInfo.Capture(failure).Throw(); }
                    return values;
                }
            }
        }

        /// <summary>Runs the same input loop on a worker thread, preserving callback and input-lease thread affinity.</summary>
        /// <param name="layout">The layout.</param>
        /// <param name="state">Shared values, or null.</param>
        /// <param name="options">Input and rendering options.</param>
        /// <param name="cancellationToken">Cancels the application.</param>
        /// <returns>The final application state.</returns>
        public static Task<AppState> RunAsync(IWidget layout, AppState? state = null, ConsoleAppOptions? options = null, CancellationToken cancellationToken = default) =>
            Task.Run(() => Run(layout, state, options, cancellationToken), cancellationToken);

        private static bool SnapshotRequested() {
            string? value = Environment.GetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT");
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }

    }
}
