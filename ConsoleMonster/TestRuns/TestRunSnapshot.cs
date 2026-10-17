using System;
using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.TestRuns {
    /// <summary>A consistent copy of a <see cref="TestRun"/> at one moment.</summary>
    public sealed class TestRunSnapshot {
        internal TestRunSnapshot(int? total, int passed, int failed, int skipped, int inconclusive, int notRun, IReadOnlyList<TestContainerState> containers,
            IReadOnlyList<TestRecord> failures, IReadOnlyList<TestRecord> slowest, IReadOnlyList<double> durations, string? currentName,
            IReadOnlyList<string>? currentBlocks, string? currentContainer, TimeSpan elapsed, bool isFinished) {
            Total = total;
            Passed = passed;
            Failed = failed;
            Skipped = skipped;
            Inconclusive = inconclusive;
            NotRun = notRun;
            Containers = containers;
            Failures = failures;
            Slowest = slowest;
            Durations = durations;
            CurrentName = currentName;
            CurrentBlocks = currentBlocks ?? new string[0];
            CurrentContainer = currentContainer;
            Elapsed = elapsed;
            IsFinished = isFinished;
        }

        /// <summary>The planned test count, once discovery has finished.</summary>
        public int? Total { get; }

        /// <summary>Tests passed.</summary>
        public int Passed { get; }

        /// <summary>Tests failed.</summary>
        public int Failed { get; }

        /// <summary>Tests skipped.</summary>
        public int Skipped { get; }

        /// <summary>Tests inconclusive.</summary>
        public int Inconclusive { get; }

        /// <summary>Tests not run.</summary>
        public int NotRun { get; }

        /// <summary>Tests that ended, whatever the outcome.</summary>
        public int Done => Passed + Failed + Skipped + Inconclusive + NotRun;

        /// <summary>Containers in the order they started.</summary>
        public IReadOnlyList<TestContainerState> Containers { get; }

        /// <summary>Failures with a message, and block failures, in the order they happened.</summary>
        public IReadOnlyList<TestRecord> Failures { get; }

        /// <summary>The slowest tests, slowest first.</summary>
        public IReadOnlyList<TestRecord> Slowest { get; }

        /// <summary>Durations of the tests that ran, in milliseconds.</summary>
        public IReadOnlyList<double> Durations { get; }

        /// <summary>The running test, or null.</summary>
        public string? CurrentName { get; }

        /// <summary>The running test's blocks.</summary>
        public IReadOnlyList<string> CurrentBlocks { get; }

        /// <summary>The running container's key, or null.</summary>
        public string? CurrentContainer { get; }

        /// <summary>The run time (fixed once the run finished).</summary>
        public TimeSpan Elapsed { get; }

        /// <summary>Whether the run finished.</summary>
        public bool IsFinished { get; }

        /// <summary>Block failures (setup or teardown outside tests).</summary>
        public int BlockFailures {
            get {
                int count = 0;
                foreach (TestContainerState container in Containers) {
                    count += container.BlockFailures;
                }
                return count;
            }
        }

        /// <summary>The overall status: running, then fail, warn, skip or pass.</summary>
        public StatusKind Status {
            get {
                if (!IsFinished) {
                    return StatusKind.Running;
                }
                if (Failed > 0 || BlockFailures > 0) {
                    return StatusKind.Fail;
                }
                if (Inconclusive > 0) {
                    return StatusKind.Warn;
                }
                return Passed == 0 && Done > 0 ? StatusKind.Skip : StatusKind.Pass;
            }
        }
    }

    /// <summary>One container (test file) of a test run.</summary>
    public sealed class TestContainerState {
        internal TestContainerState(string key, string name) {
            Key = key;
            Name = name;
        }

        /// <summary>The key (the file path).</summary>
        public string Key { get; }

        /// <summary>The display name.</summary>
        public string Name { get; internal set; }

        /// <summary>The full path, or null.</summary>
        public string? File { get; internal set; }

        /// <summary>The tests it plans to run (0 when unknown).</summary>
        public int Tests { get; internal set; }

        /// <summary>Tests passed.</summary>
        public int Passed { get; internal set; }

        /// <summary>Tests failed.</summary>
        public int Failed { get; internal set; }

        /// <summary>Tests skipped.</summary>
        public int Skipped { get; internal set; }

        /// <summary>Tests inconclusive.</summary>
        public int Inconclusive { get; internal set; }

        /// <summary>Tests not run.</summary>
        public int NotRun { get; internal set; }

        /// <summary>Blocks that failed outside tests.</summary>
        public int BlockFailures { get; internal set; }

        /// <summary>The status: pending, running, then the outcome.</summary>
        public StatusKind Status { get; internal set; } = StatusKind.Pending;

        /// <summary>The running test, or null.</summary>
        public string? Current { get; internal set; }

        /// <summary>The sum of its test durations.</summary>
        public TimeSpan Duration { get; internal set; }

        /// <summary>When it started (run clock).</summary>
        public TimeSpan? Started { get; internal set; }

        /// <summary>When it finished (run clock).</summary>
        public TimeSpan? Finished { get; internal set; }

        /// <summary>Tests that ended.</summary>
        public int Done => Passed + Failed + Skipped + Inconclusive + NotRun;

        internal int Order { get; set; }

        internal TestContainerState Clone() => (TestContainerState)MemberwiseClone();
    }
}
