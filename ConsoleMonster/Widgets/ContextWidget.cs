using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Widgets;

/// <summary>Builds a view for the destination context at measurement and rendering time.</summary>
public sealed class ContextWidget : IWidget {
    private readonly Func<RenderContext, IWidget> _build;

    /// <summary>Creates a deferred view without detecting or changing the process terminal.</summary>
    /// <param name="build">A pure factory using the supplied destination context.</param>
    public ContextWidget(Func<RenderContext, IWidget> build) { _build = build ?? throw new ArgumentNullException(nameof(build)); }

    /// <inheritdoc />
    public Size Measure(Constraints constraints, RenderContext context) => _build(context).Measure(constraints, context);

    /// <inheritdoc />
    public void Render(Region region, RenderContext context) => _build(context).Render(region, context);
}
