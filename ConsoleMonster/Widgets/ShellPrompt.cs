using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>A compact shell prompt. The host supplies status, history duration and optional cached git data.</summary>
    public sealed class ShellPrompt : IWidget {
        /// <summary>The current provider path.</summary>
        public string Path { get; set; } = string.Empty;
        /// <summary>The home directory, abbreviated only at a complete path boundary.</summary>
        public string? Home { get; set; }
        /// <summary>The branch name, or a detached commit identifier; null omits git information.</summary>
        public string? Branch { get; set; }
        /// <summary>Whether git reported working tree or index changes.</summary>
        public bool Dirty { get; set; }
        /// <summary>Whether PowerShell reported the last command as successful.</summary>
        public bool Succeeded { get; set; } = true;
        /// <summary>The last completed command's duration. Only values strictly above two seconds are shown.</summary>
        public TimeSpan Duration { get; set; }

        /// <summary>Returns the prompt as terminal text without writing or changing terminal state.</summary>
        /// <param name="context">The active theme, glyphs and terminal capabilities.</param>
        /// <param name="width">Available terminal cells, including a trailing input space.</param>
        /// <returns>One prompt line, with SGR only when colour is supported.</returns>
        public string ToAnsi(RenderContext context, int width) {
            StyledLine line = Build(context, width);
            StringBuilder result = new StringBuilder();
            foreach (TextSpan span in line) {
                SgrEncoder.Append(result, span.Style, SgrEncoder.DepthFor(context.Profile), context.Profile);
                result.Append(span.Text);
            }
            SgrEncoder.AppendReset(result, context.Profile);
            return result.ToString();
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => new Size(Build(context, constraints.MaxWidth).Width, 1);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => region.Write(0, 0, Build(context, region.Width));

        private StyledLine Build(RenderContext context, int width) {
            width = Math.Max(1, width);
            StatusKind status = Succeeded ? StatusKind.Pass : StatusKind.Fail;
            string tail = " " + context.Glyphs.StatusMarker(status) + " > ";
            string duration = Duration.TotalSeconds > 2 ? " " + Duration.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + "s" : string.Empty;
            string branch = string.IsNullOrEmpty(Branch) ? string.Empty : " (" + Clean(Branch!) + (Dirty ? "*" : string.Empty) + ")";
            // Keep the status and input cursor visible; long branches and paths share the remaining cells.
            int budget = Math.Max(0, width - Cells(tail, context) - Cells(duration, context));
            if (Cells(branch, context) > budget / 2) { branch = Shorten(branch, budget / 2, context); }
            string path = Clean(Path);
            string home = Home?.TrimEnd('/', '\\') ?? string.Empty;
            StringComparison comparison = path.IndexOf('\\') >= 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (home.Length > 0 && path.StartsWith(home, comparison)
                && (path.Length == home.Length || path[home.Length] == '/' || path[home.Length] == '\\')) {
                path = "~" + path.Substring(home.Length);
            }
            path = DisplayPath.Fit(path, Math.Max(0, budget - Cells(branch, context)), context.Profile.WidthProfile, context.Ellipsis);
            List<TextSpan> spans = new List<TextSpan> {
                new TextSpan(path, context.Style(ThemeToken.Accent)),
                new TextSpan(branch + duration, context.Style(ThemeToken.Muted)),
                new TextSpan(tail, context.Style(status))
            };
            StyledLine line = new StyledLine(spans);
            return line.Width <= width ? line : TextWrapper.Truncate(line, width, context.Profile.WidthProfile, context.Ellipsis);
        }

        private static int Cells(string value, RenderContext context) => DisplayWidth.Measure(value, context.Profile.WidthProfile);
        private static string Shorten(string value, int width, RenderContext context) => width <= 0 ? string.Empty
            : TextWrapper.TruncateMiddle(value, width, context.Profile.WidthProfile, context.Ellipsis);

        // Provider paths and branch names are data, never markup or terminal control sequences.
        private static string Clean(string value) {
            StringBuilder result = new StringBuilder(value.Length);
            foreach (char c in value) { if (!char.IsControl(c) && c != '\u2028' && c != '\u2029') { result.Append(c); } }
            return result.ToString();
        }
    }
}
