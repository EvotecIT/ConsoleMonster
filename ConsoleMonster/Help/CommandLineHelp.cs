using System.Collections.Generic;

namespace ConsoleMonster.Help {
    /// <summary>Compact command-line help metadata, independent of the host's argument parser.</summary>
    public sealed class CommandLineHelp {
        /// <summary>The concise usage, including the executable name and positional placeholders.</summary>
        public string Usage { get; set; } = string.Empty;

        /// <summary>Commands in display order, optionally grouped or collapsed by the host.</summary>
        public IList<HelpCommand> Commands { get; } = new List<HelpCommand>();
    }

    /// <summary>A command or command family shown in a command-line help table.</summary>
    public sealed class HelpCommand {
        /// <summary>Creates a command entry.</summary>
        /// <param name="name">The command name or family syntax.</param>
        /// <param name="description">The description.</param>
        /// <param name="group">An optional group heading.</param>
        public HelpCommand(string name, string? description, string? group = null) {
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            Group = group;
        }

        /// <summary>The command name or family syntax.</summary>
        public string Name { get; }

        /// <summary>The description.</summary>
        public string Description { get; }

        /// <summary>The optional group heading.</summary>
        public string? Group { get; }
    }
}
