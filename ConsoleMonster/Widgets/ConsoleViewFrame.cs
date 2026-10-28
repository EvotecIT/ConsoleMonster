using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Widgets;

/// <summary>A content-width default view with a one-cell gutter and consistent block spacing.</summary>
public sealed class ConsoleViewFrame : IWidget, IContainer {
    /// <summary>Creates a frame with a title line and body blocks.</summary>
    public ConsoleViewFrame(ViewHeader header, params IWidget[] blocks) {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        if (blocks != null) { foreach (IWidget block in blocks) { Blocks.Add(block); } }
    }
    /// <summary>The title line.</summary>
    public ViewHeader Header { get; }
    /// <summary>An optional headline directly beneath the title.</summary>
    public IWidget? Headline { get; set; }
    /// <summary>Body blocks separated by one blank line.</summary>
    public IList<IWidget> Blocks { get; } = new List<IWidget>();
    /// <summary>An optional footer, separated from the body by one blank line.</summary>
    public IWidget? Footer { get; set; }
    /// <inheritdoc />
    public IReadOnlyList<IWidget> Children {
        get {
            List<IWidget> children = new List<IWidget> { Header };
            if (Headline != null) { children.Add(Headline); }
            foreach (IWidget block in Blocks) { children.Add(block); }
            if (Footer != null) { children.Add(Footer); }
            return children;
        }
    }
    private int ContentWidth(int available, RenderContext context) {
        int width = Math.Max(0, available - 2);
        int body = 0;
        foreach (IWidget child in Children) {
            Configure(child);
            if (!ReferenceEquals(child, Header)) { body = Math.Max(body, child.Measure(new Constraints(width), context).Width); }
        }
        int natural = Header.NaturalWidth(context);
        return Math.Min(width, Math.Max(body, natural <= width ? natural : Header.CompactWidth(width, context)));
    }
    private Rows Compose() {
        Rows title = new Rows(Header);
        if (Headline != null) { title.Add(Headline); }
        Rows view = new Rows(title) { Gap = 1 };
        foreach (IWidget block in Blocks) { view.Add(block); }
        if (Footer != null) { ConfigureFooter(Footer); view.Add(Footer); }
        return view;
    }
    private static void ConfigureFooter(IWidget widget) {
        if (widget is TextBlock text && text.WrapSeparator == null) { text.WrapSeparator = " · "; }
        if (widget is IContainer container) { foreach (IWidget child in container.Children) { ConfigureFooter(child); } }
    }
    private static void Configure(IWidget widget) {
        if (widget is Table table) { table.Border = TableBorder.Rule; table.HeaderSeparator = true; table.Expand = false; }
        if (widget is IContainer container) { foreach (IWidget child in container.Children) { Configure(child); } }
    }
    /// <inheritdoc />
    public Size Measure(Constraints constraints, RenderContext context) {
        context = ConsoleViewStyle.Apply(context);
        int width = ContentWidth(constraints.MaxWidth, context);
        Size size = Compose().Measure(new Constraints(width, constraints.MaxHeight), context);
        return new Size(Math.Min(constraints.MaxWidth, width + 2), size.Height);
    }
    /// <inheritdoc />
    public void Render(Region region, RenderContext context) {
        context = ConsoleViewStyle.Apply(context);
        int width = ContentWidth(region.Width, context);
        Compose().Render(region.Sub(1, 0, width, region.Height), context);
    }
}
