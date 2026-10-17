using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>One styled line, truncated with an ellipsis, optionally filling its row with a background.</summary>
    internal sealed class UiLine : IWidget {
        private readonly Func<RenderContext, StyledLine> _line;

        public UiLine(StyledLine line) {
            if (line == null) {
                throw new ArgumentNullException(nameof(line));
            }
            _line = _ => line;
        }

        public UiLine(Func<RenderContext, StyledLine> line) {
            _line = line ?? throw new ArgumentNullException(nameof(line));
        }

        public static UiLine Empty => new UiLine(StyledLine.Empty);

        public ThemeToken? Fill { get; set; }

        public int Indent { get; set; }

        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            Style fill = Fill.HasValue ? context.Style(Fill.Value) : Style.Plain;
            if (Fill.HasValue) {
                region.Fill(0, 0, region.Width, 1, " ", fill);
            }
            StyledLine line = _line(context);
            region.Write(Indent, 0, TextWrapper.Truncate(Fill.HasValue ? line.WithBaseStyle(fill) : line, Math.Max(1, region.Width - Indent - (Fill.HasValue ? 1 : 0)), context.Ellipsis));
        }

        public static StyledLine Spans(params TextSpan[] spans) => new StyledLine(spans);
    }

    /// <summary>Wrapped plain text with a token, at most a number of lines (the rest is cut with an ellipsis).</summary>
    internal sealed class UiParagraph : IWidget {
        private readonly string _text;
        private readonly ThemeToken _token;

        public UiParagraph(string text, ThemeToken token = ThemeToken.Muted, int maxLines = 3) {
            _text = text ?? string.Empty;
            _token = token;
            MaxLines = maxLines;
        }

        public int MaxLines { get; }

        public TextDecoration Decoration { get; set; }

        public Size Measure(Constraints constraints, RenderContext context) {
            int lines = Wrap(constraints.MaxWidth, context).Count;
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? lines : Math.Min(lines, constraints.MaxHeight));
        }

        public void Render(Region region, RenderContext context) {
            IReadOnlyList<StyledLine> lines = Wrap(region.Width, context);
            for (int y = 0; y < lines.Count && y < region.Height; y++) {
                region.Write(0, y, lines[y]);
            }
        }

        private IReadOnlyList<StyledLine> Wrap(int width, RenderContext context) {
            if (string.IsNullOrEmpty(_text) || width <= 1) {
                return new StyledLine[0];
            }
            Style style = context.Style(_token).With(Decoration);
            List<StyledLine> all = new List<StyledLine>();
            foreach (string paragraph in _text.Replace("\r\n", "\n").Split('\n')) {
                all.AddRange(TextWrapper.Wrap(StyledLine.From(paragraph, style), Math.Max(1, width - 1)));
            }
            if (all.Count > MaxLines) {
                List<StyledLine> cut = all.GetRange(0, MaxLines);
                StyledLine last = cut[MaxLines - 1];
                cut[MaxLines - 1] = TextWrapper.Truncate(new StyledLine(new[] { new TextSpan(last.PlainText + " " + context.Ellipsis, style) }), Math.Max(1, width - 1), context.Ellipsis);
                return cut;
            }
            return all;
        }
    }
}
