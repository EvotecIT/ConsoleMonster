using System;
using System.Collections.Generic;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Hosting {
    /// <summary>The state of a hosted service or of an entry the application reports.</summary>
    public enum ServiceState {
        /// <summary>Not started yet.</summary>
        Waiting,

        /// <summary>Starting (its StartAsync runs).</summary>
        Starting,

        /// <summary>Started and running.</summary>
        Running,

        /// <summary>Stopping (its StopAsync runs).</summary>
        Stopping,

        /// <summary>Stopped by the host.</summary>
        Stopped,

        /// <summary>Its work finished on its own (a BackgroundService whose ExecuteAsync returned).</summary>
        Completed,

        /// <summary>Failed (an exception from StartAsync or ExecuteAsync).</summary>
        Failed
    }

    /// <summary>One line of the dashboard: a hosted service or an entry the application reports.</summary>
    public sealed class ServiceStatus {
        internal ServiceStatus(string name, ServiceState state, string? detail, DateTimeOffset since, bool hosted) {
            Name = name;
            State = state;
            Detail = detail;
            Since = since;
            IsHostedService = hosted;
        }

        /// <summary>The name.</summary>
        public string Name { get; }

        /// <summary>The state.</summary>
        public ServiceState State { get; }

        /// <summary>A detail (the failure message, what it is doing), or null.</summary>
        public string? Detail { get; }

        /// <summary>When the state last changed.</summary>
        public DateTimeOffset Since { get; }

        /// <summary>Whether it is a hosted service (tracked by the dashboard) rather than an entry the application reports.</summary>
        public bool IsHostedService { get; }

        /// <summary>The status kind the state is drawn with.</summary>
        public StatusKind Kind => ConsoleDashboardTracker.KindOf(State);
    }

    /// <summary>The dashboard as the application sees it: report entries, write to its log, read the statuses.</summary>
    public interface IConsoleDashboard {
        /// <summary>Whether the live dashboard is showing.</summary>
        bool IsShown { get; }

        /// <summary>Sets an entry's state (added when new), with an optional detail.</summary>
        /// <param name="name">The entry's name.</param>
        /// <param name="state">The state.</param>
        /// <param name="detail">A detail, or null.</param>
        void SetStatus(string name, ServiceState state, string? detail = null);

        /// <summary>Sets an entry's detail, keeping its state (adds it as running when new).</summary>
        /// <param name="name">The entry's name.</param>
        /// <param name="detail">The detail, or null to clear it.</param>
        void SetDetail(string name, string? detail);

        /// <summary>Writes a line to the dashboard's log region while it shows (ignored otherwise; use ILogger for logs that always land).</summary>
        /// <param name="message">The message.</param>
        /// <param name="level">info, warn, err, verbose, debug, pass or skip.</param>
        void Log(string message, string level = "info");

        /// <summary>The current statuses, in the order they were added.</summary>
        /// <returns>A copy of the statuses.</returns>
        IReadOnlyList<ServiceStatus> Snapshot();
    }

    /// <summary>Keeps the statuses of the hosted services and the application's entries. Thread-safe.</summary>
    internal sealed class ConsoleDashboardTracker : IConsoleDashboard {
        private readonly object _gate = new object();
        private readonly List<ServiceStatus> _items = new List<ServiceStatus>();
        private readonly Func<DateTimeOffset> _clock;

        internal ConsoleDashboardTracker(Func<DateTimeOffset>? clock = null) {
            _clock = clock ?? (() => DateTimeOffset.Now);
        }

        /// <summary>Raised after any change (the dashboard redraws).</summary>
        internal event Action? Changed;

        internal LiveSession? Session { get; set; }

        public bool IsShown => Session != null && Session.IsRunning;

        public void SetStatus(string name, ServiceState state, string? detail = null) => Set(name, state, detail, false, true);

        public void SetDetail(string name, string? detail) {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentException("The name is empty.", nameof(name));
            }
            lock (_gate) {
                int index = _items.FindIndex(item => string.Equals(item.Name, name, StringComparison.Ordinal));
                ServiceStatus? old = index >= 0 ? _items[index] : null;
                ServiceStatus updated = new ServiceStatus(name, old?.State ?? ServiceState.Running, detail, old?.Since ?? _clock(), old?.IsHostedService ?? false);
                if (index >= 0) {
                    _items[index] = updated;
                } else {
                    _items.Add(updated);
                }
            }
            Changed?.Invoke();
        }

        public void Log(string message, string level = "info") {
            LiveSession? session = Session;
            if (session != null && session.IsRunning) {
                session.Post(new LiveEvent("$log", "write", text: message ?? string.Empty, state: level));
            }
        }

        public IReadOnlyList<ServiceStatus> Snapshot() {
            lock (_gate) {
                return _items.ToArray();
            }
        }

        // A hosted service's state; the detail is kept unless a new one is given or the service failed or recovered.
        internal void SetHosted(string name, ServiceState state, string? detail = null) => Set(name, state, detail, true, detail != null || state == ServiceState.Running || state == ServiceState.Starting);

        private void Set(string name, ServiceState state, string? detail, bool hosted, bool replaceDetail) {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentException("The name is empty.", nameof(name));
            }
            lock (_gate) {
                int index = _items.FindIndex(item => string.Equals(item.Name, name, StringComparison.Ordinal));
                ServiceStatus? old = index >= 0 ? _items[index] : null;
                DateTimeOffset since = old != null && old.State == state ? old.Since : _clock();
                ServiceStatus updated = new ServiceStatus(name, state, replaceDetail ? detail : old?.Detail, since, hosted || (old?.IsHostedService ?? false));
                if (index >= 0) {
                    _items[index] = updated;
                } else {
                    _items.Add(updated);
                }
            }
            Changed?.Invoke();
        }

        internal static StatusKind KindOf(ServiceState state) {
            switch (state) {
                case ServiceState.Running:
                    return StatusKind.Running;
                case ServiceState.Completed:
                    return StatusKind.Pass;
                case ServiceState.Failed:
                    return StatusKind.Fail;
                case ServiceState.Stopped:
                    return StatusKind.Skip;
                default:
                    return StatusKind.Pending;
            }
        }
    }
}
