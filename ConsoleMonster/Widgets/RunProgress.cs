using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>An append-only progress line, keeping counts and ETA visible at compact terminal widths.</summary>
    public sealed class RunProgress : IWidget {
        private readonly RunSnapshot _snapshot;
        /// <summary>Creates a progress line from an immutable observation.</summary>
        /// <param name="snapshot">Run counts, throughput and ETA.</param>
        public RunProgress(RunSnapshot snapshot) { _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot)); }
        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));
        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            string text = _snapshot.ProgressLine();
            if (region.Width < 100) {
                bool plain = context.Depth == ColorDepth.None || context.Profile.Mode == OutputMode.Plain;
                text = _snapshot.Done + "/" + _snapshot.Total + " · ";
                foreach (var pair in new[] { (StatusKind.Pass, _snapshot.Passed, "OK"), (StatusKind.Fail, _snapshot.Failed, "ERR"), (StatusKind.Warn, _snapshot.Warned, "WARN"), (StatusKind.Skip, _snapshot.Skipped, "SKIP") }) {
                    text += (plain ? pair.Item3 : context.Glyphs.StatusSymbol(pair.Item1)) + pair.Item2 + " ";
                }
                text += "· ETA " + (_snapshot.Remaining.HasValue ? NumberFormat.Duration(_snapshot.Remaining.Value) : "--");
            }
            new TextBlock(Markup.Escape("progress " + text)) { Token = ThemeToken.Muted, Overflow = TextOverflow.Ellipsis }.Render(region, context);
        }
    }
}
