using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ConsoleMonster.Extensions.Tests {
    public sealed class HostingOwnershipTests {
        private sealed class ExternalService : IHostedService, IDisposable, IAsyncDisposable {
            public int Disposals { get; private set; }
            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public void Dispose() => Disposals++;
            public ValueTask DisposeAsync() { Disposals++; return default; }
        }

        [Theory]
        [InlineData(ConsoleDashboardMode.Auto, false)]
        [InlineData(ConsoleDashboardMode.Auto, true)]
        [InlineData(ConsoleDashboardMode.Always, false)]
        [InlineData(ConsoleDashboardMode.Always, true)]
        [InlineData(ConsoleDashboardMode.Never, false)]
        [InlineData(ConsoleDashboardMode.Never, true)]
        public async Task Dashboard_PreservesApplicationOwnedServiceLifetime(ConsoleDashboardMode mode, bool asyncDispose) {
            ExternalService service = new ExternalService();
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton<IHostedService>(service);
            services.AddConsoleDashboard(options => options.Mode = mode);
            ServiceProvider provider = services.BuildServiceProvider();
            Assert.Equal(2, provider.GetServices<IHostedService>().Count());
            if (asyncDispose) {
                await provider.DisposeAsync();
            } else {
                provider.Dispose();
            }
            Assert.Equal(0, service.Disposals);
        }
    }
}
