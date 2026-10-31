using System;
using System.Threading;
using ConsoleMonster.Output;

namespace ConsoleMonster {
    public static partial class Terminal {
        private static readonly AsyncLocal<ITerminalTarget?> AmbientTarget = new AsyncLocal<ITerminalTarget?>();
        private static readonly object DefaultTargetGate = new object();
        private static ITerminalTarget? _defaultTarget;

        /// <summary>
        /// The target the static methods write to: the one set by <see cref="Use"/> for the current asynchronous flow, or
        /// <see cref="DefaultTarget"/>. Writes with an explicit writer, live sessions and prompts with an explicit target
        /// keep theirs.
        /// </summary>
        public static ITerminalTarget Target => AmbientTarget.Value ?? DefaultTarget;

        /// <summary>
        /// The process-wide target when no scope is active (default <see cref="ConsoleTarget"/>, or a
        /// <see cref="Headless.HeadlessTerminal"/> when <c>CONSOLEMONSTER_HEADLESS</c> is set). Prefer <see cref="Use"/>,
        /// which affects only the current asynchronous flow; this is for hosts that own the whole process.
        /// </summary>
        public static ITerminalTarget DefaultTarget {
            get {
                ITerminalTarget? target = Volatile.Read(ref _defaultTarget);
                if (target != null) { return target; }
                // Created once, even when threads race: an environment-made headless terminal may start a debug socket.
                lock (DefaultTargetGate) { return _defaultTarget ??= Headless.HeadlessEnvironment.CreateDefaultTarget(); }
            }
            set {
                ITerminalTarget target = value ?? throw new ArgumentNullException(nameof(value));
                lock (DefaultTargetGate) { _defaultTarget = target; }
            }
        }

        /// <summary>
        /// Sends the static methods' output (and prompts, live sessions and status helpers without an explicit target) to
        /// <paramref name="target"/> until the returned scope is disposed. The scope flows with async calls and is local to
        /// them, so parallel tests and requests do not see each other's target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>The scope; dispose it to restore the previous target.</returns>
        public static IDisposable Use(ITerminalTarget target) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            TargetScope scope = new TargetScope(AmbientTarget.Value);
            AmbientTarget.Value = target;
            return scope;
        }

        /// <summary>Whether a target is the process console (live redraw, keyboard input, console ownership).</summary>
        /// <param name="target">The target.</param>
        /// <returns>True for <see cref="ConsoleTarget"/>.</returns>
        public static bool IsConsole(ITerminalTarget target) => target is ConsoleTarget;

        private sealed class TargetScope : IDisposable {
            private readonly ITerminalTarget? _previous;
            private int _disposed;

            internal TargetScope(ITerminalTarget? previous) {
                _previous = previous;
            }

            public void Dispose() {
                if (Interlocked.Exchange(ref _disposed, 1) == 0) {
                    AmbientTarget.Value = _previous;
                }
            }
        }
    }
}
