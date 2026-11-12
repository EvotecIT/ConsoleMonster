using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Output {
    // One startup exchange owner for OSC 4/10/11. Call only inside ConsoleInput.BeforeFirstRead.
    // With fence, a primary device attributes request (CSI c, which every VT terminal answers) follows the query and the
    // read waits for its reply, so a terminal that ignores the query never leaves a late reply in the input buffer.
    // The fenced read runs while nobody reads the keyboard, so keys typed during it go back into the terminal's input
    // queue for the shell (callers check UnixInputNative.CanReturnInput first).
    internal static class TerminalColorQuery {
        internal static void Read(string query, Func<string, bool> consume, Func<bool> complete, bool fence = false) {
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
                // Keys typed ahead belong to whoever reads next; don't mix them with the replies.
                if (fence && UnixInputNative.Ready(0)) { return; }
                bool fenced = false;
                byte[] bytes = Encoding.ASCII.GetBytes(fence ? query + "\u001b[c" : query);
                using (System.IO.Stream output = Console.OpenStandardOutput()) { output.Write(bytes, 0, bytes.Length); output.Flush(); }
                Stopwatch budget = Stopwatch.StartNew();
                byte[] one = new byte[1];
                while (!(fence ? fenced : complete()) && budget.ElapsedMilliseconds < (fence ? 250 : 100) && received.Length < 4096) {
                    if (!UnixInputNative.Ready(0)) { Thread.Sleep(1); continue; }
                    if (UnixInputNative.Read(0, one, new UIntPtr(1)).ToInt64() != 1) { break; }
                    received.Append((char)one[0]);
                    string text = received.ToString();
                    if (fence && one[0] == (byte)'c') {
                        int attributes = text.LastIndexOf("\u001b[?", StringComparison.Ordinal);
                        if (attributes >= 0) { received.Remove(attributes, text.Length - attributes); fenced = true; continue; }
                    }
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
                string left = received.ToString();
                int returned = 0;
                // Restored first, so the shell's line discipline handles the returned keys as if they were typed now.
                while (fence && returned < left.Length && UnixInputNative.ReturnInput((byte)left[returned])) { returned++; }
                for (int i = returned; i < left.Length; i++) { UnixInputNative.PendingInput.Enqueue((byte)left[i]); }
                Marshal.FreeHGlobal(raw); Marshal.FreeHGlobal(saved);
            }
        }
    }
}
