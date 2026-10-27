using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;

namespace ConsoleMonster.Live {
    public static partial class ConsoleRun {
        /// <summary>Configures a fresh run request before executing the existing item-run pipeline.</summary>
        /// <typeparam name="TItem">The item type.</typeparam><typeparam name="TResult">The result type.</typeparam>
        /// <param name="items">The input items.</param><param name="body">The work.</param><param name="configure">Run settings.</param><param name="cancellationToken">Cancellation.</param><returns>The ordered run results.</returns>
        public static Task<IReadOnlyList<RunResult<TItem, TResult>>> ForEachAsync<TItem, TResult>(IEnumerable<TItem> items, Func<TItem, CancellationToken, Task<TResult>> body, Action<RunOptions> configure, CancellationToken cancellationToken = default) =>
            ForEachAsync(items, body, OptionConfiguration.Create(configure), cancellationToken);
    }
}

namespace ConsoleMonster.Interactive {
    public sealed partial class Menu {
        /// <summary>Runs this menu with freshly configured options.</summary><param name="configure">Menu settings.</param><param name="cancellationToken">Cancellation.</param>
        public void Run(Action<MenuOptions> configure, CancellationToken cancellationToken = default) => Run(OptionConfiguration.Create(configure), cancellationToken);
        /// <summary>Runs this menu asynchronously with freshly configured options.</summary><param name="configure">Menu settings.</param><param name="cancellationToken">Cancellation.</param><returns>The menu task.</returns>
        public Task RunAsync(Action<MenuOptions> configure, CancellationToken cancellationToken = default) => RunAsync(OptionConfiguration.Create(configure), cancellationToken);
    }
    public sealed partial class Grid {
        /// <summary>Shows this grid with freshly configured options.</summary><param name="configure">Grid settings.</param><param name="cancellationToken">Cancellation.</param><returns>The grid result.</returns>
        public GridResult Show(Action<GridOptions> configure, CancellationToken cancellationToken = default) => Show(OptionConfiguration.Create(configure), cancellationToken);
    }
    public sealed partial class Form {
        /// <summary>Shows this form with freshly configured options.</summary><param name="configure">Form settings.</param><param name="cancellationToken">Cancellation.</param><returns>The answers.</returns>
        public FormResult Show(Action<FormOptions> configure, CancellationToken cancellationToken = default) => Show(OptionConfiguration.Create(configure), cancellationToken);
        /// <summary>Shows this form asynchronously with freshly configured options.</summary><param name="configure">Form settings.</param><param name="cancellationToken">Cancellation.</param><returns>The answers.</returns>
        public Task<FormResult> ShowAsync(Action<FormOptions> configure, CancellationToken cancellationToken = default) => ShowAsync(OptionConfiguration.Create(configure), cancellationToken);
    }
    public static partial class ConsoleApp {
        /// <summary>Runs an app with freshly configured options.</summary><param name="layout">The root.</param><param name="configure">App settings.</param><param name="state">Initial state.</param><param name="cancellationToken">Cancellation.</param><returns>The final state.</returns>
        public static AppState Run(IWidget layout, Action<ConsoleAppOptions> configure, AppState? state = null, CancellationToken cancellationToken = default) => Run(layout, state, OptionConfiguration.Create(configure), cancellationToken);
        /// <summary>Runs an app asynchronously with freshly configured options.</summary><param name="layout">The root.</param><param name="configure">App settings.</param><param name="state">Initial state.</param><param name="cancellationToken">Cancellation.</param><returns>The final state.</returns>
        public static Task<AppState> RunAsync(IWidget layout, Action<ConsoleAppOptions> configure, AppState? state = null, CancellationToken cancellationToken = default) => RunAsync(layout, state, OptionConfiguration.Create(configure), cancellationToken);
    }
}
