using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Output {
    /// <summary>
    /// The <see cref="Terminal"/> methods for an explicit target, for libraries that take a target through a constructor or
    /// options instead of writing to the process console: <c>target.Write(table)</c>, <c>target.WriteMarkupLine(...)</c>,
    /// <c>target.Status(...)</c>. Each call runs inside <see cref="Terminal.Use"/> for that target, so nested writes,
    /// prompts and live sessions go to the same place, and nothing global changes.
    /// </summary>
    public static class TerminalTargetExtensions {
        /// <summary>Writes a widget once.</summary>
        /// <param name="target">The target.</param>
        /// <param name="widget">The widget.</param>
        public static void Write(this ITerminalTarget target, IWidget widget) {
            using (Terminal.Use(Check(target))) {
                Terminal.Write(widget);
            }
        }

        /// <summary>Writes markup without ending the line.</summary>
        /// <param name="target">The target.</param>
        /// <param name="markup">The markup.</param>
        public static void WriteMarkup(this ITerminalTarget target, string markup) {
            using (Terminal.Use(Check(target))) {
                Terminal.WriteMarkup(markup);
            }
        }

        /// <summary>Writes one line of markup.</summary>
        /// <param name="target">The target.</param>
        /// <param name="markup">The markup.</param>
        public static void WriteMarkupLine(this ITerminalTarget target, string markup) {
            using (Terminal.Use(Check(target))) {
                Terminal.WriteMarkupLine(markup);
            }
        }

        /// <summary>Writes rich text as lines; with <paramref name="newLine"/> false the last line is left open.</summary>
        /// <param name="target">The target.</param>
        /// <param name="text">The rich text.</param>
        /// <param name="newLine">Whether to end the last line.</param>
        public static void WriteRich(this ITerminalTarget target, RichText text, bool newLine = true) {
            using (Terminal.Use(Check(target))) {
                Terminal.WriteRich(text, newLine);
            }
        }

        /// <summary>Writes styled lines; with <paramref name="newLine"/> false the last line is left open.</summary>
        /// <param name="target">The target.</param>
        /// <param name="lines">The lines.</param>
        /// <param name="newLine">Whether to end the last line.</param>
        public static void WriteLines(this ITerminalTarget target, IReadOnlyList<StyledLine> lines, bool newLine = true) {
            using (Terminal.Use(Check(target))) {
                Terminal.WriteLines(lines, newLine);
            }
        }

        /// <summary>Runs work under a status (see <see cref="Terminal.Status(string, Action{StatusContext})"/>).</summary>
        /// <param name="target">The target.</param>
        /// <param name="markup">The first status text.</param>
        /// <param name="action">The work.</param>
        public static void Status(this ITerminalTarget target, string markup, Action<StatusContext> action) {
            using (Terminal.Use(Check(target))) {
                Terminal.Status(markup, action);
            }
        }

        /// <summary>Runs asynchronous work under a status.</summary>
        /// <param name="target">The target.</param>
        /// <param name="markup">The first status text.</param>
        /// <param name="action">The work.</param>
        /// <returns>A task that completes when the work has finished.</returns>
        public static async Task StatusAsync(this ITerminalTarget target, string markup, Func<StatusContext, Task> action) {
            using (Terminal.Use(Check(target))) {
                await Terminal.StatusAsync(markup, action).ConfigureAwait(false);
            }
        }

        /// <summary>A render context for the target's profile and theme (for measuring or rendering by hand).</summary>
        /// <param name="target">The target.</param>
        /// <returns>The context.</returns>
        public static Canvas.RenderContext CreateContext(this ITerminalTarget target) {
            using (Terminal.Use(Check(target))) {
                return Terminal.CreateContext(Terminal.GetCurrentProfile(), Terminal.Theme);
            }
        }

        private static ITerminalTarget Check(ITerminalTarget target) => target ?? throw new ArgumentNullException(nameof(target));
    }
}
