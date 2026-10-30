using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Graphics {
    public sealed partial class TopologyDiagram {
        private sealed class Line {
            public Line(string text, ThemeToken token, int indent = 0) { Text = text; Token = token; Indent = indent; }
            public string Text { get; }
            public ThemeToken Token { get; }
            public int Indent { get; }
        }

        private sealed class Frame {
            public Frame(TopologyCellCanvas canvas, List<Line> above, List<Line> below) {
                Canvas = canvas; Above = above; Below = below;
                Width = Math.Max(canvas.Columns, above.Concat(below).Select(line => line.Indent + DisplayWidth.Measure(line.Text)).DefaultIfEmpty(0).Max());
                Height = above.Count + canvas.Rows + below.Count;
            }
            public TopologyCellCanvas Canvas { get; }
            public List<Line> Above { get; }
            public List<Line> Below { get; }
            public int Width { get; }
            public int Height { get; }
        }

        private Frame Compose(int width, RenderContext context) {
            bool ascii = context.Profile.Glyphs == GlyphTier.Ascii;
            _basic = context.Profile.Glyphs == GlyphTier.Basic;
            TopologyCellLayout layout = Layout(width);
            TopologyResolvedScenarioStep? step = _scenario != null && _step.HasValue && _step.Value < _scenario.Steps.Count ? _scenario.Steps[_step.Value] : null;
            TopologyCellPaintOptions paint = step == null ? new TopologyCellPaintOptions() : TopologyCellPaintOptions.ForStep(step);
            paint.Glyphs = ascii ? TopologyCellGlyphSet.Ascii : TopologyCellGlyphSet.Unicode;
            paint.FocusNodeId = _focus;
            paint.IncludeLegend = ShowLegend;
            foreach (KeyValuePair<string, string> value in _values) paint.NodeValues[value.Key] = value.Value;
            foreach (KeyValuePair<string, TopologyHealthStatus> status in _statuses) paint.NodeStatuses[status.Key] = status.Value;
            List<Line> above = new List<Line>();
            if (!string.IsNullOrWhiteSpace(_chart.Title)) above.Add(new Line(_chart.Title!, ThemeToken.Title));
            if (!string.IsNullOrWhiteSpace(_chart.Subtitle)) above.Add(new Line(_chart.Subtitle!, ThemeToken.Muted));
            if (above.Count > 0) above.Add(new Line(string.Empty, ThemeToken.Muted));
            List<Line> below = new List<Line>();
            if (ShowStepPanel && _scenario != null) AddStepPanel(below, step, ascii);
            if (_showDetails && _focus != null) AddDetails(below, ascii);
            return new Frame(layout.Paint(paint), above, below);
        }

        private void AddStepPanel(List<Line> lines, TopologyResolvedScenarioStep? active, bool ascii) {
            TopologyResolvedScenario scenario = _scenario!;
            string position = active == null ? scenario.Steps.Count.ToString(CultureInfo.InvariantCulture) + " steps" : "step " + (active.Index + 1).ToString(CultureInfo.InvariantCulture) + " of " + scenario.Steps.Count.ToString(CultureInfo.InvariantCulture);
            string state = _playing ? "  " + Mark(ascii) + "playing" : string.Empty;
            string count = _chart.Scenarios.Count > 1 ? "  (" + (_chart.Scenarios.FindIndex(item => item.Id == scenario.Id) + 1).ToString(CultureInfo.InvariantCulture) + "/" + _chart.Scenarios.Count.ToString(CultureInfo.InvariantCulture) + ")" : string.Empty;
            lines.Add(new Line(string.Empty, ThemeToken.Muted));
            lines.Add(new Line(scenario.Label + count + Dot(ascii) + position + state, ThemeToken.Header));
            if (!string.IsNullOrWhiteSpace(scenario.Scenario.Description)) lines.Add(new Line(scenario.Scenario.Description!.Trim(), ThemeToken.Muted));
            int center = active?.Index ?? 0;
            int first = Math.Max(0, Math.Min(center - MaxStepLines / 2, scenario.Steps.Count - MaxStepLines));
            int last = Math.Min(scenario.Steps.Count, first + MaxStepLines);
            int digits = scenario.Steps.Count.ToString(CultureInfo.InvariantCulture).Length;
            if (first > 0) lines.Add(new Line((ascii ? "  ^ " : "  ↑ ") + first.ToString(CultureInfo.InvariantCulture) + " earlier", ThemeToken.Subtle));
            for (int index = first; index < last; index++) {
                TopologyResolvedScenarioStep step = scenario.Steps[index];
                bool current = active != null && active.Index == index;
                string arrow = ascii ? " -> " : " → ";
                string route = step.TargetNodeId == null ? step.SourceLabel : step.SourceLabel + arrow + step.TargetLabel;
                string marker = current ? Mark(ascii) : "  ";
                ThemeToken token = current ? (step.IsFailure ? ThemeToken.Fail : ThemeToken.Accent) : step.IsFailure ? ThemeToken.Fail : ThemeToken.Muted;
                lines.Add(new Line(marker + (index + 1).ToString(CultureInfo.InvariantCulture).PadLeft(digits) + ". " + route + "  " + Kind(step) + "  " + step.Label, token));
                if (current && !string.IsNullOrEmpty(step.Payload)) {
                    string[] payload = step.Payload!.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                    foreach (string text in payload.Take(MaxPayloadLines)) lines.Add(new Line(text.TrimEnd(), ThemeToken.Info, digits + 4));
                    if (payload.Length > MaxPayloadLines) lines.Add(new Line((ascii ? "... " : "… ") + (payload.Length - MaxPayloadLines).ToString(CultureInfo.InvariantCulture) + " more lines", ThemeToken.Subtle, digits + 4));
                }
            }

            if (last < scenario.Steps.Count) lines.Add(new Line((ascii ? "  v " : "  ↓ ") + (scenario.Steps.Count - last).ToString(CultureInfo.InvariantCulture) + " more", ThemeToken.Subtle));
            if (ShowKeys) {
                string keys = ascii
                    ? "p play/pause  n/Right next  b/Left back  r restart  e end  s use case  Up/Down focus  Esc quit"
                    : "p play/pause  n/→ next  b/← back  r restart  e end  s use case  ↑/↓ focus  Esc quit";
                lines.Add(new Line(keys, ThemeToken.Subtle));
            }
        }

        private void AddDetails(List<Line> lines, bool ascii) {
            TopologyNode? node = _chart.Nodes.FirstOrDefault(item => item.Id == _focus);
            if (node == null) return;
            TopologyHealthStatus status = _statuses.TryGetValue(node.Id, out TopologyHealthStatus live) ? live : node.Status;
            List<string> facts = new List<string> { string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label, node.Kind.ToString() };
            if (status != TopologyHealthStatus.Unknown) facts.Add(status.ToString());
            if (!string.IsNullOrWhiteSpace(node.GroupId)) facts.Add(_chart.Groups.FirstOrDefault(group => group.Id == node.GroupId)?.Label ?? node.GroupId!);
            lines.Add(new Line(string.Empty, ThemeToken.Muted));
            lines.Add(new Line((ascii ? "* " : _basic ? "♦ " : "◆ ") + string.Join(Dot(ascii), facts), ThemeToken.Focus));
            if (!string.IsNullOrWhiteSpace(node.Subtitle)) lines.Add(new Line(node.Subtitle!, ThemeToken.Muted, 2));
            if (_values.TryGetValue(node.Id, out string? value)) lines.Add(new Line("Value: " + value, ThemeToken.Strong, 2));
            foreach (KeyValuePair<string, string> metric in node.Metrics.OrderBy(item => item.Key, StringComparer.Ordinal)) lines.Add(new Line(metric.Key + ": " + metric.Value, ThemeToken.Muted, 2));
            foreach (TopologyNodeDetail detail in node.Details) lines.Add(new Line(detail.Label + ": " + detail.Value, ThemeToken.Muted, 2));
            int links = _chart.Edges.Count(edge => edge.SourceNodeId == node.Id || edge.TargetNodeId == node.Id);
            lines.Add(new Line(links.ToString(CultureInfo.InvariantCulture) + (links == 1 ? " connection" : " connections"), ThemeToken.Subtle, 2));
        }

        private bool _basic;

        private string Mark(bool ascii) => ascii ? "> " : _basic ? "► " : "▶ ";

        private static string Dot(bool ascii) => ascii ? "  -  " : "  ·  ";

        private static string Kind(TopologyResolvedScenarioStep step) {
            switch (step.Interaction) {
                case TopologyScenarioInteraction.Request: return "request";
                case TopologyScenarioInteraction.Message: return "message";
                case TopologyScenarioInteraction.Response: return "response";
                case TopologyScenarioInteraction.Failure: return "failed";
                default: return step.TargetNodeId == null ? "at" : "route";
            }
        }

        private void Draw(Region region, Frame frame, RenderContext context) {
            int y = 0;
            foreach (Line line in frame.Above) {
                if (y >= region.Height) return;
                region.Write(line.Indent, y++, line.Text, context.Style(line.Token), Math.Max(0, region.Width - line.Indent));
            }

            bool basic = context.Profile.Glyphs == GlyphTier.Basic;
            for (int row = 0; row < frame.Canvas.Rows && y < region.Height; row++, y++) {
                for (int column = 0; column < frame.Canvas.Columns && column < region.Width; column++) {
                    TopologyCell cell = frame.Canvas[column, row];
                    if (cell.Width == 0 || cell.Role == TopologyCellRole.Empty) continue;
                    if (column + cell.Width > region.Width) break;
                    region.Put(column, y, basic ? BasicGlyph(cell.Glyph) : cell.Glyph, CellStyle(cell, context), cell.Width);
                }
            }

            foreach (Line line in frame.Below) {
                if (y >= region.Height) return;
                region.Write(line.Indent, y++, line.Text, context.Style(line.Token), Math.Max(0, region.Width - line.Indent));
            }
        }

        internal static Style CellStyle(TopologyCell cell, RenderContext context) {
            if (cell.Role == TopologyCellRole.NodeFill) return Style.Plain;
            if (cell.Emphasis == TopologyCellEmphasis.Dimmed && cell.Role != TopologyCellRole.LegendText) return context.Style(ThemeToken.Subtle);
            switch (cell.Role) {
                case TopologyCellRole.GroupBorder: return context.Style(ThemeToken.Muted);
                case TopologyCellRole.GroupLabel: return context.Style(ThemeToken.Header);
                case TopologyCellRole.Edge:
                case TopologyCellRole.EdgeArrow:
                case TopologyCellRole.EdgeLabel:
                    return context.Style(cell.Emphasis == TopologyCellEmphasis.Failed ? ThemeToken.Fail : cell.Emphasis == TopologyCellEmphasis.Highlighted ? ThemeToken.Accent : ThemeToken.Muted);
                case TopologyCellRole.NodeBorder:
                    return cell.Emphasis == TopologyCellEmphasis.Focused ? Foreground(context, ThemeToken.Focus) : context.Style(cell.Emphasis == TopologyCellEmphasis.Highlighted ? ThemeToken.Accent : ThemeToken.Border);
                case TopologyCellRole.NodeLabel:
                    return cell.Emphasis == TopologyCellEmphasis.Focused ? Foreground(context, ThemeToken.Focus) : context.Style(cell.Emphasis == TopologyCellEmphasis.Highlighted ? ThemeToken.Accent : ThemeToken.Strong);
                case TopologyCellRole.NodeDetail:
                case TopologyCellRole.LegendText:
                    return context.Style(ThemeToken.Muted);
                case TopologyCellRole.NodeStatus:
                    return context.Style(StatusOf(cell.Status));
                case TopologyCellRole.LegendMarker:
                    return context.Style(cell.Emphasis == TopologyCellEmphasis.Failed ? ThemeToken.Fail : cell.Emphasis == TopologyCellEmphasis.Highlighted ? ThemeToken.Accent : StatusOf(cell.Status).ToToken());
                default:
                    return Style.Plain;
            }
        }

        // Focus themes may carry a selection background; inside the diagram only the colour marks the focused box.
        private static Style Foreground(RenderContext context, ThemeToken token) {
            Style style = context.Style(token);
            return new Style(style.Color.IsDefault ? style.BackgroundColor : style.Color, default, style.Decoration);
        }

        internal static StatusKind StatusOf(TopologyHealthStatus status) {
            switch (status) {
                case TopologyHealthStatus.Healthy: return StatusKind.Pass;
                case TopologyHealthStatus.Warning: return StatusKind.Warn;
                case TopologyHealthStatus.Critical: return StatusKind.Fail;
                case TopologyHealthStatus.Disabled: return StatusKind.Skip;
                default: return StatusKind.None;
            }
        }

        /// <summary>Maps glyphs outside the Basic tier (rounded corners, dashes, heavy lines, wide arrows) to WGL4 equivalents.</summary>
        internal static string BasicGlyph(string glyph) {
            switch (glyph) {
                case "╭": return "┌";
                case "╮": return "┐";
                case "╰": return "└";
                case "╯": return "┘";
                case "╌": case "┄": return "─";
                case "╎": case "┆": return "│";
                case "━": return "═";
                case "┃": return "║";
                case "┏": return "╔";
                case "┓": return "╗";
                case "┗": return "╚";
                case "┛": return "╝";
                case "┣": return "╠";
                case "┫": return "╣";
                case "┳": return "╦";
                case "┻": return "╩";
                case "╋": return "╬";
                case "▶": return "►";
                case "◀": return "◄";
                case "✕": return "x";
                default: return glyph;
            }
        }
    }
}
