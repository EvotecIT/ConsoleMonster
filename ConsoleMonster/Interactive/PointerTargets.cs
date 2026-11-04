using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// What a frame drew that a pointer can use, in frame coordinates: footer hints and breadcrumbs (a click runs the
    /// same key or navigation the keyboard does) and lists that take pointer input directly. Widgets record while a frame
    /// is collected; outside a collected frame (a one-off render, an export) recording does nothing.
    /// </summary>
    internal sealed class PointerTargets {
        [ThreadStatic] private static PointerTargets? _current;
        private readonly object _gate = new object();
        private List<Target> _collecting = new List<Target>();
        private Target[] _frame = new Target[0];
        private Target? _pressed;
        private readonly bool _regions;

        /// <summary>Creates a registry; an app's controls route their own pointer input, so its frames keep no regions.</summary>
        internal PointerTargets(bool regions = true) { _regions = regions; }

        /// <summary>Starts collecting a frame; disposing publishes it for pointer input.</summary>
        internal IDisposable Collect() {
            _collecting = new List<Target>();
            return new Scope(this);
        }

        /// <summary>A click on the area presses this key.</summary>
        internal static void RecordKey(Rect area, KeyChord chord) => Add(new Target(area, chord, null, null, "key:" + chord));

        /// <summary>
        /// A click on the area runs this action on the input thread. <paramref name="id"/> names what the area stands for
        /// (a crumb's screen, a diagram step), so a press and release match only on the same thing even when it is drawn
        /// again in between.
        /// </summary>
        internal static void RecordAction(Rect area, Action action, string id) => Add(new Target(area, null, action, null, id));

        /// <summary>Pointer events inside the area go to the handler in frame coordinates; a double click presses the key.</summary>
        /// <remarks><paramref name="cancel"/> drops a press the region holds when the frame changes under it (resize, focus loss).</remarks>
        internal static void RecordRegion(Rect area, Func<InputEvent, bool> pointer, KeyChord? doubleClick, Action? cancel = null) =>
            Add(new Target(area, doubleClick, cancel, pointer, null));

        /// <summary>Everything recorded so far is covered (a dialog is drawn over it).</summary>
        internal static void Cover() {
            PointerTargets? current = _current;
            current?._collecting.Clear();
        }

        /// <summary>Drops targets that the rows from <paramref name="row"/> down no longer show (a toast over the footer).</summary>
        internal void CoverRows(int row) {
            lock (_gate) { _frame = Array.FindAll(_frame, target => target.Area.Bottom <= row); }
        }

        /// <summary>Drops targets a popup or menu is drawn over: the visible layer takes the pointer.</summary>
        internal void CoverArea(Rect area) {
            lock (_gate) { _frame = Array.FindAll(_frame, target => target.Area.Right <= area.X || target.Area.X >= area.Right || target.Area.Bottom <= area.Y || target.Area.Y >= area.Bottom); }
        }

        /// <summary>Drops targets inside <paramref name="body"/> (a dialog dims it); the footer below stays usable.</summary>
        internal void CoverBody(Rect body) {
            lock (_gate) { _frame = Array.FindAll(_frame, target => target.Area.Y >= body.Bottom); }
        }

        internal static int Mark() => _current?._collecting.Count ?? 0;

        /// <summary>Forgets what was recorded after <paramref name="mark"/>: the content is drawn again at another offset.</summary>
        internal static void Rewind(int mark) {
            List<Target>? list = _current?._collecting;
            if (list != null && mark < list.Count) { list.RemoveRange(mark, list.Count - mark); }
        }

        private static void Add(Target target) {
            PointerTargets? current = _current;
            if (current == null || (target.Pointer != null && !current._regions)) { return; }
            Rect area = InputArrangement.FrameBounds(target.Area);
            if (area.Width <= 0 || area.Height <= 0) { return; }
            current._collecting.Add(new Target(area, target.Key, target.Action, target.Pointer, target.Id));
        }

        /// <summary>
        /// Routes a pointer event. A press and release on the same hint or crumb activates it: <paramref name="key"/> is the
        /// key to press, or the action has already run. Regions get every event inside them. Returns whether the event was
        /// used.
        /// </summary>
        internal bool Route(InputEvent input, int clicks, out KeyChord? key) {
            key = null;
            Target[] frame;
            Target? pressed;
            lock (_gate) { frame = _frame; pressed = _pressed; }
            Target? hit = null;
            for (int i = frame.Length - 1; i >= 0; i--) {
                if (Contains(frame[i].Area, input.Column, input.Row)) { hit = frame[i]; break; }
            }
            if (input.MouseAction == MouseAction.Up && input.MouseButton == 1) {
                lock (_gate) { _pressed = null; }
                if (pressed != null && pressed.Pointer == null) {
                    if (hit == null || !Same(hit, pressed)) { return true; }
                    // The current frame's action: a target drawn again since the press runs as it is now.
                    if (hit.Action != null) { hit.Action(); }
                    else { key = hit.Key; }
                    return true;
                }
            }
            if (hit == null) { return false; }
            if (hit.Pointer != null) {
                if (input.MouseAction == MouseAction.Down && input.MouseButton == 1) { lock (_gate) { _pressed = hit; } }
                bool used = hit.Pointer(input);
                if (used && clicks >= 2 && input.MouseAction == MouseAction.Down && input.MouseButton == 1) { key = hit.Key; }
                return used;
            }
            if (input.MouseAction == MouseAction.Down && input.MouseButton == 1) {
                lock (_gate) { _pressed = hit; }
                return true;
            }
            return input.MouseAction != MouseAction.Move;
        }

        // A region's Action is its cancel hook.
        internal void Cancel() {
            Target? pressed;
            lock (_gate) { pressed = _pressed; _pressed = null; }
            if (pressed?.Pointer != null) { pressed.Action?.Invoke(); }
        }

        internal bool IsEmpty { get { lock (_gate) { return _frame.Length == 0; } } }

        private static bool Same(Target a, Target b) => a.Pointer == null && b.Pointer == null && a.Id != null && a.Id == b.Id
            && a.Area.X == b.Area.X && a.Area.Y == b.Area.Y && a.Area.Width == b.Area.Width && a.Area.Height == b.Area.Height;

        private static bool Contains(Rect area, int x, int y) => x >= area.X && x < area.Right && y >= area.Y && y < area.Bottom;

        private sealed class Target {
            internal Target(Rect area, KeyChord? key, Action? action, Func<InputEvent, bool>? pointer, string? id) { Area = area; Key = key; Action = action; Pointer = pointer; Id = id; }
            internal string? Id { get; }
            internal Rect Area { get; }
            internal KeyChord? Key { get; }
            internal Action? Action { get; }
            internal Func<InputEvent, bool>? Pointer { get; }
        }

        private sealed class Scope : IDisposable {
            private readonly PointerTargets _owner;
            private readonly PointerTargets? _previous;
            internal Scope(PointerTargets owner) { _owner = owner; _previous = _current; _current = owner; }
            public void Dispose() {
                _current = _previous;
                Target[] frame = _owner._collecting.ToArray();
                lock (_owner._gate) { _owner._frame = frame; }
            }
        }
    }

    /// <summary>
    /// Counts presses of the same button on the same cell in quick succession. Windows console records mark a double
    /// click, VT mouse reports do not, so both are counted here the same way.
    /// </summary>
    internal sealed class ClickCounter {
        internal static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(500);
        private readonly Func<TimeSpan> _clock;
        private TimeSpan _last;
        private int _column = -1, _row = -1, _button, _count;

        internal ClickCounter(Func<TimeSpan>? clock = null) {
            if (clock == null) {
                System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
                clock = () => watch.Elapsed;
            }
            _clock = clock;
        }

        /// <summary>The click count of a press (1 for a single click, 2 for a double click); zero for other events.</summary>
        internal int Count(InputEvent input) {
            if (input.Kind != InputEventKind.Mouse || input.MouseAction != MouseAction.Down) { return 0; }
            TimeSpan now = _clock();
            bool repeat = _count > 0 && input.MouseButton == _button && input.Column == _column && input.Row == _row && now - _last <= Interval;
            _count = repeat ? _count + 1 : 1;
            _last = now; _column = input.Column; _row = input.Row; _button = input.MouseButton;
            return _count;
        }

        internal void Reset() => _count = 0;
    }
}
