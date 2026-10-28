using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets;

/// <summary>The opt-in visual vocabulary for embedded default views; ordinary terminal contexts retain their defaults.</summary>
public static class ConsoleViewStyle {
    /// <summary>Creates an isolated context with compact health glyphs and the console-view palette.</summary>
    /// <param name="context">The destination's existing capabilities and theme.</param><returns>A context without changing terminal or process settings.</returns>
    public static RenderContext Apply(RenderContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (IsApplied(context)) return context;
        ITheme theme = ReferenceEquals(context.Theme, Theme.Light) ? Theme.ConsoleViewLight : ReferenceEquals(context.Theme, Theme.Dark) ? Theme.ConsoleViewDark : context.Theme;
        GlyphSet glyphs = context.Profile.Glyphs == GlyphTier.Full ? GlyphSet.ConsoleViewFull
            : context.Profile.Glyphs == GlyphTier.Basic ? GlyphSet.ConsoleViewBasic : GlyphSet.ConsoleViewAscii;
        return new RenderContext(context.Profile, theme, glyphs, context.Tick, context.Elapsed);
    }
    internal static bool IsApplied(RenderContext context) => ReferenceEquals(context.Glyphs, GlyphSet.ConsoleViewFull)
        || ReferenceEquals(context.Glyphs, GlyphSet.ConsoleViewBasic) || ReferenceEquals(context.Glyphs, GlyphSet.ConsoleViewAscii);

    /// <summary>Builds and renders a deferred view using that view's isolated destination context.</summary>
    /// <param name="build">The pure view factory.</param><returns>An opt-in widget.</returns>
    public static IWidget Create(Func<RenderContext, IWidget> build) => Wrap(new ContextWidget(build));

    /// <summary>Applies the console-view vocabulary only while measuring and rendering this widget.</summary>
    /// <param name="widget">The view.</param><returns>A wrapper that leaves other render calls unchanged.</returns>
    public static IWidget Wrap(IWidget widget) => new StyledView(widget ?? throw new ArgumentNullException(nameof(widget)));

    private sealed class StyledView : IWidget, IContainer {
        private readonly IWidget _widget;
        internal StyledView(IWidget widget) { _widget = widget; }
        public IReadOnlyList<IWidget> Children => new[] { _widget };
        public Size Measure(Constraints constraints, RenderContext context) => _widget.Measure(constraints, Apply(context));
        public void Render(Region region, RenderContext context) => _widget.Render(region, Apply(context));
    }
}
