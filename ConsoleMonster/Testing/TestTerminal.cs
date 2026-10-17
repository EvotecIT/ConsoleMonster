using System;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Testing {
    /// <summary>
    /// Helpers for testing console output in any test framework: render a widget at a fixed size, glyph tier, colour depth
    /// and culture (so results do not depend on the machine), drive prompts, menus and forms with scripted keys, and capture
    /// the screens they draw. Pair with <see cref="GoldenFile"/> for snapshot tests.
    /// </summary>
    public static class TestTerminal {
        /// <summary>A fixed terminal profile: no detection, the given size, tier, depth and culture (invariant by default).</summary>
        /// <param name="width">Width in cells.</param>
        /// <param name="height">Height in cells.</param>
        /// <param name="glyphs">The glyph tier.</param>
        /// <param name="colors">The colour depth (none by default, so text is plain).</param>
        /// <param name="culture">The culture; null for the invariant culture.</param>
        /// <returns>The profile.</returns>
        public static TerminalProfile Profile(int width = 80, int height = 24, GlyphTier glyphs = GlyphTier.Full, ColorDepth colors = ColorDepth.None, CultureInfo? culture = null) {
            CultureInfo fixedCulture = culture ?? CultureInfo.InvariantCulture;
            return TerminalProfile.Fixed(width, height, colors, glyphs).WithCulture(fixedCulture).WithLanguage(fixedCulture);
        }

        /// <summary>
        /// An in-memory target with a fixed profile (append-only output, no detection): pass it to code that takes an
        /// <see cref="ITerminalTarget"/>, or scope the static <see cref="Terminal"/> methods to it with
        /// <see cref="Terminal.Use"/>. Its <see cref="RecordingTarget.Text"/> goes to <see cref="GoldenFile"/> through
        /// <see cref="RecordingTarget.MatchGolden"/>; scripted keys answer prompts, which draw into
        /// <see cref="RecordingTarget.Screens"/>.
        /// </summary>
        /// <param name="width">Width in cells.</param>
        /// <param name="height">Height in cells (the screen size for prompts).</param>
        /// <param name="glyphs">The glyph tier.</param>
        /// <param name="colors">The colour depth (none by default, so the text is plain).</param>
        /// <param name="culture">The culture for numbers, dates and labels; null for the invariant culture (English labels).</param>
        /// <param name="keys">Scripted keys for prompts (see <see cref="Keys"/>), or null so prompts use their defaults.</param>
        /// <returns>The target.</returns>
        public static RecordingTarget Target(int width = 80, int height = 24, GlyphTier glyphs = GlyphTier.Full, ColorDepth colors = ColorDepth.None, CultureInfo? culture = null, string? keys = null) {
            return new RecordingTarget(Profile(width, height, glyphs, colors, culture).WithMode(OutputMode.Stream), keys);
        }

        /// <summary>Renders a widget into cells: as tall as it measures, or exactly <paramref name="height"/> rows.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="width">Width in cells.</param>
        /// <param name="height">Height in cells; 0 measures the widget.</param>
        /// <param name="glyphs">The glyph tier.</param>
        /// <param name="culture">The culture; null for the invariant culture.</param>
        /// <param name="theme">The theme; null for the dark theme.</param>
        /// <returns>The cells.</returns>
        public static CellBuffer Render(IWidget widget, int width = 80, int height = 0, GlyphTier glyphs = GlyphTier.Full, CultureInfo? culture = null, ITheme? theme = null) {
            if (widget == null) {
                throw new ArgumentNullException(nameof(widget));
            }
            RenderContext context = Terminal.CreateContext(Profile(width, height > 0 ? height : 24, glyphs, ColorDepth.None, culture), theme ?? Theme.Dark);
            return height > 0 ? FrameComposer.Compose(widget, width, height, context) : FrameComposer.RenderBlock(widget, width, context);
        }

        /// <summary>Renders a widget to plain text: rows joined with <c>\n</c>, trailing spaces trimmed.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="width">Width in cells.</param>
        /// <param name="height">Height in cells; 0 measures the widget.</param>
        /// <param name="glyphs">The glyph tier.</param>
        /// <param name="culture">The culture; null for the invariant culture.</param>
        /// <returns>The text.</returns>
        public static string RenderText(IWidget widget, int width = 80, int height = 0, GlyphTier glyphs = GlyphTier.Full, CultureInfo? culture = null) {
            return Render(widget, width, height, glyphs, culture).ToPlainText();
        }

        /// <summary>
        /// Scripted keys for prompts and menus, for example <c>"Down Down Space Enter"</c>, <c>"\"typed text\" Tab"</c>,
        /// <c>"Ctrl+K paste:'x' wait:200"</c>. Paced, so every step draws a frame.
        /// </summary>
        /// <param name="script">The keys.</param>
        /// <returns>The input source.</returns>
        public static ScriptedInput Keys(string script) => new ScriptedInput(script) { Paced = true };

        /// <summary>
        /// Options that run a prompt, menu or form against scripted keys and draw into <paramref name="capture"/> (rich mode,
        /// dark theme, no colour, invariant culture, the capture's size).
        /// </summary>
        /// <param name="keys">The keys (see <see cref="Keys"/>).</param>
        /// <param name="capture">Where the screens go.</param>
        /// <param name="glyphs">The glyph tier.</param>
        /// <returns>The options.</returns>
        public static InteractiveOptions Interactive(string keys, ScreenCapture capture, GlyphTier glyphs = GlyphTier.Full) {
            if (capture == null) {
                throw new ArgumentNullException(nameof(capture));
            }
            return new InteractiveOptions {
                Mode = InteractionMode.Rich,
                Input = Keys(keys),
                Capture = capture,
                Theme = Theme.Dark,
                Profile = Profile(capture.Width, capture.Height, glyphs),
                Labels = new PromptLabels()
            };
        }
    }
}
