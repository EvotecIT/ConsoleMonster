using System;
using System.Management.Automation;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Prepares a complete registered display or its automatic replacement before layout allocates its height.
    /// The replacement is built on the cmdlet thread, so deferred formatting retains no cmdlet callback.
    /// </summary>
    internal sealed class PSDefaultViewFallback : IWidget {
        private IWidget _active;
        private readonly IWidget _fallback;
        private CellBuffer? _frame;
        private RenderContext? _context;

        internal PSDefaultViewFallback(IWidget primary, IWidget fallback) {
            _active = primary;
            _fallback = fallback;
        }

        public Size Measure(Constraints constraints, RenderContext context) {
            CellBuffer frame = Prepare(constraints.MaxWidth, context);
            return new Size(frame.Width, Math.Min(frame.Height, constraints.MaxHeight));
        }

        public void Render(Region region, RenderContext context) {
            CellBuffer frame = Prepare(region.Width, context);
            region.Buffer.Blit(frame, 0, 0, Math.Min(frame.Width, region.Width), Math.Min(frame.Height, region.Height), region.Bounds.X, region.Bounds.Y);
        }

        private CellBuffer Prepare(int width, RenderContext context) {
            if (_frame != null && _frame.Width == width && ReferenceEquals(_context, context)) { return _frame; }
            try {
                _frame = Terminal.Render(_active, context.Profile.WithSize(width, context.Profile.Height), context.Theme);
            } catch (Exception error) when (!ReferenceEquals(_active, _fallback) && !(error is PipelineStoppedException) && !(error is OperationCanceledException)) {
                _active = _fallback;
                _frame = Terminal.Render(_active, context.Profile.WithSize(width, context.Profile.Height), context.Theme);
            }
            _context = context;
            return _frame;
        }
    }
}
