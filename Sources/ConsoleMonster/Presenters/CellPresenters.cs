using System;
using System.Collections.Generic;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// The built-in presenters by name, and the shared presentation step (presenter, then tone rules) used by tables,
    /// record cards, record views, key/value lists and trees.
    /// </summary>
    public static class CellPresenters {
        /// <summary>Plain text.</summary>
        public static ICellPresenter Text { get; } = new TextPresenter();

        /// <summary>Chooses by value type.</summary>
        public static ICellPresenter Auto { get; } = new AutoPresenter();

        /// <summary>Status symbol, word and tone.</summary>
        public static ICellPresenter Status { get; } = new StatusPresenter();

        /// <summary>Numbers with culture separators, right-aligned.</summary>
        public static ICellPresenter Number { get; } = new NumberPresenter();

        /// <summary>Byte counts in binary units.</summary>
        public static ICellPresenter Bytes { get; } = new BytesPresenter();

        /// <summary>Durations (TimeSpan, or numbers in seconds).</summary>
        public static ICellPresenter Duration { get; } = new DurationPresenter();

        /// <summary>Absolute dates.</summary>
        public static ICellPresenter Date { get; } = new DatePresenter();

        /// <summary>Booleans as symbol and word.</summary>
        public static ICellPresenter Boolean { get; } = new BooleanPresenter();

        /// <summary>Numbers with a data bar.</summary>
        public static ICellPresenter DataBar { get; } = new DataBarPresenter();

        /// <summary>Series as sparklines.</summary>
        public static ICellPresenter Sparkline { get; } = new SparklinePresenter();

        /// <summary>Fractions and done/total as progress.</summary>
        public static ICellPresenter Progress { get; } = new ProgressPresenter();

        /// <summary>Hyperlinks.</summary>
        public static ICellPresenter Link { get; } = new LinkPresenter();

        /// <summary>
        /// Returns a built-in presenter by name (case-insensitive): Text, Auto, Status, Number, Bytes, Duration, Date,
        /// DateRelative, DateBoth, Boolean, Bar (DataBar), Sparkline, Progress, Link.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit for Number, or null.</param>
        /// <returns>The presenter.</returns>
        /// <exception cref="ArgumentException">The name is unknown.</exception>
        public static ICellPresenter FromName(string name, string? unit = null) {
            switch ((name ?? string.Empty).Trim().ToLowerInvariant()) {
                case "text":
                    return Text;
                case "auto":
                case "":
                    return Auto;
                case "status":
                    return Status;
                case "number":
                    return unit == null ? Number : new NumberPresenter(unit);
                case "bytes":
                case "size":
                    return Bytes;
                case "duration":
                case "timespan":
                    return Duration;
                case "date":
                    return Date;
                case "daterelative":
                case "relative":
                    return new DatePresenter(DateDisplay.Relative);
                case "dateboth":
                    return new DatePresenter(DateDisplay.Both);
                case "boolean":
                case "bool":
                    return Boolean;
                case "bar":
                case "databar":
                    return DataBar;
                case "sparkline":
                    return Sparkline;
                case "progress":
                    return Progress;
                case "link":
                case "url":
                    return Link;
                default:
                    throw new ArgumentException("Unknown presenter '" + name + "'. Use Text, Auto, Status, Number, Bytes, Duration, Date, DateRelative, DateBoth, Boolean, Bar, Sparkline, Progress or Link.", nameof(name));
            }
        }

        /// <summary>
        /// Presents a value with a presenter (or <see cref="Auto"/>) and applies the first matching tone rule: the text
        /// takes the rule's colour, and without colour a status tone adds its symbol in front.
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <param name="presenter">The presenter, or null for automatic.</param>
        /// <param name="rules">Tone rules, or null.</param>
        /// <param name="context">The presenter context.</param>
        /// <returns>The presented value.</returns>
        public static PresentedValue Present(object? value, ICellPresenter? presenter, IReadOnlyList<ToneRule>? rules, PresenterContext context) {
            PresentedValue presented = (presenter ?? Auto).Present(value, context);
            if (PresenterValues.IsEmpty(value)) {
                presented.IsEmpty = true;
            }
            if (rules == null || rules.Count == 0) {
                return presented;
            }
            foreach (ToneRule rule in rules) {
                if (rule.Matches(value, presented.Number)) {
                    return WithTone(presented, rule.Tone, context);
                }
            }
            return presented;
        }

        private static PresentedValue WithTone(PresentedValue presented, ThemeToken tone, PresenterContext context) {
            Style style = context.Render.Style(tone);
            List<TextSpan> spans = new List<TextSpan>();
            if (!context.HasColor && ToStatus(tone, out StatusKind status)) {
                string marker = context.Render.Glyphs.StatusMarker(status);
                spans.Add(new TextSpan(marker + " ", style));
            }
            foreach (TextSpan span in presented.Line) {
                spans.Add(new TextSpan(span.Text, span.Style.WithForeground(style.Foreground)));
            }
            return new PresentedValue(new StyledLine(spans), presented.Alignment) {
                Tone = tone,
                Visual = presented.Visual,
                Fraction = presented.Fraction,
                Series = presented.Series,
                VisualWidth = presented.VisualWidth,
                Number = presented.Number,
                IsEmpty = presented.IsEmpty
            };
        }

        private static bool ToStatus(ThemeToken tone, out StatusKind status) {
            switch (tone) {
                case ThemeToken.Pass:
                    status = StatusKind.Pass;
                    return true;
                case ThemeToken.Fail:
                    status = StatusKind.Fail;
                    return true;
                case ThemeToken.Warn:
                    status = StatusKind.Warn;
                    return true;
                default:
                    status = StatusKind.Info;
                    return false;
            }
        }
    }
}
