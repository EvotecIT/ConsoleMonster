using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>Several labelled chains from one origin, wrapping at arrows and optionally marking shared hop identities.</summary>
    public sealed partial class PathSet : IWidget {
        private readonly List<PathHop[]> _paths = new List<PathHop[]>();

        /// <summary>Creates a set of paths from one origin.</summary>
        /// <param name="origin">The common origin, automatically prepended to each added chain.</param>
        public PathSet(PathHop origin) { Origin = origin ?? throw new ArgumentNullException(nameof(origin)); }

        /// <summary>The common origin.</summary>
        public PathHop Origin { get; }

        /// <summary>An optional markup title.</summary>
        public string? Title { get; set; }

        /// <summary>Mark hops present in more than one distinct chain with an asterisk and a legend.</summary>
        public bool HighlightSharedHops { get; set; }

        /// <summary>Show the origin once and use compact branching chains with four-cell hanging continuations.</summary>
        public bool CompactBranches { get; set; }

        /// <summary>The most chains rendered (default 10; zero shows all); omitted chains are counted.</summary>
        public int MaxPaths { get; set; } = 10;

        /// <summary>The maximum accepted hops per chain, excluding the origin (default 100).</summary>
        public int MaxHops { get; set; } = 100;

        /// <summary>The number of supplied chains.</summary>
        public int PathCount => _paths.Count;

        /// <summary>Adds the hops after the origin in one chain, preserving repeated hops and input order.</summary>
        /// <param name="hops">The hops after the common origin.</param>
        /// <returns>This set.</returns>
        public PathSet Add(params PathHop[] hops) {
            if (hops == null) throw new ArgumentNullException(nameof(hops));
            if (MaxHops < 0 || hops.Length > MaxHops) throw new ArgumentException("The chain exceeds MaxHops.", nameof(hops));
            PathHop[] copy = new PathHop[hops.Length + 1];
            copy[0] = Origin;
            for (int i = 0; i < hops.Length; i++) copy[i + 1] = hops[i] ?? throw new ArgumentException("A hop cannot be null.", nameof(hops));
            _paths.Add(copy);
            return this;
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => WidgetLines.Measure(Lines(constraints.MaxWidth, context), constraints);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => WidgetLines.Render(Lines(region.Width, context), region);

        private List<StyledLine> Lines(int width, RenderContext context) {
            if (CompactBranches) return BranchLines(width, context);
            List<StyledLine> lines = new List<StyledLine>();
            if (!string.IsNullOrEmpty(Title)) WidgetLines.Wrap(lines, context.Markup(Title!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Accent).With(TextDecoration.Bold)), width);
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            if (HighlightSharedHops) foreach (PathHop[] path in _paths) {
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (PathHop hop in path) if (hop.Key != Origin.Key && seen.Add(hop.Key)) counts[hop.Key] = counts.TryGetValue(hop.Key, out int n) ? n + 1 : 1;
            }
            string arrow = context.Glyphs.Tier == GlyphTier.Ascii ? " -> " : " → ";
            int shown = MaxPaths <= 0 ? _paths.Count : Math.Min(MaxPaths, _paths.Count);
            bool sharedShown = false;
            PresenterContext presenter = new PresentationOptions().CreateContext(context);
            for (int i = 0; i < shown; i++) {
                List<TextSpan> current = new List<TextSpan> { new TextSpan((i + 1).ToString(CultureInfo.InvariantCulture) + ". ", context.Style(ThemeToken.Muted)) };
                for (int h = 0; h < _paths[i].Length; h++) {
                    PathHop hop = _paths[i][h];
                    bool shared = HighlightSharedHops && counts.TryGetValue(hop.Key, out int count) && count > 1;
                    sharedShown |= shared;
                    List<TextSpan> item = new List<TextSpan>();
                    if (h > 0) item.Add(new TextSpan(arrow, context.Style(ThemeToken.Muted)));
                    if (hop.Status.HasValue) item.Add(new TextSpan(context.Glyphs.StatusSymbol(hop.Status.Value) + " ", context.Style(hop.Status.Value)));
                    item.Add(new TextSpan(hop.Label + (shared ? "*" : string.Empty), context.Style(shared ? ThemeToken.Accent : ThemeToken.Text).With(shared ? TextDecoration.Bold : TextDecoration.None)));
                    foreach (string chip in hop.Chips) {
                        item.Add(new TextSpan(" "));
                        item.AddRange(CellPresenters.Chip.Present(chip, presenter).Line);
                    }
                    StyledLine part = new StyledLine(item);
                    if (h > 0 && new StyledLine(current).Width + part.Width > width) {
                        WidgetLines.Wrap(lines, new StyledLine(current), width);
                        current.Clear();
                    }
                    // Only an individual hop wider than the whole region wraps inside its own text.
                    if (part.Width > width) {
                        if (current.Count > 0) { WidgetLines.Wrap(lines, new StyledLine(current), width); current.Clear(); }
                        IReadOnlyList<StyledLine> wrapped = TextWrapper.Wrap(part, Math.Max(1, width));
                        for (int w = 0; w < wrapped.Count - 1; w++) lines.Add(wrapped[w]);
                        current.AddRange(wrapped[wrapped.Count - 1]);
                    } else current.AddRange(part);
                }
                WidgetLines.Wrap(lines, new StyledLine(current), width);
            }
            if (sharedShown) WidgetLines.Wrap(lines, StyledLine.From("* shared hop", context.Style(ThemeToken.Muted)), width);
            if (shown < _paths.Count) WidgetLines.Wrap(lines, StyledLine.From("+" + (_paths.Count - shown).ToString(CultureInfo.InvariantCulture) + " more paths", context.Style(ThemeToken.Muted)), width);
            return lines;
        }
    }
}
