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
            return new PresenterContext(Render, Culture, Now, TimeZone) { ColumnMaximum = maximum };
        }
    }
}
