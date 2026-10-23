using System.Collections.Generic;

namespace ConsoleMonster.Help {
    /// <summary>Compact command-line help metadata, independent of the host's argument parser.</summary>
    public sealed class CommandLineHelp {
        /// <summary>The concise usage, including the executable name and positional placeholders.</summary>
        public string Usage { get; set; } = string.Empty;

        /// <summary>Commands in display order, optionally grouped or collapsed by the host.</summary>
        public IList<HelpCommand> Commands { get; } = new List<HelpCommand>();

        /// <summary>Heading for a catalog of positional values, or null for the render profile's localized heading.</summary>
        public string? ValuesHeading { get; set; }

        /// <summary>Named positional values described independently of subcommands.</summary>
        public IList<HelpValue> Values { get; } = new List<HelpValue>();
    }

}
