using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Overview {
    // Darwin counters avoid subprocesses, shell commands, and privileged performance facilities.
    internal static class OverviewMacNative {
        // Reuse one process-wide host send right rather than acquiring one for every login summary.
        private static readonly Lazy<uint> Host = new Lazy<uint>(mach_host_self);
        internal static bool Cpu(out double total, out double idle) {
            total = idle = 0;
            if (host_processor_info(Host.Value, 2, out uint processors, out IntPtr counters, out uint count) != 0) { return false; }
            try {
                if (count < processors * 4) { return false; }
                for (int i = 0; i < processors; i++) {
                    for (int state = 0; state < 4; state++) {
                        uint ticks = unchecked((uint)Marshal.ReadInt32(counters, (i * 4 + state) * 4));
                        total += ticks;
                        if (state == 2) { idle += ticks; }
                    }
                }
                return true;
            } finally { vm_deallocate(mach_task_self(), counters, new UIntPtr(count * 4)); }
        }

        internal static bool Memory(out double total, out double available) {
            total = available = 0;
            uint[] values = new uint[64];
            uint count = (uint)values.Length;
            uint host = Host.Value;
            if (host_statistics64(host, 4, values, ref count) != 0 || count < 4 || host_page_size(host, out UIntPtr pageSize) != 0) { return false; }
            byte[] size = Read("hw.memsize", 8);
            if (size.Length != 8) { return false; }
            total = BitConverter.ToUInt64(size, 0);
            // Available estimate: free + inactive pages, rather than process memory or a fabricated usage value.
            available = Math.Min(total, ((double)values[0] + values[2]) * pageSize.ToUInt64());
            return total > 0;
        }

        internal static TimeSpan? Uptime() {
            byte[] boot = Read("kern.boottime", 16);
            if (boot.Length < 8) { return null; }
            long seconds = BitConverter.ToInt64(boot, 0);
            return DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(seconds);
        }

        private static byte[] Read(string name, int bytes) {
            byte[] value = new byte[bytes];
            UIntPtr length = (UIntPtr)(uint)bytes;
            return sysctlbyname(name, value, ref length, IntPtr.Zero, UIntPtr.Zero) == 0 ? value : Array.Empty<byte>();
        }

        [DllImport("/usr/lib/libSystem.B.dylib")]
        private static extern uint mach_host_self();
        [DllImport("/usr/lib/libSystem.B.dylib")]
        private static extern uint mach_task_self();
        [DllImport("/usr/lib/libSystem.B.dylib")]
        private static extern int host_processor_info(uint host, int flavor, out uint processors, out IntPtr info, out uint count);
        [DllImport("/usr/lib/libSystem.B.dylib")]
        private static extern int vm_deallocate(uint task, IntPtr address, UIntPtr size);
        [DllImport("/usr/lib/libSystem.B.dylib")]
        private static extern int host_statistics64(uint host, int flavor, [Out] uint[] values, ref uint count);
        [DllImport("/usr/lib/libSystem.B.dylib")]
        // Darwin vm_size_t is uintptr_t on both LP64 architectures (mach/{arm,i386}/vm_types.h).
        private static extern int host_page_size(uint host, out UIntPtr pageSize);
        [DllImport("/usr/lib/libSystem.B.dylib", CharSet = CharSet.Ansi)]
        private static extern int sysctlbyname(string name, [Out] byte[] value, ref UIntPtr size, IntPtr newValue, UIntPtr newSize);
    }
}
