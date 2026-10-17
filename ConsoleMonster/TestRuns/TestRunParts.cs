using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.TestRuns {
    /// <summary>The running test: the spinner, the blocks (muted) and the test name; once finished, the outcome.</summary>
    internal sealed class CurrentTestLine : IWidget {
        private readonly TestRunSnapshot _snapshot;

        public CurrentTestLine(TestRunSnapshot snapshot) {
            _snapshot = snapshot;
        }

        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, 1);

        public void Render(Region region, RenderContext context) {
            if (region.Height <= 0) {
                return;
            }
            Style muted = context.Style(ThemeToken.Muted);
            List<TextSpan> spans = new List<TextSpan>();
            if (_snapshot.IsFinished) {
                StatusKind status = _snapshot.Status;
                spans.Add(new TextSpan(context.Glyphs.StatusSymbol(status) + " ", context.Style(status)));
                spans.Add(new TextSpan(status == StatusKind.Fail ? "Finished with failures" : "Finished", context.Style(ThemeToken.Text)));
            } else if (_snapshot.CurrentName == null) {
                spans.Add(new TextSpan(context.Spinner() + " ", context.Style(StatusKind.Running)));
                spans.Add(new TextSpan(_snapshot.Containers.Count == 0 ? "Discovering tests" + context.Ellipsis : "Starting" + context.Ellipsis, muted));
            } else {
                spans.Add(new TextSpan(context.Spinner() + " ", context.Style(StatusKind.Running)));
                string chevron = " " + Interactive.InteractiveGlyphs.For(context).Breadcrumb + " ";
                foreach (string block in _snapshot.CurrentBlocks) {
                    spans.Add(new TextSpan(block + chevron, muted));
                }
                spans.Add(new TextSpan(_snapshot.CurrentName, context.Style(ThemeToken.Text)));
            }
            region.Write(0, 0, TextWrapper.Truncate(new StyledLine(spans), region.Width, context.Ellipsis));
        }
    }

    /// <summary>Failures, one block each (see <see cref="TestRunText.FailureLines"/>), with "+ n more" when they do not fit.</summary>
    internal sealed class FailureList : IWidget {
        private readonly IReadOnlyList<TestRecord> _failures;
        private readonly int _limit;

        public FailureList(IReadOnlyList<TestRecord> failures, int limit) {
            _failures = failures;
            _limit = Math.Max(1, limit);
        }

        public Size Measure(Constraints constraints, RenderContext context) {
            int rows = 0;
            int shown = Math.Min(_failures.Count, _limit);
            for (int i = 0; i < shown; i++) {
                rows += TestRunText.FailureLines(_failures[i], context, 2).Count + (i > 0 ? 1 : 0);
            }
            if (shown < _failures.Count) {
                rows++;
            }
            rows = Math.Max(1, rows);
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? rows : Math.Min(rows, constraints.MaxHeight));
        }

        public void Render(Region region, RenderContext context) {
            Style muted = context.Style(ThemeToken.Muted);
            if (_failures.Count == 0) {
                region.Write(0, 0, "No failures", muted);
                return;
            }
            int y = 0;
            int shown = 0;
            int messageLines = region.Height >= 12 ? 3 : 2;
            for (int i = 0; i < _failures.Count && shown < _limit; i++) {
                List<StyledLine> lines = TestRunText.FailureLines(_failures[i], context, messageLines);
                int needed = lines.Count + (shown > 0 ? 1 : 0);
                bool last = i == _failures.Count - 1 || shown + 1 == _limit;
                if (y + needed > region.Height - (last ? 0 : 1)) {
                    break;
                }
                if (shown > 0) {
                    y++;
                }
                foreach (StyledLine line in lines) {
                    region.Write(0, y++, TextWrapper.Truncate(line, region.Width, context.Ellipsis));
                }
                shown++;
            }
            int more = _failures.Count - shown;
            if (more > 0 && y < region.Height) {
                region.Write(0, Math.Min(region.Height - 1, y), "+ " + more.ToString(CultureInfo.InvariantCulture) + (more == 1 ? " more failure" : " more failures"), muted);
            }
        }
    }

    /// <summary>The slowest tests: the duration right-aligned, the test name and its file (muted).</summary>
    internal sealed class SlowestList : IWidget {
        private readonly IReadOnlyList<TestRecord> _slowest;

        public SlowestList(IReadOnlyList<TestRecord> slowest) {
            _slowest = slowest;
        }

        public Size Measure(Constraints constraints, RenderContext context) {
            int rows = Math.Max(1, _slowest.Count);
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? rows : Math.Min(rows, constraints.MaxHeight));
        }

        public void Render(Region region, RenderContext context) {
            Style muted = context.Style(ThemeToken.Muted);
            if (_slowest.Count == 0) {
                region.Write(0, 0, "No tests finished yet", muted);
                return;
            }
            Style text = context.Style(ThemeToken.Text);
            Style value = context.Style(ThemeToken.Highlight);
            for (int i = 0; i < _slowest.Count && i < region.Height; i++) {
                TestRecord record = _slowest[i];
                string duration = NumberFormat.Duration(record.Duration).PadLeft(6);
                int x = region.Write(0, i, duration, value);
                x += 2;
                int room = Math.Max(0, region.Width - x);
                string name = record.Name;
                if (DisplayWidth.Measure(name) > room && room > 1) {
                    name = TextWrapper.Truncate(StyledLine.From(name, text), room, context.Ellipsis).PlainText;
                }
                x = region.Write(x, i, name, text, room);
                string file = System.IO.Path.GetFileName(record.Container);
                if (!string.IsNullOrEmpty(file) && x + 2 + DisplayWidth.Measure(file) <= region.Width) {
                    region.Write(x + 2, i, file, muted);
                }
            }
        }
    }
}
