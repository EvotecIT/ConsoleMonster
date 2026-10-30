using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Pipeline adaptation shared by frame export and clipboard copying; core and add-on exporters own serialization.</summary>
    internal static class PSOutputExport {
        internal static ExportFormat CoreFormat(string name) => (ExportFormat)Enum.Parse(typeof(ExportFormat), name, true);

        internal static bool IsMarkdown(string format) => string.Equals(format, "Markdown", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "FencedText", StringComparison.OrdinalIgnoreCase);

        internal static string FormatFor(string? format, string? path = null) {
            if (format != null) { return format; }
            string extension = path == null ? "" : System.IO.Path.GetExtension(path);
            if (string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".markdown", StringComparison.OrdinalIgnoreCase)) { return "Markdown"; }
            return path == null ? "Html" : ConsoleExporter.FormatFor(path).ToString();
        }

        internal static string Export(IWidget widget, string format, ExportOptions options) => IsMarkdown(format)
            ? MarkdownExportBridge.Widget(widget, options, string.Equals(format, "FencedText", StringComparison.OrdinalIgnoreCase))
            : System.Text.Encoding.UTF8.GetString(FrameExporter.Export(widget, format, options));

        /// <summary>Reject competing output modes before input is rendered, including an empty pipeline.</summary>
        internal static void Validate(PSCmdlet cmdlet) {
            int modes = 0;
            foreach (string name in new[] { "AsWidget", "PassThru" }) {
                if (cmdlet.MyInvocation.BoundParameters.TryGetValue(name, out object? value)
                    && LanguagePrimitives.IsTrue(value)) {
                    modes++;
                }
            }
            if (modes > 1) {
                cmdlet.ThrowTerminatingError(new ErrorRecord(new ArgumentException(
                    "Choose AsWidget or PassThru. Export-ConsoleFrame -Format Text returns a text document."),
                    "IncompatibleOutputOptions", ErrorCategory.InvalidArgument, null));
            }
        }

        internal static string Document(IReadOnlyList<object> items, string format, ExportOptions options, PSCmdlet cmdlet) {
            if (items.Count == 1 && Frame(items[0]) is CellBuffer frame) {
                return IsMarkdown(format) ? MarkdownExportBridge.Frame(frame) : System.Text.Encoding.UTF8.GetString(FrameExporter.Export(frame, format, options));
            }
            return Export(Widget(items, cmdlet), format, options);
        }

        internal static IWidget Widget(IReadOnlyList<object> items, PSCmdlet cmdlet, object[]? property = null, string? title = null, int depth = 3) {
            List<IWidget> widgets = new List<IWidget>();
            List<PSObject> rows = new List<PSObject>();
            foreach (object item in items) {
                object raw = PSObject.AsPSObject(item).BaseObject;
                IWidget? widget = raw is IWidget w ? w : Frame(item) is CellBuffer frame ? new RenderedFrame(frame) : null;
                if (widget != null) {
                    Flush(rows, widgets, cmdlet, property, title, depth);
                    widgets.Add(widget);
                } else {
                    rows.Add(PSObject.AsPSObject(item));
                }
            }
            Flush(rows, widgets, cmdlet, property, title, depth);
            return PSFormatting.Combine(widgets);
        }

        private static CellBuffer? Frame(object item) {
            object raw = PSObject.AsPSObject(item).BaseObject;
            return raw is CellBuffer buffer ? buffer : raw is LiveSession session
                ? session.Snapshot() ?? throw new PSInvalidOperationException("The session has not drawn a frame yet.") : null;
        }

        private static void Flush(List<PSObject> rows, List<IWidget> widgets, PSCmdlet cmdlet, object[]? property, string? title, int depth) {
            if (rows.Count == 0) {
                return;
            }
            widgets.Add(PSAutomaticView.Build(new List<PSObject>(rows), cmdlet, property, title, depth));
            rows.Clear();
        }

        private sealed class RenderedFrame : IWidget {
            private readonly CellBuffer _frame;
            internal RenderedFrame(CellBuffer frame) { _frame = frame; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(Math.Min(constraints.MaxWidth, _frame.Width), Math.Min(constraints.MaxHeight, _frame.Height));
            public void Render(Region region, RenderContext context) => region.Buffer.Blit(_frame, 0, 0, Math.Min(region.Width, _frame.Width), Math.Min(region.Height, _frame.Height), region.Bounds.X, region.Bounds.Y);
        }
    }
}
