using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Export {
    /// <summary>
    /// Writes a recording as an animated SVG with CSS keyframes and no script: every distinct row is drawn once in
    /// <c>defs</c>, each frame is a strip of references to its rows, the frames sit side by side, and one stepped keyframe
    /// animation slides the strip so the right frame shows for the right time.
    /// </summary>
    internal static class AnimatedSvgWriter {
        public static string Write(IReadOnlyList<RecordedFrame> frames, RecordingExportOptions recording) {
            ExportOptions options = recording.ToFrameOptions();
            ExportPalette palette = recording.Palette;
            SvgGeometry g = SvgGeometry.For(recording.Width, recording.Height, options);
            SvgStyles styles = new SvgStyles();
            Dictionary<string, string> rowIds = new Dictionary<string, string>(StringComparer.Ordinal);
            StringBuilder defs = new StringBuilder();
            StringBuilder strip = new StringBuilder();
            double frameWidth = g.Width;
            for (int f = 0; f < frames.Count; f++) {
                CellBuffer frame = frames[f].Frame;
                strip.Append("<g transform=\"translate(").Append(ExportText.Num(f * frameWidth)).Append(" 0)\">");
                for (int y = 0; y < frame.Height; y++) {
                    StringBuilder row = new StringBuilder();
                    SvgFrameWriter.AppendRow(row, frame, y, 0, palette, styles, "s", options);
                    if (row.Length == 0) {
                        continue;
                    }
                    string markup = row.ToString();
                    if (!rowIds.TryGetValue(markup, out string? id)) {
                        id = "r" + rowIds.Count.ToString(CultureInfo.InvariantCulture);
                        rowIds.Add(markup, id);
                        defs.Append("<g id=\"").Append(id).Append("\">").Append(markup).Append("</g>\n");
                    }
                    strip.Append("<use href=\"#").Append(id).Append("\" xlink:href=\"#").Append(id).Append("\" y=\"").Append(ExportText.Num(y * options.CellHeight + options.Padding)).Append("\" x=\"").Append(options.Padding).Append("\"/>");
                }
                strip.Append("</g>\n");
            }

            long total = 0;
            foreach (RecordedFrame frame in frames) {
                total = Math.Max(total, frame.Start + frame.Duration);
            }
            total = Math.Max(1, total);
            StringBuilder keyframes = new StringBuilder("@keyframes cm-play{");
            for (int f = 0; f < frames.Count; f++) {
                decimal percent = 100m * frames[f].Start / total;
                keyframes.Append(percent.ToString("0.####################", CultureInfo.InvariantCulture)).Append("%{transform:translateX(").Append(ExportText.Num(-f * frameWidth)).Append("px)}");
            }
            keyframes.Append("100%{transform:translateX(").Append(ExportText.Num(-(frames.Count - 1) * frameWidth)).Append("px)}}\n");

            StringBuilder svg = new StringBuilder(defs.Length + strip.Length + 4096);
            svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"").Append(ExportText.Num(g.Width)).Append("\" height=\"").Append(ExportText.Num(g.Height))
                .Append("\" viewBox=\"0 0 ").Append(ExportText.Num(g.Width)).Append(' ').Append(ExportText.Num(g.Height)).Append("\" role=\"img\"");
            if (!string.IsNullOrEmpty(options.Title)) {
                svg.Append(" aria-label=\"");
                ExportText.AppendEscaped(svg, options.Title);
                svg.Append('"');
            }
            svg.Append(">\n");
            if (!string.IsNullOrEmpty(options.Title)) {
                svg.Append("<title>");
                ExportText.AppendEscaped(svg, options.Title);
                svg.Append("</title>\n");
            }
            svg.Append("<style>\n");
            List<ExportLayer> layer = new List<ExportLayer> { new ExportLayer(frames[0].Frame, palette, null) };
            SvgFrameWriter.AppendCss(svg, layer, options);
            styles.AppendCss(svg);
            svg.Append(keyframes);
            svg.Append(".cm-strip{animation:cm-play ").Append((total / 1000.0).ToString("0.###", CultureInfo.InvariantCulture)).Append("s steps(1,end) ")
                .Append(recording.Loop ? "infinite" : "1 forwards").Append("}\n");
            svg.Append("@media (prefers-reduced-motion: reduce){.cm-strip{animation-play-state:paused}}\n");
            svg.Append("</style>\n<defs>\n").Append(defs).Append("</defs>\n");
            SvgFrameWriter.AppendChrome(svg, g, options);
            if (options.EffectiveChrome != WindowChromeStyle.None) { svg.Append("<g clip-path=\"url(#cm-inner-clip)\">"); }
            svg.Append("<svg x=\"").Append(ExportText.Num(g.ScreenX - options.Padding)).Append("\" y=\"").Append(ExportText.Num(g.ScreenY - options.Padding)).Append("\" width=\"").Append(ExportText.Num(g.Width - 2 * (g.ScreenX - options.Padding))).Append("\" height=\"").Append(ExportText.Num(g.Height - WindowChrome.BarHeight(options.EffectiveChrome) - 2 * (g.ScreenX - options.Padding))).Append("\">");
            svg.Append("<rect width=\"100%\" height=\"100%\"");
            ExportText.AppendPaint(svg, "fill", palette.Background);
            svg.Append("/>\n<g class=\"cm-strip\">\n").Append(strip).Append("</g></svg>");
            if (options.EffectiveChrome != WindowChromeStyle.None) { svg.Append("</g>"); }
            svg.Append("\n</svg>\n");
            return svg.ToString();
        }
    }
}
