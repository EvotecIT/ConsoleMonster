using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleMonster.Hosting {
    /// <summary>Adds the ConsoleMonster console dashboard to a generic host.</summary>
    public static class ConsoleDashboardServiceCollectionExtensions {
        /// <summary>
        /// Adds a live console dashboard that shows the hosted services registered so far (call it after
        /// AddHostedService), their state and elapsed lifetime through their own stop, completion or failure, the entries
        /// the application reports through <see cref="IConsoleDashboard"/> (with time in their current state), and the log
        /// region ConsoleMonster.Extensions.Logging writes into. It draws only
        /// on an interactive terminal; redirected output, CI, Windows services and daemons run the host unchanged.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Changes the options, or null for the defaults.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddConsoleDashboard(this IServiceCollection services, Action<ConsoleDashboardOptions>? configure = null) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }
            foreach (ServiceDescriptor existing in services) {
                if (existing.ServiceType == typeof(ConsoleDashboardTracker)) {
                    // Added already: the first call's options and tracking stay.
                    return services;
                }
            }
            ConsoleDashboardOptions options = new ConsoleDashboardOptions();
            configure?.Invoke(options);
            ConsoleDashboardTracker tracker = new ConsoleDashboardTracker();
            // Where the dashboard will not draw (redirected, CI, a service), the services run exactly as registered;
            // Never keeps tracking for IConsoleDashboard.Snapshot.
            bool track = options.Mode == ConsoleDashboardMode.Never || ConsoleDashboardService.ShouldShow(options.Mode, ConsoleMonster.Terminal.GetCurrentProfile(), Environment.UserInteractive);
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < services.Count; i++) {
                ServiceDescriptor descriptor = services[i];
                if (!track || descriptor.ServiceType != typeof(IHostedService) || IsKeyed(descriptor)) {
                    continue;
                }
                string name = NameOf(descriptor, options);
                // Two registrations of one type get their own lines.
                string unique = name;
                for (int n = 2; !names.Add(unique); n++) {
                    unique = name + " " + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                name = unique;
                services[i] = ServiceDescriptor.Singleton<IHostedService>(provider => Track(provider, descriptor, tracker, name));
            }
            services.AddSingleton(options);
            services.AddSingleton(tracker);
            services.AddSingleton<IConsoleDashboard>(tracker);
            // First to start and last to stop, so the view covers every other service.
            services.Insert(0, ServiceDescriptor.Singleton<IHostedService, ConsoleDashboardService>());
            return services;
        }

        private static bool IsKeyed(ServiceDescriptor descriptor) => descriptor.IsKeyedService;

        // The registered service, wrapped so its state is reported; a BackgroundService keeps its shape for the host.
        private static IHostedService Track(IServiceProvider provider, ServiceDescriptor descriptor, ConsoleDashboardTracker tracker, string name) {
            bool owned = descriptor.ImplementationInstance == null;
            IHostedService inner = Create(provider, descriptor);
            return inner is BackgroundService background
                ? new TrackedBackgroundService(background, tracker, name, owned)
                : new TrackedHostedService(inner, tracker, name, owned);
        }

        private static IHostedService Create(IServiceProvider provider, ServiceDescriptor descriptor) {
            if (descriptor.ImplementationInstance is IHostedService instance) {
                return instance;
            }
            if (descriptor.ImplementationFactory != null) {
                return (IHostedService)descriptor.ImplementationFactory(provider);
            }
            return (IHostedService)ActivatorUtilities.CreateInstance(provider, ImplementationType(descriptor));
        }

#if NET5_0_OR_GREATER
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        private static Type ImplementationType(ServiceDescriptor descriptor) => descriptor.ImplementationType ?? throw new InvalidOperationException("The hosted service registration has no implementation.");

        private static string NameOf(ServiceDescriptor descriptor, ConsoleDashboardOptions options) {
            Type? type = descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();
            if (type == null) {
                // A factory registration: the type is known only when it runs.
                Type[] arguments = descriptor.ImplementationFactory?.GetType().GetGenericArguments() ?? Type.EmptyTypes;
                type = arguments.Length == 2 && arguments[1] != typeof(object) ? arguments[1] : typeof(IHostedService);
            }
            if (options.ServiceName != null) {
                return options.ServiceName(type);
            }
            string name = type.Name;
            int tick = name.IndexOf('`');
            if (tick > 0) {
                name = name.Substring(0, tick);
            }
            foreach (string suffix in new[] { "HostedService", "BackgroundService", "Service", "Worker" }) {
                if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal)) {
                    return name.Substring(0, name.Length - suffix.Length);
                }
            }
            return name;
        }
    }
}
