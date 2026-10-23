using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleMonster.PowerShell {
    /// <summary>One asynchronous git probe per runspace, with a five-second cache and a 100 ms child-process budget.</summary>
    internal sealed class PromptGitCache : IDisposable {
        private readonly object _gate = new object();
        private readonly Func<long> _clock;
        private readonly Func<string, (string? Branch, bool Dirty)> _probe;
        private readonly Action<Action> _queue;
        private string? _path;
        private string? _branch;
        private bool _dirty;
        private long _next;
        private bool _busy;
        private bool _disposed;
        private Process? _child;

        internal PromptGitCache() {
            _clock = () => (long)(Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency));
            _probe = RunGit;
            _queue = action => Task.Run(action);
        }

        // Clocks, child execution and scheduling are external boundaries; controlled tests need no real sleeps.
        internal PromptGitCache(Func<long> clock, Func<string, (string? Branch, bool Dirty)> probe, Action<Action> queue) {
            _clock = clock;
            _probe = probe;
            _queue = queue;
        }

        internal (string? Branch, bool Dirty) Read(string path) {
            lock (_gate) {
                long now = _clock();
                bool same = string.Equals(path, _path, StringComparison.Ordinal);
                if (!_disposed && !_busy && (!same || now >= _next)) {
                    _path = path;
                    if (!same) { _branch = null; _dirty = false; }
                    _next = now + 5000;
                    _busy = true;
                    _queue(() => Probe(path));
                }
                return same ? (_branch, _dirty) : (null, false);
            }
        }

        private void Probe(string path) {
            try {
                var result = _probe(path);
                lock (_gate) {
                    if (!_disposed && string.Equals(path, _path, StringComparison.Ordinal)) {
                        _branch = result.Branch;
                        _dirty = result.Dirty;
                    }
                }
            } finally {
                lock (_gate) { _busy = false; }
            }
        }

        private (string? Branch, bool Dirty) RunGit(string path) {
            string? branch = null;
            bool dirty = false;
            try {
                using (Process process = new Process { StartInfo = new ProcessStartInfo {
                    FileName = "git", WorkingDirectory = path,
                    Arguments = "--no-optional-locks -c core.fsmonitor=false status --porcelain=v1 --branch --untracked-files=normal --ignore-submodules=all",
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8
                } }) {
                    lock (_gate) {
                        if (_disposed) { return (null, false); }
                    }
                    // Process creation can be slow (for example under antivirus scanning). Never hold the cache
                    // lock across it: the foreground prompt must be able to return its cached value immediately.
                    process.Start();
                    bool cancelled;
                    lock (_gate) {
                        cancelled = _disposed;
                        _child = process;
                    }
                    if (cancelled) { Stop(process); return (null, false); }
                    object outputGate = new object();
                    process.OutputDataReceived += (_, args) => {
                        lock (outputGate) {
                            string? line = args.Data;
                            if (line == null) { return; }
                            if (line.StartsWith("## ", StringComparison.Ordinal)) { branch = ParseBranch(line); }
                            else if (line.Length >= 3) { dirty = true; }
                        }
                    };
                    process.ErrorDataReceived += (_, __) => { };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    bool completed = process.WaitForExit(100);
                    if (!completed) {
                        Stop(process);
                        process.CancelOutputRead();
                        process.CancelErrorRead();
                    } else {
                        process.WaitForExit();
                        if (process.ExitCode != 0) { branch = null; dirty = false; }
                    }
                    lock (outputGate) { return completed && process.ExitCode == 0 ? (branch, dirty) : (null, false); }
                }
            } catch (Exception error) when (error is Win32Exception || error is InvalidOperationException || error is System.IO.IOException) {
                // Missing git, a removed directory or an unavailable repository omits the optional segment.
            } finally {
                lock (_gate) { _child = null; }
            }
            return (null, false);
        }

        internal static string ParseBranch(string header) {
            string branch = header.Substring(3);
            const string initial = "No commits yet on ";
            const string unborn = "Initial commit on ";
            if (branch.StartsWith(initial, StringComparison.Ordinal)) { return branch.Substring(initial.Length); }
            if (branch.StartsWith(unborn, StringComparison.Ordinal)) { return branch.Substring(unborn.Length); }
            int tracking = branch.IndexOf("...", StringComparison.Ordinal);
            return tracking < 0 ? branch : branch.Substring(0, tracking);
        }

        public void Dispose() {
            Process? child;
            lock (_gate) {
                _disposed = true;
                child = _child;
            }
            if (child != null) { Stop(child); }
        }

        private static void Stop(Process process) {
            try { if (!process.HasExited) { process.Kill(); } }
            catch (InvalidOperationException) { }
            catch (Win32Exception) { }
        }
    }
}
