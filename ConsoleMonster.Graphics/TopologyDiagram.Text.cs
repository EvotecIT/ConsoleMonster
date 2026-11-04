using System;
using System.Collections.Generic;
using ChartForgeX.Topology;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;

namespace ConsoleMonster.Graphics {
    internal sealed partial class TopologyDiagram {
        /// <summary>Lets the engine measure tier-specific text and status cues before projecting detached geometry.</summary>
        private PreparedTopology PrepareText(GlyphTier tier) {
            string Constrain(string text) => tier == GlyphTier.Ascii ? SymbolFallback.Constrain(text, tier) : text;
            var restore = new List<Action>();
            try {
                foreach (TopologyNode node in _chart.Nodes) {
                    string label = Constrain(node.Label);
                    if (label != node.Label) {
                        var original = node.LabelContent;
                        restore.Add(() => node.LabelContent = original);
                        node.Label = label;
                    }
                    if (node.Subtitle != null) {
                        string original = node.Subtitle;
                        string subtitle = Constrain(original);
                        if (subtitle != original) { restore.Add(() => node.Subtitle = original); node.Subtitle = subtitle; }
                    }
                }
                foreach (TopologyGroup group in _chart.Groups) {
                    string label = Constrain(group.Label);
                    if (label == group.Label) continue;
                    var original = group.LabelContent;
                    restore.Add(() => group.LabelContent = original);
                    group.Label = label;
                }
                foreach (TopologyEdge edge in _chart.Edges) {
                    string? label = edge.Label == null ? null : Constrain(edge.Label);
                    if (_linkStatusKinds.TryGetValue(edge.Id, out var status)) {
                        IGlyphSet glyphs = tier == GlyphTier.Ascii ? GlyphSet.Ascii : GlyphSet.Full;
                        label = glyphs.StatusSymbol(status) + (string.IsNullOrEmpty(label) ? string.Empty : " " + label);
                    }
                    if (label == edge.Label) continue;
                    var original = edge.LabelContent;
                    restore.Add(() => edge.LabelContent = original);
                    edge.Label = label;
                }
                // Prepare owns cloning and geometry. Restore exact immutable rich captions, including on errors.
                return _chart.Prepare();
            } finally {
                for (int index = restore.Count - 1; index >= 0; index--) restore[index]();
            }
        }
    }
}
