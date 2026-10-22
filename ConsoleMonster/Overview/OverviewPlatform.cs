using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Overview {
    internal static class OverviewPlatform {
        internal static bool Windows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        internal static bool Linux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        internal static bool Mac => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        internal static Task<OverviewSection?> Run(Func<CancellationToken, OverviewSection?> read, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(read(token));
        }

        internal static TimeSpan? Uptime() {
            try {
                if (Windows) { return TimeSpan.FromMilliseconds(OverviewWindowsNative.TickCount()); }
                if (Linux) {
                    string value = File.ReadAllText("/proc/uptime").Split(' ')[0];
                    return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) ? TimeSpan.FromSeconds(seconds) : (TimeSpan?)null;
                }
                if (Mac) { return OverviewMacNative.Uptime(); }
            } catch (Exception ex) when (OptionalFailure(ex)) { }
            return null;
        }

        internal static async Task<OverviewSection?> ResourcesAsync(CancellationToken token) {
            List<OverviewItem> items = new List<OverviewItem>();
            if (TryCpu(out double total, out double idle)) {
                await Task.Delay(80, token).ConfigureAwait(false);
                if (TryCpu(out double nextTotal, out double nextIdle) && nextTotal > total) {
                    double used = Math.Max(0, Math.Min(100, 100 * (1 - (nextIdle - idle) / (nextTotal - total))));
                    items.Add(new OverviewItem("CPU", Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture) + " logical CPUs", used));
                }
            }
            token.ThrowIfCancellationRequested();
            if (TryMemory(out double bytes, out double available) && bytes > 0) {
                items.Add(MemoryObservation(bytes, available));
            }
            return new OverviewSection("Resources", "CPU / memory", items);
        }

        private static bool TryCpu(out double total, out double idle) {
            total = idle = 0;
            if (Windows) { return OverviewWindowsNative.Cpu(out total, out idle); }
            if (Mac) { return OverviewMacNative.Cpu(out total, out idle); }
            if (!Linux) { return false; }
            // Linux guest counters are already included in user/nice and must not be counted twice.
            string[] fields = File.ReadLines("/proc/stat").First().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 5 || fields[0] != "cpu") { return false; }
            for (int i = 1; i < Math.Min(fields.Length, 9); i++) {
                if (!double.TryParse(fields[i], NumberStyles.None, CultureInfo.InvariantCulture, out double value)) { return false; }
                total += value;
                if (i == 4 || i == 5) { idle += value; }
            }
            return true;
        }

        private static bool TryMemory(out double total, out double available) {
            total = available = 0;
            if (Windows) { return OverviewWindowsNative.Memory(out total, out available); }
            if (Mac) { return OverviewMacNative.Memory(out total, out available); }
            if (!Linux) { return false; }
            return TryLinuxMemory(File.ReadLines("/proc/meminfo"), out total, out available);
        }

        internal static bool TryLinuxMemory(IEnumerable<string> lines, out double total, out double available) {
            total = available = 0;
            bool hasAvailable = false;
            foreach (string line in lines) {
                string[] fields = line.Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length < 2 || !double.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out double value)) { continue; }
                if (fields[0] == "MemTotal") { total = value * 1024; }
                if (fields[0] == "MemAvailable") { available = value * 1024; hasAvailable = true; }
            }
            return total > 0 && hasAvailable && available >= 0;
        }

        internal static OverviewItem MemoryObservation(double total, double available) => new OverviewItem("Memory", GiB(total - available) + " / " + GiB(total), Math.Max(0, Math.Min(100, 100 * (1 - available / total))));

        internal static OverviewSection? Disks(CancellationToken token) {
            List<OverviewItem> items = new List<OverviewItem>();
            foreach (DriveInfo drive in DriveInfo.GetDrives()) {
                token.ThrowIfCancellationRequested();
                try {
                    // Network and removable volumes can block on disconnected hardware or authentication.
                    if (drive.DriveType != DriveType.Fixed || !drive.IsReady) { continue; }
                    long total = drive.TotalSize;
                    long free = drive.AvailableFreeSpace;
                    if (total > 0) {
                        items.Add(new OverviewItem(drive.Name, GiB(total - free) + " / " + GiB(total), Math.Max(0, Math.Min(100, 100d * (total - free) / total))));
                    }
                } catch (Exception ex) when (OptionalFailure(ex)) { }
            }
            return new OverviewSection("Disks", "Disks", items);
        }

        internal static OverviewSection? Network(CancellationToken token) {
            List<OverviewItem> items = new List<OverviewItem>();
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces()) {
                token.ThrowIfCancellationRequested();
                try {
                    if (adapter.OperationalStatus != OperationalStatus.Up || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback) { continue; }
                    List<string> addresses = new List<string>();
                    foreach (UnicastIPAddressInformation address in adapter.GetIPProperties().UnicastAddresses) {
                        if (address.Address.AddressFamily == AddressFamily.InterNetwork || (address.Address.AddressFamily == AddressFamily.InterNetworkV6 && !address.Address.IsIPv6LinkLocal)) {
                            addresses.Add(address.Address.ToString());
                        }
                    }
                    if (addresses.Count > 0) { items.Add(new OverviewItem(adapter.Name, string.Join(", ", addresses))); }
                } catch (Exception ex) when (OptionalFailure(ex) || ex is NetworkInformationException) { }
            }
            return new OverviewSection("Network", "Network", items);
        }

        internal static OverviewSection? Services(CancellationToken token) => Windows ? OverviewWindowsNative.Services(token) : Linux ? OverviewLinuxServices.Collect(token) : null;

        internal static OverviewSection? Updates(CancellationToken token) {
            token.ThrowIfCancellationRequested();
            if (Windows) { return OverviewWindowsNative.Updates(token); }
            if (!Linux) { return null; }
            List<OverviewItem> items = new List<OverviewItem>();
            const string statusPath = "/var/lib/update-notifier/updates-available";
            if (File.Exists(statusPath)) {
                foreach (string line in File.ReadLines(statusPath).Take(4)) {
                    if (!string.IsNullOrWhiteSpace(line)) { items.Add(new OverviewItem("Packages", line.Trim())); }
                }
            }
            if (File.Exists("/run/reboot-required")) { items.Add(new OverviewItem("Reboot", "Reboot required", null, ThemeToken.Warn)); }
            const string installLog = "/var/log/dpkg.log";
            if (File.Exists(installLog)) {
                try {
                    string? installed = OverviewFiles.ReadTail(installLog).Split('\n').AsEnumerable().Reverse().FirstOrDefault(line => line.Length > 20 && line.IndexOf(" status installed ", StringComparison.Ordinal) >= 0);
                    if (installed != null && DateTime.TryParseExact(installed.Substring(0, 19), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime time)) {
                        items.Add(new OverviewItem("Last install (log)", time.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)));
                    }
                } catch (Exception ex) when (OptionalFailure(ex)) { }
            }
            return new OverviewSection("Updates", "Updates / reboot", items);
        }

        private static string GiB(double bytes) => (bytes / (1024 * 1024 * 1024)).ToString("0.0", CultureInfo.InvariantCulture) + " GiB";

        internal static bool OptionalFailure(Exception ex) => ex is IOException || ex is UnauthorizedAccessException || ex is PlatformNotSupportedException
            || ex is System.Security.SecurityException || ex is DllNotFoundException || ex is EntryPointNotFoundException;
    }
}
