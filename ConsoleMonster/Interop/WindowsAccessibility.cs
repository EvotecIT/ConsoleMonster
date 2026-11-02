using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    internal static class WindowsAccessibility {
        // SPI_GETHIGHCONTRAST reads one process-local snapshot: no registry traversal, subprocess or UI.
        internal static bool HighContrastEnabled() {
            if (!WindowsConsole.IsWindows) return false;
            try {
                var settings = new HighContrast { Size = (uint)Marshal.SizeOf<HighContrast>() };
                return SystemParametersInfo(0x0042, settings.Size, ref settings, 0) && (settings.Flags & 1) != 0;
            } catch (Exception) {
                // User32 may be unavailable in a service or a restricted host. Keep ordinary theme detection.
                return false;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HighContrast {
            internal uint Size;
            internal uint Flags;
            internal IntPtr DefaultScheme;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint action, uint parameter, ref HighContrast settings, uint flags);
    }
}
