using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Overview {
    internal static partial class OverviewPlatform {
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
                // A one-shot welcome screen needs a complete interval on its first collection.
                // The collector keeps its own shorter caller budget and returns pending if this cannot fit.
                await Task.Delay(250, token).ConfigureAwait(false);
                if (TryCpu(out double nextTotal, out double nextIdle)) {
                    OverviewItem? cpu = CpuObservation(total, idle, nextTotal, nextIdle);
                    if (cpu != null) { items.Add(cpu); }
                }
            }
            token.ThrowIfCancellationRequested();
            if (TryMemory(out double bytes, out double available) && bytes > 0) {
                items.Add(MemoryObservation(bytes, available));
            }
            return new OverviewSection("Resources", "CPU / memory", items);
        }

        /// <summary>Builds CPU usage only from advancing counters; cached or reset samples have no measurable interval.</summary>
        internal static OverviewItem? CpuObservation(double total, double idle, double nextTotal, double nextIdle) {
            if (!(nextTotal > total) || nextIdle < idle || nextIdle - idle > nextTotal - total) { return null; }
            double used = Math.Max(0, Math.Min(100, 100 * (1 - (nextIdle - idle) / (nextTotal - total))));
            return new OverviewItem("CPU", Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture) + " logical CPUs", used);
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
                    if (!VisibleVolume(drive.Name, Mac) || drive.DriveType != DriveType.Fixed || !drive.IsReady) { continue; }
                    long total = drive.TotalSize;
                    long free = drive.AvailableFreeSpace;
                    if (total > 0) {
                        items.Add(new OverviewItem(Mac && drive.Name == "/" ? (string.IsNullOrEmpty(drive.VolumeLabel) || drive.VolumeLabel == "/" ? "Macintosh HD" : drive.VolumeLabel) : Mac ? Path.GetFileName(drive.Name.TrimEnd('/')) : drive.Name, GiB(total - free) + " / " + GiB(total), Math.Max(0, Math.Min(100, 100d * (total - free) / total))));
                    }
                } catch (Exception ex) when (OptionalFailure(ex)) { }
            }
            return new OverviewSection("Disks", "Disks", items);
        }

        internal static OverviewSection? Network(CancellationToken token) {
            List<OverviewItem> items = new List<OverviewItem>();
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces().OrderByDescending(HasGateway).ThenBy(adapter => adapter.Name, StringComparer.Ordinal)) {
                token.ThrowIfCancellationRequested();
                try {
                    if (HiddenInterface(adapter.Name) || adapter.OperationalStatus != OperationalStatus.Up || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback) { continue; }
                    IPAddress[] addresses = NetworkAddresses(adapter.GetIPProperties().UnicastAddresses.Select(address => address.Address));
                    if (addresses.Length > 0) { items.Add(new OverviewItem(adapter.Name, string.Join(", ", addresses.Select(address => address.ToString())))); }
                } catch (Exception ex) when (OptionalFailure(ex) || ex is NetworkInformationException) { }
            }
            return new OverviewSection("Network", "Network", items);
        }

        /// <summary>Prefers IPv4, retaining global IPv6 alongside it and local IPv6 only as an IPv6-only fallback.</summary>
        internal static IPAddress[] NetworkAddresses(IEnumerable<IPAddress> addresses) {
            IPAddress[] usable = addresses.Where(address => !IPAddress.IsLoopback(address)
                && (address.AddressFamily == AddressFamily.InterNetwork
                    || (address.AddressFamily == AddressFamily.InterNetworkV6 && !address.IsIPv6LinkLocal && !address.IsIPv6Multicast
                        && !address.Equals(IPAddress.IPv6Any)))).Distinct().ToArray();
            bool ipv4 = usable.Any(address => address.AddressFamily == AddressFamily.InterNetwork);
            // Global unicast IPv6 occupies 2000::/3; ULA, site-local and mapped addresses are not global.
            return usable.Where(address => address.AddressFamily == AddressFamily.InterNetwork || !ipv4 || (address.GetAddressBytes()[0] & 0xe0) == 0x20)
                .OrderBy(address => address.AddressFamily == AddressFamily.InterNetwork ? 0 : 1).ToArray();
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
