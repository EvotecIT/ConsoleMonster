using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

// Demo driver only: relay a real child terminal without a PowerShell callback on its reader thread.
public static class TypedSession {
    private static readonly object Gate = new object();
    private static readonly StringBuilder Received = new StringBuilder();

    public static void Output(string text) {
        Console.Write(text);
        lock (Gate) { Received.Append(text); }
    }

    public static void WaitForPrompt() {
        Stopwatch clock = Stopwatch.StartNew();
        while (clock.Elapsed.TotalSeconds < 20) {
            lock (Gate) {
                if (Received.ToString().Contains("PS C:\\demo>")) { Received.Clear(); return; }
            }
            Thread.Sleep(20);
        }
        throw new TimeoutException("Demo shell did not return to its prompt.");
    }
}