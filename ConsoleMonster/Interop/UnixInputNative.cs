using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    /// <summary>POSIX TTY entry points; callers guard Linux/macOS before loading libc.</summary>
    internal static class UnixInputNative {
        // termios is 60 bytes on Linux and 72 on macOS. It remains opaque; no pointer is passed to a smaller allocation.
        internal const int AttributesCapacity = 1024;

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
