using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live {
    /// <summary>Options for <see cref="ConsoleRun.ForEachAsync{TItem, TResult}"/> and <see cref="RunMonitor"/>.</summary>
    public sealed class RunOptions {
        private int _throttleLimit = Math.Max(1, Environment.ProcessorCount);

        /// <summary>How many items run at the same time (default the processor count).</summary>
        public int ThrottleLimit {
            get => _throttleLimit;
            set => _throttleLimit = Math.Max(1, value);
        }

        /// <summary>The time one item may take, or null for no limit; an item past it is cancelled and counts as failed.</summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>A title above the live view (markup), or null.</summary>
        public string? Title { get; set; }

        /// <summary>The label of an item in the view, or null for its title (a name-like property, or its text).</summary>
        public Func<object?, string>? Label { get; set; }

        /// <summary>Whether to show the live view (true), print a line per item (false) or decide from the terminal (null, default).</summary>
        public bool? Interactive { get; set; }

        /// <summary>The live session's options (inline with the final frame kept, by default).</summary>
        public LiveSessionOptions? Session { get; set; }

        /// <summary>Where a printed line goes when the view is not live, or null for the current terminal.</summary>
        public Action<string>? Print { get; set; }
    }

    /// <summary>The outcome of one item of <see cref="ConsoleRun.ForEachAsync{TItem, TResult}"/>.</summary>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public sealed class RunResult<TItem, TResult> {
        internal RunResult(int index, TItem item, TResult value, Exception? error, TimeSpan duration, bool timedOut) {
            Index = index;
            Item = item;
            Value = value;
            Error = error;
            Duration = duration;
            TimedOut = timedOut;
        }

        /// <summary>The item's position in the input (0-based).</summary>
        public int Index { get; }

        /// <summary>The item.</summary>
        public TItem Item { get; }

        /// <summary>The result, or the default value when the item failed.</summary>
        public TResult Value { get; }

        /// <summary>The error, or null when the item succeeded.</summary>
        public Exception? Error { get; }

        /// <summary>How long the item took.</summary>
        public TimeSpan Duration { get; }

        /// <summary>Whether the item ran out of time.</summary>
        public bool TimedOut { get; }

        /// <summary>Whether the item succeeded.</summary>
        public bool Succeeded => Error == null;
    }

    /// <summary>
    /// Shows a run of many items while it happens: the live <see cref="RunView"/> on an interactive terminal, or one line per
    /// finished item (and a summary at the end) in redirected output and CI. Safe to call from any thread.
    /// <see cref="ConsoleRun.ForEachAsync{TItem, TResult}"/> and ForEach-ConsoleItem use it; other runners can too.
    /// </summary>
    public sealed class RunMonitor : IDisposable {
        private readonly RunOptions _options;
        private readonly LiveSession? _session;
        private readonly object _printGate = new object();
        private readonly IGlyphSet? _glyphs;
        private bool _completed;

        /// <summary>Starts showing a run.</summary>
        /// <param name="total">The number of items.</param>
        /// <param name="options">Options, or null for the defaults.</param>
        public RunMonitor(int total, RunOptions? options = null) {
            _options = options ?? new RunOptions();
            Tracker = new RunTracker(total);
            IsLive = _options.Interactive ?? Terminal.GetCurrentProfile().Mode == OutputMode.Interactive;
            if (IsLive) {
                _session = new LiveSession(new RunView(Tracker) { Title = _options.Title }, _options.Session ?? new LiveSessionOptions { Mode = LiveMode.Inline, PrintFinalState = true });
                _session.Start();
            } else {
                _glyphs = Terminal.CreateContext(Terminal.GetCurrentProfile()).Glyphs;
                if (!string.IsNullOrEmpty(_options.Title)) {
                    Print(Text.Markup.Strip(_options.Title!));
                }
            }
        }

        /// <summary>The run's state.</summary>
        public RunTracker Tracker { get; }

        /// <summary>Whether the live view is showing.</summary>
        public bool IsLive { get; }

        /// <summary>The live session, or null when the run is printed.</summary>
        public LiveSession? Session => _session;

        /// <summary>The label of an item: <see cref="RunOptions.Label"/>, otherwise its title.</summary>
        /// <param name="item">The item.</param>
        /// <returns>The label.</returns>
        public string LabelOf(object? item) {
            if (_options.Label != null) {
                try {
                    return _options.Label(item) ?? string.Empty;
                } catch (Exception) {
                    // A label that fails must not stop the run: the item's own title stands in.
                }
            }
            return item is string text ? text : item == null ? string.Empty : Values.ValueShaper.Title(item);
        }

        /// <summary>An item started.</summary>
        /// <param name="index">Its position (0-based).</param>
        /// <param name="label">Its label.</param>
        public void Started(int index, string label) => Tracker.Started(index, label);

        /// <summary>An item finished.</summary>
        /// <param name="index">Its position (0-based).</param>
        /// <param name="outcome">Pass, Fail, Warn or Skip.</param>
        /// <param name="label">Its label.</param>
        /// <param name="duration">How long it took.</param>
        /// <param name="message">The error message of a failed item, or null.</param>
        /// <param name="timedOut">Whether it ran out of time.</param>
        public void Finished(int index, StatusKind outcome, string label, TimeSpan duration, string? message = null, bool timedOut = false) {
            Tracker.Finished(index, outcome, label, message, timedOut);
            if (!IsLive) {
                string line = _glyphs!.StatusSymbol(outcome) + " " + label + " (" + NumberFormat.Duration(duration) + ")"
                    + (string.IsNullOrEmpty(message) ? string.Empty : ": " + message!.Replace("\r\n", " ").Replace('\n', ' '));
                Print(line);
            }
        }

        /// <summary>The run ended: items never started count as skipped; the live view keeps its final frame, printed runs end with a summary.</summary>
        public void Complete() {
            if (_completed) {
                return;
            }
            _completed = true;
            Tracker.Complete();
            if (_session != null) {
                _session.Stop();
                _session.Dispose();
                return;
            }
            RunSnapshot snapshot = Tracker.Snapshot();
            Localization.WidgetLabels labels = Localization.WidgetLabels.Current;
            List<string> parts = new List<string> {
                Localization.WidgetLabels.Format(labels.TestsPassedCount, snapshot.Passed.ToString(CultureInfo.InvariantCulture)),
                Localization.WidgetLabels.Format(labels.FailedCount, snapshot.Failed.ToString(CultureInfo.InvariantCulture))
            };
            if (snapshot.Warned > 0) {
                parts.Add(Localization.WidgetLabels.Format(labels.WarningCount, snapshot.Warned.ToString(CultureInfo.InvariantCulture)));
            }
            if (snapshot.Skipped > 0) {
                parts.Add(Localization.WidgetLabels.Format(labels.SkippedCount, snapshot.Skipped.ToString(CultureInfo.InvariantCulture)));
            }
            Print(Localization.WidgetLabels.Format(snapshot.Total == 1 ? labels.OneItem : labels.ManyItems, snapshot.Total.ToString(CultureInfo.InvariantCulture)) + ": " + string.Join(", ", parts.ToArray()) + " (" + NumberFormat.Duration(snapshot.Elapsed) + ")");
        }

        /// <inheritdoc />
        public void Dispose() => Complete();

        private void Print(string line) {
            lock (_printGate) {
                if (_options.Print != null) {
                    _options.Print(line);
                } else {
                    Terminal.Write(new TextBlock(Text.Markup.Escape(line)));
                }
            }
        }
    }

    /// <summary>Runs work over many items with a live view of the run (the C# side of ForEach-ConsoleItem).</summary>
    public static class ConsoleRun {
        /// <summary>
        /// Runs <paramref name="body"/> for every item, at most <see cref="RunOptions.ThrottleLimit"/> at a time, showing the run
        /// (summary line, item cells, running items, failures; one line per item when the output is not interactive). An
        /// item that throws, or runs past <see cref="RunOptions.Timeout"/>, counts as failed and the run goes on; cancelling
        /// stops starting new items, cancels the running ones and counts the rest as skipped.
        /// </summary>
        /// <typeparam name="TItem">The item type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="items">The items.</param>
        /// <param name="body">The work for one item; observe the token for timeouts and cancellation.</param>
        /// <param name="options">Options, or null for the defaults.</param>
        /// <param name="cancellationToken">Stops the run.</param>
        /// <returns>One result per item, in input order (items never started are left out).</returns>
        public static async Task<IReadOnlyList<RunResult<TItem, TResult>>> ForEachAsync<TItem, TResult>(IEnumerable<TItem> items, Func<TItem, CancellationToken, Task<TResult>> body, RunOptions? options = null, CancellationToken cancellationToken = default) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            if (body == null) {
                throw new ArgumentNullException(nameof(body));
            }
            RunOptions o = options ?? new RunOptions();
            List<TItem> list = items.ToList();
            if (list.Count == 0) {
                return new List<RunResult<TItem, TResult>>();
            }
            RunResult<TItem, TResult>?[] results = new RunResult<TItem, TResult>?[list.Count];
            using (RunMonitor monitor = new RunMonitor(list.Count, o))
            using (SemaphoreSlim gate = new SemaphoreSlim(o.ThrottleLimit)) {
                List<Task> running = new List<Task>(list.Count);
                for (int i = 0; i < list.Count; i++) {
                    try {
                        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                    } catch (OperationCanceledException) {
                        break;
                    }
                    int index = i;
                    running.Add(Task.Run(async () => {
                        try {
                            results[index] = await RunOne(list[index], index, body, o, monitor, cancellationToken).ConfigureAwait(false);
                        } finally {
                            gate.Release();
                        }
                    }));
                }
                await Task.WhenAll(running).ConfigureAwait(false);
            }
            return results.Where(result => result != null).Select(result => result!).ToList();
        }

        private static async Task<RunResult<TItem, TResult>> RunOne<TItem, TResult>(TItem item, int index, Func<TItem, CancellationToken, Task<TResult>> body, RunOptions options, RunMonitor monitor, CancellationToken cancellationToken) {
            string label = monitor.LabelOf(item);
            monitor.Started(index, label);
            DateTime started = DateTime.UtcNow;
            using (CancellationTokenSource itemToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) {
                if (options.Timeout.HasValue) {
                    itemToken.CancelAfter(options.Timeout.Value);
                }
                try {
                    TResult value = await body(item, itemToken.Token).ConfigureAwait(false);
                    TimeSpan duration = DateTime.UtcNow - started;
                    monitor.Finished(index, StatusKind.Pass, label, duration);
                    return new RunResult<TItem, TResult>(index, item, value, null, duration, false);
                } catch (Exception ex) {
                    TimeSpan duration = DateTime.UtcNow - started;
                    bool timedOut = ex is OperationCanceledException && !cancellationToken.IsCancellationRequested && itemToken.IsCancellationRequested;
                    if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested) {
                        monitor.Finished(index, StatusKind.Skip, label, duration);
                        return new RunResult<TItem, TResult>(index, item, default!, ex, duration, false);
                    }
                    string message = timedOut ? "timed out after " + NumberFormat.Duration(options.Timeout!.Value) : ex.Message;
                    monitor.Finished(index, StatusKind.Fail, label, duration, message, timedOut);
                    return new RunResult<TItem, TResult>(index, item, default!, timedOut ? new TimeoutException(message, ex) : ex, duration, timedOut);
                }
            }
        }
    }
}
