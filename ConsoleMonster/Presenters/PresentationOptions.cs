using System;
using System.Globalization;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// Culture, reference time and time zone for presenting values. Tables, record views, cards and key/value lists take
    /// one, so output is deterministic when these are fixed (tests, snapshots) and follows the user's settings otherwise.
    /// </summary>
    public sealed class PresentationOptions {
        /// <summary>The culture for numbers and dates, or null for the render culture (the profile's <see cref="Capabilities.TerminalProfile.Culture"/>, else the current culture).</summary>
        public CultureInfo? Culture { get; set; }

        /// <summary>The reference time for relative dates, or null for the current time at render.</summary>
        public DateTimeOffset? Now { get; set; }

        /// <summary>The time zone for absolute dates, or null for local time.</summary>
        public TimeZoneInfo? TimeZone { get; set; }

        /// <summary>Creates a presenter context for a render.</summary>
        /// <param name="render">The render context.</param>
        /// <returns>The presenter context.</returns>
        public PresenterContext CreateContext(RenderContext render) => new PresenterContext(render, Culture, Now, TimeZone);
    }
}
