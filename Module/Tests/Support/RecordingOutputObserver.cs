using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

// Collect a recorder's background callback without invoking a PowerShell script block on its reader thread.
public sealed class RecordingOutputObserver {
    private readonly object gate = new object();
    private readonly StringBuilder output = new StringBuilder();
    public string Text { get { lock (gate) { return output.ToString(); } } }
    public void Receive(string text) { lock (gate) { output.Append(text); } }
    public bool WaitFor(string text, TimeSpan timeout) { return WaitFor(text, timeout, 0); }
    // A later prompt must not be satisfied by a prompt already collected before the command ran.
    public bool WaitFor(string text, TimeSpan timeout, int start) {
        if (start < 0) { throw new ArgumentOutOfRangeException("start"); }
        Stopwatch clock = Stopwatch.StartNew();
        while (clock.Elapsed < timeout) {
            lock (gate) {
                if (output.Length >= start && output.ToString().IndexOf(text, start, StringComparison.Ordinal) >= 0) { return true; }
            }
            Thread.Sleep(20);
        }
        return false;
    }
}
