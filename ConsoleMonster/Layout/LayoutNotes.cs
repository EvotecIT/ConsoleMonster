using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ConsoleMonster.Layout {
    /// <summary>Why a widget has its size in the last app frame: its natural size, the size it was given and the rule.</summary>
    internal sealed class LayoutNote {
        internal LayoutNote(Size measured, Size arranged, string rule, long frame) { Measured = measured; Arranged = arranged; Rule = rule; Frame = frame; }
        /// <summary>The size the widget asked for at the space it was offered.</summary>
        internal Size Measured { get; }
        /// <summary>The size it was given.</summary>
        internal Size Arranged { get; }
        /// <summary>The rule that decided it: <c>fixed 3</c>, <c>fill 1 min 16</c> or <c>auto</c>, then <c>shrunk</c> or <c>collapsed</c> when it got less.</summary>
        internal string Rule { get; }
        internal long Frame { get; }
    }

    /// <summary>
    /// Records why each child of a row or column layout got its size, for a tool inspecting a running app (the debug
    /// socket's <c>tree</c>). Nothing is recorded unless such a tool is attached, and only notes from the last app frame
    /// are reported, so widgets that were not drawn (a hidden screen, a collapsed row) report nothing.
    /// </summary>
    internal static class LayoutNotes {
        [ThreadStatic] private static long _current;
        [ThreadStatic] private static int _paused;
        private static int _listeners;
        private static long _frames;
        private static long _completed;
        private static readonly ConditionalWeakTable<IWidget, LayoutNote> Notes = new ConditionalWeakTable<IWidget, LayoutNote>();
        private static readonly object Gate = new object();

        internal static bool IsRecording => _current != 0 && _paused == 0;

        /// <summary>Whether a recorded frame exists for <see cref="Get"/> to report from.</summary>
        internal static bool HasFrame => Interlocked.Read(ref _completed) != 0;

        // The debug socket listens while it runs.
        internal static void Listen() => Interlocked.Increment(ref _listeners);
        internal static void Unlisten() => Interlocked.Decrement(ref _listeners);

        /// <summary>Brackets one app frame; it records only while a tool listens.</summary>
        internal static IDisposable? Frame() => Volatile.Read(ref _listeners) > 0 && _current == 0 ? new FrameScope(Interlocked.Increment(ref _frames)) : null;

        /// <summary>Brackets a render pass whose result is discarded (a trial layout).</summary>
        internal static IDisposable Pause() => new PauseScope();

        internal static LayoutNote? Get(IWidget widget) {
            lock (Gate) {
                return Notes.TryGetValue(widget, out LayoutNote? note) && note.Frame == Interlocked.Read(ref _completed) ? note : null;
            }
        }

        /// <summary>Records a child of a row (vertical) or column layout: its track size against what it measured.</summary>
        internal static void Record(IWidget child, LayoutSize spec, Size measured, Size arranged, bool vertical) {
            int wanted = vertical ? measured.Height : measured.Width;
            int given = vertical ? arranged.Height : arranged.Width;
            string rule = spec.Kind == LayoutSizeKind.Fixed ? "fixed " + Number(spec.Value)
                : spec.Kind == LayoutSizeKind.Fraction ? "fill " + Number(spec.Value) + (spec.Min > 0 ? " min " + spec.Min.ToString(CultureInfo.InvariantCulture) : string.Empty)
                : "auto";
            if (given == 0 && wanted > 0) { rule += ", collapsed"; }
            // A column that fills takes whatever is left, so only a content-sized column is said to have shrunk.
            else if (given < wanted && (vertical ? spec.Kind != LayoutSizeKind.Fixed : spec.Kind == LayoutSizeKind.Auto)) { rule += ", shrunk"; }
            LayoutNote note = new LayoutNote(measured, arranged, rule, _current);
            lock (Gate) {
                Notes.Remove(child);
                Notes.Add(child, note);
            }
        }

        private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private sealed class FrameScope : IDisposable {
            internal FrameScope(long frame) { _current = frame; }
            public void Dispose() {
                long frame = _current;
                _current = 0;
                // A later frame on another thread (a resize composed on the input thread) may have completed first.
                long seen;
                do { seen = Interlocked.Read(ref _completed); } while (frame > seen && Interlocked.CompareExchange(ref _completed, frame, seen) != seen);
            }
        }

        private sealed class PauseScope : IDisposable {
            internal PauseScope() { _paused++; }
            public void Dispose() => _paused--;
        }
    }
}
