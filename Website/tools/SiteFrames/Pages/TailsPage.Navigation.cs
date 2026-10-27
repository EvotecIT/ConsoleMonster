using System;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Output;

namespace SiteFrames.Pages {
    internal static partial class TailsPage {
        /// <summary>Moves only after the preceding complete tail state reached the recording target.</summary>
        internal sealed class NavigationInput : IInputSource {
            private static readonly string[] States = { "following", "paused", "resumed" };
            private readonly RecordingTarget _target;
            private readonly int _width;
            private int _step;
            private int _waits;

            internal NavigationInput(RecordingTarget target, int width) {
                _target = target;
                _width = width;
            }

            public string Name => "tail-navigation";

            public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
                cancellationToken.ThrowIfCancellationRequested();
                if (_step == States.Length) { throw new InputClosedException("The tail navigation finished."); }
                if (FindFrames(_target.Output, _width, requireFollowing: false).ContainsKey(States[_step])) {
                    _waits = 0;
                    return _step++ switch {
                        0 => InputEvent.Press(ConsoleKey.PageUp),
                        1 => InputEvent.Press(ConsoleKey.End),
                        _ => InputEvent.Character('q')
                    };
                }
                if (timeout > TimeSpan.Zero) {
                    if (cancellationToken.WaitHandle.WaitOne(timeout)) { cancellationToken.ThrowIfCancellationRequested(); }
                    // The shown program polls every 250 ms. Fail explicitly after twenty unready polls;
                    // a missing render must not turn the website build into an endless input wait.
                    if (++_waits >= 20) { throw new InvalidOperationException("The tail did not draw its " + States[_step] + " state."); }
                }
                return null;
            }

            public void Dispose() { }
        }
    }
}
