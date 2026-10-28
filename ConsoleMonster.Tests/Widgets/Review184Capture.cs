using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;

namespace ConsoleMonster.Tests.Widgets {
    internal sealed class Review184Capture {
        internal Review184Capture(IWidget widget, int width, GlyphTier tier, ColorDepth depth) {
            var profile = TestTerminal.Profile(width, glyphs: tier, colors: depth).WithMode(depth == ColorDepth.None ? OutputMode.Plain : OutputMode.Stream);
            Context = Terminal.CreateContext(profile, Theme.Dark);
            Raw = FrameComposer.RenderBlock(widget, width, Context);
            StringTerminalWriter writer = new StringTerminalWriter();
            using (LineOutput output = new LineOutput(writer, profile, false)) output.WriteBlock(Raw);
            Encoded = writer.ToString();
            Decoded = Regex.Replace(Encoded, @"\x1b\[[0-?]*[ -/]*[@-~]", string.Empty).Replace("\r\n", "\n").TrimEnd('\n');
        }

        internal RenderContext Context { get; }
        internal CellBuffer Raw { get; }
        internal string Encoded { get; }
        internal string Decoded { get; }
    }
}
