using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>Constructs an outline using the same definition as New-ConsoleOutline.</summary>
    public sealed class OutlineInputOptions : AppInputOptions {
        /// <summary>The entries, in order.</summary>
        public IList<OutlineItem> Items { get; } = new List<OutlineItem>();
        /// <summary>The index of the entry that starts current, or -1 for none.</summary>
        public int SelectedIndex { get; set; } = -1;
    }
}