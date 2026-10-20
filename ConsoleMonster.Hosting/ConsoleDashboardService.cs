using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using Microsoft.Extensions.Hosting;

namespace ConsoleMonster.Hosting {
    /// <summary>
    /// Shows the dashboard while the host runs: it starts first and stops last, draws the header, the services and the
    /// log as an inline live view, and leaves the final state in the scrollback. Without an interactive terminal (output
    /// redirected, CI, a Windows service or daemon) it draws nothing and the host runs as usual.
    /// </summary>
    internal sealed class ConsoleDashboardService : IHostedService, IDisposable {
        private readonly ConsoleDashboardTracker _tracker;
        private readonly ConsoleDashboardOptions _options;
        private readonly IHostEnvironment? _environment;
        private LiveSession? _session;
        private Timer? _ticker;

        public ConsoleDashboardService(ConsoleDashboardTracker tracker, ConsoleDashboardOptions options, IHostEnvironment? environment = null) {
            _tracker = tracker;
            _options = options;
            _environment = environment;
        }

        /// <summary>Whether the dashboard should draw here.</summary>
        internal static bool ShouldShow(ConsoleDashboardMode mode, TerminalProfile profile, bool userInteractive) {
            switch (mode) {
                case ConsoleDashboardMode.Never:
                    return false;
                case ConsoleDashboardMode.Always:
                    return profile.Mode == OutputMode.Interactive;
                default:
                    return profile.Mode == OutputMode.Interactive && userInteractive;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            if (!ShouldShow(_options.Mode, Terminal.GetCurrentProfile(), Environment.UserInteractive)) {
                return Task.CompletedTask;
            }
            string title = _options.Title ?? _environment?.ApplicationName ?? "Application";
            HeaderBar header = new HeaderBar(title) {
                Subtitle = _environment?.EnvironmentName,
                ShowClock = true,
                Right = "ctrl+c stops"
            };
            List<IWidget> parts = new List<IWidget> { header };
            if (_options.ShowServices) {
                parts.Add(new ServicesView(_tracker));
            }
            if (_options.ShowLog) {
                parts.Add(new LogRegion("log") { PreferredHeight = Math.Max(1, _options.LogHeight) });
            }
            _session = new LiveSession(new Rows(parts.ToArray()), new LiveSessionOptions {
                Mode = LiveMode.Inline,
                PrintFinalState = _options.KeepFinalState,
                // Ctrl+C belongs to the host: it stops the application, which stops the dashboard last.
                HandleCancelKey = false
            });
            _session.Start();
            _tracker.Session = _session;
            _tracker.Changed += Redraw;
            // Elapsed times and the clock move on even when nothing reports.
            _ticker = new Timer(_ => Redraw(), null, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            Close();
            return Task.CompletedTask;
        }

        public void Dispose() => Close();

        private void Redraw() => _session?.Invalidate();

        private void Close() {
            _ticker?.Dispose();
            _ticker = null;
            _tracker.Changed -= Redraw;
            LiveSession? session = _session;
            _session = null;
            if (session != null) {
                session.Invalidate();
                session.Stop();
                session.Dispose();
            }
            _tracker.Session = null;
        }
    }
}
