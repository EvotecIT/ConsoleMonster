using System.Globalization;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Turns Write-Progress data into events for an <see cref="ActivityList"/>.</summary>
    internal static class ProgressEvents {
        public static LiveEvent From(int id, int parent, string? activity, string? status, int percent, int secondsRemaining, bool completed, long time = 0) {
            string key = id.ToString(CultureInfo.InvariantCulture) + (parent != -1 ? "/" + parent.ToString(CultureInfo.InvariantCulture) : string.Empty);
            if (completed) {
                return new LiveEvent(ActivityList.ProgressTarget, "complete", key: key, time: time);
            }
            return new LiveEvent(ActivityList.ProgressTarget, "update", key: key, text: activity ?? string.Empty, status: string.IsNullOrEmpty(status) ? null : status, value: percent < 0 ? -1 : percent, total: secondsRemaining < 0 ? -1 : secondsRemaining, time: time);
        }
    }
}
