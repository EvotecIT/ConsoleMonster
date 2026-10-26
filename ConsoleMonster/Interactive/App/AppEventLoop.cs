using System;
using System.Threading;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>The common input pump for apps, prompts, menus, grids and explorers. Bounded bursts leave time for work and rendering.</summary>
    internal static class AppEventLoop {
        internal static void Run(IInputSource input, Func<bool> done, Action tick, Action<InputEvent> handle,
            CancellationToken cancellationToken, Func<TimeSpan>? wait = null, bool endOnInputClosed = false) {
            bool closed = false;
            InputEvent? Read(TimeSpan timeout) {
                try { return input.Read(timeout, cancellationToken); }
                catch (InputClosedException) when (endOnInputClosed) { closed = true; return null; }
            }
            while (!done() && !closed) {
                cancellationToken.ThrowIfCancellationRequested();
                tick();
                if (done()) { break; }
                InputEvent? next = Read(wait?.Invoke() ?? TimeSpan.FromMilliseconds(30));
                int budget = 64;
                while (next != null) {
                    InputEvent current = next;
                    if (current.Kind == InputEventKind.Mouse && input is WindowsConsoleInput
                        && Output.TerminalSize.TryGetWindow(out int left, out int top, out _, out _)) {
                        current = current.WithPosition(current.Column - left, current.Row - top);
                    }
                    handle(current);
                    if (done() || --budget == 0) { break; }
                    next = Read(TimeSpan.Zero);
                }
            }
        }
    }
}
