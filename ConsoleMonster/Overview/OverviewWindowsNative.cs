using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Overview {
    // Called only after the platform guard. All SCM/registry access is read-only and handles are closed here.
    internal static class OverviewWindowsNative {
        internal static ulong TickCount() => GetTickCount64();

        internal static bool Cpu(out double total, out double idle) {
            bool success = GetSystemTimes(out long idleTime, out long kernel, out long user);
            total = (double)kernel + user;
            idle = idleTime;
            return success;
        }

        internal static bool Memory(out double total, out double available) {
            byte[] status = new byte[64];
            status[0] = 64;
            bool success = GlobalMemoryStatusEx(status);
            total = BitConverter.ToUInt64(status, 8);
            available = BitConverter.ToUInt64(status, 16);
            return success;
        }

        internal static OverviewSection? Services(CancellationToken token) {
            IntPtr manager = OpenSCManager(null, null, 1); // SC_MANAGER_CONNECT
            if (manager == IntPtr.Zero) { return null; }
            List<OverviewItem> stopped = new List<OverviewItem>();
            int observed = 0;
            try {
                foreach (string name in new[] { "RpcSs", "EventLog", "Schedule" }) {
                    token.ThrowIfCancellationRequested();
                    IntPtr service = OpenService(manager, name, 4); // SERVICE_QUERY_STATUS
                    if (service == IntPtr.Zero) { continue; }
                    try {
                        byte[] status = new byte[28];
                        if (!QueryServiceStatus(service, status)) { continue; }
                        observed++;
                        uint state = BitConverter.ToUInt32(status, 4);
                        if (state != 4) { stopped.Add(new OverviewItem(name, state == 1 ? "Stopped" : "Not running (state " + state.ToString(CultureInfo.InvariantCulture) + ")", null, ThemeToken.Warn)); }
                    } finally { CloseServiceHandle(service); }
                }
            } finally { CloseServiceHandle(manager); }
            if (observed == 0) { return null; }
            if (stopped.Count == 0) { stopped.Add(new OverviewItem("Status", "All " + observed.ToString(CultureInfo.InvariantCulture) + " running", null, ThemeToken.Pass)); }
            return new OverviewSection("Services", "Key services", stopped);
        }

        internal static OverviewSection? Updates(CancellationToken token) {
            List<OverviewItem> items = new List<OverviewItem>();
            int? pending = OverviewWindowsUpdates.PendingCount(token);
            if (pending.HasValue) { items.Add(new OverviewItem("Pending (cached)", pending.Value.ToString(CultureInfo.InvariantCulture) + " updates")); }
            if (KeyExists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired")
                || KeyExists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending")) {
                items.Add(new OverviewItem("Reboot", "Reboot required", null, ThemeToken.Warn));
            }
            string? installed = RegistryText(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\Results\Install", "LastSuccessTime");
            if (!string.IsNullOrWhiteSpace(installed)) { items.Add(new OverviewItem("Last install", installed!)); }
            return new OverviewSection("Updates", "Updates / reboot", items);
        }

        private static bool KeyExists(string path) {
            int result = RegOpenKeyEx(new IntPtr(unchecked((int)0x80000002)), path, 0, 0x20019 | 0x100, out IntPtr key);
            if (result != 0) { return false; }
            RegCloseKey(key);
            return true;
        }

        private static string? RegistryText(string path, string value) {
            StringBuilder buffer = new StringBuilder(256);
            uint bytes = 512;
            return RegGetValue(new IntPtr(unchecked((int)0x80000002)), path, value, 2 | 0x10000, out uint _, buffer, ref bytes) == 0 ? buffer.ToString() : null;
        }

        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemTimes(out long idle, out long kernel, out long user);
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] byte[] status);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr OpenSCManager(string? machine, string? database, uint access);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr OpenService(IntPtr manager, string name, uint access);
        [DllImport("advapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryServiceStatus(IntPtr service, [Out] byte[] status);
        [DllImport("advapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr handle);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegOpenKeyEx(IntPtr root, string path, uint options, uint access, out IntPtr key);
        [DllImport("advapi32.dll")]
        private static extern int RegCloseKey(IntPtr key);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegGetValue(IntPtr root, string path, string value, uint flags, out uint type, StringBuilder buffer, ref uint bytes);
    }
}
