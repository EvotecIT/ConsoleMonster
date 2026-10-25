using System;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Live {
    /// <summary>Options for <see cref="ConsoleWatch.Run"/>.</summary>
    public sealed class WatchOptions {
        private TimeSpan _interval = TimeSpan.FromSeconds(2);

        /// <summary>The time between the end of one run and the start of the next (default 2 seconds, at least 50 ms).</summary>
        public TimeSpan PollInterval {
            get => _interval;
            set => _interval = value < MinimumInterval ? MinimumInterval : value;
        }

        /// <summary>The shortest interval (50 ms), so a watch never spins.</summary>
        public static readonly TimeSpan MinimumInterval = TimeSpan.FromMilliseconds(50);

        /// <summary>How many runs, or 0 (default) until cancelled.</summary>
        public int Count { get; set; }

        /// <summary>Whether cells that changed since the previous run are highlighted.</summary>
        public bool Differences { get; set; }

        /// <summary>What is being watched, shown in the heading (markup), or null.</summary>
        public string? Title { get; set; }

        /// <summary>
        /// Whether to show a live view (true), print each run (false), or decide from the terminal (null, default): a live
        /// view only on an interactive terminal.
        /// </summary>
        public bool? Live { get; set; }

        /// <summary>The live session's options for the interactive view (inline with the final frame kept, by default).</summary>
        public LiveSessionOptions? Session { get; set; }

        /// <summary>Where a printed run goes when the view is not live, or null for the current terminal.</summary>
        public Action<IWidget>? Print { get; set; }

        /// <summary>Waits between runs (for tests), or null for a real wait that ends early on cancellation.</summary>
        public Action<TimeSpan, CancellationToken>? Delay { get; set; }
    }

    /// <summary>One run's view with a status for the heading (for example "2 errors" when the run partly failed).</summary>
    public sealed class WatchFrame : IWidget {
        /// <summary>Creates a frame.</summary>
        /// <param name="content">The run's view.</param>
        /// <param name="status">The status, or null.</param>
        public WatchFrame(IWidget content, string? status) {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Status = status;
        }

        /// <summary>The run's view.</summary>
        public IWidget Content { get; }

        /// <summary>The status for the heading, or null.</summary>
        public string? Status { get; }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, Canvas.RenderContext context) => Content.Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Canvas.Region region, Canvas.RenderContext context) => Content.Render(region, context);
    }

    /// <summary>
    /// Runs a function again and again and shows its latest result as a live view (the way <c>watch</c> does): the heading
    /// counts the runs, changed cells can be highlighted, a failed run shows its error and the watch goes on. On a terminal
    /// that is not interactive (redirected output, CI) every run is printed instead. Watch-Console is the PowerShell face.
    /// </summary>
    public static class ConsoleWatch {
        /// <summary>Watches until cancelled or <see cref="WatchOptions.Count"/> runs have finished.</summary>
        /// <param name="produce">Builds the view of one run, given its number (1 for the first). It runs on the calling thread.</param>
        /// <param name="options">Options, or null for the defaults.</param>
        /// <param name="cancellationToken">Ends the watch; the final frame stays.</param>
        /// <returns>The number of runs.</returns>
        public static int Run(Func<int, IWidget> produce, WatchOptions? options = null, CancellationToken cancellationToken = default) {
            if (produce == null) {
                throw new ArgumentNullException(nameof(produce));
            }
            WatchOptions o = options ?? new WatchOptions();
            WatchView view = new WatchView { Title = o.Title, PollInterval = o.PollInterval, Differences = o.Differences };
            bool live = o.Live ?? Terminal.GetCurrentProfile().Mode == OutputMode.Interactive;
            if (!live) {
                return Loop(produce, o, view, null, cancellationToken);
            }
            LiveSessionOptions session = o.Session ?? new LiveSessionOptions { Mode = LiveMode.Inline, PrintFinalState = true };
            using (LiveSession running = new LiveSession(view, session)) {
                running.Start();
                using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, running.Cancellation)) {
                    try {
                        return Loop(produce, o, view, running, linked.Token);
                    } finally {
                        running.Stop();
                    }
                }
            }
        }

        private static int Loop(Func<int, IWidget> produce, WatchOptions o, WatchView view, LiveSession? session, CancellationToken token) {
            int runs = 0;
            while (!token.IsCancellationRequested && (o.Count <= 0 || runs < o.Count)) {
                runs++;
                IWidget content;
                string? status = null;
                try {
                    content = produce(runs);
                    if (content is WatchFrame frame) {
                        status = frame.Status;
                        content = frame.Content;
                    }
                } catch (OperationCanceledException) when (token.IsCancellationRequested) {
                    break;
                } catch (Exception ex) when (!(ex is ThreadAbortException) && !token.IsCancellationRequested) {
                    // A failed run shows its error; the next run may succeed.
                    content = new ExceptionView(ex) { ShowStack = false };
                    status = ex.GetType().Name;
                }
                if (token.IsCancellationRequested) {
                    break;
                }
                if (session != null) {
                    session.Post(new LiveEvent(view.WidgetId, "update", value: runs, text: status) { Payload = content });
                } else {
                    view.Update(content, runs, status);
                    if (o.Print != null) {
                        o.Print(view);
                    } else {
                        Terminal.Write(view);
                    }
                }
                if (o.Count > 0 && runs >= o.Count) {
                    break;
                }
                if (o.Delay != null) {
                    o.Delay(o.PollInterval, token);
                } else {
                    token.WaitHandle.WaitOne(o.PollInterval);
                }
            }
            return runs;
        }
    }
}
