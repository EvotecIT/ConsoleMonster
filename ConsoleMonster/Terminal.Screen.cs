using System;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;

namespace ConsoleMonster {
    public static partial class Terminal {
        private const string HideCursorSequence = "\u001b[?25l";
        private const string ShowCursorSequence = "\u001b[?25h";
        private static int _cursorHidden;
        private static int _cursorHooks;

        /// <summary>
        /// Clears the screen and its scrollback and puts the cursor at the top left, as <c>Console.Clear</c> and Spectre's
        /// <c>AnsiConsole.Clear</c> do. A no-op where clearing makes no sense: redirected output, CI logs, plain or stream
        /// mode, and while a live session owns the console (its area would be lost).
        /// </summary>
        public static void Clear() {
            TerminalProfile p = GetCurrentProfile();
            if (!CanControlScreen(p) || ConsoleOwner != null) {
                return;
            }
            if (p.SupportsVirtualTerminal) {
                WriteControl(p, "\u001b[2J\u001b[3J\u001b[H");
                return;
            }
            try {
                Console.Clear();
            } catch (Exception) {
                // No console window after all (a host without one): nothing to clear.
            }
        }

        /// <summary>
        /// Hides the cursor until the returned handle is disposed or <see cref="ShowCursor"/> is called. The cursor is also
        /// shown again when the process exits or Ctrl+C ends it. A no-op in redirected output, CI logs and plain mode.
        /// </summary>
        /// <returns>A handle that shows the cursor again when disposed.</returns>
        public static IDisposable HideCursor() {
            TerminalProfile p = GetCurrentProfile();
            if (!CanControlScreen(p)) {
                return new CursorHandle(false);
            }
            HookCursorRestore();
            Interlocked.Exchange(ref _cursorHidden, 1);
            SetCursor(p, false);
            return new CursorHandle(true);
        }

        /// <summary>Shows the cursor (after <see cref="HideCursor"/>). A no-op in redirected output, CI logs and plain mode.</summary>
        public static void ShowCursor() {
            TerminalProfile p = GetCurrentProfile();
            Interlocked.Exchange(ref _cursorHidden, 0);
            if (CanControlScreen(p)) {
                SetCursor(p, true);
            }
        }

        /// <summary>Whether <see cref="HideCursor"/> has hidden the cursor and it has not been shown again.</summary>
        public static bool IsCursorHidden => Volatile.Read(ref _cursorHidden) == 1;

        private static bool CanControlScreen(TerminalProfile p) {
            return p.Mode == OutputMode.Interactive && !p.IsOutputRedirected && !p.IsContinuousIntegration;
        }

        private static void SetCursor(TerminalProfile p, bool visible) {
            if (p.SupportsVirtualTerminal) {
                WriteControl(p, visible ? ShowCursorSequence : HideCursorSequence);
                return;
            }
            try {
                Console.CursorVisible = visible;
            } catch (Exception) {
                // Not supported by this console (or not a console).
            }
        }

        private static void WriteControl(TerminalProfile p, string sequence) {
            lock (Gate) {
                try {
                    using (ITerminalWriter writer = TerminalWriterFactory.Create(p)) {
                        writer.Write(sequence);
                        writer.Flush();
                    }
                } catch (Exception) {
                    // The console went away: a control sequence is not worth a failure.
                }
            }
        }

        private static void HookCursorRestore() {
            if (Interlocked.Exchange(ref _cursorHooks, 1) != 0) {
                return;
            }
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => RestoreCursor();
            try {
                Console.CancelKeyPress += (sender, e) => RestoreCursor();
            } catch (Exception) {
                // No console to receive Ctrl+C (a service or some hosts): process exit still restores.
            }
        }

        private static void RestoreCursor() {
            if (Interlocked.Exchange(ref _cursorHidden, 0) == 1) {
                try {
                    SetCursor(GetCurrentProfile(), true);
                } catch (Exception) {
                    // An exit handler must not fail.
                }
            }
        }

        private sealed class CursorHandle : IDisposable {
            private int _active;

            internal CursorHandle(bool active) {
                _active = active ? 1 : 0;
            }

            public void Dispose() {
                if (Interlocked.Exchange(ref _active, 0) == 1) {
                    ShowCursor();
                }
            }
        }

        /// <summary>
        /// Runs work under a one-line status with a spinner (Spectre's <c>AnsiConsole.Status().Start</c>):
        /// <c>Terminal.Status("Resolving…", ctx =&gt; { … ctx.Update("Querying [accent]dc01[/]"); })</c>. The line is a transient
        /// inline live area: output written meanwhile scrolls above it, and it is removed when the work ends, even when it throws. Redirected output, CI logs and plain mode get each
        /// status text once as a plain line instead (see <see cref="StatusContext"/>).
        /// </summary>
        /// <param name="markup">The first status text, as markup.</param>
        /// <param name="action">The work.</param>
        public static void Status(string markup, Action<StatusContext> action) {
            if (action == null) {
                throw new ArgumentNullException(nameof(action));
            }
            using (StatusContext context = new StatusContext(GetCurrentProfile(), markup)) {
                action(context);
            }
        }

        /// <summary>Runs work that returns a value under a status (see <see cref="Status(string, Action{StatusContext})"/>).</summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="markup">The first status text, as markup.</param>
        /// <param name="func">The work.</param>
        /// <returns>The work's result.</returns>
        public static T Status<T>(string markup, Func<StatusContext, T> func) {
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }
            using (StatusContext context = new StatusContext(GetCurrentProfile(), markup)) {
                return func(context);
            }
        }

        /// <summary>Runs asynchronous work under a status (see <see cref="Status(string, Action{StatusContext})"/>).</summary>
        /// <param name="markup">The first status text, as markup.</param>
        /// <param name="action">The work.</param>
        /// <returns>A task that completes when the work has finished and the status is removed.</returns>
        public static async Task StatusAsync(string markup, Func<StatusContext, Task> action) {
            if (action == null) {
                throw new ArgumentNullException(nameof(action));
            }
            using (StatusContext context = new StatusContext(GetCurrentProfile(), markup)) {
                await action(context).ConfigureAwait(false);
            }
        }

        /// <summary>Runs asynchronous work that returns a value under a status.</summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="markup">The first status text, as markup.</param>
        /// <param name="func">The work.</param>
        /// <returns>The work's result.</returns>
        public static async Task<T> StatusAsync<T>(string markup, Func<StatusContext, Task<T>> func) {
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }
            using (StatusContext context = new StatusContext(GetCurrentProfile(), markup)) {
                return await func(context).ConfigureAwait(false);
            }
        }
    }
}
