using System;
using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.TestRuns {
    /// <summary>
    /// One finished test of a <see cref="TestRun"/>: where it lives (container, blocks), how it ended, how long it took, and
    /// for a failure the message and the location. Test frameworks are mapped onto it by adapters (for example
    /// Invoke-ConsolePester for Pester).
    /// </summary>
    public sealed class TestRecord {
        /// <summary>Creates a record.</summary>
        /// <param name="container">The container key (a test file, usually its full path).</param>
        /// <param name="name">The test name (expanded, as reported).</param>
        /// <param name="status">How it ended: Pass, Fail, Skip, Warn (inconclusive) or Pending (not run).</param>
        public TestRecord(string container, string name, StatusKind status) {
            Container = container ?? string.Empty;
            Name = name ?? string.Empty;
            Status = status;
        }

        /// <summary>The container key.</summary>
        public string Container { get; }

        /// <summary>The test name.</summary>
        public string Name { get; }

        /// <summary>How the test ended.</summary>
        public StatusKind Status { get; }

        /// <summary>The enclosing blocks, outermost first (Describe, Context).</summary>
        public IReadOnlyList<string> Blocks { get; set; } = new string[0];

        /// <summary>How long the test took.</summary>
        public TimeSpan Duration { get; set; }

        /// <summary>The failure message, or null.</summary>
        public string? Message { get; set; }

        /// <summary>The file of the failing line (or of the test), or null.</summary>
        public string? File { get; set; }

        /// <summary>The failing line (or the test's first line), or 0 when unknown.</summary>
        public int Line { get; set; }

        /// <summary>The exception type of an error that is not an assertion (shown before the message), or null.</summary>
        public string? ErrorType { get; set; }

        /// <summary>Whether the record stands for a block or container that failed outside a test (setup, teardown, discovery).</summary>
        public bool IsBlockFailure { get; set; }
    }
}
