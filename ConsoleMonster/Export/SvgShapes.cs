using System.Collections.Generic;
using System.Text;

namespace ConsoleMonster.Export {
    /// <summary>
    /// Collects the shapes of one row and writes them as one path per paint (fill colour, or stroke colour and width), so a
    /// row of borders, bars and braille dots costs a few elements instead of one per cell.
    /// </summary>
    internal sealed class SvgShapes {
        private readonly Dictionary<string, StringBuilder> _fills = new Dictionary<string, StringBuilder>(System.StringComparer.Ordinal);
        private readonly Dictionary<string, StringBuilder> _strokes = new Dictionary<string, StringBuilder>(System.StringComparer.Ordinal);
        private readonly List<string> _order = new List<string>();

        public bool IsEmpty => _order.Count == 0;

        public void Rect(double x, double y, double w, double h, string fill, double opacity = 1) {
            if (w <= 0 || h <= 0) {
                return;
            }
            Fill(fill, opacity).Append('M').Append(ExportText.Num(x)).Append(' ').Append(ExportText.Num(y))
                .Append('h').Append(ExportText.Num(w)).Append('v').Append(ExportText.Num(h)).Append('h').Append(ExportText.Num(-w)).Append('z');
        }

        public void Circle(double cx, double cy, double r, string fill) {
            string d = ExportText.Num(r * 2);
            string rr = ExportText.Num(r);
            Fill(fill, 1).Append('M').Append(ExportText.Num(cx - r)).Append(' ').Append(ExportText.Num(cy))
                .Append('a').Append(rr).Append(' ').Append(rr).Append(" 0 1 0 ").Append(d).Append(" 0")
                .Append('a').Append(rr).Append(' ').Append(rr).Append(" 0 1 0 -").Append(d).Append(" 0z");
        }

        public void Stroke(string data, string color, double width) {
            string key = color + "|" + ExportText.Num(width);
            if (!_strokes.TryGetValue(key, out StringBuilder? sb)) {
                sb = new StringBuilder();
                _strokes.Add(key, sb);
                _order.Add("s" + key);
            }
            sb.Append(data);
        }

        private StringBuilder Fill(string color, double opacity) {
            string key = color + "|" + ExportText.Num(opacity);
            if (!_fills.TryGetValue(key, out StringBuilder? sb)) {
                sb = new StringBuilder();
                _fills.Add(key, sb);
                _order.Add("f" + key);
            }
            return sb;
        }

        public void AppendTo(StringBuilder sb) {
            foreach (string entry in _order) {
                string key = entry.Substring(1);
                int bar = key.LastIndexOf('|');
                string color = key.Substring(0, bar);
                string amount = key.Substring(bar + 1);
                if (entry[0] == 'f') {
                    sb.Append("<path class=\"cm-s\" d=\"").Append(_fills[key]).Append('"');
                    ExportText.AppendPaint(sb, "fill", color);
                    if (amount != "1") {
                        sb.Append(" fill-opacity=\"").Append(amount).Append('"');
                    }
                } else {
                    sb.Append("<path d=\"").Append(_strokes[key]).Append("\" fill=\"none\" stroke-width=\"").Append(amount).Append('"');
                    ExportText.AppendPaint(sb, "stroke", color);
                }
                sb.Append("/>");
            }
        }
    }
}
