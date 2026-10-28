using System;
using System.Globalization;

namespace ConsoleMonster.Presenters;

/// <summary>Compact count, duration and observation-time wording shared by default views.</summary>
public static class DisplayText {
    /// <summary>Formats a count using the supplied singular and plural nouns.</summary>
    public static string Count(long count, string singular, string? plural = null) => count.ToString("N0", CultureInfo.CurrentCulture) + " " + (count == 1 ? singular : plural ?? singular + "s");
    /// <summary>Formats a nonnegative duration using at most two units.</summary>
    public static string Duration(TimeSpan duration) => Duration(duration, CultureInfo.CurrentCulture);
    /// <summary>Formats a nonnegative duration using the destination's numeric culture.</summary>
    /// <param name="duration">The supplied elapsed time.</param><param name="culture">The destination's numeric culture.</param>
    /// <returns>A compact duration with at most two units.</returns>
    public static string Duration(TimeSpan duration, CultureInfo culture) {
        if (duration < TimeSpan.Zero) { duration = TimeSpan.Zero; }
        if (duration.TotalDays >= 1) { return (int)duration.TotalDays + "d" + (duration.Hours > 0 ? " " + duration.Hours + "h" : string.Empty); }
        if (duration.TotalHours >= 1) { return (int)duration.TotalHours + "h" + (duration.Minutes > 0 ? " " + duration.Minutes + "m" : string.Empty); }
        if (duration.TotalMinutes >= 1) { return (int)duration.TotalMinutes + "m" + (duration.Seconds > 0 ? " " + duration.Seconds.ToString("00", CultureInfo.InvariantCulture) + "s" : string.Empty); }
        if (duration.TotalSeconds >= 1 || duration == TimeSpan.Zero) { return duration.TotalSeconds.ToString("0.#", culture) + "s"; }
        return Math.Round(duration.TotalMilliseconds).ToString(culture) + " ms";
    }
    /// <summary>Formats an age at minute precision, retaining seconds for ages below one minute.</summary>
    /// <param name="age">The elapsed age; negative values are treated as zero.</param>
    /// <returns>A compact age without an "ago" suffix.</returns>
    public static string Age(TimeSpan age) {
        if (age < TimeSpan.Zero) { age = TimeSpan.Zero; }
        if (age.TotalMinutes >= 1 && age.TotalHours < 1) { return (int)age.TotalMinutes + "m"; }
        return Duration(age);
    }
    /// <summary>Formats local observation time, adding a date when it is not today.</summary>
    public static string Time(DateTimeOffset value, DateTimeOffset? now = null) {
        DateTimeOffset local = value.ToLocalTime();
        return local.ToString(local.Date == (now ?? DateTimeOffset.Now).ToLocalTime().Date ? "HH:mm" : "d MMM HH:mm", CultureInfo.CurrentCulture);
    }
    /// <summary>Formats a structured health glyph and literal label; healthy labels stay in the normal text role.</summary>
    /// <param name="status">The collected health state.</param><param name="text">The literal label.</param>
    /// <param name="context">The destination's glyphs and styles.</param><returns>Escaped status markup.</returns>
    public static string Status(Styling.StatusKind status, string text, Canvas.RenderContext context) {
        string role = Styling.StatusKinds.ToToken(status).ToString().ToLowerInvariant();
        string glyph = "[" + role + "]" + Text.Markup.Escape(context.Glyphs.StatusMarker(status)) + "[/]";
        return glyph + (text.Length == 0 ? string.Empty : " [" + (status == Styling.StatusKind.Pass ? "text" : role) + "]" + Text.Markup.Escape(text) + "[/]");
    }
}
