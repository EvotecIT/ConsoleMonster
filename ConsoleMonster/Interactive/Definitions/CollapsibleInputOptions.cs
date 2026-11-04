using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>Constructs a collapsible section using the same definition as New-ConsoleCollapsible.</summary>
    public sealed class CollapsibleInputOptions : AppInputOptions, IContentOwner {
        /// <summary>Whether the section starts expanded.</summary>
        public bool Expanded { get; set; }
        /// <summary>Muted text after the label while collapsed, such as a count.</summary>
        public string? Summary { get; set; }
        /// <summary>The folded content.</summary>
        public IWidget? Content { get; set; }

        void IContentOwner.ReplaceContent(IWidget[] content) => Content = ContentBuilder.Compose(content);
    }
}
