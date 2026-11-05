using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    public sealed partial class BehaviourContractTests {
        [Theory]
        [InlineData("area")]
        [InlineData("area burst")]
        [InlineData("line")]
        public void EditableProjection_UsesDisplayCellsAndPreservesOriginalPositions(string entry) {
            var context = new RenderContext(TerminalProfile.Fixed(20, 6, ColorDepth.TrueColor, GlyphTier.Ascii), Theme.Dark);
            var caret = new CaretSlot();
            if (entry.StartsWith("area", StringComparison.Ordinal)) {
                var editor = new TextAreaEditor("ßabc");
                var area = new TextArea(editor, caret) { Height = 3 };
                CellBuffer frame = FrameComposer.Compose(area, 6, 3, context); // Four cells after the leading bar.
                Assert.Contains("ssab", frame.GetRowText(0));
                Assert.Contains("c", frame.GetRowText(1));
                Assert.Equal(new[] { 0, 3 }, editor.Rows[0]);
                Assert.Equal(new[] { 3, 4 }, editor.Rows[1]);
                Assert.Equal(3, caret.Caret!.Value.X);
                Assert.Equal(1, caret.Caret.Value.Y);
                if (entry == "area burst") {
                    // Prompts/dialogs also dispatch straight to the editor between drawn frames.
                    editor.Text = string.Empty;
                    Assert.True(editor.Handle(InputEvent.Paste("ßabc")));
                    Assert.True(editor.Handle(InputEvent.Press(ConsoleKey.UpArrow)));
                    Assert.Equal(0, editor.Caret);
                    Assert.True(editor.Handle(InputEvent.Press(ConsoleKey.DownArrow)));
                    Assert.Equal(4, editor.Caret);
                }
                Assert.True(area.HandleInput(InputEvent.Press(ConsoleKey.UpArrow), context));
                Assert.Equal(0, editor.Caret); // Column one cannot split the two-cell expansion of ß.
                Assert.True(area.HandleInput(InputEvent.Press(ConsoleKey.DownArrow), context));
                Assert.Equal(4, editor.Caret);
                Assert.True(area.HandleInput(InputEvent.Press(ConsoleKey.Backspace), context));
                Assert.Equal("ßab", editor.Text);
            } else {
                var editor = new LineEditor("ßa");
                var input = new TextInput(editor, caret);
                CellBuffer frame = FrameComposer.Compose(input, 6, 1, context);
                Assert.Contains("ssa", frame.GetRowText(0));
                Assert.Equal(3, caret.Caret!.Value.X);
                Assert.True(input.HandleInput(InputEvent.Press(ConsoleKey.LeftArrow), context));
                Assert.True(input.HandleInput(InputEvent.Character('X'), context));
                Assert.Equal("ßXa", editor.Text);
            }
        }

        public static IEnumerable<object[]> PrintedUriCases() {
            foreach (string entry in new[] { "markup", "rich", "presenter", "markdown fallback" })
                foreach (bool addressLabel in new[] { false, true })
                    foreach (GlyphTier tier in new[] { GlyphTier.Ascii, GlyphTier.Basic }) {
                        yield return new object[] { entry, addressLabel, tier, "https://example.test/Łódź/東京", "https://example.test/%C5%81%C3%B3d%C5%BA/%E6%9D%B1%E4%BA%AC" };
                        yield return new object[] { entry, addressLabel, tier, "https://例え.テスト/报告", "https://xn--r8jz45g.xn--zckzah/%E6%8A%A5%E5%91%8A" };
                    }
        }

        [Theory]
        [MemberData(nameof(PrintedUriCases))]
        public void PrintedLinks_PreserveTheDestinationInReducedTiers(string entry, bool addressLabel, GlyphTier tier, string address, string encoded) {
            string label = addressLabel ? address : "Straße";
            var rich = new RichTextBuilder().AppendLink(label, address).Build();
            IWidget widget = entry == "markup" ? new TextBlock("[link=" + address + "]" + label + "[/]")
                : entry == "rich" ? rich
                : entry == "presenter" ? new Table().AddColumn(new TableColumn("Resource") { Presenter = new LinkPresenter(_ => label) }).AddValues(address)
                : new MarkdownText(rich);
            var profile = TerminalProfile.Fixed(240, 12, ColorDepth.None, tier, tier == GlyphTier.Ascii ? OutputMode.Plain : OutputMode.Stream);
            string output = FrameComposer.RenderBlock(widget, 240, new RenderContext(profile, Theme.Dark)).ToPlainText();
            Assert.Contains(encoded, output);
            if (!addressLabel) Assert.Contains(tier == GlyphTier.Ascii ? "Strasse" : "Straße", output);
            string target = Regex.Matches(output, @"https://[^\s)│]+").Cast<Match>().Select(match => match.Value).Single();
            var sourceUri = new Uri(address);
            var printedUri = new Uri(target);
            Assert.Equal(sourceUri.IdnHost, printedUri.IdnHost);
            Assert.Equal(sourceUri.Port, printedUri.Port);
            Assert.Equal(sourceUri.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.Unescaped),
                printedUri.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.Unescaped));
            Assert.All(target, c => Assert.InRange((int)c, 0x21, 0x7e));
        }

        [Fact]
        public void ExpandingTabLabels_KeepTheirTextAndMouseBoundsAligned() {
            var tabs = new Tabs().Add("ßß", new TextBlock("First")).Add("Next", new TextBlock("Second"));
            var context = new RenderContext(TerminalProfile.Fixed(30, 3, ColorDepth.None, GlyphTier.Ascii), Theme.Dark);
            CellBuffer frame = FrameComposer.Compose(tabs, 30, 3, context);
            Assert.StartsWith(" ssss  Next ", frame.GetRowText(0));
            Assert.StartsWith("======------", frame.GetRowText(1));
            Assert.True(((IAppInputContainer)tabs).HandleContainerInput(InputEvent.Mouse(5, 0), null));
            Assert.Equal(0, tabs.SelectedIndex);
            Assert.True(((IAppInputContainer)tabs).HandleContainerInput(InputEvent.Mouse(7, 0), null));
            Assert.Equal(1, tabs.SelectedIndex);
            Assert.StartsWith("------======", FrameComposer.Compose(tabs, 30, 3, context).GetRowText(1));
        }
    }
}
