using System;
using System.Threading;

namespace ConsoleMonster.Input {
    /// <summary>
    /// A source of input events: the Windows console (<see cref="WindowsConsoleInput"/>), a VT terminal, the .NET console
    /// key reader, or a script (<see cref="ScriptedInput"/>) in tests and demos. Only one component reads at a time; get the
    /// source through <see cref="ConsoleInput.Acquire"/>, which enforces that.
    /// </summary>
    public interface IInputSource : IDisposable {
        /// <summary>A short description for diagnostics, for example <c>windows console (ReadConsoleInputW)</c>.</summary>
        string Name { get; }

        /// <summary>Waits up to <paramref name="timeout"/> for the next event.</summary>
        /// <param name="timeout">The longest wait; <see cref="TimeSpan.Zero"/> polls.</param>
        /// <param name="cancellationToken">Cancels the wait with <see cref="OperationCanceledException"/>.</param>
        /// <returns>The event, or <c>null</c> when the timeout passed without input.</returns>
        /// <exception cref="InputClosedException">The input ended (end of a script, closed stdin).</exception>
        InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken);
    }
}
