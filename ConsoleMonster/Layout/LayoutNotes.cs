using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ConsoleMonster.Layout {
    /// <summary>Why a widget has its size in the last app frame: its natural size, the size it was given and the rule.</summary>
    internal sealed class LayoutNote {
        internal LayoutNote(Size measured, Size arranged, string rule) { Measured = measured; Arranged = arranged; Rule = rule; }
        /// <summary>The size the widget asked for at the width it was offered.</summary>
        internal Size Measured { get; }
        /// <summary>The size it was given.</summary>
        internal Size Arranged { get; }
        /// <summary>The rule that decided it: <c>fixed 3</c>, <c>fill 1 min 16</c> or <c>auto</c>, then <c>shrunk</c> or <c>collapsed</c> when it got less.</summary>
        internal string Rule { get; }
    }

    /// <summary>
    /// Records, while an app frame is drawn, why each child of a row or column layout got its size, for tools that inspect
    /// a running app (the debug socket's <c>tree</c>). Plain renders record nothing.
    /// </summary>
    internal static class LayoutNotes {
        [ThreadStatic] private static int _recording;
        private static readonly ConditionalWeakTable<IWidget, LayoutNote> Notes = new ConditionalWeakTable<IWidget, LayoutNote>();
        private static readonly object Gate = new object();

        internal static bool IsRecording => _recording > 0;

        internal static IDisposable Recording() => new Scope();

        internal static LayoutNote? Get(IWidget widget) { lock (Gate) { return Notes.TryGetValue(widget, out LayoutNote? note) ? note : null; } }

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
            LayoutNote note = new LayoutNote(measured, arranged, rule);
            lock (Gate) {
                Notes.Remove(child);
                Notes.Add(child, note);
            }
        }

        private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private sealed class Scope : IDisposable {
            internal Scope() { _recording++; }
            public void Dispose() => _recording--;
        }
    }
}
