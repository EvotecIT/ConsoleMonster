using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    /// <summary>POSIX TTY entry points; callers guard Linux/macOS before loading libc.</summary>
    internal static class UnixInputNative {
        // termios is 60 bytes on Linux and 72 on macOS. It remains opaque; no pointer is passed to a smaller allocation.
        internal const int AttributesCapacity = 1024;
        internal static readonly System.Collections.Concurrent.ConcurrentQueue<byte> PendingInput = new System.Collections.Concurrent.ConcurrentQueue<byte>();

        [StructLayout(LayoutKind.Sequential)]
        private struct PollDescriptor {
            internal int Descriptor;
            internal short Events;
            internal short Returned;
        }

        internal static bool Ready(int descriptor) {
            PollDescriptor poll = new PollDescriptor { Descriptor = descriptor, Events = 1 };
            int result = Poll(ref poll, new UIntPtr(1), 0);
            // POLLHUP/POLLERR also need a read so closed input becomes an InputClosedException.
            return result > 0 && (poll.Returned & (1 | 8 | 16 | 32)) != 0;
        }

        /// <summary>
        /// Whether bytes read from standard input can go back into the terminal's input queue (TIOCSTI): only on our
        /// controlling terminal, and not on Linux kernels that disable it (dev.tty.legacy_tiocsti=0).
        /// </summary>
        internal static bool CanReturnInput() {
            bool mac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            if (!mac && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return false; }
            if (!mac) {
                try {
                    const string setting = "/proc/sys/dev/tty/legacy_tiocsti";
                    if (System.IO.File.Exists(setting) && System.IO.File.ReadAllText(setting).Trim() != "1") { return false; }
                } catch (Exception) { return false; }
            }
            return SessionOfTerminal(0) != -1;
        }

        /// <summary>Puts one byte back into the terminal's input queue as if it had been typed.</summary>
        internal static bool ReturnInput(byte value) {
            bool mac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            UIntPtr request = mac ? (UIntPtr)0x80017472 : (UIntPtr)0x5412;
            return (mac && RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? IoctlMacArm64(0, request, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref value)
                : Ioctl(0, request, ref value)) == 0;
        }

        [DllImport("libc", EntryPoint = "tcgetsid", SetLastError = true)]
        private static extern int SessionOfTerminal(int descriptor);
        [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
        private static extern int Ioctl(int descriptor, UIntPtr request, ref byte value);
        // Darwin arm64 puts variadic arguments on the stack after the fixed register arguments.
        [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
        private static extern int IoctlMacArm64(int descriptor, UIntPtr request, IntPtr x2, IntPtr x3, IntPtr x4, IntPtr x5, IntPtr x6, IntPtr x7, ref byte value);
        [DllImport("libc", EntryPoint = "dup", SetLastError = true)]
        internal static extern int Duplicate(int descriptor);
        [DllImport("libc", EntryPoint = "close", SetLastError = true)]
        internal static extern int Close(int descriptor);
        [DllImport("libc", EntryPoint = "tcgetattr", SetLastError = true)]
        internal static extern int GetAttributes(int descriptor, IntPtr attributes);
        [DllImport("libc", EntryPoint = "tcsetattr", SetLastError = true)]
        internal static extern int SetAttributes(int descriptor, int action, IntPtr attributes);
        [DllImport("libc", EntryPoint = "cfmakeraw")]
        internal static extern void MakeRaw(IntPtr attributes);
        [DllImport("libc", EntryPoint = "read", SetLastError = true)]
        internal static extern IntPtr Read(int descriptor, [Out] byte[] bytes, UIntPtr count);
        [DllImport("libc", EntryPoint = "poll", SetLastError = true)]
        private static extern int Poll(ref PollDescriptor descriptor, UIntPtr count, int timeout);
    }
}
