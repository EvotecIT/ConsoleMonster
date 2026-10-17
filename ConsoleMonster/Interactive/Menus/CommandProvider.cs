using System;
using System.Collections.Generic;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>A command provider from a function.</summary>
    public sealed class CommandProvider : ICommandProvider {
        private readonly Func<IEnumerable<PaletteCommand>> _commands;

        /// <summary>Creates a provider.</summary>
        /// <param name="name">The group name.</param>
        /// <param name="commands">Returns the commands.</param>
        public CommandProvider(string name, Func<IEnumerable<PaletteCommand>> commands) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public IEnumerable<PaletteCommand> GetCommands() => _commands();
    }
}
