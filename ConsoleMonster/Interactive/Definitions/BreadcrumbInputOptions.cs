using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    /// <summary>Constructs a breadcrumb using the same definition as New-ConsoleBreadcrumb.</summary>
    public sealed class BreadcrumbInputOptions : AppInputOptions {
        /// <summary>The segments, outermost first, shown when no outline is followed.</summary>
        public IList<string> Segments { get; } = new List<string>();
        /// <summary>Segments shown before a followed outline's headings.</summary>
        public IList<string> Root { get; } = new List<string>();
        /// <summary>The outline whose current entry the breadcrumb shows, or null.</summary>
        public Outline? Outline { get; set; }
    }
}