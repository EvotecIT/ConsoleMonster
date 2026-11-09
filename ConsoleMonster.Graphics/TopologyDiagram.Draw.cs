using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Graphics {
    internal sealed partial class TopologyDiagram {
        private sealed class Line {
            public Line(string text, ThemeToken token, int indent = 0, Action? click = null, string? clickId = null) { Text = text; Token = token; Indent = indent; Click = click; ClickId = clickId; }
            public string? ClickId { get; }
            public string Text { get; }
            // In an app, a click on the line runs this, as its key does (a step line goes to that step).
            public Action? Click { get; }
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
            TopologyCellLayout layout = Layout(width, context.Profile.Glyphs);
            TopologyResolvedScenarioStep? step = _scenario != null && _step.HasValue && _step.Value < _scenario.Steps.Count ? _scenario.Steps[_step.Value] : null;
            TopologyCellPaintOptions paint = step == null ? new TopologyCellPaintOptions() : TopologyCellPaintOptions.ForStep(step);
            paint.Glyphs = ascii ? TopologyCellGlyphSet.Ascii : TopologyCellGlyphSet.Unicode;
            paint.FocusNodeId = _focus;
            paint.IncludeLegend = ShowLegend;
            foreach (KeyValuePair<string, string> value in _values) paint.NodeValues[value.Key] = DiagramText.ForTier(value.Value, context);
            foreach (KeyValuePair<string, TopologyHealthStatus> status in _statuses) paint.NodeStatuses[status.Key] = status.Value;
            List<Line> above = new List<Line>();
            if (!string.IsNullOrWhiteSpace(_chart.Title)) above.Add(new Line(_chart.Title!, ThemeToken.Title));
            if (!string.IsNullOrWhiteSpace(_chart.Subtitle)) above.Add(new Line(_chart.Subtitle!, ThemeToken.Muted));
            if (above.Count > 0) above.Add(new Line(string.Empty, ThemeToken.Muted));
            List<Line> below = new List<Line>();
            if (ShowStepPanel && _scenario != null) AddStepPanel(below, step, ascii);
            if (_showDetails && _focus != null) AddDetails(below, ascii);
            if (ShowKeys) AddKeyHints(below, ascii);
            if (ascii) {
                SanitizeLines(above, context);
                SanitizeLines(below, context);
            }
            return new Frame(layout.Paint(paint), above, below);
        }

        internal IWidget? Panels(RenderContext context) {
            lock (_gate) {
                var lines = new List<Line>();
                bool ascii = context.Profile.Glyphs == GlyphTier.Ascii;
                _basic = context.Profile.Glyphs == GlyphTier.Basic;
                TopologyResolvedScenarioStep? step = _scenario != null && _step.HasValue && _step.Value < _scenario.Steps.Count ? _scenario.Steps[_step.Value] : null;
                if (ShowStepPanel && _scenario != null) AddStepPanel(lines, step, ascii);
                if (ShowDetails && _focus != null) AddDetails(lines, ascii);
                if (ShowKeys) AddKeyHints(lines, ascii);
                return lines.Count == 0 ? null : new Rows(lines.Select(line => (IWidget)new PanelLine(TextBlock.Plain(
                    new string(' ', line.Indent) + DiagramText.ForTier(line.Text, context), line.Token), line)).ToArray());
            }
        }

        // A panel line keeps its click (a step goes to that step, the use case plays) where the panels are drawn apart
        // from the diagram, as in the tree fallback.
        private sealed class PanelLine : IWidget {
            private readonly IWidget _text;
            private readonly Line _line;
            internal PanelLine(IWidget text, Line line) { _text = text; _line = line; }
            public Size Measure(Constraints constraints, RenderContext context) => _text.Measure(constraints, context);
            public void Render(Region region, RenderContext context) {
                _text.Render(region, context);
                if (_line.Click != null && region.Width > _line.Indent && region.Height > 0) {
                    Interactive.PointerTargets.RecordAction(new Rect(region.Bounds.X + _line.Indent, region.Bounds.Y, region.Width - _line.Indent, region.Height), _line.Click, _line.ClickId!);
                }
            }
        }

        private void AddStepPanel(List<Line> lines, TopologyResolvedScenarioStep? active, bool ascii) {
            TopologyResolvedScenario scenario = _scenario!;
            string position = active == null ? scenario.Steps.Count.ToString(CultureInfo.InvariantCulture) + " steps" : "step " + (active.Index + 1).ToString(CultureInfo.InvariantCulture) + " of " + scenario.Steps.Count.ToString(CultureInfo.InvariantCulture);
            string state = _playing ? "  " + Mark(ascii) + "playing" : string.Empty;
            string count = _chart.Scenarios.Count > 1 ? "  (" + (_chart.Scenarios.FindIndex(item => item.Id == scenario.Id) + 1).ToString(CultureInfo.InvariantCulture) + "/" + _chart.Scenarios.Count.ToString(CultureInfo.InvariantCulture) + ")" : string.Empty;
            lines.Add(new Line(string.Empty, ThemeToken.Muted));
            // A click on the use-case line plays or pauses it, as p and Space do.
            lines.Add(new Line(scenario.Label + count + Dot(ascii) + position + state, ThemeToken.Header, 0, TogglePlay, ScenarioScope + ":play"));
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
                int target = index;
                lines.Add(new Line(marker + (index + 1).ToString(CultureInfo.InvariantCulture).PadLeft(digits) + ". " + route + "  " + Kind(step) + "  " + step.Label, token, 0, () => Step(target), ScenarioScope + ":step:" + target.ToString(CultureInfo.InvariantCulture)));
                if (current && !string.IsNullOrEmpty(step.Payload)) {
                    string[] payload = step.Payload!.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                    foreach (string text in payload.Take(MaxPayloadLines)) lines.Add(new Line(text.TrimEnd(), ThemeToken.Info, digits + 4));
                    if (payload.Length > MaxPayloadLines) lines.Add(new Line((ascii ? "... " : "… ") + (payload.Length - MaxPayloadLines).ToString(CultureInfo.InvariantCulture) + " more lines", ThemeToken.Subtle, digits + 4));
                }
            }

            if (last < scenario.Steps.Count) lines.Add(new Line((ascii ? "  v " : "  ↓ ") + (scenario.Steps.Count - last).ToString(CultureInfo.InvariantCulture) + " more", ThemeToken.Subtle));
        }

        private void AddKeyHints(List<Line> lines, bool ascii) {
            if (_scenario != null && _scenario.Steps.Count > 0) {
                lines.Add(new Line(ascii ? "p play/pause  n/Right next  b/Left back" : "p play/pause  n/→ next  b/← back", ThemeToken.Subtle));
                lines.Add(new Line("r/Home restart  e/End last  x clear step", ThemeToken.Subtle));
            }
            if (_chart.Scenarios.Count > 1) lines.Add(new Line("s/Tab next use case  Shift+Tab previous use case", ThemeToken.Subtle));
            string navigation = ScrollKeyHints ? (ascii ? "Up/Down scroll" : "↑/↓ scroll")
                : _chart.Nodes.Count > 0 ? (ascii ? "Up/Down focus" : "↑/↓ focus") : string.Empty;
            if (_chart.Nodes.Count > 0) navigation += (navigation.Length > 0 ? "  " : string.Empty) + "d details";
            if (navigation.Length > 0) lines.Add(new Line(navigation, ThemeToken.Subtle));
        }

        private static void SanitizeLines(List<Line> lines, RenderContext context) {
            for (int index = 0; index < lines.Count; index++) {
                Line line = lines[index];
                string text = DiagramText.ForTier(line.Text, context);
                lines[index] = new Line(text, line.Token, line.Indent, line.Click, line.ClickId);
            }
        }

        private void AddDetails(List<Line> lines, bool ascii) {
            TopologyNode? node = _chart.Nodes.FirstOrDefault(item => item.Id == _focus);
            if (node == null) return;
            TopologyHealthStatus status = _statuses.TryGetValue(node.Id, out TopologyHealthStatus live) ? live : node.Status;
            List<string> facts = new List<string> { string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label,
                _nodeKinds.TryGetValue(node.Id, out DiagramNodeKind ownKind) ? ownKind.ToString() : node.Kind.ToString() };
            if (_statusKinds.TryGetValue(node.Id, out StatusKind ownStatus)) {
                if (!_unmarkedNodes.Contains(node.Id)) facts.Add(ownStatus.ToString());
            }
            else if (status != TopologyHealthStatus.Unknown) facts.Add(status.ToString());
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

        // In an app, a click on a node's box focuses it and shows its details, as Up/Down and d do.
        private void RecordNodeTargets(Region region, TopologyCellCanvas canvas, int top) {
            var boxes = new Dictionary<string, int[]>(StringComparer.Ordinal);
            for (int row = 0; row < canvas.Rows && top + row < region.Height; row++) {
                for (int column = 0; column < canvas.Columns && column < region.Width; column++) {
                    TopologyCell cell = canvas[column, row];
                    if (cell.ElementId == null || !IsNodeCell(cell.Role)) continue;
                    if (!boxes.TryGetValue(cell.ElementId, out int[]? box)) boxes[cell.ElementId] = new[] { column, row, column, row };
                    else { box[0] = Math.Min(box[0], column); box[1] = Math.Min(box[1], row); box[2] = Math.Max(box[2], column); box[3] = Math.Max(box[3], row); }
                }
            }
            foreach (KeyValuePair<string, int[]> box in boxes) {
                string id = box.Key;
                Interactive.PointerTargets.RecordAction(new Rect(region.Bounds.X + box.Value[0], region.Bounds.Y + top + box.Value[1], box.Value[2] - box.Value[0] + 1, box.Value[3] - box.Value[1] + 1), () => ShowNode(id), TargetScope + ":node:" + id);
            }
        }

        // Click targets name this diagram and, for steps and playback, the use case they belong to, so a press is not
        // completed by another diagram's or another use case's line drawn in the same place.
        private string TargetScope => "diagram:" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this).ToString(CultureInfo.InvariantCulture);
        private string ScenarioScope => TargetScope + ":" + (_scenario?.Id ?? string.Empty);

        private static bool IsNodeCell(TopologyCellRole role) => role == TopologyCellRole.NodeBorder || role == TopologyCellRole.NodeFill
            || role == TopologyCellRole.NodeLabel || role == TopologyCellRole.NodeDetail || role == TopologyCellRole.NodeStatus;

        private void Draw(Region region, Frame frame, RenderContext context) {
            int y = 0;
            foreach (Line line in frame.Above) {
                if (y >= region.Height) return;
                region.Write(line.Indent, y++, line.Text, context.Style(line.Token), Math.Max(0, region.Width - line.Indent));
            }

            bool basic = context.Profile.Glyphs == GlyphTier.Basic;
            bool ascii = context.Profile.Glyphs == GlyphTier.Ascii;
            var markedLinks = new HashSet<string>(StringComparer.Ordinal);
            RecordNodeTargets(region, frame.Canvas, y);
            for (int row = 0; row < frame.Canvas.Rows && y < region.Height; row++, y++) {
                for (int column = 0; column < frame.Canvas.Columns && column < region.Width; column++) {
                    TopologyCell cell = frame.Canvas[column, row];
                    if (cell.Role == TopologyCellRole.NodeStatus && cell.ElementId != null && _statusKinds.ContainsKey(cell.ElementId)) continue;
                    if (cell.Role == TopologyCellRole.GroupBorder && cell.ElementId != null && BorderlessGroups.Contains(cell.ElementId)) continue;
                    if (cell.Width == 0 || cell.Role == TopologyCellRole.Empty) continue;
                    if (column + cell.Width > region.Width) break;
                    string glyph = ascii ? SymbolFallback.GetGlyph(cell.Glyph, GlyphTier.Ascii, context.Profile.WidthProfile)
                        : basic ? BasicGlyph(cell.Glyph) : cell.Glyph;
                    Style style = CellStyle(cell, context);
                    // A failing or warning link is coloured along its whole length, and a failing node's box in its
                    // status, so the problem reads from across the screen, not only from one glyph on the label.
                    if (cell.ElementId != null && cell.Emphasis != TopologyCellEmphasis.Dimmed && cell.Emphasis != TopologyCellEmphasis.Focused) {
                        if ((cell.Role == TopologyCellRole.Edge || cell.Role == TopologyCellRole.EdgeArrow || cell.Role == TopologyCellRole.EdgeLabel)
                            && _linkStatusKinds.TryGetValue(cell.ElementId, out StatusKind edgeStatus) && (edgeStatus == StatusKind.Fail || edgeStatus == StatusKind.Warn)) {
                            style = context.Style(edgeStatus);
                        } else if (cell.Role == TopologyCellRole.NodeBorder && _statusKinds.TryGetValue(cell.ElementId, out StatusKind nodeStatus)
                            && (nodeStatus == StatusKind.Fail || nodeStatus == StatusKind.Warn)) {
                            style = context.Style(nodeStatus);
                        }
                    }
                    if (cell.Role == TopologyCellRole.EdgeLabel && cell.ElementId != null && cell.Glyph != " " &&
                        _linkStatusKinds.TryGetValue(cell.ElementId, out StatusKind linkStatus) && markedLinks.Add(cell.ElementId)) {
                        glyph = context.Glyphs.StatusSymbol(linkStatus);
                        style = context.Style(linkStatus);
                    }
                    region.Put(column, y, glyph, style, ascii ? 1 : cell.Width);
                }
            }

            // The engine omits markers for Unknown. Paint the core status at its projected node position so
            // Info, Pending, Running and the other StatusKind meanings survive that health-only projection.
            if (_layout != null) foreach (TopologyCellNode node in _layout.Nodes) {
                if (!_statusKinds.TryGetValue(node.Id, out StatusKind status) || _unmarkedNodes.Contains(node.Id)) continue;
                int column = node.Column + node.Width - 2, row = frame.Above.Count + node.Row + 1;
                if (column >= region.Width || row >= region.Height) continue;
                string marker = context.Glyphs.StatusSymbol(status);
                if (DisplayWidth.Measure(marker, context.Profile.WidthProfile) != 1) marker = SymbolFallback.GetGlyph(marker, context.Profile.Glyphs, context.Profile.WidthProfile);
                region.Put(column, row, marker, context.Style(status), 1);
            }

            foreach (Line line in frame.Below) {
                if (y >= region.Height) return;
                int end = region.Write(line.Indent, y, line.Text, context.Style(line.Token), Math.Max(0, region.Width - line.Indent));
                if (line.Click != null && end > line.Indent) Interactive.PointerTargets.RecordAction(new Rect(region.Bounds.X + line.Indent, region.Bounds.Y + y, end - line.Indent, 1), line.Click, line.ClickId!);
                y++;
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
                // Mixed strokes keep highlights distinct from the all-double focused box.
                case "┃": return "│";
                case "┏": return "╒";
                case "┓": return "╕";
                case "┗": return "╘";
                case "┛": return "╛";
                case "┣": return "╞";
                case "┫": return "╡";
                case "┳": return "╤";
                case "┻": return "╧";
                case "╋": return "╪";
                case "▶": return "►";
                case "◀": return "◄";
                case "✕": return "x";
                default: return glyph;
            }
        }
    }
}
