using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// One record (or a few) as a readable sheet: a summary header line, sections and groups, aligned label/value fields
    /// that hide when empty, and nested widgets such as tables. It is the console counterpart of a record drawer, the
    /// fallback a table uses for a single row, the layout of record cards, and the view of the object inspector.
    /// </summary>
    public sealed partial class RecordView : IWidget {
        private readonly List<RecordItem> _items = new List<RecordItem>();
        private int _level = -1;

        /// <summary>Creates a record view.</summary>
        /// <param name="title">The title (markup) shown on the summary header line, or null.</param>
        public RecordView(string? title = null) {
            Title = title;
        }

        /// <summary>The title (markup) on the summary header line, or null.</summary>
        public string? Title { get; set; }

        /// <summary>A muted summary after the title (<c>12 targets · 4 failing</c>), or null.</summary>
        public string? Summary { get; set; }

        /// <summary>Whether empty values (null, empty text) are left out (default true).</summary>
        public bool HideEmpty { get; set; } = true;

        /// <summary>Cells each nesting level indents (default 2).</summary>
        public int IndentWidth { get; set; } = 2;

        /// <summary>The widest a label column may be, as a fraction of the width (default 0.4).</summary>
        public double MaxLabelFraction { get; set; } = 0.4;

        /// <summary>Culture, clock and time zone for raw values.</summary>
        public PresentationOptions Presentation { get; set; } = new PresentationOptions();

        /// <summary>The items in order.</summary>
        public IReadOnlyList<RecordItem> Items => _items;

        /// <summary>Adds a top-level section heading; following fields belong to it.</summary>
        /// <param name="title">The heading (markup).</param>
        /// <param name="tone">An optional tone (for example the record's status), or null.</param>
        /// <param name="note">An optional muted note after the heading, or null.</param>
        /// <returns>This instance.</returns>
        public RecordView AddSection(string title, ThemeToken? tone = null, string? note = null) => AddHeading(title, 0, tone, note);

        /// <summary>Adds a group heading under the current section; following fields belong to it.</summary>
        /// <param name="title">The heading (markup).</param>
        /// <param name="note">An optional muted note, or null.</param>
        /// <returns>This instance.</returns>
        public RecordView AddGroup(string title, string? note = null) => AddHeading(title, 1, null, note);

        /// <summary>Adds a heading at a nesting level (0 = section, 1 = group, deeper for nested objects).</summary>
        /// <param name="title">The heading (markup).</param>
        /// <param name="level">The level.</param>
        /// <param name="tone">An optional tone, or null.</param>
        /// <param name="note">An optional muted note, or null.</param>
        /// <returns>This instance.</returns>
        public RecordView AddHeading(string title, int level, ThemeToken? tone = null, string? note = null) {
            _level = Math.Max(0, level);
            _items.Add(new RecordItem(RecordItemKind.Heading, _level) { Text = title ?? string.Empty, Tone = tone, Note = note });
            return this;
        }

        /// <summary>Adds a field with a raw value, formatted by a presenter.</summary>
        /// <param name="label">The label (plain text).</param>
        /// <param name="value">The value.</param>
        /// <param name="presenter">The presenter, or null for automatic.</param>
        /// <param name="rules">Tone rules, or null.</param>
        /// <returns>This instance.</returns>
        public RecordView AddField(string label, object? value, ICellPresenter? presenter = null, IReadOnlyList<ToneRule>? rules = null) {
            return AddField(label, TableCell.FromValue(value), presenter, rules);
        }

        /// <summary>Adds a field with a cell (markup, status or raw value).</summary>
        /// <param name="label">The label (plain text).</param>
        /// <param name="cell">The cell.</param>
        /// <param name="presenter">The presenter for raw values, or null for automatic.</param>
        /// <param name="rules">Tone rules, or null.</param>
        /// <returns>This instance.</returns>
        public RecordView AddField(string label, TableCell cell, ICellPresenter? presenter = null, IReadOnlyList<ToneRule>? rules = null) {
            _items.Add(new RecordItem(RecordItemKind.Field, _level + 1) {
                Text = label ?? string.Empty,
                Cell = cell ?? TableCell.FromValue(null),
                Presenter = presenter ?? CellPresenters.ForLabel(label, cell?.Value),
                Rules = rules
            });
            return this;
        }

        /// <summary>Adds a nested widget (a table, a list) under the current heading.</summary>
        /// <param name="widget">The widget.</param>
        /// <returns>This instance.</returns>
        public RecordView AddWidget(IWidget widget) {
            _items.Add(new RecordItem(RecordItemKind.Widget, _level + 1) { Widget = widget ?? throw new ArgumentNullException(nameof(widget)) });
            return this;
        }

        /// <summary>Adds a field at an explicit level (used by the object inspector for nested members).</summary>
        /// <param name="level">The level.</param>
        /// <param name="label">The label.</param>
        /// <param name="cell">The cell.</param>
        /// <param name="presenter">The presenter, or null.</param>
        /// <returns>This instance.</returns>
        public RecordView AddFieldAt(int level, string label, TableCell cell, ICellPresenter? presenter = null) {
            _items.Add(new RecordItem(RecordItemKind.Field, Math.Max(0, level)) { Text = label ?? string.Empty, Cell = cell, Presenter = presenter ?? CellPresenters.ForLabel(label, cell?.Value) });
            return this;
        }

        /// <summary>Adds a nested widget at an explicit level.</summary>
        /// <param name="level">The level.</param>
        /// <param name="widget">The widget.</param>
        /// <returns>This instance.</returns>
        public RecordView AddWidgetAt(int level, IWidget widget) {
            _items.Add(new RecordItem(RecordItemKind.Widget, Math.Max(0, level)) { Widget = widget ?? throw new ArgumentNullException(nameof(widget)) });
            return this;
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            List<Entry> entries = Build(constraints.MaxWidth, context);
            int height = 0;
            int width = 0;
            foreach (Entry entry in entries) {
                height += entry.Height;
                width = Math.Max(width, entry.Width);
            }
            return new Size(Math.Min(width, constraints.MaxWidth), constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            List<Entry> entries = Build(region.Width, context);
            int y = 0;
            foreach (Entry entry in entries) {
                if (y >= region.Height) {
                    break;
                }
                Draw(region, y, entry, context);
                y += entry.Height;
            }
        }
    }
}
