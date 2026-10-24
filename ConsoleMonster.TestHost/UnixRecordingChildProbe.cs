using System;
#if !NETFRAMEWORK
using System.Runtime.InteropServices;
using System.Threading;
#endif

namespace ConsoleMonster.TestHost {
    /// <summary>A direct child that cannot repair missing controlling-terminal setup as a shell might.</summary>
    internal static class UnixRecordingChildProbe {
        internal static int Run() {
#if NETFRAMEWORK
            throw new PlatformNotSupportedException();
#else
            if (OperatingSystem.IsWindows()) { throw new PlatformNotSupportedException(); }
            using ManualResetEventSlim resized = new ManualResetEventSlim();
            using PosixSignalRegistration signal = PosixSignalRegistration.Create(PosixSignal.SIGWINCH, _ => resized.Set());
            Console.WriteLine("READY " + Console.WindowWidth);
            if (!resized.Wait(TimeSpan.FromSeconds(8))) { throw new InvalidOperationException("The child did not receive SIGWINCH."); }
            Console.WriteLine("WINCH " + Console.WindowHeight + " " + Console.WindowWidth);
            return 0;
#endif
        }
    }
}
