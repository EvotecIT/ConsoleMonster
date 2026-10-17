using System;

namespace ConsoleMonster.Output {

    /// <summary>
    /// The lowest output sink: turns already-encoded text (including VT sequences) into calls or bytes.
    /// On a real Windows console handle the text is written as UTF-16 through <c>WriteConsoleW</c>, so Unicode
    /// shows correctly whatever the code page; redirected output is written as UTF-8. Writers never go through
    /// <see cref="Console.Out"/>, because a live session redirects it into a log region.
    /// </summary>
    public interface ITerminalWriter : IDisposable {

        /// <summary>Whether this writer targets a real console handle (as opposed to a file, pipe or buffer).</summary>
        bool IsConsole { get; }

        /// <summary>Writes text. Implementations may buffer until <see cref="Flush"/>.</summary>
        /// <param name="text">Text, possibly containing VT sequences.</param>
        void Write(string text);

        /// <summary>Flushes buffered text to the device.</summary>
        void Flush();
    }
}
