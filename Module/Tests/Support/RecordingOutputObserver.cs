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
    public bool WaitFor(string text, TimeSpan timeout) {
        Stopwatch clock = Stopwatch.StartNew();
        while (clock.Elapsed < timeout) {
            lock (gate) { if (output.ToString().Contains(text)) { return true; } }
            Thread.Sleep(20);
        }
        return false;
    }
}
