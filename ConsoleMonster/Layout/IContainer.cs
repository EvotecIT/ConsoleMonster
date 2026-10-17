using System.Collections.Generic;

namespace ConsoleMonster.Layout {
    /// <summary>A widget with children and a layout policy (rows, columns, grid, panel, padding, custom layouts).</summary>
    public interface IContainer : IWidget {
        /// <summary>The child widgets in layout order.</summary>
        IReadOnlyList<IWidget> Children { get; }
    }
}
