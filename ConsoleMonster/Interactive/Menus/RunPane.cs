using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A run started from a menu, shown in the app's own frame: the same header bar (the breadcrumb gains "Running"), the
    /// same content panel (titled with the dashboard's title, with its subtitle, status and the clock as the caption; a run
    /// of any other widget is titled "Run" with the clock) and the same footer (ctrl+c stop). A dashboard's own header and
    /// footer are left out, so going from choosing to running changes only the content. Rendered by the live session's
    /// thread while the run lasts.
    /// </summary>
    internal sealed class RunPane : IPane {
        private readonly IWidget _body;
        private readonly Dashboard? _dashboard;
        private readonly PromptLabels _labels;

        public RunPane(IWidget dashboard, PromptLabels labels) {
            _dashboard = dashboard as Dashboard;
            _body = _dashboard != null ? _dashboard.Body : dashboard;
            _labels = labels;
        }

        // The dashboard's own title names the panel (the breadcrumb says "Running"); a plain widget run is just "Run".
        public string Title => _dashboard != null && !string.IsNullOrWhiteSpace(_dashboard.Title) ? Markup.Strip(_dashboard.Title) : _labels.Run;

        /// <summary>The run clock, set by the view before each frame.</summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>The mark between the caption's parts for the glyph tier ("·", or "|" in ASCII), set by the view before each frame.</summary>
        public string Separator { get; set; } = "·";

        // The caption carries what the dashboard's header would have shown: its subtitle and status, then the clock. The
        // status is read on every frame, so a status the work posts while it runs shows at once.
        public string? Caption {
            get {
                string clock = NumberFormat.Clock(Elapsed);
                if (_dashboard == null) {
                    return clock;
                }
                List<string> parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(_dashboard.Subtitle)) {
                    parts.Add(Markup.Strip(_dashboard.Subtitle!));
                }
                if (!string.IsNullOrWhiteSpace(_dashboard.Status)) {
                    parts.Add(Markup.Strip(_dashboard.Status!));
                }
                parts.Add(clock);
                return string.Join("  " + Separator + "  ", parts.ToArray());
            }
        }

        public bool IsAnimating => true;

        public bool IsDirty => false;

        public IReadOnlyList<string> Crumbs => new string[0];

        public IWidget Build(ViewFrame frame, bool focused) => _body;

        public void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            bindings.Add(new KeyBinding(_labels.KeyStop, () => { }, new KeyChord(ConsoleKey.C, KeyModifiers.Control)) { KeyText = "ctrl+c", Order = 10 });
        }

        public bool HandleUnbound(InputEvent input) => false;

        public bool Poll(TimeSpan now) => false;

        public bool Back() => true;
    }

    /// <summary>
    /// The live session's root during a menu run: it draws the whole app frame (header, sidebar, content, footer) around
    /// the run, and exposes the dashboard as its child so the session finds its live widgets.
    /// </summary>
    internal sealed class MenuRunView : IContainer {
        private readonly InteractiveView _app;
        private readonly RunPane _pane;
        private readonly IWidget _dashboard;

        public MenuRunView(InteractiveView app, RunPane pane, IWidget dashboard) {
            _app = app;
            _pane = pane;
            _dashboard = dashboard;
        }

        public IReadOnlyList<IWidget> Children => new[] { _dashboard };

        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? 24 : constraints.MaxHeight);

        public void Render(Region region, RenderContext context) {
            _pane.Elapsed = context.Elapsed;
            _pane.Separator = InteractiveGlyphs.For(context).Separator;
            ViewFrame frame = new ViewFrame(context, region.Width, region.Height, false, context.Elapsed);
            _app.BuildFrame(frame).Render(region, context);
        }
    }

    /// <summary>
    /// Runs work with a live dashboard inside an interactive view's own frame (a menu app or a full-screen form): the view
    /// shows a run pane while the session draws the view, on the same screen or capture, then the caller shows the result.
    /// </summary>
    internal static class RunHost {
        public static RunResult Run(InteractiveView view, IPromptSurface surface, ScreenSurface? screen, IInputSource input, RenderContext appContext, MenuContext context, IWidget dashboard, Func<RunContext, RunResult?> work, LiveSessionOptions? options, Action<RunPane> show, Action hide, string fallbackTitle) {
            LiveSessionOptions o = options ?? new LiveSessionOptions();
            o.PrintFinalState = false;
            o.Summary = null;
            if (o.Profile == null) {
                o.Profile = appContext.Profile.WithSize(surface.Width, surface.Height);
            }
            if (o.Theme == null) {
                o.Theme = appContext.Theme;
            }
            if (screen != null) {
                o.Output = new BorrowedOutput(screen.Output);
            } else {
                o.Output = new SurfaceOutput(surface, o.Profile);
                o.WindowSource = () => new[] { 0, 0, surface.Width, surface.Height };
            }
            WindowsConsoleInput? raw = input as WindowsConsoleInput;
            raw?.Suspend();
            RunResult result;
            RunPane pane = new RunPane(dashboard, view.Labels);
            show(pane);
            LiveSession session = new LiveSession(new MenuRunView(view, pane, dashboard), o);
            Func<RenderContext, IPromptSurface?>? previous = PromptHost.Current;
            PromptHost.Current = null;
            try {
                session.Start();
                try {
                    result = work(new RunContext(session, context)) ?? new RunResult(fallbackTitle);
                } catch (OperationCanceledException) {
                    result = new RunResult(fallbackTitle, StatusKind.Warn) { Summary = view.Labels.Cancelled };
                } catch (Exception ex) {
                    result = new RunResult(fallbackTitle, StatusKind.Fail) { Error = ex };
                } finally {
                    session.Stop();
                }
            } finally {
                hide();
                PromptHost.Current = previous;
                raw?.Resume();
            }
            // The run's time on the session clock: real time, or the simulated clock the options give.
            if (result.Elapsed == TimeSpan.Zero) {
                result.Elapsed = session.Elapsed;
            }
            return result;
        }
    }

    /// <summary>A live output that hands each frame to a prompt surface (a screen capture, or a borrowed app screen).</summary>
    internal sealed class SurfaceOutput : ITerminalOutput {
        private readonly IPromptSurface _surface;

        public SurfaceOutput(IPromptSurface surface, Capabilities.TerminalProfile profile) {
            _surface = surface;
            Profile = profile;
        }

        public Capabilities.TerminalProfile Profile { get; }

        public bool SupportsLiveFrames => true;

        public void WriteBlock(CellBuffer buffer) {
        }

        public void WriteLine(StyledLine line) {
        }

        public void WriteInline(StyledLine line) {
        }

        public void BeginLive() {
        }

        public void RenderFrame(CellBuffer frame) => _surface.Present(frame, null);

        public void Invalidate() {
        }

        public void EndLive() {
        }

        public void Dispose() {
        }
    }
}
