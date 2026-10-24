using System;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;

namespace ConsoleMonster.Interactive {
    /// <summary>Pages a reading view through the existing console input and prompt surface lifecycle.</summary>
    public static class WidgetPager {
        /// <summary>
        /// Shows a pager only when rich interaction is available and the content exceeds the screen height. Returns
        /// false without acquiring input or emitting output when the caller should print the complete view statically.
        /// </summary>
        /// <param name="content">The document or other reading widget.</param>
        /// <param name="title">The pager title, shown literally.</param>
        /// <param name="options">Input, screen capture, profile and theme; null uses the shared defaults.</param>
        /// <param name="cancellationToken">Cancels the interaction.</param>
        /// <returns>True if a pager ran, including when the reader quit; false if static output is appropriate.</returns>
        public static bool TryShow(IWidget content, string title = "Document", InteractiveOptions? options = null,
            CancellationToken cancellationToken = default) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            cancellationToken.ThrowIfCancellationRequested();
            // First-use capability detection can enable Windows VT output before the surface exists.
            using TerminalGuard outputMode = new TerminalGuard(() => { }, false);
            InteractiveOptions interactive = options ?? InteractiveOptions.Default;
            TerminalProfile profile = interactive.ResolveProfile();
            if (interactive.ResolveMode() != InteractionMode.Rich || profile.IsOutputRedirected || profile.IsContinuousIntegration
                || profile.Mode != OutputMode.Interactive || !profile.SupportsVirtualTerminal || interactive.NonInteractive) {
                return false;
            }
            RenderContext context = Terminal.CreateContext(profile, interactive.ResolveTheme());
            IPromptSurface? surface = interactive.Surface;
            int width = Math.Max(1, surface?.Width ?? profile.Width);
            int height = Math.Max(1, surface?.Height ?? profile.Height);
            if (height < 3 || content.Measure(new Constraints(width), context).Height <= height) {
                return false;
            }
            using (InputLease lease = ConsoleInput.Acquire(interactive.Input)) {
                bool owns = false;
                try {
                    if (surface == null) {
                        surface = PromptHost.Resolve(context) ?? new ScreenSurface(profile);
                        owns = true;
                    }
                    WidgetPagerView view = new WidgetPagerView(content, title, interactive.ResolveLabels());
                    InteractionLoop.Run(view, lease.Source, surface, context, cancellationToken);
                    surface.Complete(null);
                    return true;
                } finally {
                    if (owns) {
                        surface?.Dispose();
                    }
                }
            }
        }
    }
}
