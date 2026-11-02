using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// How log levels look in ConsoleMonster (the log region, Write-ConsoleLogEntry, the logging provider of
    /// ConsoleMonster.Extensions.Logging): the tag word with a status marker when glyphs allow ("✗ ERR", "▴ WARN",
    /// "✓ OK"), its style, and which levels are quiet (verbose and debug text is muted).
    /// </summary>
    public static class LogTags {
        /// <summary>The level as one of <see cref="LogLevels.Names"/> (info, warn, err, verbose, debug, pass, skip); unknown words are info.</summary>
        /// <param name="level">A level name or one of PowerShell's words.</param>
        /// <returns>The normalized level.</returns>
        public static string Normalize(string? level) {
            LogLevels.TryNormalize(level, out string normalized);
            return normalized;
        }

        /// <summary>Whether text at a level is shown muted (verbose and debug).</summary>
        /// <param name="level">A normalized level.</param>
        /// <returns>True for quiet levels.</returns>
        public static bool IsQuiet(string level) => level == "verbose" || level == "debug";

        /// <summary>The level column's text: the tag word, with the status marker in front when the context's glyphs allow; empty for info.</summary>
        /// <param name="level">A normalized level.</param>
        /// <param name="context">The render context, or null for the word alone (narrated lines).</param>
        /// <returns>The tag.</returns>
        public static string Tag(string level, RenderContext? context = null) {
            string word;
            StatusKind? status = null;
            switch (level) {
                case "err":
                    word = "ERR";
                    status = StatusKind.Fail;
                    break;
                case "warn":
                    word = "WARN";
                    status = StatusKind.Warn;
                    break;
                case "verbose":
                    word = "VERB";
                    break;
                case "debug":
                    word = "DBG";
                    break;
                case "pass":
                    word = "OK";
                    status = StatusKind.Pass;
                    break;
                case "skip":
                    word = "SKIP";
                    status = StatusKind.Skip;
                    break;
                default:
                    return string.Empty;
            }
            if (context == null || status == null || context.Glyphs.Tier == Capabilities.GlyphTier.Ascii) {
                return word;
            }
            return context.Glyphs.StatusMarker(status.Value) + " " + word;
        }

        /// <summary>The tag's style: failure, warning or pass colours; muted for the rest.</summary>
        /// <param name="level">A normalized level.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The style.</returns>
        public static Style Style(string level, RenderContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            switch (level) {
                case "err":
                    return context.Style(ThemeToken.Fail);
                case "warn":
                    return context.Style(ThemeToken.Warn);
                case "pass":
                    return context.Style(ThemeToken.Pass);
                default:
                    return context.Style(ThemeToken.Muted);
            }
        }
    }
}
