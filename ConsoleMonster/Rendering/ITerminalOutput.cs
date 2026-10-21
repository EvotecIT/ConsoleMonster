using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Text;

namespace ConsoleMonster.Rendering {
    /// <summary>
    /// Turns frames and lines into output for one mode: VT (diffed live frames, alternate screen),
    /// Stream (append-only lines, optional colour) or Plain (ASCII lines, no colour). Not thread-safe:
    /// a live session serializes calls from its render thread and the stopping caller's final drain and restoration.
    /// </summary>
    public interface ITerminalOutput : IDisposable {
        /// <summary>The profile this output encodes for.</summary>
        TerminalProfile Profile { get; }

        /// <summary>Whether the output can redraw a live frame in place.</summary>
        bool SupportsLiveFrames { get; }

        /// <summary>Appends a rendered block (print-once output) to the scrollback.</summary>
        /// <param name="buffer">The rendered block; trailing blank cells on each row are trimmed.</param>
        void WriteBlock(CellBuffer buffer);

        /// <summary>Appends one styled line (stream narration, captured output, summaries).</summary>
        /// <param name="line">The line.</param>
        void WriteLine(StyledLine line);

        /// <summary>Writes one styled line without ending it (Write-ConsoleText -NoNewLine); the next write continues on it.</summary>
        /// <param name="line">The line.</param>
        void WriteInline(StyledLine line);

        /// <summary>Starts live rendering (VT: alternate screen, hidden cursor). No effect for line-based outputs.</summary>
        void BeginLive();

        /// <summary>Draws a full-screen frame, writing only the cells that changed since the previous frame.</summary>
        /// <param name="frame">The frame; its size is the screen size.</param>
        void RenderFrame(CellBuffer frame);

        /// <summary>Forces the next frame to be redrawn completely (after a resize or external output).</summary>
        void Invalidate();

        /// <summary>Ends live rendering and restores the terminal (cursor, screen, attributes). Safe to call more than once.</summary>
        void EndLive();
    }
}
