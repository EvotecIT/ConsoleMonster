using System;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Presenters {
    /// <summary>Everything a presenter may depend on, so output is deterministic for a given culture, clock and terminal.</summary>
    public sealed class PresenterContext {
        /// <summary>Creates a presenter context.</summary>
        /// <param name="render">The render context (profile, theme, glyphs).</param>
        /// <param name="culture">The culture for numbers and dates, or null for the render culture (<see cref="RenderContext.Culture"/>).</param>
        /// <param name="now">The reference time for relative dates, or null for the current time.</param>
        /// <param name="timeZone">The time zone for absolute dates, or null for local time.</param>
        public PresenterContext(RenderContext render, CultureInfo? culture = null, DateTimeOffset? now = null, TimeZoneInfo? timeZone = null) {
            Render = render ?? throw new ArgumentNullException(nameof(render));
            Culture = culture ?? render.Culture;
            Now = now ?? DateTimeOffset.Now;
            TimeZone = timeZone ?? TimeZoneInfo.Local;
        }

        /// <summary>The render context.</summary>
        public RenderContext Render { get; }

        /// <summary>The culture for numbers (thousand separators, decimals) and dates.</summary>
        public CultureInfo Culture { get; }

        /// <summary>The reference time for relative dates.</summary>
        public DateTimeOffset Now { get; }

        /// <summary>The time zone absolute dates are shown in.</summary>
        public TimeZoneInfo TimeZone { get; }

        /// <summary>The largest numeric value in the column (data bars scale to it), or null.</summary>
        public double? ColumnMaximum { get; set; }

        /// <summary>
        /// The decimals every number in the column is shown with (the most any of its values needs, at most two), so the
        /// decimal separators line up; null outside a column, where each number shows up to two decimals of its own.
        /// </summary>
        public int? ColumnDecimals { get; set; }

        /// <summary>Whether the output can carry colour; without it, tones are expressed with symbols and words.</summary>
        public bool HasColor => Render.HasColor;

        /// <summary>
        /// Whether OSC 8 hyperlinks can be emitted: interactive VT output on a terminal known to support them (Windows
        /// Terminal, VS Code). Elsewhere links are shown as text, with the address when it differs from the text.
        /// </summary>
        public bool Hyperlinks => Render.Profile.Mode == OutputMode.Interactive
            && (Render.Profile.Kind == TerminalKind.WindowsTerminal || Render.Profile.Kind == TerminalKind.VisualStudioCode);

        /// <summary>Returns a copy with a column maximum.</summary>
        /// <param name="maximum">The maximum.</param>
        /// <returns>The copy.</returns>
        public PresenterContext WithColumnMaximum(double? maximum) {
            return new PresenterContext(Render, Culture, Now, TimeZone) { ColumnMaximum = maximum, ColumnDecimals = ColumnDecimals };
        }

        /// <summary>Returns a copy with column statistics: the largest value and the decimals the column's numbers share.</summary>
        /// <param name="maximum">The maximum, or null.</param>
        /// <param name="decimals">The shared decimals, or null.</param>
        /// <returns>The copy.</returns>
        public PresenterContext WithColumn(double? maximum, int? decimals) {
            return new PresenterContext(Render, Culture, Now, TimeZone) { ColumnMaximum = maximum, ColumnDecimals = decimals };
        }

        /// <summary>
        /// The decimals a number needs once rounded to two: 0 for whole numbers, 1 for 11.7, 2 for 3.14159 (numeric text
        /// such as "1.5" counts by its digits). Integer types, and numbers too large to carry fractions, need none.
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <returns>0, 1 or 2; 0 for non-numbers.</returns>
        public static int DecimalsNeeded(object? value) {
            if (value is string || value is double || value is float || value is decimal) {
                if (!PresenterValues.TryGetNumber(value, out double number) || Math.Abs(number) >= 1e15) {
                    return 0;
                }
                double hundredths = Math.Round(Math.Abs(number) * 100, MidpointRounding.AwayFromZero);
                if (hundredths % 100 == 0) {
                    return 0;
                }
                return hundredths % 10 == 0 ? 1 : 2;
            }
            return 0;
        }
    }
}
