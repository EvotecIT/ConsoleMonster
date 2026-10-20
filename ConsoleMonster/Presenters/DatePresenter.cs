using System;
using System.Globalization;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// A point in time, absolute (in the presenter's or the context's time zone), relative to now, or both. Accepts
    /// <see cref="DateTime"/>, <see cref="DateTimeOffset"/> and ISO 8601 strings. <see cref="PresentedValue.Number"/> is the
    /// number of days from now (negative in the past), so tone rules can flag, for example, certificates that expire soon.
    /// <see cref="DateTime.MinValue"/> and <see cref="DateTime.MaxValue"/> (and values within 14 hours of them) show as
    /// "never" with no number.
    /// </summary>
    public sealed class DatePresenter : ICellPresenter {
        /// <summary>Creates a date presenter.</summary>
        /// <param name="display">Absolute, relative or both (default absolute).</param>
        /// <param name="format">The absolute format (default <c>yyyy-MM-dd HH:mm</c>).</param>
        /// <param name="timeZone">The time zone for absolute dates, or null for the context's.</param>
        public DatePresenter(DateDisplay display = DateDisplay.Absolute, string? format = null, TimeZoneInfo? timeZone = null) {
            Display = display;
            Format = format ?? DefaultFormat;
            TimeZone = timeZone;
        }

        /// <summary>Absolute, relative or both.</summary>
        public DateDisplay Display { get; }

        /// <summary>The absolute format.</summary>
        public string Format { get; }

        /// <summary>The time zone override, or null.</summary>
        public TimeZoneInfo? TimeZone { get; }

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            if (IsSentinel(value)) {
                // DateTime.MinValue/MaxValue are the usual "never" values (directory attributes, default structs).
                return new PresentedValue(StyledLine.From(context.Render.Labels.Never, context.Render.Style(ThemeToken.Muted)));
            }
            if (!TryGetTime(value, out DateTimeOffset time)) {
                return new TextPresenter().Present(value, context);
            }
            DateTimeOffset local;
            try {
                local = TimeZoneInfo.ConvertTime(time, TimeZone ?? context.TimeZone);
            } catch (ArgumentOutOfRangeException) {
                // Within a day of the representable range the zone shift cannot be applied; show the value unshifted.
                local = time;
            }
            string absolute = local.ToString(Format, context.Culture);
            string relative = Relative(time - context.Now);
            Style text = context.Render.Style(ThemeToken.Text);
            Style muted = context.Render.Style(ThemeToken.Muted);
            StyledLine line;
            switch (Display) {
                case DateDisplay.Relative:
                    line = StyledLine.From(relative, text);
                    break;
                case DateDisplay.Both:
                    line = new StyledLine(new[] { new TextSpan(absolute, text), new TextSpan(" (" + relative + ")", muted) });
                    break;
                default:
                    line = StyledLine.From(absolute, text);
                    break;
            }
            return new PresentedValue(line) { Number = (time - context.Now).TotalDays };
        }

        /// <summary>
        /// A point in time as text the same way everywhere (tables, records, titles, trees, Show-Console and the change
        /// view): ISO-style <c>yyyy-MM-dd HH:mm</c> in the local time zone (or <paramref name="timeZone"/>), whatever the
        /// culture, so dates sort and read the same on every machine; "never" for <see cref="DateTime.MinValue"/> and
        /// <see cref="DateTime.MaxValue"/>. With <paramref name="precise"/> the seconds (and milliseconds when present) are
        /// added, for two values that differ below a minute.
        /// </summary>
        /// <param name="value">A <see cref="DateTime"/> or <see cref="DateTimeOffset"/>.</param>
        /// <param name="timeZone">The time zone, or null for local time.</param>
        /// <param name="precise">Whether to show seconds.</param>
        /// <returns>The text, or null when the value is not a point in time.</returns>
        public static string? Text(object? value, TimeZoneInfo? timeZone = null, bool precise = false) {
            if (!(value is DateTime) && !(value is DateTimeOffset)) {
                return null;
            }
            if (IsSentinel(value)) {
                return "never";
            }
            TryGetTime(value, out DateTimeOffset time);
            DateTimeOffset local;
            try {
                local = TimeZoneInfo.ConvertTime(time, timeZone ?? TimeZoneInfo.Local);
            } catch (ArgumentOutOfRangeException) {
                local = time;
            }
            string format = !precise ? DefaultFormat : local.Millisecond != 0 ? "yyyy-MM-dd HH:mm:ss.fff" : "yyyy-MM-dd HH:mm:ss";
            return local.ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>A point in time to the tick, in the same time zone as <see cref="Text"/> (for values that differ below a millisecond).</summary>
        /// <param name="value">A <see cref="DateTime"/> or <see cref="DateTimeOffset"/>.</param>
        /// <returns>The text, or null when the value is not a point in time.</returns>
        public static string? ExactText(object? value) {
            if (!(value is DateTime) && !(value is DateTimeOffset) || IsSentinel(value)) {
                return Text(value);
            }
            TryGetTime(value, out DateTimeOffset time);
            DateTimeOffset local;
            try {
                local = TimeZoneInfo.ConvertTime(time, TimeZoneInfo.Local);
            } catch (ArgumentOutOfRangeException) {
                local = time;
            }
            return local.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
        }

        /// <summary>The absolute format every command uses by default.</summary>
        public const string DefaultFormat = "yyyy-MM-dd HH:mm";

        /// <summary>Formats an offset from now: <c>just now</c>, <c>45 s ago</c>, <c>3 h ago</c>, <c>in 12 d</c>, <c>2 y ago</c>.</summary>
        /// <param name="offset">The value minus now.</param>
        /// <returns>The text.</returns>
        public static string Relative(TimeSpan offset) {
            TimeSpan d = offset.Duration();
            string amount;
            if (d.TotalSeconds < 5) {
                return "just now";
            } else if (d.TotalSeconds < 60) {
                amount = ((int)d.TotalSeconds).ToString(CultureInfo.InvariantCulture) + " s";
            } else if (d.TotalMinutes < 60) {
                amount = ((int)d.TotalMinutes).ToString(CultureInfo.InvariantCulture) + " m";
            } else if (d.TotalHours < 24) {
                amount = ((int)d.TotalHours).ToString(CultureInfo.InvariantCulture) + " h";
            } else if (d.TotalDays < 60) {
                amount = ((int)d.TotalDays).ToString(CultureInfo.InvariantCulture) + " d";
            } else if (d.TotalDays < 730) {
                amount = ((int)(d.TotalDays / 30.44)).ToString(CultureInfo.InvariantCulture) + " mo";
            } else {
                amount = ((int)(d.TotalDays / 365.25)).ToString(CultureInfo.InvariantCulture) + " y";
            }
            return offset < TimeSpan.Zero ? amount + " ago" : "in " + amount;
        }

        // The representable limits, or so close to them that a time-zone shift (at most 14 h) would overflow.
        private static readonly TimeSpan SentinelMargin = TimeSpan.FromHours(14);

        private static bool IsSentinel(object? value) {
            switch (value) {
                case DateTime date:
                    return date.Ticks - DateTime.MinValue.Ticks <= SentinelMargin.Ticks || DateTime.MaxValue.Ticks - date.Ticks <= SentinelMargin.Ticks;
                case DateTimeOffset offset:
                    return offset.UtcTicks - DateTime.MinValue.Ticks <= SentinelMargin.Ticks || DateTime.MaxValue.Ticks - offset.UtcTicks <= SentinelMargin.Ticks;
                default:
                    return false;
            }
        }

        private static bool TryGetTime(object? value, out DateTimeOffset time) {
            switch (value) {
                case DateTimeOffset offset:
                    time = offset;
                    return true;
                case DateTime date:
                    time = date.Kind == DateTimeKind.Utc ? new DateTimeOffset(date, TimeSpan.Zero) : new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Local));
                    return true;
                case string text:
                    return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out time);
                default:
                    time = default;
                    return false;
            }
        }
    }
}
