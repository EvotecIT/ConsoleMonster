using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Hosting;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ConsoleMonster.Extensions.Tests {
    /// <summary>The console dashboard: hosted service states, failures, reported entries, the view and when it shows.</summary>
    public sealed class HostingTests {
        private sealed class QuickWorker : BackgroundService {
            protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
        }

        private sealed class BrokenWorker : BackgroundService {
            public TaskCompletionSource<bool> Fail { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
                await Fail.Task;
                throw new InvalidOperationException("template folder missing");
            }
        }

        private sealed class LoopService : BackgroundService {
            protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
                try {
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                } catch (OperationCanceledException) {
                }
            }
        }

        private sealed class PlainService : IHostedService {
            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private static ServiceStatus Find(IConsoleDashboard dashboard, string name) => dashboard.Snapshot().Single(item => item.Name == name);

        internal static async Task<ServiceStatus> WaitFor(IConsoleDashboard dashboard, string name, ServiceState state) {
            for (int i = 0; i < 200; i++) {
                ServiceStatus status = Find(dashboard, name);
                if (status.State == state) {
                    return status;
                }
                await Task.Delay(10);
            }
            return Find(dashboard, name);
        }

        [Fact]
        public async Task Dashboard_TracksHostedServicesThroughTheirLife() {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
            builder.Services.Configure<HostOptions>(options => options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
            builder.Services.AddHostedService<QuickWorker>();
            BrokenWorker brokenWorker = new BrokenWorker();
            builder.Services.AddHostedService(_ => brokenWorker);
            builder.Services.AddHostedService<LoopService>();
            builder.Services.AddSingleton<IHostedService, PlainService>();
            builder.Services.AddConsoleDashboard(options => options.Mode = ConsoleDashboardMode.Never);
            using (IHost host = builder.Build()) {
                IConsoleDashboard dashboard = host.Services.GetRequiredService<IConsoleDashboard>();
                await host.StartAsync();
                brokenWorker.Fail.SetResult(true);
                Assert.Equal(ServiceState.Completed, (await WaitFor(dashboard, "Quick", ServiceState.Completed)).State);
                ServiceStatus broken = await WaitFor(dashboard, "Broken", ServiceState.Failed);
                Assert.Equal(ServiceState.Failed, broken.State);
                Assert.Contains("template folder missing", broken.Detail);
                Assert.Equal(ServiceState.Running, Find(dashboard, "Loop").State);
                Assert.Equal(ServiceState.Running, Find(dashboard, "Plain").State);
                Assert.False(dashboard.IsShown);
                dashboard.SetStatus("Import", ServiceState.Running, "file 3 of 10");
                await host.StopAsync();
                Assert.Equal(ServiceState.Stopped, Find(dashboard, "Loop").State);
                Assert.Equal(ServiceState.Completed, Find(dashboard, "Quick").State);
                Assert.Equal(ServiceState.Failed, Find(dashboard, "Broken").State);
                Assert.Equal(new[] { "Quick", "Broken", "Loop", "Plain", "Import" }, dashboard.Snapshot().Select(item => item.Name));
            }
        }

        private sealed class LifecycleService : IHostedLifecycleService, IDisposable {
            public List<string> Calls { get; } = new List<string>();

            public bool Disposed { get; private set; }

            public Task StartingAsync(CancellationToken cancellationToken) { Calls.Add("starting"); return Task.CompletedTask; }

            public Task StartAsync(CancellationToken cancellationToken) { Calls.Add("start"); return Task.CompletedTask; }

            public Task StartedAsync(CancellationToken cancellationToken) { Calls.Add("started"); return Task.CompletedTask; }

            public Task StoppingAsync(CancellationToken cancellationToken) { Calls.Add("stopping"); return Task.CompletedTask; }

            public Task StopAsync(CancellationToken cancellationToken) { Calls.Add("stop"); return Task.CompletedTask; }

            public Task StoppedAsync(CancellationToken cancellationToken) { Calls.Add("stopped"); return Task.CompletedTask; }

            public void Dispose() => Disposed = true;
        }

        [Fact]
        public async Task Dashboard_KeepsTheHostsBehaviour_LifecycleFailuresAndOwnership() {
            LifecycleService instance = new LifecycleService();
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
            builder.Services.AddSingleton<IHostedService>(instance);
            builder.Services.AddHostedService(provider => new QuickWorker());
            BrokenWorker brokenWorker = new BrokenWorker();
            builder.Services.AddHostedService(_ => brokenWorker);
            builder.Services.AddConsoleDashboard(options => options.Mode = ConsoleDashboardMode.Never);
            IHost host = builder.Build();
            IConsoleDashboard dashboard = host.Services.GetRequiredService<IConsoleDashboard>();
            // The default StopHost behaviour still sees the failing BackgroundService and stops the host.
            List<IHostedService> hosted = host.Services.GetServices<IHostedService>().ToList();
            Assert.Contains(hosted, service => service is BackgroundService);
            await host.StartAsync();
            // .NET 10 starts ExecuteAsync on the pool; observe completion before the failing worker stops the host.
            Assert.Equal(ServiceState.Completed, (await WaitFor(dashboard, "Quick", ServiceState.Completed)).State);
            brokenWorker.Fail.SetResult(true);
            IHostApplicationLifetime lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            Task stopped = Task.Delay(Timeout.Infinite, lifetime.ApplicationStopping);
            Task first = await Task.WhenAny(stopped, Task.Delay(5000));
            Assert.Same(stopped, first);
            await host.StopAsync();
            Assert.Equal(new[] { "starting", "start", "started", "stopping", "stop", "stopped" }, instance.Calls);
            Assert.Equal(ServiceState.Failed, Find(dashboard, "Broken").State);
            Assert.Equal(ServiceState.Completed, Find(dashboard, "Quick").State);
            host.Dispose();
            // An instance the application registered is not disposed by the dashboard (nor by the container).
            Assert.False(instance.Disposed);
        }

        [Fact]
        public void Dashboard_WhereItWillNotDraw_LeavesTheServicesAsRegistered() {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
            builder.Services.AddHostedService<QuickWorker>();
            builder.Services.AddConsoleDashboard(options => options.Mode = ConsoleDashboardMode.Auto);
            using (IHost host = builder.Build()) {
                // The test run's output is redirected, so Auto never draws.
                Assert.Contains(host.Services.GetServices<IHostedService>(), service => service is QuickWorker);
            }
        }

        [Fact]
        public void AddConsoleDashboard_StartsFirstAndIsAddedOnce() {
            ServiceCollection services = new ServiceCollection();
            services.AddHostedService<QuickWorker>();
            services.AddConsoleDashboard();
            services.AddConsoleDashboard();
            List<ServiceDescriptor> hosted = services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService)).ToList();
            Assert.Equal(2, hosted.Count);
            Assert.Equal("ConsoleDashboardService", hosted[0].ImplementationType!.Name);
        }

        [Fact]
        public void ServicesView_ShowsStateElapsedTimeAndDetail() {
            DateTimeOffset now = new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
            ConsoleDashboardTracker tracker = new ConsoleDashboardTracker(() => now.AddSeconds(-75));
            tracker.SetStatus("Queue", ServiceState.Running, "42 waiting");
            tracker.SetStatus("Reports", ServiceState.Failed, "InvalidOperationException: missing");
            ServicesView view = new ServicesView(tracker, () => now);
            RenderContext context = new RenderContext(TerminalProfile.Fixed(80, 5, ColorDepth.None, GlyphTier.Ascii), Theme.Dark);
            CellBuffer buffer = new CellBuffer(80, view.Measure(new Constraints(80, 5), context).Height);
            view.Render(new Region(buffer), context);
            string text = buffer.ToPlainText();
            Assert.Contains("Queue    running", text);
            Assert.Contains("1m 15s", text);
            Assert.Contains("42 waiting", text);
            Assert.Contains("Reports  failed", text);
        }

        [Theory]
        [InlineData(ConsoleDashboardMode.Auto, OutputMode.Interactive, true, true)]
        [InlineData(ConsoleDashboardMode.Auto, OutputMode.Interactive, false, false)]
        [InlineData(ConsoleDashboardMode.Auto, OutputMode.Stream, true, false)]
        [InlineData(ConsoleDashboardMode.Always, OutputMode.Interactive, false, true)]
        [InlineData(ConsoleDashboardMode.Always, OutputMode.Plain, true, false)]
        [InlineData(ConsoleDashboardMode.Never, OutputMode.Interactive, true, false)]
        public void ShouldShow_StaysOutOfTheWayWithoutAnInteractiveTerminal(ConsoleDashboardMode mode, OutputMode output, bool userInteractive, bool expected) {
            Assert.Equal(expected, ConsoleDashboardService.ShouldShow(mode, TerminalProfile.Fixed(80, 20, ColorDepth.None, GlyphTier.Ascii, output), userInteractive));
        }
    }
}
