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
        public Size Measure(Constraints constraints, RenderContext context) => Facts(constraints.MaxWidth, context).Measure(constraints, context);
        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Facts(region.Width, context).Render(region, context);

        private ViewFacts Facts(int width, RenderContext context) {
            bool compact = width < 100;
            bool plain = context.Depth == ColorDepth.None || context.Profile.Mode == OutputMode.Plain;
            ViewFacts facts = new ViewFacts { Indent = 0 }.Add(new ViewFact(_snapshot.Done.ToString("N0", context.Culture) + "/" + _snapshot.Total.ToString("N0", context.Culture), label: string.Empty, caption: "progress"));
            foreach (var pair in new[] { (StatusKind.Pass, _snapshot.Passed, "OK", "passed"), (StatusKind.Fail, _snapshot.Failed, "ERR", "failed"), (StatusKind.Warn, _snapshot.Warned, "WARN", "warnings"), (StatusKind.Skip, _snapshot.Skipped, "SKIP", "skipped") }) {
                facts.Add(new ViewFact(pair.Item2.ToString("N0", context.Culture), label: compact ? string.Empty : pair.Item4,
                    caption: compact ? plain ? pair.Item3 : context.Glyphs.StatusSymbol(pair.Item1) : null,
                    role: pair.Item2 == 0 ? ThemeToken.Muted : pair.Item1.ToToken()));
            }
            facts.Add(new ViewFact(_snapshot.Remaining.HasValue ? NumberFormat.Duration(_snapshot.Remaining.Value) : "--", caption: "ETA"));
            if (!compact) { facts.Add((_snapshot.Rate * 60).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "/min"); }
            return facts;
        }
    }
}
