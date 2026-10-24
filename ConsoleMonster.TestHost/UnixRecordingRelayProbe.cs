using System;
#if !NETFRAMEWORK
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ConsoleMonster.Sessions;
#endif

namespace ConsoleMonster.TestHost {
    /// <summary>Exercises a lost stdin descriptor in an owned PTY, away from the test runner's console.</summary>
    internal static class UnixRecordingRelayProbe {
#if !NETFRAMEWORK
        internal static void Run(Action<object> report) {
            int saved = dup(0);
            if (saved < 0) { throw new IOException("Cannot duplicate probe input."); }
            IDisposable? relay = null;
            try {
                Type type = typeof(SessionRecorder).Assembly.GetType("ConsoleMonster.Sessions.TerminalInputRelay", true)!;
                relay = (IDisposable)Activator.CreateInstance(type)!;
                if (close(0) != 0) { throw new IOException("Cannot close probe input."); }
                try {
                    type.GetMethod("ReadAvailable")!.Invoke(relay, null);
                    throw new InvalidOperationException("A lost terminal descriptor was silently ignored.");
                } catch (TargetInvocationException error) when (error.InnerException is IOException) {
                    report(new { phase = "relay_invalid_pass" });
                }
            } finally {
                if (dup2(saved, 0) < 0) { throw new IOException("Cannot restore probe input."); }
                close(saved);
                relay?.Dispose();
            }
        }
        [DllImport("libc")] private static extern int dup(int descriptor);
        [DllImport("libc")] private static extern int dup2(int descriptor, int target);
        [DllImport("libc")] private static extern int close(int descriptor);
#endif
    }
}
