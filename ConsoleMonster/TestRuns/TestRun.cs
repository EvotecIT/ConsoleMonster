using System;
using System.Collections.Generic;
using System.Diagnostics;
using ConsoleMonster.Styling;

namespace ConsoleMonster.TestRuns {
    /// <summary>
    /// The state of a test run, fed with events from a test framework adapter (discovery, container start, test start, test
    /// end, container end, run end) on any thread, and read by <see cref="TestRunView"/> and <see cref="TestRunSummary"/>
    /// through consistent snapshots. It knows nothing about a specific framework.
    /// </summary>
    public sealed class TestRun {
        private readonly object _gate = new object();
        private readonly Func<TimeSpan> _clock;
        private readonly List<TestContainerState> _containers = new List<TestContainerState>();
        private readonly Dictionary<string, TestContainerState> _byKey = new Dictionary<string, TestContainerState>(StringComparer.OrdinalIgnoreCase);
        private readonly List<TestRecord> _failures = new List<TestRecord>();
        private readonly List<TestRecord> _slowest = new List<TestRecord>();
        private readonly List<double> _durations = new List<double>();
        private int? _total;
        private int _passed;
        private int _failed;
        private int _skipped;
        private int _inconclusive;
        private int _notRun;
        private string? _currentContainer;
        private IReadOnlyList<string>? _currentBlocks;
        private string? _currentName;
        private TimeSpan? _finished;

        /// <summary>Creates a run.</summary>
        /// <param name="clock">The run clock (elapsed time since the run started); null uses a stopwatch started now.</param>
        public TestRun(Func<TimeSpan>? clock = null) {
            if (clock == null) {
                Stopwatch watch = Stopwatch.StartNew();
                clock = () => watch.Elapsed;
            }
            _clock = clock;
        }

        /// <summary>How many slowest tests snapshots keep (default 5).</summary>
        public int SlowestCount { get; set; } = 5;

        /// <summary>The run clock.</summary>
        public TimeSpan Elapsed => _clock();

        /// <summary>Discovery finished: the number of tests that will run.</summary>
        /// <param name="total">The planned test count.</param>
        public void Discovered(int total) {
            lock (_gate) {
                _total = Math.Max(0, total);
            }
        }

        /// <summary>A container is known before it runs (a test file found up front, or discovered): it shows as queued.</summary>
        /// <param name="key">A unique key (the file path).</param>
        /// <param name="name">The display name (the file name).</param>
        /// <param name="file">The full path, or null.</param>
        /// <param name="tests">The tests it will run, or 0 when not known yet.</param>
        public void ContainerQueued(string key, string name, string? file, int tests = 0) {
            lock (_gate) {
                TestContainerState container = Container(key, name);
                container.File ??= file;
                if (tests > 0 && container.Status == StatusKind.Pending) {
                    container.Tests = tests;
                }
            }
        }

        /// <summary>A container (a test file or script block) starts.</summary>
        /// <param name="key">A unique key (the file path).</param>
        /// <param name="name">The display name (the file name).</param>
        /// <param name="file">The full path, or null.</param>
        /// <param name="tests">The tests it will run, or 0 when unknown.</param>
        public void ContainerStarted(string key, string name, string? file, int tests) {
            lock (_gate) {
                TestContainerState container = Container(key, name);
                container.File = file;
                container.Tests = Math.Max(0, tests);
                container.Status = StatusKind.Running;
                container.Started = _clock();
                _currentContainer = key;
            }
        }

        /// <summary>A test starts.</summary>
        /// <param name="container">The container key.</param>
        /// <param name="blocks">The enclosing blocks, outermost first.</param>
        /// <param name="name">The test name.</param>
        public void TestStarted(string container, IReadOnlyList<string>? blocks, string name) {
            lock (_gate) {
                Container(container, null).Current = name;
                _currentContainer = container;
                _currentBlocks = blocks ?? new string[0];
                _currentName = name;
            }
        }

        /// <summary>
        /// A test ended (or, with <see cref="TestRecord.IsBlockFailure"/>, a block failed outside a test). Failures with a
        /// message are listed; a failed test without a message (it failed because its block did) is only counted.
        /// </summary>
        /// <param name="record">The record.</param>
        public void TestFinished(TestRecord record) {
            if (record == null) {
                throw new ArgumentNullException(nameof(record));
            }
            lock (_gate) {
                TestContainerState container = Container(record.Container, null);
                if (record.IsBlockFailure) {
                    container.BlockFailures++;
                    _failures.Add(record);
                    return;
                }
                switch (record.Status) {
                    case StatusKind.Pass:
                        _passed++;
                        container.Passed++;
                        break;
                    case StatusKind.Fail:
                        _failed++;
                        container.Failed++;
                        if (!string.IsNullOrEmpty(record.Message)) {
                            _failures.Add(record);
                        }
                        break;
                    case StatusKind.Skip:
                        _skipped++;
                        container.Skipped++;
                        break;
                    case StatusKind.Warn:
                        _inconclusive++;
                        container.Inconclusive++;
                        break;
                    default:
                        _notRun++;
                        container.NotRun++;
                        break;
                }
                if (record.Status == StatusKind.Pass || record.Status == StatusKind.Fail || record.Status == StatusKind.Warn) {
                    container.Duration += record.Duration;
                    _durations.Add(record.Duration.TotalMilliseconds);
                    AddSlowest(record);
                }
                if (string.Equals(container.Current, record.Name, StringComparison.Ordinal)) {
                    container.Current = null;
                }
            }
        }

        /// <summary>A container ended.</summary>
        /// <param name="key">The container key.</param>
        public void ContainerFinished(string key) {
            lock (_gate) {
                TestContainerState container = Container(key, null);
                container.Finished = _clock();
                container.Current = null;
                container.Status = container.Failed > 0 || container.BlockFailures > 0 ? StatusKind.Fail
                    : container.Inconclusive > 0 ? StatusKind.Warn
                    : container.Passed == 0 && (container.Skipped > 0 || container.NotRun > 0) ? StatusKind.Skip
                    : StatusKind.Pass;
                if (string.Equals(_currentContainer, key, StringComparison.OrdinalIgnoreCase)) {
                    _currentName = null;
                    _currentBlocks = null;
                }
            }
        }

        /// <summary>The run ended; the clock stops.</summary>
        public void Finished() {
            lock (_gate) {
                _finished ??= _clock();
                _currentName = null;
                _currentBlocks = null;
            }
        }

        /// <summary>A consistent copy of the state, for rendering on another thread.</summary>
        /// <returns>The snapshot.</returns>
        public TestRunSnapshot Snapshot() {
            lock (_gate) {
                List<TestContainerState> containers = new List<TestContainerState>(_containers.Count);
                foreach (TestContainerState container in _containers) {
                    containers.Add(container.Clone());
                }
                List<TestRecord> slowest = new List<TestRecord>(_slowest);
                return new TestRunSnapshot(_total, _passed, _failed, _skipped, _inconclusive, _notRun, containers, new List<TestRecord>(_failures), slowest,
                    new List<double>(_durations), _currentName, _currentBlocks, _currentContainer, _finished ?? _clock(), _finished.HasValue);
            }
        }

        private TestContainerState Container(string key, string? name) {
            string k = key ?? string.Empty;
            if (!_byKey.TryGetValue(k, out TestContainerState? container)) {
                container = new TestContainerState(k, name ?? Text.DisplayPath.FileName(k)) { Order = _containers.Count };
                _byKey[k] = container;
                _containers.Add(container);
            } else if (name != null) {
                container.Name = name;
            }
            return container;
        }

        private void AddSlowest(TestRecord record) {
            int index = 0;
            while (index < _slowest.Count && _slowest[index].Duration >= record.Duration) {
                index++;
            }
            if (index >= SlowestCount) {
                return;
            }
            _slowest.Insert(index, record);
            if (_slowest.Count > SlowestCount) {
                _slowest.RemoveAt(_slowest.Count - 1);
            }
        }
    }
}
