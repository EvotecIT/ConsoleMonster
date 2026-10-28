using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>A live inventory filter built from a named input, shared state and an ordinary table.</summary>
    public static class LiveFilterApp {
        /// <summary>One synthetic host, used only to demonstrate local filtering.</summary>
        public sealed class Host {
            /// <summary>Creates a synthetic inventory record.</summary>
            /// <param name="name">The host name.</param>
            /// <param name="role">The host's role.</param>
            /// <param name="site">The example site.</param>
            /// <param name="status">A status understood by the existing status presenter.</param>
            public Host(string name, string role, string site, string status) {
                Name = name;
                Role = role;
                Site = site;
                Status = status;
            }

            /// <summary>The synthetic host name.</summary>
            public string Name { get; }

            /// <summary>The host's role.</summary>
            public string Role { get; }

            /// <summary>The example site.</summary>
            public string Site { get; }

            /// <summary>The status shown by the table's standard presenter.</summary>
            public string Status { get; }
        }

        private static readonly IReadOnlyList<Host> Inventory = Array.AsReadOnly(new[] {
            new Host("WEB01", "Web", "North", "Pass"),
            new Host("WEB02", "Web", "West", "Warn"),
            new Host("SQL01", "Database", "North", "Pass"),
            new Host("SQL02", "Database", "West", "Fail"),
            new Host("FS01", "Files", "North", "Pass"),
            new Host("JOB01", "Worker", "West", "Running")
        });

        /// <summary>The fixed example inventory; no services or remote machines are queried.</summary>
        public static IReadOnlyList<Host> Hosts => Inventory;

        /// <summary>Matches literal text against host, role, site or status, ignoring case.</summary>
        /// <param name="query">Text from the query input; empty text matches every host.</param>
        /// <returns>A stable array in inventory order.</returns>
        public static IReadOnlyList<Host> Filter(string query) {
            if (query == null) { throw new ArgumentNullException(nameof(query)); }
            return Inventory.Where(host =>
                host.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                host.Role.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                host.Site.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                host.Status.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        }

        /// <summary>Builds the stable input and the display rebuilt after each committed edit.</summary>
        /// <returns>The application layout, ready for <c>ConsoleApp.Run</c>.</returns>
        public static IWidget Build() {
            // <example>
            return new Rows(
                new TextBlock("[bold]Host inventory[/] [muted]· synthetic data[/]"),
                new TextInput("Query") {
                    Label = "Query",
                    Placeholder = "Host, role, site or status"
                },
                new AppBinding(state => Results(state.Get<string>("Query", string.Empty)))) { Gap = 1 };
            // </example>
        }

        /// <summary>Builds the matching count and table from one consistent query snapshot.</summary>
        /// <param name="query">Literal text to find across the example inventory.</param>
        /// <returns>The current matching count and table, or an empty-state explanation.</returns>
        public static IWidget Results(string query) {
            IReadOnlyList<Host> matches = Filter(query);
            TextBlock count = new TextBlock("[bold]" + matches.Count.ToString(CultureInfo.InvariantCulture) +
                "[/] of " + Inventory.Count.ToString(CultureInfo.InvariantCulture) + " hosts");
            Table table = Table.For(matches)
                .Column("Host", host => host.Name)
                .Column("Role", host => host.Role)
                .Column("Site", host => host.Site)
                .Column("Status", host => host.Status, column => column.Presenter = CellPresenters.StatusWord);
            table.HeaderSeparator = false;
            if (matches.Count == 0) {
                return new Rows(count, table, new TextBlock("[muted]No matching hosts. Backspace to broaden the query.[/]"));
            }
            return new Rows(count, table);
        }

        /// <summary>Runs the example with the existing live engine and returns the final query state.</summary>
        /// <param name="options">Optional terminal, input source, theme and capture settings.</param>
        /// <returns>The state after Escape, cancellation or completion of the supplied input.</returns>
        public static AppState Run(InteractiveOptions? options = null) {
            // <example>
            AppState state = new AppState();
            return ConsoleApp.Run(Build(), state, new ConsoleAppOptions { Interactive = options });
            // </example>
        }
    }
}
