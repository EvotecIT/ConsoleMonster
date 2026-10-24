using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>One item of a <see cref="RecordView"/>.</summary>
    public sealed class RecordItem {
        internal RecordItem(RecordItemKind kind, int level) {
            Kind = kind;
            Level = level;
        }

        /// <summary>The kind.</summary>
        public RecordItemKind Kind { get; }

        /// <summary>The nesting level (0 = top); each level indents by <see cref="RecordView.IndentWidth"/>.</summary>
        public int Level { get; }

        internal int MaxLines;
        internal ColumnOverflow Overflow = ColumnOverflow.Wrap;

        /// <summary>The heading text or the field label (markup for headings, plain for labels).</summary>
        public string Text { get; internal set; } = string.Empty;

        /// <summary>An optional tone for headings (for example a card's status).</summary>
        public ThemeToken? Role { get; internal set; }

        /// <summary>The field cell (markup, status or raw value).</summary>
        public TableCell? Cell { get; internal set; }

        /// <summary>The presenter for raw values, or null for automatic.</summary>
        public ICellPresenter? Presenter { get; internal set; }

        /// <summary>Tone rules for raw values.</summary>
        public IReadOnlyList<RoleRule>? Rules { get; internal set; }

        /// <summary>The nested widget.</summary>
        public IWidget? Widget { get; internal set; }

        /// <summary>An optional muted note after a heading (for example <c>12 items</c>).</summary>
        public string? Note { get; internal set; }
    }
}
