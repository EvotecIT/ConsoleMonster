using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;
using ConsoleMonster.Sessions;

namespace SiteFrames.Vt {
    /// <summary>One chunk of output and its arrival time in milliseconds.</summary>
    internal readonly struct OutputChunk {
        public OutputChunk(long time, byte[] data) { Time = time; Data = data; }
        public long Time { get; }
        public byte[] Data { get; }
    }

    /// <summary>Site capture adapter over the library's ConPTY transport.</summary>
    internal static class PseudoConsole {
        public static List<OutputChunk> Run(string commandLine, int width, int height, IDictionary<string, string?> environment, string workingDirectory, TimeSpan timeout, TimeSpan? interrupt = null, string? inputText = null, string? readyMarker = null, string? capturePath = null, string? completionText = null, string? readyText = null, string? inputKeys = null) {
            if (!OperatingSystem.IsWindows()) { throw new PlatformNotSupportedException("Captures run in a Windows pseudo console."); }
            SessionRecordingOptions options = new SessionRecordingOptions { Width = width, Height = height, WorkingDirectory = workingDirectory };
            foreach (KeyValuePair<string, string?> entry in environment) { options.Environment[entry.Key] = entry.Value; }
            using (WindowsPseudoTerminal terminal = new WindowsPseudoTerminal(options, commandLine)) {
                List<OutputChunk> chunks = new List<OutputChunk>();
                Stopwatch clock = Stopwatch.StartNew();
                Exception? failure = null;
                Thread reader = new Thread(() => {
                    byte[] buffer = new byte[65536];
                    try {
                        int count;
                        while ((count = terminal.Read(buffer)) > 0) {
                            byte[] copy = new byte[count]; Buffer.BlockCopy(buffer, 0, copy, 0, count);
                            lock (chunks) { chunks.Add(new OutputChunk(clock.ElapsedMilliseconds, copy)); }
                        }
                    } catch (IOException) { }
                    catch (Exception error) { failure = error; }
                }) { IsBackground = true, Name = "ConsoleMonster site capture" };
                reader.Start();
                int exitCode;
                bool interrupted = false;
                try {
                    string CapturedText() {
                        using (MemoryStream buffer = new MemoryStream()) {
                            lock (chunks) { foreach (OutputChunk chunk in chunks) { buffer.Write(chunk.Data, 0, chunk.Data.Length); } }
                            return Encoding.UTF8.GetString(buffer.ToArray());
                        }
                    }
                    void WaitForText(string marker) {
                        while (!CapturedText().Contains(marker, StringComparison.Ordinal)) {
                            if (terminal.HasExited) { throw new InvalidOperationException("Capture exited before its input-ready marker: " + CapturedText()); }
                            if (clock.Elapsed > timeout) { throw new TimeoutException("Capture did not reach its input-ready marker."); }
                            Thread.Sleep(20);
                        }
                    }
                    if (readyText != null && inputKeys != null) {
                        Console.WriteLine("ConPTY child PID " + terminal.ProcessId);
                        WaitForText(readyText);
                        terminal.Write(Encoding.UTF8.GetBytes(inputKeys));
                    }
                    if (inputText != null) {
                        if (readyMarker != null) { WaitForText(readyMarker); }
                        terminal.Write(Encoding.UTF8.GetBytes(inputText));
                        if (completionText != null) {
                            Thread.Sleep(250);
                            if (terminal.HasExited) { throw new InvalidOperationException("Capture exited before completing native input: " + commandLine); }
                            terminal.Write(Encoding.UTF8.GetBytes(completionText));
                        }
                    }
                    while (!terminal.HasExited) {
                        if (interrupt != null && !interrupted && clock.Elapsed >= interrupt.Value) { terminal.Write(new byte[] { 3 }); interrupted = true; }
                        if (clock.Elapsed > timeout) { throw new TimeoutException("Capture timed out: " + commandLine); }
                        Thread.Sleep(20);
                    }
                    exitCode = terminal.ExitCode;
                    if (interrupt != null && !interrupted) { throw new InvalidOperationException("The program exited before Ctrl+C was pressed: " + commandLine); }
                    // Preserve the site's existing capture settling interval.
                    Thread.Sleep(400);
                } finally {
                    terminal.Stop(); terminal.Complete();
                    if (!reader.Join(TimeSpan.FromSeconds(10))) { throw new IOException("The capture output reader did not finish."); }
                    if (capturePath != null) {
                        using (FileStream captured = File.Create(capturePath)) {
                            lock (chunks) { foreach (OutputChunk chunk in chunks) { captured.Write(chunk.Data, 0, chunk.Data.Length); } }
                        }
                    }
                }
                if (failure != null) { throw new IOException("Reading site capture output failed.", failure); }
                if (exitCode != 0) {
                    using (MemoryStream captured = new MemoryStream()) {
                        lock (chunks) { foreach (OutputChunk chunk in chunks) { captured.Write(chunk.Data, 0, chunk.Data.Length); } }
                        throw new InvalidOperationException("Capture process exited with code " + exitCode + ": " + commandLine + "\n" + Encoding.UTF8.GetString(captured.ToArray()));
                    }
                }
                lock (chunks) { return new List<OutputChunk>(chunks); }
            }
        }
    }
}
