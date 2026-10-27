using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>A retained page definition; its title becomes a stable tab identity at Build.</summary>
    public sealed class TabOptions : IContentOwner {
        /// <summary>The unique page label.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>The retained subtree of inputs and displays.</summary>
        public IWidget Content { get; set; } = new Rows();
        void IContentOwner.ReplaceContent(IWidget[] content) => Content = ContentBuilder.Compose(content);
        /// <summary>Creates the retained page without running input.</summary>
        public LayoutRegion Build() => new LayoutRegion(Title, Content);
    }
}
