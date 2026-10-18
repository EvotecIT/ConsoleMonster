using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Output {
    /// <summary>The state of the terminal's progress indicator (the tab and taskbar progress in Windows Terminal).</summary>
    public enum TaskbarState {
        /// <summary>No progress shown.</summary>
        Clear = 0,

        /// <summary>A percentage.</summary>
        Normal = 1,

        /// <summary>An error (red), with a percentage.</summary>
        Error = 2,

        /// <summary>Work without a known percentage.</summary>
        Indeterminate = 3,

        /// <summary>Paused or warnings (yellow), with a percentage.</summary>
        Paused = 4
    }

    /// <summary>
    /// A widget that contributes to the terminal's progress indicator while it is in a live session: progress bars and
    /// lanes report a percentage, spinners and activities report work in progress, a dashboard reports failures and
    /// warnings. Implement it on custom widgets to take part.
    /// </summary>
    public interface ITaskbarProgressSource {
        /// <summary>The widget's contribution, if any.</summary>
        /// <param name="state">The state (Normal, Indeterminate, Error or Paused).</param>
        /// <param name="fraction">The fraction done (0 to 1), or NaN when unknown.</param>
        /// <returns>False when the widget has nothing to report.</returns>
        bool TryGetTaskbarProgress(out TaskbarState state, out double fraction);
    }

    /// <summary>
    /// Drives the terminal's progress indicator with <c>OSC 9;4</c>: in Windows Terminal the tab and the taskbar button show
    /// a percentage, an error, warnings or indeterminate work. Live sessions, <c>Write-ConsoleStatus</c> and
    /// <see cref="Terminal.Status(string, Action{StatusContext})"/> drive it automatically, and it is cleared when they end,
    /// on Ctrl+C and on process exit. It is only written to terminals known to show it (Windows Terminal, ConEmu), never in
    /// CI, redirected or plain output, and <c>CONSOLEMONSTER_TASKBAR=0</c> or <see cref="Enabled"/> turns it off.
    /// </summary>
    public static class TaskbarProgress {
        private static readonly object Gate = new object();
        private static TaskbarState _state = TaskbarState.Clear;
        private static int _percent = -1;
        private static int _hooked;

        /// <summary>Whether the indicator may be used at all (default true; <c>CONSOLEMONSTER_TASKBAR=0</c> turns it off).</summary>
        public static bool Enabled { get; set; } = !string.Equals(Environment.GetEnvironmentVariable("CONSOLEMONSTER_TASKBAR"), "0", StringComparison.Ordinal)
            && !string.Equals(Environment.GetEnvironmentVariable("CONSOLEMONSTER_TASKBAR"), "false", StringComparison.OrdinalIgnoreCase);

        /// <summary>The state last written.</summary>
        public static TaskbarState Current {
            get {
                lock (Gate) {
                    return _state;
                }
            }
        }

        /// <summary>Whether a terminal shows the indicator: Windows Terminal (or ConEmu), interactive VT output, not CI, not redirected.</summary>
        /// <param name="profile">The profile.</param>
        /// <returns>True when the sequence may be written.</returns>
        public static bool IsSupported(TerminalProfile profile) {
            if (profile == null || !Enabled) {
                return false;
            }
            bool known = profile.Kind == TerminalKind.WindowsTerminal
                || string.Equals(Environment.GetEnvironmentVariable("ConEmuANSI"), "ON", StringComparison.OrdinalIgnoreCase);
            return known && profile.Mode == OutputMode.Interactive && profile.SupportsVirtualTerminal && !profile.IsOutputRedirected && !profile.IsContinuousIntegration;
        }

        /// <summary>The escape sequence for a state: <c>ESC ] 9 ; 4 ; state ; percent BEL</c>.</summary>
        /// <param name="state">The state.</param>
        /// <param name="percent">The percentage, 0 to 100 (ignored for Clear and Indeterminate).</param>
        /// <returns>The sequence.</returns>
        public static string Sequence(TaskbarState state, int percent) {
            int value = state == TaskbarState.Clear || state == TaskbarState.Indeterminate ? 0 : Math.Max(0, Math.Min(100, percent));
            return "\u001b]9;4;" + ((int)state).ToString(CultureInfo.InvariantCulture) + ";" + value.ToString(CultureInfo.InvariantCulture) + "\u0007";
        }

        /// <summary>
        /// Combines contributions: an error wins, then warnings, then a percentage (the average of those that have one), then
        /// indeterminate work.
        /// </summary>
        /// <param name="sources">The contributions.</param>
        /// <param name="percent">The combined percentage.</param>
        /// <returns>The combined state (Clear when nothing reports).</returns>
        public static TaskbarState Combine(IEnumerable<KeyValuePair<TaskbarState, double>> sources, out int percent) {
            bool error = false;
            bool paused = false;
            bool any = false;
            double sum = 0;
            int counted = 0;
            foreach (KeyValuePair<TaskbarState, double> source in sources) {
                if (source.Key == TaskbarState.Clear) {
                    continue;
                }
                any = true;
                error |= source.Key == TaskbarState.Error;
                paused |= source.Key == TaskbarState.Paused;
                if (!double.IsNaN(source.Value)) {
                    sum += Math.Max(0, Math.Min(1, source.Value));
                    counted++;
                }
            }
            percent = counted == 0 ? (error || paused ? 100 : 0) : (int)Math.Round(sum / counted * 100);
            if (!any) {
                return TaskbarState.Clear;
            }
            if (error) {
                return TaskbarState.Error;
            }
            if (paused) {
                return TaskbarState.Paused;
            }
            return counted > 0 ? TaskbarState.Normal : TaskbarState.Indeterminate;
        }

        /// <summary>Shows a state on the console when it shows the indicator; repeated states are not written again.</summary>
        /// <param name="state">The state.</param>
        /// <param name="percent">The percentage, 0 to 100.</param>
        public static void Set(TaskbarState state, int percent = 0) {
            if (!Terminal.IsConsole(Terminal.Target)) {
                return;
            }
            TerminalProfile profile = Terminal.GetCurrentProfile();
            if (!IsSupported(profile)) {
                return;
            }
            Write(state, percent, profile);
        }

        /// <summary>Clears the indicator if it shows anything.</summary>
        public static void Clear() {
            lock (Gate) {
                if (_state == TaskbarState.Clear) {
                    return;
                }
            }
            try {
                Write(TaskbarState.Clear, 0, Terminal.GetConsoleProfile());
            } catch (Exception) {
                // An exit path must not fail.
            }
        }

        /// <summary>Rings the terminal bell (BEL) on the console, for a finished run; not in CI or redirected output.</summary>
        public static void Bell() {
            TerminalProfile profile = Terminal.GetConsoleProfile();
            if (Terminal.IsConsole(Terminal.Target) && profile.Mode == OutputMode.Interactive && !profile.IsOutputRedirected && !profile.IsContinuousIntegration) {
                Terminal.WriteControl(profile, "\u0007");
            }
        }

        private static void Write(TaskbarState state, int percent, TerminalProfile profile) {
            int value = state == TaskbarState.Clear || state == TaskbarState.Indeterminate ? 0 : Math.Max(0, Math.Min(100, percent));
            lock (Gate) {
                if (state == _state && value == _percent) {
                    return;
                }
                _state = state;
                _percent = value;
            }
            if (state != TaskbarState.Clear) {
                Hook();
            }
            Terminal.WriteControl(profile, Sequence(state, value));
        }

        // The indicator outlives the process in Windows Terminal, so it is cleared on exit and Ctrl+C.
        private static void Hook() {
            if (Interlocked.Exchange(ref _hooked, 1) != 0) {
                return;
            }
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Clear();
            try {
                Console.CancelKeyPress += (sender, e) => Clear();
            } catch (Exception) {
                // No console for Ctrl+C: process exit still clears.
            }
        }
    }
}
