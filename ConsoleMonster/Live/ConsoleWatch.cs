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

        /// <summary>How many runs; zero (default) follows until cancelled interactively, or prints one snapshot without a user.</summary>
        public int Count { get; set; }

        // PowerShell's process flag is independent of whether its standard handles are attached to a TTY.
        internal bool NonInteractive { get; set; }

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
    /// that is not interactive (redirected output, CI) the current view is printed once, or Count times when supplied.
    /// Watch-Console is the PowerShell face.
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
            Output.ITerminalTarget target = o.Session?.Target ?? Terminal.Target;
            TerminalProfile profile = o.Session?.Profile ?? Terminal.GetTargetProfile(target);
            if (o.Session?.Culture != null) profile = profile.WithCulture(o.Session.Culture);
            Styling.ITheme theme = o.Session?.Theme ?? target.Theme ?? Terminal.Theme;
            bool interactive = profile.IsLive && !profile.IsOutputRedirected && !profile.IsContinuousIntegration
                && !o.NonInteractive && !Interactive.InteractiveOptions.Default.IsNonInteractive
                && (target is Output.RecordingTarget || !Console.IsInputRedirected);
            bool live = interactive && (o.Live ?? true);
            int count = !interactive && o.Count <= 0 ? 1 : o.Count;
            Action<IWidget> print = o.Print ?? (widget => {
                using (Terminal.Use(target)) {
                    if (o.Session?.Output is Rendering.ITerminalOutput output) {
                        foreach (Canvas.CellBuffer block in Terminal.RenderChunks(widget, profile.Width, Terminal.CreateContext(profile, theme))) output.WriteBlock(block);
                    } else { Terminal.Write(widget, profile, target.Writer, theme); }
                }
            });
            if (!live) {
                return Loop(produce, o, view, null, count, print, cancellationToken);
            }
            LiveSessionOptions session = o.Session ?? new LiveSessionOptions { Mode = LiveMode.Inline, PrintFinalState = true };
            using (LiveSession running = new LiveSession(view, session)) {
                running.Start();
                using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, running.Cancellation)) {
                    try {
                        return Loop(produce, o, view, running, count, print, linked.Token);
                    } finally {
                        running.Stop();
                    }
                }
            }
        }

        private static int Loop(Func<int, IWidget> produce, WatchOptions o, WatchView view, LiveSession? session, int count, Action<IWidget> print, CancellationToken token) {
            int runs = 0;
            while (!token.IsCancellationRequested && (count <= 0 || runs < count)) {
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
                    if (!Delay(session, TimeSpan.Zero, token)) { break; }
                } else {
                    view.Update(content, runs, status);
                    print(view);
                }
                if (count > 0 && runs >= count) {
                    break;
                }
                if (o.Delay != null) {
                    o.Delay(o.PollInterval, token);
                } else if (session != null) {
                    if (!Delay(session, o.PollInterval, token)) { break; }
                } else {
                    token.WaitHandle.WaitOne(o.PollInterval);
                }
            }
            return runs;
        }

        // Cancellation ends a watch by returning its run count, including when a checkpoint is waiting for the dispatcher.
        private static bool Delay(LiveSession session, TimeSpan interval, CancellationToken token) {
            try { session.DelayCore(interval, token); return true; }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return false; }
        }
    }
}
