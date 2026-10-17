using System;
using System.Collections.Generic;
using ConsoleMonster.Styling;
using ConsoleMonster.TestRuns;

namespace ConsoleMonster.PowerShell.Pester {
    /// <summary>
    /// Receives the events of the Pester adapter (plain values, so nothing here depends on Pester types) and feeds a
    /// <see cref="TestRun"/>. Called on the pipeline thread from inside Pester's plugin steps; it never throws back into
    /// Pester (a problem is recorded in <see cref="Faults"/> and the run goes on).
    /// </summary>
    public sealed class PesterBridge {
        private readonly TestRun _run;

        internal PesterBridge(TestRun run) {
            _run = run;
        }

        /// <summary>Called when the route is known: the Pester version, whether the adapter runs, why not, and whether output is off.</summary>
        internal Action<string, bool, string?, bool>? OnBegin { get; set; }

        /// <summary>Called for each failure with a message, as it happens (stream output).</summary>
        internal Action<TestRecord>? OnFailure { get; set; }

        /// <summary>Called for each error, warning, verbose, debug or information record the tests wrote while the live view ran.</summary>
        internal Action<string, string>? OnRecord { get; set; }

        /// <summary>Called when a container starts or ends (the live view repaints fully then, clearing anything written over it).</summary>
        internal Action? OnContainer { get; set; }

        /// <summary>The Pester version that ran.</summary>
        public string? PesterVersion { get; private set; }

        /// <summary>Whether the adapter ran (false: Pester ran unchanged).</summary>
        public bool UsedAdapter { get; private set; }

        /// <summary>Why the adapter did not run, or null.</summary>
        public string? FallbackReason { get; private set; }

        /// <summary>Problems inside plugin steps (step and message); the run continued.</summary>
        public List<string> Faults { get; } = new List<string>();

        /// <summary>The route was chosen.</summary>
        /// <param name="version">The Pester version.</param>
        /// <param name="adapter">Whether the adapter runs.</param>
        /// <param name="reason">Why it does not, or null.</param>
        /// <param name="quiet">Whether output is None.</param>
        public void Begin(string version, bool adapter, string? reason, bool quiet) {
            PesterVersion = version;
            UsedAdapter = adapter;
            FallbackReason = reason;
            OnBegin?.Invoke(version, adapter, reason, quiet);
        }

        /// <summary>Discovery finished.</summary>
        /// <param name="total">Tests that will run.</param>
        public void Discovered(int total) => _run.Discovered(total);

        /// <summary>A container is known before it runs (found up front or discovered).</summary>
        /// <param name="key">The key.</param>
        /// <param name="name">The display name.</param>
        /// <param name="file">The file, or null.</param>
        /// <param name="tests">Tests it will run, or 0 when not known yet.</param>
        public void ContainerQueued(string key, string name, string? file, int tests) => _run.ContainerQueued(key ?? string.Empty, name ?? string.Empty, file, tests);

        /// <summary>A container started.</summary>
        /// <param name="key">The key.</param>
        /// <param name="name">The display name.</param>
        /// <param name="file">The file, or null.</param>
        /// <param name="tests">Tests it will run.</param>
        public void ContainerStarted(string key, string name, string? file, int tests) {
            _run.ContainerStarted(key ?? string.Empty, name ?? string.Empty, file, tests);
            OnContainer?.Invoke();
        }

        /// <summary>A test started.</summary>
        /// <param name="container">The container key.</param>
        /// <param name="blocks">The enclosing blocks.</param>
        /// <param name="name">The test name.</param>
        public void TestStarted(string? container, string[]? blocks, string name) => _run.TestStarted(container ?? string.Empty, blocks, name ?? string.Empty);

        /// <summary>A test ended.</summary>
        /// <param name="container">The container key.</param>
        /// <param name="blocks">The enclosing blocks.</param>
        /// <param name="name">The test name.</param>
        /// <param name="result">Pester's result: Passed, Failed, Skipped, Inconclusive or NotRun.</param>
        /// <param name="milliseconds">The duration.</param>
        /// <param name="message">The failure message, or null.</param>
        /// <param name="file">The file of the failing line or the test.</param>
        /// <param name="line">The line.</param>
        public void TestFinished(string? container, string[]? blocks, string name, string? result, double milliseconds, string? message, string? file, int line) {
            TestRecord record = new TestRecord(container ?? string.Empty, name ?? string.Empty, Status(result)) {
                Blocks = blocks ?? new string[0],
                Duration = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds)),
                Message = string.IsNullOrEmpty(message) ? null : message,
                File = string.IsNullOrEmpty(file) ? null : file,
                Line = Math.Max(0, line)
            };
            _run.TestFinished(record);
            if (record.Status == StatusKind.Fail && record.Message != null) {
                OnFailure?.Invoke(record);
            }
        }

        /// <summary>A block (or a whole container) failed outside a test: in setup, teardown or discovery.</summary>
        /// <param name="container">The container key.</param>
        /// <param name="blocks">The enclosing blocks.</param>
        /// <param name="name">The block name.</param>
        /// <param name="message">The message.</param>
        /// <param name="file">The file.</param>
        /// <param name="line">The line.</param>
        public void BlockFailed(string? container, string[]? blocks, string name, string? message, string? file, int line) {
            TestRecord record = new TestRecord(container ?? string.Empty, name ?? string.Empty, StatusKind.Fail) {
                Blocks = blocks ?? new string[0],
                Message = string.IsNullOrEmpty(message) ? "failed" : message,
                File = string.IsNullOrEmpty(file) ? null : file,
                Line = Math.Max(0, line),
                IsBlockFailure = true
            };
            _run.TestFinished(record);
            OnFailure?.Invoke(record);
        }

        /// <summary>A container ended.</summary>
        /// <param name="key">The key.</param>
        public void ContainerFinished(string? key) {
            _run.ContainerFinished(key ?? string.Empty);
            OnContainer?.Invoke();
        }

        /// <summary>The run ended.</summary>
        public void End() => _run.Finished();

        /// <summary>A record the tests wrote (error, warning, verbose, debug or information), collected while the live view runs.</summary>
        /// <param name="kind">The stream.</param>
        /// <param name="text">The text.</param>
        public void Record(string kind, string? text) => OnRecord?.Invoke(kind ?? string.Empty, text ?? string.Empty);

        /// <summary>A plugin step failed; the run goes on.</summary>
        /// <param name="step">The step.</param>
        /// <param name="message">The message.</param>
        public void Fault(string step, string message) => Faults.Add(step + ": " + message);

        /// <summary>Maps Pester's result words to statuses.</summary>
        /// <param name="result">The result.</param>
        /// <returns>The status.</returns>
        internal static StatusKind Status(string? result) {
            switch ((result ?? string.Empty).ToLowerInvariant()) {
                case "passed":
                    return StatusKind.Pass;
                case "failed":
                    return StatusKind.Fail;
                case "skipped":
                    return StatusKind.Skip;
                case "inconclusive":
                    return StatusKind.Warn;
                default:
                    return StatusKind.Pending;
            }
        }
    }
}
