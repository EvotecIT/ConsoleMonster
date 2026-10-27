using ConsoleMonster.Canvas;
using ConsoleMonster.Text;

namespace ConsoleMonster.Sessions {
    internal sealed partial class VtScreen {
        /// <summary>Cursor state needed when re-encoding transport without losing delayed wrap or scroll-margin effects.</summary>
        internal CursorState Cursor {
            get {
                int edge = Width - 1;
                if (Current[edge, _y].IsContinuation && edge > 0) { edge--; }
                return new CursorState(_x, _y, _pendingWrap, _origin ? _top : 0, _bottom, edge, Current[edge, _y]);
            }
        }

        internal Cell CellAt(int x, int y) => Current[x, y];

        /// <summary>Uses the same grapheme rule as printing to identify a cell that a following scalar will extend.</summary>
        internal bool TryJoiningCell(string glyph, out int x, out Cell previous) {
            x = _pendingWrap ? _x : _x - 1;
            previous = default;
            if (x < 0) { return false; }
            if (Current[x, _y].IsContinuation && x > 0) { x--; }
            previous = Current[x, _y];
            string joined = previous.Glyph + glyph;
            foreach (TextCluster cluster in DisplayWidth.Clusters(joined)) {
                return cluster.Length == joined.Length && cluster.Width <= 2;
            }
            return false;
        }

        internal readonly struct CursorState {
            internal CursorState(int x, int y, bool wrap, int originTop, int bottom, int edge, Cell edgeCell) {
                X = x; Y = y; PendingWrap = wrap; OriginTop = originTop; Bottom = bottom; Edge = edge; EdgeCell = edgeCell;
            }
            internal int X { get; }
            internal int Y { get; }
            internal bool PendingWrap { get; }
            internal int OriginTop { get; }
            internal int Bottom { get; }
            internal int Edge { get; }
            internal Cell EdgeCell { get; }
        }
    }
}
