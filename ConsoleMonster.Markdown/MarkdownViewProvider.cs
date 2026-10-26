using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Layout;
using ConsoleMonster.Views;

namespace ConsoleMonster.Markdown {
    /// <summary>Supplies Markdown display to the core's view factory without introducing a parser dependency into the core.</summary>
    public sealed class MarkdownViewProvider : IConsoleViewProvider {
        /// <inheritdoc />
        public IWidget Create(IReadOnlyList<object?> items, ConsoleViewOptions options) {
            if (items == null) { throw new ArgumentNullException(nameof(items)); }
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            if (options.View != ConsoleViewKind.Markdown) { throw new ArgumentException("MarkdownViewProvider requires View Markdown.", nameof(options)); }
            return MarkdownConsole.Parse(string.Join("\n", items.Select(item => Convert.ToString(item, CultureInfo.CurrentCulture) ?? string.Empty)));
        }
    }
}
