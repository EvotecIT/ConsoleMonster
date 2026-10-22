using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Overview {
    internal static class OverviewSecurity {
        internal static OverviewSection? Collect(CancellationToken token) {
            if (OverviewPlatform.Windows) { return OverviewWindowsSecurity.Collect(token); }
            if (!OverviewPlatform.Linux) { return null; }
            // Never elevate to read authentication logs. Journal-only and inaccessible systems hide this panel.
            const string path = "/var/log/auth.log";
            if (!File.Exists(path)) { return null; }
            string tail = OverviewFiles.ReadTail(path);
            DateTimeOffset now = DateTimeOffset.Now;
            int failed = 0;
            List<OverviewItem> logins = new List<OverviewItem>();
            bool observed = false;
            foreach (string line in tail.Split('\n')) {
                token.ThrowIfCancellationRequested();
                if (!TryTime(line, now, out DateTimeOffset time) || time < now.AddDays(-1) || time > now) { continue; }
                if (line.IndexOf("Failed password", StringComparison.Ordinal) >= 0) { failed++; observed = true; }
                int accepted = line.IndexOf("Accepted ", StringComparison.Ordinal);
                if (accepted < 0) { continue; }
                observed = true;
                int start = line.IndexOf(" for ", accepted, StringComparison.Ordinal);
                if (start < 0) { continue; }
                start += 5;
                int end = line.IndexOf(' ', start);
                if (end > start) { logins.Add(new OverviewItem(time.ToString("HH:mm", CultureInfo.InvariantCulture), line.Substring(start, end - start))); }
            }
            if (!observed) { return null; }
            List<OverviewItem> items = new List<OverviewItem> {
                new OverviewItem("Failed SSH logons", failed.ToString(CultureInfo.InvariantCulture) + "+ in 24 h (log tail)", null, failed > 0 ? ThemeToken.Warn : ThemeToken.Text)
            };
            items.AddRange(logins.AsEnumerable().Reverse().Take(3));
            return new OverviewSection("Security", "Security (24 h sample)", items);
        }

        internal static bool TryTime(string line, DateTimeOffset now, out DateTimeOffset time) {
            int space = line.IndexOf(' ');
            if (space > 0 && DateTimeOffset.TryParse(line.Substring(0, space), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out time)) { return true; }
            time = default;
            if (line.Length < 15 || !DateTime.TryParseExact(line.Substring(0, 15), new[] { "MMM dd HH:mm:ss", "MMM  d HH:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime parsed)) { return false; }
            DateTime local = new DateTime(now.Year, parsed.Month, parsed.Day, parsed.Hour, parsed.Minute, parsed.Second);
            if (local > now.LocalDateTime.AddDays(1)) { local = local.AddYears(-1); }
            time = new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
            return true;
        }
    }
}
