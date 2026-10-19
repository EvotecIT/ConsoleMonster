using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Localization;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The built-in progress columns. Each call returns a new column, so its <see cref="ProgressColumn.Priority"/>,
    /// <see cref="ProgressColumn.MaxWidth"/> and alignment can be changed per display.
    /// </summary>
    public static class ProgressColumns {
        /// <summary>
        /// The usual set: spinner, description, bar, percent, count, elapsed, remaining time and status text. Narrow regions
        /// drop the status text, rate, elapsed time, count and remaining time first, then the percentage.
        /// </summary>
        /// <returns>A new list.</returns>
        public static IList<ProgressColumn> Standard() {
            return new List<ProgressColumn> { Spinner(), Description(), Bar(), Percent(), Count(), Elapsed(), Remaining(), Status() };
        }

        /// <summary>A spinner while running, the status marker once finished.</summary>
        /// <returns>The column.</returns>
        public static ProgressColumn Spinner() => new SpinnerColumn();

        /// <summary>The label (rich text kept; muted while pending).</summary>
        /// <param name="maxWidth">The widest it gets (default 28).</param>
        /// <returns>The column.</returns>
        public static ProgressColumn Description(int maxWidth = 28) => new DescriptionColumn { MaxWidth = maxWidth };

        /// <summary>The bar; it takes the width the fixed columns leave.</summary>
        /// <param name="maxWidth">The widest it gets (default 40).</param>
        /// <param name="shape">The bar shape.</param>
        /// <returns>The column.</returns>
        public static ProgressColumn Bar(int maxWidth = 40, BarShape shape = BarShape.Line) => new BarColumn(shape) { MaxWidth = maxWidth };

        /// <summary>The percentage done.</summary>
        /// <returns>The column.</returns>
        public static ProgressColumn Percent() => new TextColumn("Percent", 70, HorizontalAlignment.Right,
            (row, context) => row.Status == StatusKind.Pending ? string.Empty : NumberFormat.Percent(row.Fraction));

        /// <summary>Done of total (<c>12/30</c>), or the count alone when the total is unknown.</summary>
        /// <returns>The column.</returns>
        public static ProgressColumn Count() => new CountColumn();

        /// <summary>The time spent so far (the total time once finished).</summary>
        /// <returns>The column.</returns>
        public static ProgressColumn Elapsed() => new TextColumn("Elapsed", 40, HorizontalAlignment.Right,
            (row, context) => row.Elapsed.HasValue ? NumberFormat.Duration(row.Elapsed.Value) : string.Empty);

        /// <summary>The estimated time left (<c>12s left</c>) from the smoothed rate; empty when it cannot be estimated.</summary>
        /// <returns>The column.</returns>
        public static ProgressColumn Remaining() => new TextColumn("Remaining", 50, HorizontalAlignment.Right,
            (row, context) => !row.IsFinished && row.Remaining.HasValue ? WidgetLabels.Format(context.Labels.TimeLeft, Eta(row.Remaining.Value)) : string.Empty);

        /// <summary>The smoothed rate: items per second, or bytes per second (<c>1.5 MB/s</c>) with <paramref name="bytes"/>.</summary>
        /// <param name="bytes">Whether the amounts are bytes.</param>
        /// <param name="unit">A unit after the number for items (<c>files</c> gives <c>12 files/s</c>), or null.</param>
        /// <returns>The column.</returns>
        public static ProgressColumn Rate(bool bytes = false, string? unit = null) => new TextColumn("Rate", 30, HorizontalAlignment.Right,
            (row, context) => {
                if (row.IsFinished || row.Rate <= 0) {
                    return string.Empty;
                }
                string amount = bytes ? Bytes(row.Rate) : NumberFormat.Compact(row.Rate) + (string.IsNullOrEmpty(unit) ? string.Empty : " " + unit);
                return WidgetLabels.Format(context.Labels.PerSecond, amount);
            });

        /// <summary>Pass, fail and warning counts (<c>✓12 ✗1 ⚠0</c>).</summary>
        /// <returns>The column.</returns>
        public static ProgressColumn Results() => new ResultsColumn();

        /// <summary>The detail while running, <c>done in 12s</c> once finished, <c>queued</c> while pending. It takes leftover width.</summary>
        /// <returns>The column.</returns>
        public static ProgressColumn Status() => new StatusColumn();

        /// <summary>A custom column from a delegate.</summary>
        /// <param name="name">The column name.</param>
        /// <param name="render">Renders a row.</param>
        /// <param name="maxWidth">The widest it gets, or 0 for the widest row.</param>
        /// <param name="priority">The drop priority when narrow (higher stays longer).</param>
        /// <returns>The column.</returns>
        public static ProgressColumn Custom(string name, Func<ProgressRow, RenderContext, StyledLine> render, int maxWidth = 0, int priority = 50) {
            return new DelegateColumn(name, render ?? throw new ArgumentNullException(nameof(render))) { MaxWidth = maxWidth, Priority = priority };
        }

        /// <summary>A custom text column from a delegate.</summary>
        /// <param name="name">The column name.</param>
        /// <param name="text">Returns the plain text for a row.</param>
        /// <param name="maxWidth">The widest it gets, or 0 for the widest row.</param>
        /// <param name="priority">The drop priority when narrow (higher stays longer).</param>
        /// <returns>The column.</returns>
        public static ProgressColumn Custom(string name, Func<ProgressRow, string> text, int maxWidth = 0, int priority = 50) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            return new DelegateColumn(name, (row, context) => StyledLine.From(text(row) ?? string.Empty, context.Style(ThemeToken.Muted))) { MaxWidth = maxWidth, Priority = priority };
        }

        /// <summary>Built-in columns by name, in order; <c>Standard</c> expands to <see cref="Standard"/>.</summary>
        /// <param name="names">The names (see <see cref="TryCreate"/>).</param>
        /// <returns>A new list.</returns>
        /// <exception cref="ArgumentException">A name is not a built-in column.</exception>
        public static IList<ProgressColumn> FromNames(IEnumerable<string> names) {
            if (names == null) {
                throw new ArgumentNullException(nameof(names));
            }
            List<ProgressColumn> columns = new List<ProgressColumn>();
            foreach (string name in names) {
                if (string.Equals((name ?? string.Empty).Trim(), "standard", StringComparison.OrdinalIgnoreCase)) {
                    columns.AddRange(Standard());
                } else if (TryCreate(name!, out ProgressColumn? column)) {
                    columns.Add(column!);
                } else {
                    throw new ArgumentException("Unknown progress column '" + name + "'. Use Standard, " + string.Join(", ", Names) + ".", nameof(names));
                }
            }
            return columns;
        }

        /// <summary>The names <see cref="TryCreate"/> accepts.</summary>
        public static IReadOnlyList<string> Names { get; } = new[] { "Spinner", "Description", "Bar", "Percent", "Count", "Elapsed", "Remaining", "Rate", "ByteRate", "Results", "Status" };

        /// <summary>A built-in column by name (Spinner, Description, Bar, Percent, Count, Elapsed, Remaining, Rate, ByteRate, Results, Status).</summary>
        /// <param name="name">The name, case-insensitive.</param>
        /// <param name="column">The column.</param>
        /// <returns>Whether the name is known.</returns>
        public static bool TryCreate(string name, out ProgressColumn? column) {
            switch ((name ?? string.Empty).Trim().ToLowerInvariant()) {
                case "spinner":
                    column = Spinner();
                    return true;
                case "description":
                case "label":
                    column = Description();
                    return true;
                case "bar":
                    column = Bar();
                    return true;
                case "percent":
                    column = Percent();
                    return true;
                case "count":
                    column = Count();
                    return true;
                case "elapsed":
                    column = Elapsed();
                    return true;
                case "remaining":
                case "eta":
                    column = Remaining();
                    return true;
                case "rate":
                    column = Rate();
                    return true;
                case "byterate":
                    column = Rate(true);
                    return true;
                case "results":
                    column = Results();
                    return true;
                case "status":
                case "detail":
                    column = Status();
                    return true;
                default:
                    column = null;
                    return false;
            }
        }

        private static readonly string[] ByteUnits = { "B", "KB", "MB", "GB", "TB" };

        // An estimate in whole seconds under a minute ("6s"), otherwise the usual duration ("1m 05s").
        private static string Eta(TimeSpan remaining) {
            return remaining.TotalSeconds < 60
                ? Math.Ceiling(remaining.TotalSeconds).ToString("0", CultureInfo.InvariantCulture) + "s"
                : NumberFormat.Duration(TimeSpan.FromSeconds(Math.Ceiling(remaining.TotalSeconds)));
        }

        private static string Bytes(double value) {
            int unit = 0;
            while (value >= 1024 && unit < ByteUnits.Length - 1) {
                value /= 1024;
                unit++;
            }
            return value.ToString(value < 10 && unit > 0 ? "0.0" : "0", CultureInfo.InvariantCulture) + " " + ByteUnits[unit];
        }

        private sealed class TextColumn : ProgressColumn {
            private readonly Func<ProgressRow, RenderContext, string> _text;

            internal TextColumn(string name, int priority, HorizontalAlignment alignment, Func<ProgressRow, RenderContext, string> text) : base(name) {
                _text = text;
                Priority = priority;
                Alignment = alignment;
            }

            public override StyledLine Render(ProgressRow row, RenderContext context) => StyledLine.From(_text(row, context), context.Style(ThemeToken.Muted));
        }

        private sealed class DelegateColumn : ProgressColumn {
            private readonly Func<ProgressRow, RenderContext, StyledLine> _render;

            internal DelegateColumn(string name, Func<ProgressRow, RenderContext, StyledLine> render) : base(name) {
                _render = render;
            }

            public override StyledLine Render(ProgressRow row, RenderContext context) => _render(row, context) ?? StyledLine.From(string.Empty, Style.Plain);
        }

        private sealed class SpinnerColumn : ProgressColumn {
            internal SpinnerColumn() : base("Spinner") {
                Priority = 90;
            }

            public override StyledLine Render(ProgressRow row, RenderContext context) {
                IGlyphSet glyphs = context.Glyphs;
                // A running row whose work is all done reads as passed: a check mark, not a spinner.
                StatusKind status = row.Status == StatusKind.Running && row.Total > 0 && row.Done >= row.Total ? StatusKind.Pass : row.Status;
                if (status == StatusKind.Running) {
                    return StyledLine.From(context.Spinner(glyphs.Tier == GlyphTier.Ascii ? 0 : row.Order), context.Style(ThemeToken.Running));
                }
                string marker = glyphs.Tier == GlyphTier.Ascii ? glyphs.StatusSymbol(status) : glyphs.StatusMarker(status);
                return StyledLine.From(marker, context.Style(status));
            }

            public override string? Summarize(ProgressRow row, RenderContext context) => null;
        }

        private sealed class DescriptionColumn : ProgressColumn {
            internal DescriptionColumn() : base("Description") {
                Priority = 100;
            }

            public override StyledLine Render(ProgressRow row, RenderContext context) {
                Style style = row.Status == StatusKind.Pending ? context.Style(ThemeToken.Muted) : context.Style(ThemeToken.Text);
                return RichLines.Line(row.LabelText, row.Label, style, context);
            }
        }

        private sealed class BarColumn : ProgressColumn {
            private readonly BarShape _shape;

            internal BarColumn(BarShape shape) : base("Bar") {
                _shape = shape;
                Priority = 80;
            }

            public override bool Flexible => true;

            public override int MinWidth => 4;

            public override StyledLine Render(ProgressRow row, RenderContext context) => StyledLine.From(string.Empty, Style.Plain);

            public override void Draw(Region region, int x, int y, int width, ProgressRow row, RenderContext context) {
                Style fill = row.IsFinished ? context.Style(row.Status == StatusKind.Fail ? ThemeToken.Fail : ThemeToken.Muted) : context.Style(ThemeToken.Accent);
                BarDrawer.Draw(region, x, y, width, row.Fraction, fill, context, true, _shape);
            }

            public override string? Summarize(ProgressRow row, RenderContext context) => null;
        }

        private sealed class CountColumn : ProgressColumn {
            internal CountColumn() : base("Count") {
                Priority = 60;
                Alignment = HorizontalAlignment.Right;
            }

            // The width a finished row would need, so the column never grows while counting.
            public override int Measure(IReadOnlyList<ProgressRow> rows, RenderContext context) {
                int width = 0;
                foreach (ProgressRow row in rows) {
                    width = Math.Max(width, Text(row).Length);
                    if (row.Total > 0) {
                        string total = NumberFormat.Compact(row.Total);
                        width = Math.Max(width, total.Length * 2 + 1);
                    }
                }
                return MaxWidth > 0 ? Math.Min(width, MaxWidth) : width;
            }

            public override StyledLine Render(ProgressRow row, RenderContext context) => StyledLine.From(Text(row), context.Style(ThemeToken.Muted));

            private static string Text(ProgressRow row) {
                if (row.Status == StatusKind.Pending && row.Done <= 0) {
                    return string.Empty;
                }
                return row.Total > 0 ? NumberFormat.Compact(row.Done) + "/" + NumberFormat.Compact(row.Total) : NumberFormat.Compact(row.Done);
            }
        }

        private sealed class ResultsColumn : ProgressColumn {
            internal ResultsColumn() : base("Results") {
                Priority = 45;
            }

            public override StyledLine Render(ProgressRow row, RenderContext context) {
                if (row.Status == StatusKind.Pending && row.Done <= 0) {
                    return StyledLine.From(string.Empty, Style.Plain);
                }
                IGlyphSet g = context.Glyphs;
                bool ascii = g.Tier == GlyphTier.Ascii;
                Style muted = context.Style(ThemeToken.Muted);
                List<TextSpan> spans = new List<TextSpan> {
                    new TextSpan((ascii ? "P" : g.Get(Glyph.Check)) + row.Passed.ToString(CultureInfo.InvariantCulture), muted),
                    new TextSpan(" ", Style.Plain),
                    new TextSpan((ascii ? "F" : g.Get(Glyph.Cross)) + row.Failed.ToString(CultureInfo.InvariantCulture), row.Failed > 0 ? context.Style(ThemeToken.Fail) : muted),
                    new TextSpan(" ", Style.Plain),
                    new TextSpan((ascii ? "W" : g.Get(Glyph.Warning)) + row.Warned.ToString(CultureInfo.InvariantCulture), row.Warned > 0 ? context.Style(ThemeToken.Warn) : muted)
                };
                return new StyledLine(spans);
            }
        }

        private sealed class StatusColumn : ProgressColumn {
            internal StatusColumn() : base("Status") {
                Priority = 20;
            }

            public override bool Flexible => true;

            public override int MinWidth => 8;

            public override StyledLine Render(ProgressRow row, RenderContext context) {
                Style muted = context.Style(ThemeToken.Muted);
                WidgetLabels labels = context.Labels;
                if (row.IsFinished) {
                    bool timed = row.Started.HasValue && row.Finished.HasValue && row.Finished.Value > row.Started.Value;
                    return StyledLine.From(timed ? WidgetLabels.Format(labels.DoneIn, NumberFormat.Duration(row.Finished!.Value - row.Started!.Value)) : labels.Done, muted);
                }
                if (row.Status == StatusKind.Pending) {
                    return StyledLine.From(labels.Queued, muted);
                }
                return RichLines.Line(row.DetailText, row.Detail ?? string.Empty, muted, context);
            }
        }
    }
}
