using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Localization;
using ConsoleMonster.Output;

namespace ConsoleMonster.Live.Tails {
    /// <summary>Runs a reusable log tail from a file, event reader or other caller-owned polling source.</summary>
    public static class ConsoleLogTail {
        /// <summary>Follows a polling source until Count, cancellation, input closure, q, Escape or Ctrl+C.</summary>
        /// <param name="read">Returns currently available completed entries. Called synchronously; it must return promptly.</param>
        /// <param name="options">Configuration, or null for full-screen defaults with automatic plain fallback.</param>
        /// <param name="cancellationToken">Cooperative cancellation. Cancellation ends normally.</param>
        /// <remarks>
        /// The caller owns the reader and its resources. Redirected, CI, plain and non-live outputs never acquire input.
        /// Plain output preserves original lines without added clocks, level tags or markup processing.
        /// Unknown source timestamps stay blank in the live timestamp column. The live view uses each entry's display
        /// message so recognized metadata is shown once; original lines remain intact for plain output and filtering.
        /// </remarks>
        public static void Run(Func<IReadOnlyList<TailedLogEntry>> read, LogTailOptions? options = null,
            CancellationToken cancellationToken = default) {
            if (read == null) { throw new ArgumentNullException(nameof(read)); }
            options = options ?? new LogTailOptions();
            options.Validate();
            if (cancellationToken.IsCancellationRequested) { return; }
            // Profile detection may enable Windows VT. Preserve the entry mode before resolving it.
            using (TerminalGuard modeGuard = new TerminalGuard(() => { }, false)) {
                TerminalProfile profile = options.Profile ?? Terminal.Target.Profile;
                bool plain = options.Plain || profile.IsOutputRedirected || profile.IsContinuousIntegration
                    || profile.Mode != OutputMode.Interactive || !profile.SupportsVirtualTerminal
                    || (options.Input == null && Terminal.Target.Input == null && ConsoleInput.Override == null && Console.IsInputRedirected)
                    || (options.Output != null && !options.Output.SupportsLiveFrames);
                using (WidgetLabels.Scope(WidgetLabels.For(profile.Language))) {
                    if (plain) { RunPlain(read, options, profile, cancellationToken); }
                    else { RunLive(read, options, profile, cancellationToken); }
                }
            }
        }

        private static void RunPlain(Func<IReadOnlyList<TailedLogEntry>> read, LogTailOptions options,
            TerminalProfile profile, CancellationToken cancellationToken) {
            ITerminalWriter? writer = null;
            bool ownsWriter = false;
            try {
                Action<string>? write = options.WriteLine;
                if (write == null) {
                    writer = Terminal.Target.Writer;
                    if (writer == null) { writer = TerminalWriterFactory.Create(profile); ownsWriter = true; }
                    ITerminalWriter output = writer;
                    write = text => { output.Write(text + Environment.NewLine); output.Flush(); };
                }
                using (TerminalGuard guard = new TerminalGuard(() => { }, options.HandleCancelKey))
                using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, guard.Cancellation)) {
                    Poll(read, options, entry => write(entry.Text), null, linked.Token);
                }
            } finally {
                if (ownsWriter) { writer?.Dispose(); }
            }
        }

        private static void RunLive(Func<IReadOnlyList<TailedLogEntry>> read, LogTailOptions options,
            TerminalProfile profile, CancellationToken cancellationToken) {
            LogTailView view = new LogTailView(options.Capacity, highlight: options.Highlight) { TimestampFormat = options.TimestampFormat };
            using (LiveSession session = new LiveSession(view, new LiveSessionOptions {
                Mode = options.Mode, Profile = profile, Theme = options.Theme, Output = options.Output,
                CaptureConsole = false, HandleCancelKey = options.HandleCancelKey, PrintFinalState = false,
                TaskbarProgress = false, StreamHeartbeat = TimeSpan.Zero, StreamTimestamps = false, Discoverable = false
            })) {
                session.Start();
                try {
                    using (InputLease lease = ConsoleInput.Acquire(options.Input ?? Terminal.Target.Input))
                    using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.Cancellation)) {
                        Poll(read, options, entry => view.Post(session, entry), input => {
                            if (LogTailKeys.Quit(input)) { return false; }
                            view.Navigate(session, input);
                            return true;
                        }, linked.Token, lease.Source);
                    }
                } finally { session.Stop(); }
                if (session.Failure != null) { throw new InvalidOperationException(WidgetLabels.Current.TailSessionFailed, session.Failure); }
                if (session.ApplyErrors > 0 || session.RenderErrors > 0) {
                    throw new InvalidOperationException(WidgetLabels.Current.TailViewFailed, session.LastError);
                }
            }
        }

        private static void Poll(Func<IReadOnlyList<TailedLogEntry>> read, LogTailOptions options,
            Action<TailedLogEntry> accept, Func<InputEvent, bool>? key, CancellationToken cancellationToken, IInputSource? input = null) {
            Stopwatch watch = Stopwatch.StartNew();
            TimeSpan nextRead = TimeSpan.Zero;
            long count = 0;
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    if (watch.Elapsed >= nextRead) {
                        IReadOnlyList<TailedLogEntry> entries = read() ?? throw new InvalidOperationException(WidgetLabels.Current.TailReaderReturnedNull);
                        foreach (TailedLogEntry entry in entries) {
                            if (cancellationToken.IsCancellationRequested) { return; }
                            if (entry == null) { throw new InvalidOperationException(WidgetLabels.Current.TailReaderReturnedNullEntry); }
                            if (string.IsNullOrEmpty(options.Filter) || entry.Text.IndexOf(options.Filter, StringComparison.OrdinalIgnoreCase) >= 0) {
                                accept(entry);
                            }
                        }
                        count++;
                        if (options.Count > 0 && count >= options.Count) { return; }
                        nextRead = watch.Elapsed + options.PollInterval;
                    }
                    TimeSpan wait = nextRead - watch.Elapsed;
                    if (wait <= TimeSpan.Zero) { continue; }
                    if (input == null) { cancellationToken.WaitHandle.WaitOne(wait); }
                    else {
                        InputEvent? pressed = input.Read(wait > TimeSpan.FromMilliseconds(50) ? TimeSpan.FromMilliseconds(50) : wait, cancellationToken);
                        if (pressed != null && key != null && !key(pressed)) { return; }
                    }
                }
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                // The reader and the input source may both honor the same cooperative stop.
            } catch (InputClosedException) when (input != null) {
                // A closed interactive source ends the tail and releases its lease.
            }
        }
    }
}
