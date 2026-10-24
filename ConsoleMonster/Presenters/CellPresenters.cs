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

        /// <summary>Text with <c>:shortcode:</c> emoji, degraded per glyph tier (see <see cref="EmojiPresenter"/>).</summary>
        public static ICellPresenter Emoji { get; } = new EmojiPresenter();

        /// <summary>Identifier numbers (ids, ports, indexes, years) without thousands separators (see <see cref="IdentifierPresenter"/>).</summary>
        public static ICellPresenter Identifier { get; } = new IdentifierPresenter();

        /// <summary>Enum or text values that read as a status, shown with a marker (see <see cref="StatusEnumPresenter"/>).</summary>
        public static ICellPresenter StatusWord { get; } = new StatusEnumPresenter();

        /// <summary>
        /// The presenter a column gets from its member name and value type, or null for the default: plain digits for
        /// integer identifiers (Id, PID, Port, Index, Year, names ending in Id or Port), byte counts for
        /// integer members named Size, Length, Bytes, WorkingSet or Memory (and names ending in them), dates for
        /// DateTime and DateTimeOffset, durations for TimeSpan, Yes/No for booleans, and status markers for enums whose
        /// names read as a status (Running, Stopped, Failed...).
        /// </summary>
        /// <param name="name">The member or column name.</param>
        /// <param name="type">The value type, or null when unknown.</param>
        /// <returns>The presenter, or null.</returns>
        public static ICellPresenter? ForMember(string? name, Type? type) {
            if (type == null) {
                return null;
            }
            Type actual = Nullable.GetUnderlyingType(type) ?? type;
            if (actual == typeof(DateTime) || actual == typeof(DateTimeOffset)) {
                return Date;
            }
            if (actual == typeof(TimeSpan)) {
                return Duration;
            }
            if (actual == typeof(bool)) {
                return Boolean;
            }
            if (actual.IsEnum) {
                return StatusEnumPresenter.LooksLikeStatus(actual) ? StatusWord : null;
            }
            bool integer = actual == typeof(long) || actual == typeof(ulong) || actual == typeof(int) || actual == typeof(uint) || actual == typeof(short) || actual == typeof(ushort);
            if (integer && IdentifierPresenter.LooksLikeIdentifier(name)) {
                return Identifier;
            }
            if (integer && IsSizeName(name)) {
                return Bytes;
            }
            return null;
        }

        /// <summary>
        /// The status presenter for a column whose values read as statuses, whatever presenters are otherwise chosen: an enum
        /// with status names (ServiceControllerStatus), or text in a column named like a status (Status, State, Result,
        /// Outcome, Health, Level, Severity) whose value is a status word (Success, Warning, Error, Running...). The value's
        /// own word is kept, with a marker and tone: <c>✓ Success</c>, <c>▴ Warning</c>.
        /// </summary>
        /// <param name="name">The column or member name.</param>
        /// <param name="sample">A sample value.</param>
        /// <returns>The status presenter, or null.</returns>
        public static ICellPresenter? ForStatus(string? name, object? sample) {
            if (sample == null) {
                return null;
            }
            Type type = Nullable.GetUnderlyingType(sample.GetType()) ?? sample.GetType();
            if (type.IsEnum) {
                return StatusEnumPresenter.LooksLikeStatus(type) ? StatusWord : null;
            }
            if (sample is string text && IsStatusName(name) && StatusEnumPresenter.TryRead(text, out _)) {
                return StatusWord;
            }
            return null;
        }

        /// <summary>
        /// The member-name and value-type presenter used by tables, also for record fields, trees and key/value lists:
        /// identifiers, byte counts, dates, durations, booleans and recognized status words.
        /// </summary>
        /// <param name="label">The field label or member name.</param>
        /// <param name="value">The value.</param>
        /// <returns>The matching presenter, or null for automatic value presentation.</returns>
        public static ICellPresenter? ForLabel(string? label, object? value) => ForStatus(label, value) ?? ForMember(label, value?.GetType());

        private static bool IsStatusName(string? name) {
            if (string.IsNullOrEmpty(name)) {
                return false;
            }
            foreach (string word in new[] { "Status", "State", "Result", "Outcome", "Health", "Level", "Severity", "LevelDisplayName" }) {
                if (name!.EndsWith(word, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

        private static bool IsSizeName(string? name) {
            if (string.IsNullOrEmpty(name)) {
                return false;
            }
            foreach (string word in new[] { "Size", "Length", "Bytes", "WorkingSet", "WorkingSet64", "Memory", "PagedMemorySize64", "VirtualMemorySize64", "PrivateMemorySize64" }) {
                if (name!.EndsWith(word, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

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

        /// <summary>Literal category text in a bracketed chip.</summary>
        public static ICellPresenter Chip { get; } = new ChipPresenter();

        /// <summary>PowerShell member signatures with muted types and highlighted member names.</summary>
        public static ICellPresenter MemberSignature { get; } = new HighlightPresenter(new Highlighting.MemberSignatureHighlighter()) { TypeRole = ThemeToken.Muted };

        /// <summary>Text values interpreted as markup (<c>[pass]OK[/]</c>); invalid markup is shown literally. Data stays literal unless a column opts in with this presenter.</summary>
        public static ICellPresenter Markup { get; } = new MarkupPresenter();

        /// <summary>
        /// Returns a built-in presenter by name (case-insensitive): Text, Auto, Status, Number, Bytes, Duration, Date,
        /// DateRelative, DateBoth, Boolean, Bar (DataBar), Sparkline, Progress, Link, Chip and MemberSignature.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="unit">The unit for Number, or null.</param>
        /// <returns>The presenter.</returns>
        /// <exception cref="ArgumentException">The name is unknown.</exception>
        public static ICellPresenter FromName(string name, string? unit = null) {
            switch ((name ?? string.Empty).Trim().ToLowerInvariant()) {
                case "chip":
                    return Chip;
                case "membersignature":
                    return MemberSignature;
                case "text":
                    return Text;
                case "emoji":
                    return Emoji;
                case "statusword":
                    return StatusWord;
                case "identifier":
                case "id":
                    return Identifier;
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
                case "markup":
                case "rich":
                    return Markup;
                case "link":
                case "url":
                    return Link;
                default:
                    throw new ArgumentException("Unknown presenter '" + name + "'. Use Text, Emoji, Auto, Status, StatusWord, Identifier, Number, Bytes, Duration, Date, DateRelative, DateBoth, Boolean, Bar, Sparkline, Progress or Link.", nameof(name));
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
        public static PresentedValue Present(object? value, ICellPresenter? presenter, IReadOnlyList<RoleRule>? rules, PresenterContext context) {
            if (value is Text.RichText rich) {
                // Rich text brings its own colours; presenters and tone rules do not restyle it.
                return MarkupPresenter.FromStyledText(rich.ToStyledText(context.Render), rich.PlainText.Length == 0);
            }
            if (PresenterValues.IsNonFinite(value)) {
                // NaN and infinities (a rate computed over zero elapsed time) have no meaningful display.
                return new PresentedValue(StyledLine.From(PresenterValues.Dash(context), context.Render.Style(ThemeToken.Muted)), Layout.HorizontalAlignment.Right);
            }
            PresentedValue presented = (presenter ?? Auto).Present(value, context);
            if (PresenterValues.IsEmpty(value)) {
                presented.IsEmpty = true;
            }
            if (rules == null || rules.Count == 0) {
                return presented;
            }
            foreach (RoleRule rule in rules) {
                if (rule.Matches(value, presented.Number)) {
                    return WithRole(presented, rule.Role, context);
                }
            }
            return presented;
        }

        private static PresentedValue WithRole(PresentedValue presented, ThemeToken tone, PresenterContext context) {
            Style style = context.Render.Style(tone);
            List<TextSpan> spans = new List<TextSpan>();
            if (!context.HasColor && ToStatus(tone, out StatusKind status)) {
                string marker = context.Render.Glyphs.StatusMarker(status);
                spans.Add(new TextSpan(marker + " ", style));
            }
            foreach (TextSpan span in presented.Line) {
                spans.Add(new TextSpan(span.Text, span.Style.WithColor(style.Color)));
            }
            return new PresentedValue(new StyledLine(spans), presented.Alignment) {
                Role = tone,
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
