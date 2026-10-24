using System;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>A compact completion line with a status, literal item and target, and a fixed right-aligned duration column.</summary>
    public sealed class RunCompletion : IWidget {
        private readonly RunItemSnapshot _item;
        /// <summary>Creates a line over an immutable completed item.</summary>
        /// <param name="item">The completion to display. Full detail remains in the run snapshot and summary.</param>
        public RunCompletion(RunItemSnapshot item) { _item = item ?? throw new ArgumentNullException(nameof(item)); }
        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));
        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Width == 0 || region.Height == 0) { return; }
            Style muted = context.Style(ThemeToken.Muted);
            Style state = context.Style(_item.State.ToToken());
            string tag = context.Glyphs.StatusSymbol(_item.State);
            bool plain = context.Depth == ColorDepth.None || context.Profile.Mode == OutputMode.Plain;
            if (plain) {
                tag += " " + (_item.State == StatusKind.Pass ? "OK" : _item.State == StatusKind.Fail ? "ERR" : _item.State == StatusKind.Warn ? "WARN" : "SKIP");
            }
            string duration = NumberFormat.Duration(_item.Duration);
            int durationWidth = Math.Min(region.Width, Math.Max(7, DisplayWidth.Measure(duration)));
            int bodyWidth = Math.Max(0, region.Width - durationWidth - 2);
            int prefixWidth = plain ? 8 : 2;
            region.Write(0, 0, TextWrapper.Truncate(tag + " ", Math.Min(prefixWidth, bodyWidth), context.Ellipsis), state);
            int x = prefixWidth;
            int available = Math.Max(0, bodyWidth - x);
            int nameWidth = Math.Min(40, Math.Max(0, available * 11 / 20));
            int targetWidth = Math.Min(18, Math.Max(0, available / 5));
            string target = OneLine(_item.Item.Target);
            region.Write(x, 0, TextWrapper.Truncate(OneLine(_item.Item.Name), nameWidth, context.Ellipsis), _item.State == StatusKind.Fail || _item.State == StatusKind.Warn ? state : context.Style(ThemeToken.Text));
            x += nameWidth + 3;
            if (target.Length > 0) {
                region.Write(x, 0, TextWrapper.Truncate(target, targetWidth, context.Ellipsis), muted);
                x += targetWidth + 3;
            }
            if (_item.Message.Length > 0) {
                region.Write(x, 0, TextWrapper.Truncate(OneLine(_item.Message), Math.Max(0, bodyWidth - x), context.Ellipsis), muted);
            }
            duration = TextWrapper.Truncate(duration, durationWidth, context.Ellipsis);
            region.Write(region.Width - DisplayWidth.Measure(duration), 0, TextWrapper.Truncate(duration, durationWidth, context.Ellipsis), muted);
        }
        private static string OneLine(string value) => value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
    }
}
