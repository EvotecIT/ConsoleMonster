using System;
using System.Collections.Generic;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>One entry of the command palette.</summary>
    public sealed class PaletteCommand {
        /// <summary>Creates a command.</summary>
        /// <param name="title">The title (what the filter matches).</param>
        /// <param name="invoke">What it does.</param>
        public PaletteCommand(string title, Action<MenuContext> invoke) {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Invoke = invoke ?? throw new ArgumentNullException(nameof(invoke));
        }

        /// <summary>The title.</summary>
        public string Title { get; }

        /// <summary>Where it comes from or leads ("Go to", "Preset", a path), shown muted.</summary>
        public string? Category { get; set; }

        /// <summary>A sentence shown while focused.</summary>
        public string? Description { get; set; }

        /// <summary>A key hint shown at the right.</summary>
        public string? Keys { get; set; }

        /// <summary>What it does.</summary>
        public Action<MenuContext> Invoke { get; }
    }
}
