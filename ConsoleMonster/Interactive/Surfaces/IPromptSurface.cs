using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>Where the text cursor should blink after a frame (the caret of a text box), in frame coordinates.</summary>
    internal readonly struct Caret {
        public Caret(int x, int y) {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }
    }

    /// <summary>
    /// The drawing target of an interactive view: an inline region below the cursor (scrollback stays intact), the
    /// alternate screen (menus and app shells), an overlay over a running live dashboard, or a recorder in tests.
    /// </summary>
    internal interface IPromptSurface : IDisposable {
        /// <summary>Usable width in cells.</summary>
        int Width { get; }

        /// <summary>The most rows a frame may have.</summary>
        int Height { get; }

        /// <summary>Whether frames are only as tall as their content (inline) rather than the full screen.</summary>
        bool IsInline { get; }

        /// <summary>Re-reads the window size; returns true when it changed (the next frame is drawn completely).</summary>
        bool Refresh();

        /// <summary>Draws a frame, changing only what differs from the previous one, and places the caret.</summary>
        void Present(CellBuffer frame, Caret? caret);

        /// <summary>Ends the interaction: an inline surface replaces its region with the summary lines (kept in scrollback).</summary>
        void Complete(IReadOnlyList<StyledLine>? summary);
    }
}
