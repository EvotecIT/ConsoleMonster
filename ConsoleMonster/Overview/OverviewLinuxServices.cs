using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Overview {
    // Only the local systemd manager's GetUnit/ActiveState API. No general D-Bus client or shell fallback.
    internal static class OverviewLinuxServices {
        internal static OverviewSection? Collect(CancellationToken token) {
            IntPtr bus = IntPtr.Zero;
            try {
                if (sd_bus_open_system(out bus) < 0) { return null; }
                if (sd_bus_set_method_call_timeout(bus, 100000) < 0) { return null; } // 100 ms per local RPC
                List<OverviewItem> stopped = new List<OverviewItem>();
                int observed = 0;
                foreach (string service in new[] { "systemd-journald.service", "dbus.service", "ssh.service", "sshd.service", "cron.service", "crond.service" }) {
                    token.ThrowIfCancellationRequested();
                    string? state = State(bus, service);
                    if (state == null) { continue; }
                    observed++;
                    if (state != "active") { stopped.Add(new OverviewItem(service, state, null, ThemeToken.Warn)); }
                }
                if (observed == 0) { return null; }
                if (stopped.Count == 0) { stopped.Add(new OverviewItem("Status", "All " + observed.ToString(CultureInfo.InvariantCulture) + " running", null, ThemeToken.Pass)); }
                return new OverviewSection("Services", "Key services", stopped);
            } finally { if (bus != IntPtr.Zero) { sd_bus_unref(bus); } }
        }

        private static string? State(IntPtr bus, string service) {
            IntPtr reply = IntPtr.Zero;
            IntPtr value = IntPtr.Zero;
            try {
                if (sd_bus_call_method(bus, "org.freedesktop.systemd1", "/org/freedesktop/systemd1", "org.freedesktop.systemd1.Manager", "GetUnit", IntPtr.Zero, out reply, "s", service) < 0) { return null; }
                if (sd_bus_message_read(reply, "o", out IntPtr path) < 0) { return null; }
                string? objectPath = Marshal.PtrToStringAnsi(path);
                if (objectPath == null || sd_bus_get_property_string(bus, "org.freedesktop.systemd1", objectPath, "org.freedesktop.systemd1.Unit", "ActiveState", IntPtr.Zero, out value) < 0) { return null; }
                return Marshal.PtrToStringAnsi(value);
            } finally {
                if (value != IntPtr.Zero) { free(value); }
                if (reply != IntPtr.Zero) { sd_bus_message_unref(reply); }
            }
        }

        [DllImport("libsystemd.so.0")]
        private static extern int sd_bus_open_system(out IntPtr bus);
        [DllImport("libsystemd.so.0")]
        private static extern int sd_bus_set_method_call_timeout(IntPtr bus, ulong microseconds);
        [DllImport("libsystemd.so.0", CharSet = CharSet.Ansi)]
        private static extern int sd_bus_call_method(IntPtr bus, string destination, string path, string interfaceName, string member, IntPtr error, out IntPtr reply, string signature, string argument);
        [DllImport("libsystemd.so.0", CharSet = CharSet.Ansi)]
        private static extern int sd_bus_message_read(IntPtr message, string signature, out IntPtr value);
        [DllImport("libsystemd.so.0", CharSet = CharSet.Ansi)]
        private static extern int sd_bus_get_property_string(IntPtr bus, string destination, string path, string interfaceName, string member, IntPtr error, out IntPtr value);
        [DllImport("libsystemd.so.0")]
        private static extern IntPtr sd_bus_message_unref(IntPtr message);
        [DllImport("libsystemd.so.0")]
        private static extern IntPtr sd_bus_unref(IntPtr bus);
        [DllImport("libc")]
        private static extern void free(IntPtr value);
    }
}
