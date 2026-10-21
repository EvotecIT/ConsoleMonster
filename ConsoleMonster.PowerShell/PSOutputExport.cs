using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Pipeline adaptation shared by frame export and clipboard copying; ConsoleExporter owns all serialization.</summary>
    internal static class PSOutputExport {
        internal static ExportFormat Format(string name) => (ExportFormat)Enum.Parse(typeof(ExportFormat), name, true);

        /// <summary>Reject competing output modes before input is rendered, including an empty pipeline.</summary>
        internal static void Validate(PSCmdlet cmdlet) {
            int modes = 0;
            foreach (string name in new[] { "As", "AsString", "AsWidget", "PassThru" }) {
                if (cmdlet.MyInvocation.BoundParameters.TryGetValue(name, out object? value)
                    && (name == "As" ? value != null : LanguagePrimitives.IsTrue(value))) {
                    modes++;
                }
            }
            if (modes > 1) {
                bool originalConflict = cmdlet.MyInvocation.BoundParameters.ContainsKey("AsString") && cmdlet.MyInvocation.BoundParameters.ContainsKey("PassThru");
                cmdlet.ThrowTerminatingError(new ErrorRecord(new ArgumentException(
                    "Choose one output mode: -As, -AsString, -AsWidget or -PassThru. Exported documents cannot be mixed with widgets or input objects."),
                    originalConflict ? "AsStringWithPassThru" : "IncompatibleOutputOptions", ErrorCategory.InvalidArgument, null));
            }
        }

        internal static string Document(IReadOnlyList<object> items, ExportFormat format, ExportOptions options, PSCmdlet cmdlet) {
            if (items.Count == 1 && Frame(items[0]) is CellBuffer frame) {
                return ConsoleExporter.Export(frame, format, options);
            }
            return ConsoleExporter.Export(Widget(items, cmdlet), format, options);
        }

        internal static IWidget Widget(IReadOnlyList<object> items, PSCmdlet cmdlet) {
            List<IWidget> widgets = new List<IWidget>();
            List<PSObject> rows = new List<PSObject>();
            foreach (object item in items) {
                object raw = PSObject.AsPSObject(item).BaseObject;
                IWidget? widget = raw is IWidget w ? w : Frame(item) is CellBuffer frame ? new RenderedFrame(frame) : null;
                if (widget != null || raw is string) {
                    Flush(rows, widgets, cmdlet);
                    widgets.Add(widget ?? new TextBlock((string)raw) { IsMarkup = false });
                } else {
                    rows.Add(PSObject.AsPSObject(item));
                }
            }
            Flush(rows, widgets, cmdlet);
            return PSFormatting.Combine(widgets);
        }

        private static CellBuffer? Frame(object item) {
            object raw = PSObject.AsPSObject(item).BaseObject;
            return raw is CellBuffer buffer ? buffer : raw is LiveSession session
                ? session.Snapshot() ?? throw new PSInvalidOperationException("The session has not drawn a frame yet.") : null;
        }

        private static void Flush(List<PSObject> rows, List<IWidget> widgets, PSCmdlet cmdlet) {
            if (rows.Count == 0) {
                return;
            }
            widgets.AddRange(PSFormatting.BuildTables(new List<PSObject>(rows), null, null, new TableBuildOptions { Cmdlet = cmdlet }));
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
