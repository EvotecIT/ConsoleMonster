using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ConsoleMonster.Hosting {
    /// <summary>
    /// Reports a hosted service's state to the dashboard while forwarding every call to it unchanged: starting, running,
    /// stopping and stopped, and for a BackgroundService also when its ExecuteAsync finishes or throws. The host sees the
    /// same shape as before: a BackgroundService stays a BackgroundService (its ExecuteTask is watched as usual) and
    /// lifecycle callbacks reach the service.
    /// </summary>
    internal sealed class ServiceReporter {
        private readonly ConsoleDashboardTracker _tracker;
        private readonly string _name;
        private int _stopping;

        internal ServiceReporter(ConsoleDashboardTracker tracker, string name) {
            _tracker = tracker;
            _name = name;
            _tracker.SetHosted(name, ServiceState.Waiting);
        }

        internal async Task StartAsync(IHostedService inner, CancellationToken cancellationToken) {
            _tracker.SetHosted(_name, ServiceState.Starting);
            try {
                await inner.StartAsync(cancellationToken).ConfigureAwait(false);
            } catch (Exception ex) {
                _tracker.SetHosted(_name, ServiceState.Failed, Message(ex));
                throw;
            }
            if (inner is BackgroundService background && background.ExecuteTask != null) {
                Task execute = background.ExecuteTask;
                if (execute.IsCompleted) {
                    Finished(execute);
                } else {
                    _tracker.SetHosted(_name, ServiceState.Running);
                    _ = execute.ContinueWith(Finished, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
                return;
            }
            _tracker.SetHosted(_name, ServiceState.Running);
        }

        internal async Task StopAsync(IHostedService inner, CancellationToken cancellationToken) {
            Interlocked.Exchange(ref _stopping, 1);
            if (!Finished()) {
                _tracker.SetHosted(_name, ServiceState.Stopping);
            }
            try {
                await inner.StopAsync(cancellationToken).ConfigureAwait(false);
            } catch (Exception ex) {
                _tracker.SetHosted(_name, ServiceState.Failed, Message(ex));
                throw;
            }
            if (inner is BackgroundService background && background.ExecuteTask != null && background.ExecuteTask.IsFaulted) {
                // Faulted while stopping: BackgroundService.StopAsync does not rethrow it.
                Finished(background.ExecuteTask);
            }
            // Read again: a failure reported meanwhile stays.
            if (!Finished()) {
                _tracker.SetHosted(_name, ServiceState.Stopped);
            }
        }

        private void Finished(Task execute) {
            if (execute.IsFaulted) {
                Exception? error = execute.Exception?.GetBaseException();
                _tracker.SetHosted(_name, ServiceState.Failed, error == null ? null : Message(error));
            } else if (Volatile.Read(ref _stopping) == 0) {
                // Returned (or was cancelled) on its own, before the host asked it to stop.
                _tracker.SetHosted(_name, execute.IsCanceled ? ServiceState.Stopped : ServiceState.Completed);
            }
        }

        private bool Finished() {
            foreach (ServiceStatus status in _tracker.Snapshot()) {
                if (string.Equals(status.Name, _name, StringComparison.Ordinal)) {
                    return status.State == ServiceState.Completed || status.State == ServiceState.Failed;
                }
            }
            return false;
        }

        private static string Message(Exception error) => error.GetType().Name + ": " + error.Message.Replace("\r\n", " ").Replace('\n', ' ');

        // Disposes the wrapped service when the dashboard took it over from the container (it created it, or a factory
        // returned it); an instance the application registered stays the application's.
        internal static void Dispose(IHostedService inner, bool owned) {
            if (owned) {
                if (inner is IDisposable disposable) {
                    disposable.Dispose();
                } else if (inner is IAsyncDisposable) {
                    // Match the container's contract for an async-only service instead of silently losing cleanup.
                    throw new InvalidOperationException(inner.GetType() + " only implements IAsyncDisposable. Use DisposeAsync to dispose the container.");
                }
            }
        }

        internal static async ValueTask DisposeAsync(IHostedService inner, bool owned) {
            if (!owned) {
                return;
            }
            if (inner is IAsyncDisposable asyncDisposable) {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            } else {
                (inner as IDisposable)?.Dispose();
            }
        }

        internal static Task Lifecycle(IHostedService inner, Func<IHostedLifecycleService, Task> call) => inner is IHostedLifecycleService lifecycle ? call(lifecycle) : Task.CompletedTask;
    }

    /// <summary>A hosted service (not a BackgroundService) with its state reported to the dashboard.</summary>
    internal sealed class TrackedHostedService : IHostedService, IHostedLifecycleService, IDisposable, IAsyncDisposable {
        private readonly ServiceReporter _reporter;
        private readonly bool _owned;

        internal TrackedHostedService(IHostedService inner, ConsoleDashboardTracker tracker, string name, bool owned) {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _reporter = new ServiceReporter(tracker, name);
            _owned = owned;
        }

        internal IHostedService Inner { get; }

        public Task StartAsync(CancellationToken cancellationToken) => _reporter.StartAsync(Inner, cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => _reporter.StopAsync(Inner, cancellationToken);

        public Task StartingAsync(CancellationToken cancellationToken) => ServiceReporter.Lifecycle(Inner, service => service.StartingAsync(cancellationToken));

        public Task StartedAsync(CancellationToken cancellationToken) => ServiceReporter.Lifecycle(Inner, service => service.StartedAsync(cancellationToken));

        public Task StoppingAsync(CancellationToken cancellationToken) => ServiceReporter.Lifecycle(Inner, service => service.StoppingAsync(cancellationToken));

        public Task StoppedAsync(CancellationToken cancellationToken) => ServiceReporter.Lifecycle(Inner, service => service.StoppedAsync(cancellationToken));

        public void Dispose() => ServiceReporter.Dispose(Inner, _owned);

        public ValueTask DisposeAsync() => ServiceReporter.DisposeAsync(Inner, _owned);
    }

    /// <summary>
    /// A BackgroundService with its state reported to the dashboard. It stays a BackgroundService whose ExecuteTask is the
    /// wrapped service's, so the host logs its failure and applies BackgroundServiceExceptionBehavior as before.
    /// </summary>
    internal sealed class TrackedBackgroundService : BackgroundService, IHostedLifecycleService, IAsyncDisposable {
        private readonly ServiceReporter _reporter;
        private readonly bool _owned;

        internal TrackedBackgroundService(BackgroundService inner, ConsoleDashboardTracker tracker, string name, bool owned) {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _reporter = new ServiceReporter(tracker, name);
            _owned = owned;
        }

        internal BackgroundService Inner { get; }

        public override Task? ExecuteTask => Inner.ExecuteTask;

        public override Task StartAsync(CancellationToken cancellationToken) => _reporter.StartAsync(Inner, cancellationToken);

        public override Task StopAsync(CancellationToken cancellationToken) => _reporter.StopAsync(Inner, cancellationToken);

        // Never called: StartAsync starts the wrapped service, which runs its own ExecuteAsync.
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Inner.ExecuteTask ?? Task.CompletedTask;

        public Task StartingAsync(CancellationToken cancellationToken) => ServiceReporter.Lifecycle(Inner, service => service.StartingAsync(cancellationToken));

        public Task StartedAsync(CancellationToken cancellationToken) => ServiceReporter.Lifecycle(Inner, service => service.StartedAsync(cancellationToken));

        public Task StoppingAsync(CancellationToken cancellationToken) => ServiceReporter.Lifecycle(Inner, service => service.StoppingAsync(cancellationToken));

        public Task StoppedAsync(CancellationToken cancellationToken) => ServiceReporter.Lifecycle(Inner, service => service.StoppedAsync(cancellationToken));

        public override void Dispose() {
            ServiceReporter.Dispose(Inner, _owned);
            base.Dispose();
        }

        public ValueTask DisposeAsync() {
            base.Dispose();
            return ServiceReporter.DisposeAsync(Inner, _owned);
        }
    }
}
