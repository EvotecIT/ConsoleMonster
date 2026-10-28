using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Frames;

/// <summary>Where a section prints.</summary>
public enum FramePlacement {
    /// <summary>In the summary rows under the meters, packed side by side with neighbours while they fit.</summary>
    Summary,
    /// <summary>After the summary, full width, flowed into columns on wide terminals; omitted in Summary length.</summary>
    Detail
}

/// <summary>What a section's content factory knows when it builds: the width it got and the frame's scale.</summary>
public sealed class FrameSectionContext {
    internal FrameSectionContext(int width, FrameScale scale, RenderContext render) { Width = width; Scale = scale; Render = render; }
    /// <summary>Cells allocated to this section.</summary>
    public int Width { get; }
    /// <summary>Size and length policy for this render.</summary>
    public FrameScale Scale { get; }
    /// <summary>The destination's glyph tier, theme and culture.</summary>
    public RenderContext Render { get; }
}

/// <summary>
/// A titled block of a frame. The content is built after the frame decides the section's width, so a section can
/// choose its own tier (for example a DC matrix, a site matrix or a top-N list) for the space it actually got.
/// </summary>
public sealed class FrameSection {
    /// <summary>Creates a section.</summary>
    /// <param name="title">The literal title.</param><param name="content">Builds the content for an allocated width. It must be pure.</param>
    public FrameSection(string title, Func<FrameSectionContext, IWidget> content) {
        Title = title ?? string.Empty;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }
    /// <summary>The literal title.</summary>
    public string Title { get; }
    /// <summary>The content factory.</summary>
    public Func<FrameSectionContext, IWidget> Content { get; }
    /// <summary>Literal Muted facts after the title.</summary>
    public IList<string> Facts { get; } = new List<string>();
    /// <summary>The section's states at its right edge, worst first.</summary>
    public IList<FrameState> States { get; } = new List<FrameState>();
    /// <summary>Literal Muted meta at the right edge when there is no state (<c>log axis</c>).</summary>
    public string? Meta { get; set; }
    /// <summary>Summary or detail.</summary>
    public FramePlacement Placement { get; set; }
    /// <summary>The narrowest useful width; null uses the content's natural width, so the section never shrinks below it.</summary>
    public int? MinWidth { get; set; }
    /// <summary>The widest the section may become, or null for no limit.</summary>
    public int? MaxWidth { get; set; }
    /// <summary>Share of leftover row width; zero keeps the section at its natural width.</summary>
    public double Grow { get; set; }
    /// <summary>Whether the section always takes a row of its own.</summary>
    public bool FullWidth { get; set; }

    private int _cachedWidth = -1;
    private FrameScale? _cachedScale;
    private RenderContext? _cachedContext;
    private IWidget? _cached;

    internal IWidget Build(int width, FrameScale scale, RenderContext context) {
        if (_cached != null && _cachedWidth == width && ReferenceEquals(_cachedScale, scale) && ReferenceEquals(_cachedContext, context)) return _cached;
        _cachedWidth = width; _cachedScale = scale; _cachedContext = context;
        return _cached = new SectionBlock(new SectionTitle(Title, Facts, States, Meta), Content(new FrameSectionContext(width, scale, context)));
    }

    /// <summary>The width the section wants: its title line or its content, whichever is wider, within its limits.</summary>
    internal int Natural(int available, FrameScale scale, RenderContext context) {
        int limit = Math.Min(available, MaxWidth ?? available);
        IWidget block = Build(limit, scale, context);
        return Math.Max(Math.Min(MinWidth ?? 0, limit), Math.Min(limit, block.Measure(new Constraints(limit), context).Width));
    }

    /// <summary>The narrowest width the packer may give this section.</summary>
    internal int Minimum(int available, FrameScale scale, RenderContext context) => MinWidth.HasValue ? Math.Min(MinWidth.Value, available) : Natural(available, scale, context);
}

/// <summary>A section title over its content, with no gap.</summary>
internal sealed class SectionBlock : IWidget {
    private readonly SectionTitle _title;
    private readonly IWidget _content;
    internal SectionBlock(SectionTitle title, IWidget content) { _title = title; _content = content; }

    public Size Measure(Constraints constraints, RenderContext context) {
        Size content = _content.Measure(new Constraints(constraints.MaxWidth), context);
        int width = Math.Max(_title.NaturalWidth(context), content.Width);
        return new Size(Math.Min(constraints.MaxWidth, width), Math.Min(constraints.MaxHeight, 1 + content.Height));
    }

    public void Render(Region region, RenderContext context) {
        _title.Render(region.Sub(0, 0, region.Width, Math.Min(1, region.Height)), context);
        if (region.Height > 1) _content.Render(region.Sub(0, 1, region.Width, region.Height - 1), context);
    }
}

/// <summary>Widgets side by side at fixed widths, top aligned, with a fixed gap.</summary>
internal sealed class Lane : IWidget {
    private readonly IReadOnlyList<IWidget> _items;
    private readonly IReadOnlyList<int> _widths;
    private readonly int _gap;
    internal Lane(IReadOnlyList<IWidget> items, IReadOnlyList<int> widths, int gap) { _items = items; _widths = widths; _gap = gap; }

    public Size Measure(Constraints constraints, RenderContext context) {
        int height = 0;
        int width = 0;
        for (int i = 0; i < _items.Count; i++) {
            Size size = _items[i].Measure(new Constraints(_widths[i]), context);
            height = Math.Max(height, size.Height);
            width += (i > 0 ? _gap : 0) + (i == _items.Count - 1 ? size.Width : _widths[i]);
        }
        return new Size(Math.Min(constraints.MaxWidth, width), Math.Min(constraints.MaxHeight, height));
    }

    public void Render(Region region, RenderContext context) {
        int x = 0;
        for (int i = 0; i < _items.Count && x < region.Width; i++) {
            int width = Math.Min(_widths[i], region.Width - x);
            int height = Math.Min(region.Height, _items[i].Measure(new Constraints(width), context).Height);
            if (height > 0) _items[i].Render(region.Sub(x, 0, width, height), context);
            x += _widths[i] + _gap;
        }
    }
}
