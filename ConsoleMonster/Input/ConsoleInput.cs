using System;
using System.Threading;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Input {
    /// <summary>
    /// The single owner of keyboard input. Prompts, menus and forms read the console through a lease from
    /// <see cref="Acquire"/>, so two components never fight over the keyboard: nested prompts on the same thread share the
    /// lease, and a second thread that tries to read while a prompt is open gets a clear error instead of stolen keys.
    /// A lease for an explicit source (a script, an embedding host's own input) does not touch the console and is
    /// independent of the console owner.
    /// </summary>
    public static class ConsoleInput {
        private static readonly object Gate = new object();
        private static IInputSource? _source;
        private static int _ownerThread;
        private static int _depth;
        private static bool _inputStarted;
        private static bool _ownsSource;

        /// <summary>
        /// A source to use instead of the console (a script in tests or demos, or an embedding host's own input).
        /// Null uses the console.
        /// </summary>
        public static IInputSource? Override { get; set; }

        /// <summary>Whether the console (or <see cref="Override"/>) is currently leased.</summary>
        public static bool IsAcquired {
            get {
                lock (Gate) {
                    return _depth > 0;
                }
            }
        }

        /// <summary>
        /// Takes the keyboard. The first lease opens the source (switching the Windows console to raw key input); the last
        /// release restores it. Reentrant on the owning thread. With an explicit <paramref name="source"/> the lease
        /// simply reads that source.
        /// </summary>
        /// <param name="source">A source for this lease, or null for <see cref="Override"/> or the console.</param>
        /// <returns>The lease; dispose it when done.</returns>
        /// <exception cref="InvalidOperationException">Another thread holds the keyboard, or no console input exists.</exception>
        public static InputLease Acquire(IInputSource? source = null) {
            if (source != null) {
                return new InputLease(source, false);
            }
            lock (Gate) {
                int thread = Thread.CurrentThread.ManagedThreadId;
                if (_depth > 0) {
                    if (_ownerThread != thread) {
                        throw new InvalidOperationException("Another prompt is reading the keyboard on a different thread; prompts must not run concurrently.");
                    }
                    _depth++;
                    return new InputLease(_source!, true);
                }
                IInputSource opened = Override ?? OpenConsole() ?? throw new InvalidOperationException("No console input is available (standard input is redirected or there is no console).");
                _source = opened;
                _ownerThread = thread;
                _depth = 1;
                _ownsSource = Override == null;
                return new InputLease(opened, true);
            }
        }

        /// <summary>Serializes a startup terminal query against all console input leases.</summary>
        internal static void BeforeFirstRead(Action probe) {
            lock (Gate) {
                if (!_inputStarted && _depth == 0 && Override == null) { probe(); }
            }
        }

        internal static void MarkInputStarted() { lock (Gate) { _inputStarted = true; } }

        internal static void Release(InputLease lease) {
            if (!lease.Global) {
                return;
            }
            IInputSource? dispose = null;
            lock (Gate) {
                if (_depth == 0) {
                    return;
                }
                _depth--;
                if (_depth == 0) {
                    if (_ownsSource) {
                        dispose = _source;
                    }
                    _source = null;
                    _ownerThread = 0;
                }
            }
            dispose?.Dispose();
        }

        /// <summary>Opens the platform console reader: <see cref="WindowsConsoleInput"/> on Windows, <see cref="ConsoleKeyInput"/> elsewhere.</summary>
        /// <returns>The source, or null when standard input is not a console.</returns>
        public static IInputSource? OpenConsole() {
            if (WindowsConsole.IsWindows) {
                MarkInputStarted();
                return WindowsConsoleInput.TryOpen();
            }
            try {
                if (Console.IsInputRedirected) {
                    return null;
                }
            } catch (System.IO.IOException) {
                return null;
            }
            return new ConsoleKeyInput();
        }
    }
}
