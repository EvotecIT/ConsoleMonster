using System;
using System.Collections.Generic;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>A source of palette commands ("jump to item…", "open preset…", app-specific searches).</summary>
    public interface ICommandProvider {
        /// <summary>The group name shown with its commands.</summary>
        string Name { get; }

        /// <summary>The commands; called when the palette opens.</summary>
        /// <returns>The commands.</returns>
        IEnumerable<PaletteCommand> GetCommands();
    }
}
