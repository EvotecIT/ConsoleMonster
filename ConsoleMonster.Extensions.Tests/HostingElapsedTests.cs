using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Hosting;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ConsoleMonster.Extensions.Tests {
    /// <summary>Service lifetimes end at their own outcome, independently of state age and host shutdown order.</summary>
    public sealed class HostingElapsedTests {
        private sealed class Clock {
            internal DateTimeOffset WallTime { get; set; } = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
            internal long Timestamp { get; private set; }

            internal void Advance(int seconds) {
                Timestamp += seconds * Stopwatch.Frequency;
                WallTime = WallTime.AddSeconds(seconds);
            }
        }

        private sealed class Service : IHostedService {
            internal Func<Task> Start { get; set; } = () => Task.CompletedTask;
            internal Func<Task> Stop { get; set; } = () => Task.CompletedTask;
            public Task StartAsync(CancellationToken cancellationToken) => Start();
            public Task StopAsync(CancellationToken cancellationToken) => Stop();
        }

        private sealed class Worker : BackgroundService {
            // The test observes the terminal state before advancing its controlled clock.
            internal TaskCompletionSource<bool> Work { get; } = new TaskCompletionSource<bool>();
            protected override Task ExecuteAsync(CancellationToken stoppingToken) => Work.Task;
        }

        private static ConsoleDashboardTracker Tracker(Clock clock) => new ConsoleDashboardTracker(() => clock.WallTime, () => clock.Timestamp);

        private static string Render(IConsoleDashboard tracker, Clock clock) {
            ServicesView view = new ServicesView(tracker, () => clock.WallTime);
            RenderContext context = new RenderContext(TerminalProfile.Fixed(120, 10, ColorDepth.None, GlyphTier.Ascii), Theme.Dark);
            CellBuffer buffer = new CellBuffer(120, view.Measure(new Constraints(120, 10), context).Height);
            view.Render(new Region(buffer), context);
            return buffer.ToPlainText();
        }

        private static string Row(string frame, string name) => frame.Split('\n').Single(line => line.Contains(name));

        [Fact]
        public async Task HostedElapsed_UsesEachServiceStartAndFreezesAtItsOwnFailureOrStop() {
            Clock clock = new Clock();
            ConsoleDashboardTracker tracker = Tracker(clock);
            Service queue = new Service();
            TaskCompletionSource<bool> stopped = new TaskCompletionSource<bool>();
            queue.Stop = () => stopped.Task;
            TrackedHostedService trackedQueue = new TrackedHostedService(queue, tracker, "Queue", false);
            using (Worker worker = new Worker()) {
                TrackedBackgroundService trackedWorker = new TrackedBackgroundService(worker, tracker, "ReportBuilder", false);
                await trackedQueue.StartAsync(CancellationToken.None);
                clock.Advance(3);
                await trackedWorker.StartAsync(CancellationToken.None);
                clock.Advance(4);
                worker.Work.SetException(new InvalidOperationException("template missing"));
                Assert.Equal(ServiceState.Failed, tracker.Snapshot().Single(item => item.Name == "ReportBuilder").State);
                DateTimeOffset failedSince = tracker.Snapshot().Single(item => item.Name == "ReportBuilder").Since;
                clock.Advance(3);
                Task stopping = trackedQueue.StopAsync(CancellationToken.None);
                Assert.Equal(ServiceState.Stopping, tracker.Snapshot().Single(item => item.Name == "Queue").State);
                clock.Advance(1);
                stopped.SetResult(true);
                await stopping;
                await trackedWorker.StopAsync(CancellationToken.None);
                clock.Advance(19);
                // A second failure observation during host shutdown must not replace its original end timestamp.
                await trackedWorker.StopAsync(CancellationToken.None);
                tracker.SetDetail("Queue", "drained");
                string frame = Render(tracker, clock);
                Assert.Contains("stopped", Row(frame, "Queue"));
                Assert.Contains("11s", Row(frame, "Queue"));
                Assert.Contains("drained", Row(frame, "Queue"));
                Assert.Contains("failed", Row(frame, "ReportBuilder"));
                Assert.Contains("4.0s", Row(frame, "ReportBuilder"));
                Assert.Contains("template missing", Row(frame, "ReportBuilder"));
                Assert.Equal(TimeSpan.FromSeconds(11), tracker.Snapshot().Single(item => item.Name == "Queue").HostedElapsed);
                Assert.Equal(TimeSpan.FromSeconds(4), tracker.Snapshot().Single(item => item.Name == "ReportBuilder").HostedElapsed);
                Assert.Equal(failedSince, tracker.Snapshot().Single(item => item.Name == "ReportBuilder").Since);
            }
        }

        [Fact]
        public async Task HostedElapsed_IncludesStartingAndStoppingWithoutFollowingWallClockChanges() {
            Clock clock = new Clock();
            ConsoleDashboardTracker tracker = Tracker(clock);
            TaskCompletionSource<bool> started = new TaskCompletionSource<bool>();
            TaskCompletionSource<bool> stopped = new TaskCompletionSource<bool>();
            Service service = new Service { Start = () => started.Task, Stop = () => stopped.Task };
            TrackedHostedService tracked = new TrackedHostedService(service, tracker, "Queue", false);
            clock.Advance(40);
            Assert.Contains("0ms", Row(Render(tracker, clock), "Queue"));
            Task starting = tracked.StartAsync(CancellationToken.None);
            clock.Advance(2);
            Assert.Contains("starting", Row(Render(tracker, clock), "Queue"));
            Assert.Contains("2.0s", Row(Render(tracker, clock), "Queue"));
            Assert.Equal(TimeSpan.FromSeconds(2), tracker.Snapshot().Single().HostedElapsed);
            started.SetResult(true);
            await starting;
            clock.Advance(3);
            clock.WallTime = clock.WallTime.AddHours(-2);
            Assert.Contains("running", Row(Render(tracker, clock), "Queue"));
            Assert.Contains("5.0s", Row(Render(tracker, clock), "Queue"));
            Assert.Equal(TimeSpan.FromSeconds(5), tracker.Snapshot().Single().HostedElapsed);
            Task stopping = tracked.StopAsync(CancellationToken.None);
            clock.Advance(1);
            Assert.Contains("stopping", Row(Render(tracker, clock), "Queue"));
            Assert.Contains("6.0s", Row(Render(tracker, clock), "Queue"));
            Assert.Equal(TimeSpan.FromSeconds(6), tracker.Snapshot().Single().HostedElapsed);
            clock.Advance(1);
            stopped.SetResult(true);
            await stopping;
            clock.Advance(20);
            clock.WallTime = clock.WallTime.AddDays(2);
            Assert.Contains("stopped", Row(Render(tracker, clock), "Queue"));
            Assert.Contains("7.0s", Row(Render(tracker, clock), "Queue"));
            Assert.Equal(TimeSpan.FromSeconds(7), tracker.Snapshot().Single().HostedElapsed);
        }

        [Theory]
        [InlineData("completed", ServiceState.Completed)]
        [InlineData("cancelled", ServiceState.Stopped)]
        [InlineData("start-failed", ServiceState.Failed)]
        [InlineData("stop-failed", ServiceState.Failed)]
        public async Task HostedElapsed_FreezesForEveryTerminalOutcomeAndSurvivesLaterShutdown(string outcome, ServiceState expected) {
            Clock clock = new Clock();
            ConsoleDashboardTracker tracker = Tracker(clock);
            if (outcome == "start-failed" || outcome == "stop-failed") {
                Service service = new Service();
                Func<Task> fail = () => {
                    clock.Advance(4);
                    throw new InvalidOperationException("service failure");
                };
                TrackedHostedService tracked = new TrackedHostedService(service, tracker, "Worker", false);
                if (outcome == "start-failed") {
                    service.Start = fail;
                    await Assert.ThrowsAsync<InvalidOperationException>(() => tracked.StartAsync(CancellationToken.None));
                } else {
                    await tracked.StartAsync(CancellationToken.None);
                    service.Stop = fail;
                    await Assert.ThrowsAsync<InvalidOperationException>(() => tracked.StopAsync(CancellationToken.None));
                }
                clock.Advance(25);
                service.Stop = () => Task.CompletedTask;
                await tracked.StopAsync(CancellationToken.None);
            } else {
                using (Worker service = new Worker()) {
                    TrackedBackgroundService tracked = new TrackedBackgroundService(service, tracker, "Worker", false);
                    await tracked.StartAsync(CancellationToken.None);
                    clock.Advance(4);
                    if (outcome == "completed") {
                        service.Work.SetResult(true);
                    } else {
                        service.Work.SetCanceled();
                    }
                    // Hosting 10 runs ExecuteAsync on a background task, including its synchronous prefix.
                    await HostingTests.WaitFor(tracker, "Worker", expected);
                    Assert.Equal(expected, tracker.Snapshot().Single().State);
                    clock.Advance(25);
                    await tracked.StopAsync(CancellationToken.None);
                }
            }
            Assert.Equal(expected, tracker.Snapshot().Single().State);
            Assert.Contains("4.0s", Render(tracker, clock));
            Assert.Equal(TimeSpan.FromSeconds(4), tracker.Snapshot().Single().HostedElapsed);
        }
    }
}
