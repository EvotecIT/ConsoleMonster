using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Frames;

/// <summary>Counts in labelled buckets with line bars on one shared scale; a bucket gets a role only when its owner marks it.</summary>
public sealed class Distribution : IWidget {
    private readonly List<Bucket> _buckets = new List<Bucket>();

    /// <summary>Adds a bucket.</summary>
    /// <param name="label">The literal label.</param><param name="count">The count.</param><param name="state">Optional state for buckets past a threshold the data owner supplied.</param>
    /// <returns>This instance.</returns>
    public Distribution Add(string label, long count, HealthState? state = null) {
        _buckets.Add(new Bucket(label ?? string.Empty, count, state, null));
        return this;
    }

    /// <summary>Adds a row without a bar, such as items that were not read.</summary>
    /// <param name="label">The literal label.</param><param name="count">The count.</param><param name="state">The note's state.</param><param name="note">A literal note in place of the bar.</param>
    /// <returns>This instance.</returns>
    public Distribution AddNote(string label, long count, HealthState state, string note) {
        _buckets.Add(new Bucket(label ?? string.Empty, count, state, note ?? string.Empty));
        return this;
    }

    /// <summary>The bar width the distribution would like (default 25).</summary>
    public int BarWidth { get; set; } = 25;

    private List<StyledLine> Lines(int width, RenderContext context) {
        int label = 0, count = 0;
        long maximum = 0;
        foreach (Bucket bucket in _buckets) {
            label = Math.Max(label, FrameText.Width(FrameText.Escape(bucket.Label), context));
            count = Math.Max(count, FrameText.Number(bucket.Count, context).Length);
            if (bucket.Note == null) maximum = Math.Max(maximum, bucket.Count);
        }
        if (!context.HasColor) count += 2;
        int bar = Math.Max(6, Math.Min(BarWidth, width - label - count - 4));
        List<StyledLine> lines = new List<StyledLine>();
        foreach (Bucket bucket in _buckets) {
            string role = bucket.State.HasValue && bucket.State.Value != HealthState.Healthy ? FrameText.Tag(bucket.State.Value) : string.Empty;
            List<TextSpan> spans = new List<TextSpan>();
            spans.AddRange(FrameText.Fit(FrameText.Line(FrameText.Escape(bucket.Label), context), label, false, context));
            spans.Add(new TextSpan("  "));
            string middle = bucket.Note != null
                ? FrameBars.Note(bucket.Note, bucket.State ?? HealthState.Unknown, bar, context)
                : FrameBars.Line(maximum == 0 ? 0 : (double)bucket.Count / maximum, bar, bucket.State, context);
            spans.AddRange(FrameText.Fit(FrameText.Line(middle, context), bar, false, context));
            spans.Add(new TextSpan("  "));
            string number = FrameText.Number(bucket.Count, context);
            // Without colour a marked bucket keeps its state as a glyph before the count.
            if (role.Length > 0 && bucket.Count > 0 && !context.HasColor) number = FrameText.Glyph(bucket.State!.Value, context) + " " + number;
            spans.AddRange(FrameText.Fit(FrameText.Line(role.Length == 0 || bucket.Count == 0 ? number : "[" + role + "]" + number + "[/]", context), count, true, context));
            StyledLine line = new StyledLine(spans);
            lines.Add(line.Width > width ? TextWrapper.Truncate(line, width, context.Ellipsis) : line);
        }
        return lines;
    }

    /// <inheritdoc />
    public Size Measure(Constraints constraints, RenderContext context) {
        List<StyledLine> lines = Lines(constraints.MaxWidth, context);
        int width = 0;
        foreach (StyledLine line in lines) width = Math.Max(width, line.Width);
        return new Size(Math.Min(width, constraints.MaxWidth), Math.Min(lines.Count, constraints.MaxHeight));
    }

    /// <inheritdoc />
    public void Render(Region region, RenderContext context) {
        List<StyledLine> lines = Lines(region.Width, context);
        for (int y = 0; y < lines.Count && y < region.Height; y++) region.Write(0, y, lines[y]);
    }

    private sealed class Bucket {
        internal Bucket(string label, long count, HealthState? state, string? note) { Label = label; Count = count; State = state; Note = note; }
        internal string Label { get; }
        internal long Count { get; }
        internal HealthState? State { get; }
        internal string? Note { get; }
    }
}
