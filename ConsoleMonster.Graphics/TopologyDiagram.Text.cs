using System;
using System.Collections.Generic;
using ChartForgeX.Topology;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;

namespace ConsoleMonster.Graphics {
    internal sealed partial class TopologyDiagram {
        /// <summary>Lets the engine measure expanding ASCII text before projecting its detached cell geometry.</summary>
        private PreparedTopology PrepareAscii() {
            var restore = new List<Action>();
            try {
                foreach (TopologyNode node in _chart.Nodes) {
                    string label = SymbolFallback.Constrain(node.Label, GlyphTier.Ascii);
                    if (label != node.Label) {
                        var original = node.LabelContent;
                        restore.Add(() => node.LabelContent = original);
                        node.Label = label;
                    }
                    if (node.Subtitle != null) {
                        string original = node.Subtitle;
                        string subtitle = SymbolFallback.Constrain(original, GlyphTier.Ascii);
                        if (subtitle != original) { restore.Add(() => node.Subtitle = original); node.Subtitle = subtitle; }
                    }
                }
                foreach (TopologyGroup group in _chart.Groups) {
                    string label = SymbolFallback.Constrain(group.Label, GlyphTier.Ascii);
                    if (label == group.Label) continue;
                    var original = group.LabelContent;
                    restore.Add(() => group.LabelContent = original);
                    group.Label = label;
                }
                foreach (TopologyEdge edge in _chart.Edges) {
                    if (edge.Label == null) continue;
                    string label = SymbolFallback.Constrain(edge.Label, GlyphTier.Ascii);
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
