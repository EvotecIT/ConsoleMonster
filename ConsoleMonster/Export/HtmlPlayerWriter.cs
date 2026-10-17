using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Export {
    /// <summary>
    /// Writes a recording as a self-contained HTML player: the distinct rows as HTML strings, each frame as a list of row
    /// numbers with its start time, and a small inline script with play/pause, speed and a scrubber. No external requests.
    /// </summary>
    internal static class HtmlPlayerWriter {
        public static string Write(IReadOnlyList<RecordedFrame> frames, RecordingExportOptions recording) {
            ExportOptions options = recording.ToFrameOptions();
            ExportPalette palette = recording.Palette;
            HtmlStyles styles = new HtmlStyles();
            Dictionary<string, int> rowIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            List<string> rows = new List<string>();
            StringBuilder frameData = new StringBuilder("[");
            long total = 0;
            for (int f = 0; f < frames.Count; f++) {
                CellBuffer frame = frames[f].Frame;
                if (f > 0) {
                    frameData.Append(',');
                }
                frameData.Append('[').Append(frames[f].Start.ToString(CultureInfo.InvariantCulture)).Append(",[");
                for (int y = 0; y < frame.Height; y++) {
                    StringBuilder row = new StringBuilder();
                    HtmlFrameWriter.AppendRow(row, frame, y, palette, styles, "s");
                    string rowHtml = row.ToString();
                    if (!rowIndex.TryGetValue(rowHtml, out int index)) {
                        index = rows.Count;
                        rowIndex.Add(rowHtml, index);
                        rows.Add(rowHtml);
                    }
                    if (y > 0) {
                        frameData.Append(',');
                    }
                    frameData.Append(index.ToString(CultureInfo.InvariantCulture));
                }
                frameData.Append("]]");
                total = Math.Max(total, frames[f].Start + frames[f].Duration);
            }
            frameData.Append(']');

            StringBuilder rowData = new StringBuilder("[");
            for (int i = 0; i < rows.Count; i++) {
                if (i > 0) {
                    rowData.Append(',');
                }
                AppendScriptString(rowData, rows[i]);
            }
            rowData.Append(']');

            List<ExportLayer> layers = new List<ExportLayer> { new ExportLayer(frames[0].Frame, palette, null) };
            StringBuilder css = new StringBuilder();
            HtmlFrameWriter.AppendCss(css, layers, options, recording.Width);
            styles.AppendCss(css);
            css.Append(".cm-export .cm-controls{display:flex;align-items:center;gap:10px;padding:6px ").Append(options.Padding).Append("px 10px;font:12px system-ui,-apple-system,'Segoe UI',sans-serif;color:var(--cm-chrome-fg);background:var(--cm-bg)}\n");
            css.Append(".cm-export .cm-controls button{font:inherit;min-width:64px;padding:3px 10px;border-radius:6px;border:1px solid var(--cm-chrome-fg);background:transparent;color:inherit;cursor:pointer}\n");
            css.Append(".cm-export .cm-controls input[type=range]{flex:1;min-width:80px}\n.cm-export .cm-controls select{font:inherit;background:transparent;color:inherit;border:1px solid var(--cm-chrome-fg);border-radius:6px}\n");
            css.Append(".cm-export .cm-controls option{color:#000}\n.cm-export .cm-time{font-variant-numeric:tabular-nums;min-width:90px;text-align:right}\n");

            StringBuilder html = new StringBuilder();
            html.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<meta name=\"color-scheme\" content=\"").Append(palette.Name).Append("\">\n<meta name=\"generator\" content=\"ConsoleMonster\">\n<title>");
            ExportText.AppendEscaped(html, string.IsNullOrEmpty(options.Title) ? "Console recording" : options.Title);
            html.Append("</title>\n<style>\n").Append(css).Append("</style>\n</head>\n<body>\n<div class=\"cm-export\">");
            if (options.WindowChrome) {
                html.Append("<div class=\"cm-window\"><div class=\"cm-titlebar\"><span class=\"cm-dots\"><i></i><i></i><i></i></span><span class=\"cm-title\">");
                ExportText.AppendEscaped(html, options.Title ?? string.Empty);
                html.Append("</span></div>");
            } else {
                html.Append("<div class=\"cm-player\">");
            }
            html.Append("<pre class=\"cm-screen\" id=\"cm-screen\" aria-live=\"off\"></pre>");
            html.Append("<div class=\"cm-controls\"><button type=\"button\" id=\"cm-play\">Play</button><input type=\"range\" id=\"cm-seek\" min=\"0\" max=\"").Append(total.ToString(CultureInfo.InvariantCulture))
                .Append("\" value=\"0\" step=\"10\" aria-label=\"Position\"><span class=\"cm-time\" id=\"cm-time\"></span><select id=\"cm-speed\" aria-label=\"Speed\"><option value=\"0.5\">0.5×</option><option value=\"1\" selected>1×</option><option value=\"2\">2×</option><option value=\"4\">4×</option></select></div>");
            html.Append("</div></div>\n<script>\n(function(){\n");
            html.Append("var R=").Append(rowData).Append(";\nvar F=").Append(frameData).Append(";\nvar T=").Append(total.ToString(CultureInfo.InvariantCulture)).Append(";\n");
            html.Append(Script);
            html.Append("})();\n</script>\n</body>\n</html>\n");
            return html.ToString();
        }

        private const string Script =
            "var screen=document.getElementById('cm-screen'),play=document.getElementById('cm-play'),seek=document.getElementById('cm-seek'),time=document.getElementById('cm-time'),speed=document.getElementById('cm-speed');\n" +
            "var t=0,shown=-1,playing=false,last=0;\n" +
            "function fmt(ms){var s=Math.floor(ms/1000);return Math.floor(s/60)+':'+('0'+s%60).slice(-2);}\n" +
            "function at(ms){var lo=0,hi=F.length-1;while(lo<hi){var mid=(lo+hi+1)>>1;if(F[mid][0]<=ms){lo=mid;}else{hi=mid-1;}}return lo;}\n" +
            "function draw(){var i=at(t);if(i!==shown){shown=i;var rows=F[i][1],out=new Array(rows.length);for(var r=0;r<rows.length;r++){out[r]=R[rows[r]];}screen.innerHTML=out.join('\\n');}seek.value=Math.round(t);time.textContent=fmt(t)+' / '+fmt(T);}\n" +
            "function tick(now){if(!playing){return;}var dt=now-last;last=now;t=Math.min(T,t+dt*parseFloat(speed.value));draw();if(t>=T){playing=false;play.textContent='Play';return;}requestAnimationFrame(tick);}\n" +
            "function start(){if(t>=T){t=0;}playing=true;play.textContent='Pause';last=performance.now();requestAnimationFrame(tick);}\n" +
            "play.addEventListener('click',function(){if(playing){playing=false;play.textContent='Play';}else{start();}});\n" +
            "seek.addEventListener('input',function(){t=parseFloat(seek.value)||0;draw();});\n" +
            "document.addEventListener('keydown',function(e){if(e.target===seek||e.target===speed){return;}if(e.key===' '){e.preventDefault();play.click();}});\n" +
            "var m=/[#&]t=(\\d+)/.exec(location.hash);if(m){t=Math.min(T,parseInt(m[1],10));draw();}else{draw();start();}\n";

        // A JSON string that is also safe inside a script element: <, > and & are escaped, so no row can close the script.
        private static void AppendScriptString(StringBuilder sb, string value) {
            StringBuilder json = new StringBuilder();
            ExportText.AppendJson(json, value);
            sb.Append(json.ToString().Replace("<", "\\u003c").Replace(">", "\\u003e").Replace("&", "\\u0026").Replace("\u2028", "\\u2028").Replace("\u2029", "\\u2029"));
        }
    }
}
