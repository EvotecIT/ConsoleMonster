using System;
using System.Globalization;

namespace ConsoleMonster.Presenters;

/// <summary>Compact count, duration and observation-time wording shared by default views.</summary>
public static class DisplayText {
    /// <summary>Formats a count using the supplied singular and plural nouns.</summary>
    public static string Count(long count, string singular, string? plural = null) => count.ToString("N0", CultureInfo.CurrentCulture) + " " + (count == 1 ? singular : plural ?? singular + "s");
    /// <summary>Formats a nonnegative duration using at most two units.</summary>
    public static string Duration(TimeSpan duration) {
        if (duration < TimeSpan.Zero) { duration = TimeSpan.Zero; }
        if (duration.TotalDays >= 1) { return (int)duration.TotalDays + "d" + (duration.Hours > 0 ? " " + duration.Hours + "h" : string.Empty); }
        if (duration.TotalHours >= 1) { return (int)duration.TotalHours + "h" + (duration.Minutes > 0 ? " " + duration.Minutes + "m" : string.Empty); }
        if (duration.TotalMinutes >= 1) { return (int)duration.TotalMinutes + "m" + (duration.Seconds > 0 ? " " + duration.Seconds.ToString("00", CultureInfo.InvariantCulture) + "s" : string.Empty); }
        if (duration.TotalSeconds >= 1 || duration == TimeSpan.Zero) { return duration.TotalSeconds.ToString("0.#", CultureInfo.InvariantCulture) + "s"; }
        return Math.Round(duration.TotalMilliseconds).ToString(CultureInfo.InvariantCulture) + " ms";
    }
    /// <summary>Formats local observation time, adding a date when it is not today.</summary>
    public static string Time(DateTimeOffset value, DateTimeOffset? now = null) {
        DateTimeOffset local = value.ToLocalTime();
        return local.ToString(local.Date == (now ?? DateTimeOffset.Now).ToLocalTime().Date ? "HH:mm" : "yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);
    }
}
