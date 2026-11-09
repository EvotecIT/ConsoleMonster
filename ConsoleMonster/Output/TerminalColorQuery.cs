using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Output {
    // One startup exchange owner for OSC 4/10/11. Call only inside ConsoleInput.BeforeFirstRead.
    internal static class TerminalColorQuery {
        internal static void Read(string query, Func<string, bool> consume, Func<bool> complete) {
            string? term = Environment.GetEnvironmentVariable("TERM");
            if (WindowsConsole.IsWindows || Console.IsInputRedirected || Console.IsOutputRedirected || string.IsNullOrEmpty(term) || term == "dumb"
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"))
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD"))) { return; }
            IntPtr saved = Marshal.AllocHGlobal(UnixInputNative.AttributesCapacity), raw = Marshal.AllocHGlobal(UnixInputNative.AttributesCapacity);
            bool changed = false;
            StringBuilder received = new StringBuilder();
            try {
                if (UnixInputNative.GetAttributes(0, saved) != 0) { return; }
                byte[] mode = new byte[UnixInputNative.AttributesCapacity];
                Marshal.Copy(saved, mode, 0, mode.Length); Marshal.Copy(mode, 0, raw, mode.Length);
                UnixInputNative.MakeRaw(raw);
                if (UnixInputNative.SetAttributes(0, 0, raw) != 0) { return; }
                changed = true;
                byte[] bytes = Encoding.ASCII.GetBytes(query);
                using (System.IO.Stream output = Console.OpenStandardOutput()) { output.Write(bytes, 0, bytes.Length); output.Flush(); }
                Stopwatch budget = Stopwatch.StartNew();
                byte[] one = new byte[1];
                while (!complete() && budget.ElapsedMilliseconds < 100 && received.Length < 4096) {
                    if (!UnixInputNative.Ready(0)) { Thread.Sleep(1); continue; }
                    if (UnixInputNative.Read(0, one, new UIntPtr(1)).ToInt64() != 1) { break; }
                    received.Append((char)one[0]);
                    string text = received.ToString();
                    int start = text.LastIndexOf("\u001b]", StringComparison.Ordinal);
                    if (start < 0) { continue; }
                    int end = text.IndexOf('\u0007', start), terminator = 1;
                    if (end < 0) { end = text.IndexOf("\u001b\\", start, StringComparison.Ordinal); terminator = 2; }
                    if (end < 0) { continue; }
                    string reply = text.Substring(start, end + terminator - start);
                    if (consume(reply)) { received.Remove(start, reply.Length); }
                }
            } finally {
                if (changed) { UnixInputNative.SetAttributes(0, 0, saved); }
                foreach (char value in received.ToString()) { UnixInputNative.PendingInput.Enqueue((byte)value); }
                Marshal.FreeHGlobal(raw); Marshal.FreeHGlobal(saved);
            }
        }
    }
}
