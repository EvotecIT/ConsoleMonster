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
    public static class ConsoleApp {
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
            InteractiveOptions interactive = settings.Interactive ?? InteractiveOptions.Default;
            TerminalProfile profile = interactive.ResolveProfile();
            if (!settings.StaticSnapshot && interactive.ResolveMode() != InteractionMode.Rich) { throw new PromptUnavailableException(AppMessages.Get("Unavailable", profile.Language)); }
            cancellationToken.ThrowIfCancellationRequested();
            AppState values = state ?? new AppState();
            values.BeginRun();
            using InputLease? lease = settings.StaticSnapshot ? null : ConsoleInput.Acquire(interactive.Input);
            RenderContext context = Terminal.CreateContext(profile, interactive.ResolveTheme());
            using (AppController app = new AppController(layout, values, context, settings.ReflowBelow)) {
                if (settings.StaticSnapshot) {
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
                using (MouseReporting? mouse = OpenMouse(settings, interactive, profile, live, lease!.Source))
                using (LiveSession session = new LiveSession(app.Root, live)) {
                    session.Start();
                    try {
                        while (!values.ExitRequested && session.IsRunning) {
                            cancellationToken.ThrowIfCancellationRequested();
                            settings.ProcessEvents?.Invoke(values);
                            app.Refresh(session);
                            if (values.ExitRequested) { break; }
                            InputEvent? input;
                            try { input = lease.Source.Read(TimeSpan.FromMilliseconds(30), cancellationToken); }
                            catch (InputClosedException) { break; }
                            if (input != null) {
                                if (interactive.Capture != null && input.Kind == InputEventKind.Resize && input.Width > 0 && input.Height > 0) {
                                    Interlocked.Exchange(ref captureWindow, new[] { 0, 0, input.Width, input.Height });
                                }
                                if (input.Kind == InputEventKind.Mouse && lease.Source is WindowsConsoleInput
                                    && TerminalSize.TryGetWindow(out int left, out int top, out _, out _)) {
                                    if (input.Column < left || input.Row < top) { continue; }
                                    input = InputEvent.Mouse(input.Column - left, input.Row - top, input.MouseButton, input.MousePressed, input.Modifiers);
                                }
                                app.Handle(input, settings.Mouse);
                            }
                        }
                        if (session.Failure == null) {
                            settings.ProcessEvents?.Invoke(values);
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

        private static MouseReporting? OpenMouse(ConsoleAppOptions settings, InteractiveOptions interactive, TerminalProfile profile, LiveSessionOptions live, IInputSource input) {
            if (!settings.Mouse || !(input is ConsoleKeyInput) || live.Output != null || interactive.Capture != null
                || !profile.SupportsVirtualTerminal || profile.IsContinuousIntegration || profile.IsOutputRedirected || profile.Mode != OutputMode.Interactive) { return null; }
            ITerminalWriter? writer = interactive.ResolveTarget().Writer;
            bool owns = writer == null;
            writer ??= TerminalWriterFactory.Create(profile);
            if (!writer.IsConsole) { if (owns) { writer.Dispose(); } return null; }
            return new MouseReporting(writer, owns);
        }
    }
}
