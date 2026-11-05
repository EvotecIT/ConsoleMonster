using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Help;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    // The document viewer WidgetPager opens for Markdown with headings and for command help, driven through its input.
    [Collection("Live sessions")]
    public sealed class DocumentViewerTests {
        private static readonly RenderContext Context = new RenderContext(TerminalProfile.Fixed(100, 30), Theme.Dark);

        [Theory]
        [InlineData(GlyphTier.Ascii)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Full)]
        public void LongMarkdownWithHeadingsOpensTheViewerAtEveryGlyphTier(GlyphTier tier) {
            RecordingSurface surface = new RecordingSurface(100, 24, false);
            InteractiveOptions options = PromptHarness.Options("Down*30 Escape", surface, tier);
            Assert.True(PromptHarness.Guard(surface, () => WidgetPager.TryShow(Guide(), "Guide", options)));
            string first = surface.Frames.First().ToPlainText();
            string scrolled = surface.Frames.Last().ToPlainText();
            // The outline lists the headings beside the text, and the pinned row names the section being read.
            int divider = Divider(first);
            Assert.Equal(new[] { "Guide", "Install", "Windows", "Usage" }, Rows(first).Skip(1).Take(4).Select(row => row.Substring(1, divider - 1).Trim()));
            Assert.StartsWith("Guide " + (tier == GlyphTier.Ascii ? ">" : "›") + " Install", Pinned(scrolled));
            if (tier == GlyphTier.Ascii) { Assert.All(surface.Frames, frame => Assert.True(frame.ToPlainText().All(c => c < 128), frame.ToPlainText())); }
        }

        [Fact]
        public void OutlineFollowsTheReadingPositionAndJumpsOnEnterAndClick() {
            DocumentView view = View(Guide());
            Draw(view);
            for (int i = 0; i < 50; i++) { Press(view, ConsoleKey.DownArrow); }
            Assert.Equal("Windows", Selected(Draw(view)));

            Press(view, ConsoleKey.Tab);
            Press(view, ConsoleKey.DownArrow);
            Press(view, ConsoleKey.Enter);
            string usage = Draw(view);
            Assert.StartsWith("Guide › Usage", Pinned(usage));

            int install = Rows(usage).ToList().FindIndex(row => row.StartsWith("    Install", StringComparison.Ordinal));
            Click(view, 6, install);
            Assert.StartsWith("Guide › Install", Pinned(Draw(view)));
        }

        [Fact]
        public void DividerDragAndResizeKeysWidenTheOutline() {
            DocumentView view = View(Guide());
            int start = Divider(Draw(view));
            view.Handle(InputEvent.Pointer(start, 5, MouseAction.Down, button: 1));
            view.Handle(InputEvent.Pointer(40, 5, MouseAction.Move, heldButtons: 1));
            view.Handle(InputEvent.Pointer(40, 5, MouseAction.Up, button: 1));
            Assert.Equal(40, Divider(Draw(view)));
            Press(view, ConsoleKey.LeftArrow, KeyModifiers.Control | KeyModifiers.Shift);
            Assert.True(Divider(Draw(view)) < 40);
        }

        [Fact]
        public void HeadingsFoldFromTheKeyboardAndOnAClick() {
            DocumentView view = View(Guide());
            Draw(view);
            for (int i = 0; i < 4; i++) { Press(view, ConsoleKey.DownArrow); }
            Assert.StartsWith("Guide › Install", Pinned(Draw(view)));
            Press(view, ConsoleKey.Enter);
            string folded = Draw(view);
            Assert.Contains("▸ Install", folded);
            Assert.DoesNotContain("install line 3", folded);

            int heading = Rows(folded).ToList().FindIndex(row => row.Contains("▸ Install"));
            Click(view, Divider(folded) + 6, heading);
            Assert.Contains("install line 3", Draw(view));
        }

        [Fact]
        public void HelpShowsTheHeaderCardAndStartsParametersAndExamplesFolded() {
            DocumentView view = View(Help());
            string overview = Draw(view);
            Assert.Contains("Get-Thing", overview); Assert.Contains("Demo", overview); Assert.Contains("v1.2.0", overview); Assert.Contains("-WhatIf", overview);
            Assert.Contains("Gets things from a store.", overview);
            Assert.Contains("Overview  Parameters  Examples  Related", overview);

            Type(view, "2");
            string parameters = Draw(view);
            Assert.Contains("▸ -Path  <string[]>  required  default .  Where things are.", parameters);
            Assert.DoesNotContain("Position:", parameters);
            Type(view, "3");
            string examples = Draw(view);
            Assert.Contains("▸ 1. List things  Get-Thing -Path .", examples);
            Assert.DoesNotContain("Lists every thing", examples);
        }

        [Fact]
        public void TabsSwitchWithNumberKeysTheFocusedStripAndAClick() {
            DocumentView view = View(Help());
            Draw(view);
            Type(view, "4");
            Assert.Contains("Get-Other", Draw(view));

            Press(view, ConsoleKey.Tab);
            Press(view, ConsoleKey.RightArrow);
            string overview = Draw(view);
            Assert.Contains("Syntax", Pinned(overview));
            Assert.Contains("←→ tabs", overview);

            string[] rows = Rows(overview);
            int strip = Array.FindIndex(rows, row => row.Contains(" Examples "));
            Click(view, rows[strip].IndexOf("Examples", StringComparison.Ordinal), strip);
            Assert.Contains("1. List things", Draw(view));
        }

        [Fact]
        public void FindOpensAFoldedSectionHoldingTheMatch() {
            DocumentView view = View(Help());
            Draw(view);
            Type(view, "3/every thing");
            string found = Draw(view);
            Assert.Contains("▾ 1. List things", found);
            Assert.Contains("Lists every thing", found);
            Assert.Contains("1 of 1", found);
            Assert.Contains("▸ 2. Remove a thing", found);
        }

        [Fact]
        public void AnchorLinksAreFoundFollowedAndRetracedWithBackAndForward() {
            DocumentView view = View(Guide());
            Draw(view);
            Press(view, ConsoleKey.End);
            Type(view, "]");
            CellBuffer selected = DrawFrame(view);
            string before = selected.ToPlainText();
            // The anchor is a link the viewer found: ] selects it, drawn reversed, without a printed (#install) target.
            int row = Rows(before).ToList().FindIndex(line => line.Contains("Go to Install"));
            Assert.True(row >= 0, before);
            Assert.DoesNotContain("#install", before);
            int column = Rows(before)[row].IndexOf("Install", StringComparison.Ordinal);
            Assert.True((selected[column, row].Style.Decoration & TextDecoration.Reverse) != 0);

            Press(view, ConsoleKey.Enter);
            string followed = Draw(view);
            Assert.Equal("Guide › Install", Pinned(followed).Trim());
            Assert.DoesNotContain("Go to Install", followed);

            Press(view, ConsoleKey.LeftArrow, KeyModifiers.Alt);
            Assert.Equal(before, Draw(view));
            Press(view, ConsoleKey.RightArrow, KeyModifiers.Alt);
            Assert.Equal(followed, Draw(view));

            Press(view, ConsoleKey.LeftArrow, KeyModifiers.Alt);
            Click(view, column, row);
            Assert.Equal("Guide › Install", Pinned(Draw(view)).Trim());
        }

        [Fact]
        public void OutlineKeepsItsSelectionAcrossFramesSoEnterOpensTheChosenEntry() {
            // Real frames bring a new render context each time; the outline must not reset between keys.
            RecordingSurface surface = new RecordingSurface(100, 24, false);
            InteractiveOptions options = PromptHarness.Options("Tab Down Enter Escape", surface);
            Assert.True(PromptHarness.Guard(surface, () => WidgetPager.TryShow(Guide(), "Guide", options)));
            Assert.Contains(surface.Frames, frame => Pinned(frame.ToPlainText()).Trim() == "Guide › Install");
        }

        [Fact]
        public void ClickingAnOutlineEntryKeepsTheKeysInTheOutline() {
            DocumentView view = View(Guide());
            string first = Draw(view);
            int install = Rows(first).ToList().FindIndex(row => row.StartsWith("    Install", StringComparison.Ordinal));
            Click(view, 6, install);
            Assert.Equal("Guide › Install", Pinned(Draw(view)).Trim());
            Press(view, ConsoleKey.DownArrow);
            Press(view, ConsoleKey.Enter);
            Assert.Equal("Guide › Install › Windows", Pinned(Draw(view)).Trim());
        }

        [Fact]
        public void TheWheelScrollsALongOutline() {
            var long_ = new MarkdownView();
            for (int i = 1; i <= 30; i++) {
                long_.AddHeading(RichText.Plain("Section " + i), 2);
                long_.AddParagraph(RichText.Plain("text " + i));
            }
            DocumentView view = View(long_);
            string before = Draw(view, 100, 10);
            Assert.Contains("Section 1", Rows(before)[1].Substring(0, Divider(before)));
            view.Handle(InputEvent.Pointer(4, 3, MouseAction.Wheel, wheelDelta: -1));
            string after = Draw(view, 100, 10);
            Assert.DoesNotContain("Section 1 ", Rows(after)[1].Substring(0, Divider(after)) + " ");
            Assert.Contains("Section 4", Rows(after)[1].Substring(0, Divider(after)));
        }
        [Fact]
        public void RelatedCommandsOpenTheirOwnHelpAndBackReturns() {
            string? opened = null;
            DocumentView view = View(Help(), name => { opened = name; return new HelpView(new HelpTopic(name) { Synopsis = "The other command." }); });
            Draw(view);
            Type(view, "4]");
            Press(view, ConsoleKey.Enter);
            string other = Draw(view);
            Assert.Equal("Get-Other", opened);
            Assert.Contains("The other command.", other);
            Assert.Contains("back/forward", other);

            Press(view, ConsoleKey.Backspace);
            Assert.Contains("https://example.com/other", Draw(view));
        }

        [Fact]
        public void PaletteListsSectionsAndParametersAndOpensTheChosenOne() {
            DocumentView view = View(Help());
            Draw(view);
            Press(view, ConsoleKey.K, KeyModifiers.Control);
            string palette = Draw(view);
            Assert.Contains("-Path", palette); Assert.Contains("Parameter", palette); Assert.Contains("Syntax", palette);
            Type(view, "-Force");
            Assert.Contains("Includes hidden things.", Draw(view));
            Press(view, ConsoleKey.Enter);
            string chosen = Draw(view);
            Assert.StartsWith("Parameters › -Force", Pinned(chosen));
            Assert.Contains("Position: named", chosen);
        }

        [Fact]
        public void TabStripIsAFocusStopThatSwitchesTabsWithArrowsAndNumberKeys() {
            var first = new TextInput("First");
            var tabs = new Tabs().Add("General", first).Add("Advanced", new TextInput("Second")).Add("Logging", TextBlock.Plain("log output"));
            using var app = new AppController(tabs, new AppState(), Context, 60);
            app.PrepareFrame(80, 12);
            Assert.Same(first, app.Focus.Current);
            Assert.Contains("ctrl+pgup/pgdn tabs", app.PrepareFrame(160, 12).ToPlainText());

            app.Handle(InputEvent.Press(ConsoleKey.Tab, KeyModifiers.Shift), true); app.Refresh(null);
            string strip = app.PrepareFrame(80, 12).ToPlainText();
            Assert.Contains("←→ tabs", strip); Assert.Contains("1-3 open tab", strip);
            app.Handle(InputEvent.Press(ConsoleKey.RightArrow), true); app.Refresh(null);
            Assert.Equal(1, tabs.SelectedIndex);
            app.Handle(InputEvent.Character('3'), true); app.Refresh(null);
            Assert.Equal(2, tabs.SelectedIndex);
            Assert.Contains("log output", app.PrepareFrame(80, 12).ToPlainText());
        }

        [Fact]
        public void NarrowTabStripScrollsToKeepTheActiveLabelInView() {
            var tabs = new Tabs().Add("General", TextBlock.Plain("a")).Add("Advanced settings", TextBlock.Plain("b")).Add("Logging", TextBlock.Plain("c"));
            var context = new RenderContext(TerminalProfile.Fixed(24, 4, ColorDepth.None, GlyphTier.Ascii), Theme.Dark);
            tabs.SelectedIndex = 2;
            string[] rows = FrameComposer.Compose(tabs, 24, 4, context).ToPlainText().Split('\n');
            Assert.StartsWith("<", rows[0]);
            int at = rows[0].IndexOf(" Logging", StringComparison.Ordinal);
            Assert.Equal(1, at);
            Assert.Equal("=========", rows[1].Substring(at, 9));
        }

        // A guide long enough to scroll: Install with a nested Windows section, then Usage with a link back to Install.
        private static MarkdownView Guide() {
            var guide = new MarkdownView();
            guide.AddHeading(RichText.Plain("Guide"), 1);
            guide.AddParagraph(RichText.Plain("Read this first."));
            guide.AddHeading(RichText.Plain("Install"), 2);
            for (int i = 0; i < 20; i++) { guide.AddParagraph(RichText.Plain("install line " + i)); }
            guide.AddHeading(RichText.Plain("Windows"), 3);
            for (int i = 0; i < 20; i++) { guide.AddParagraph(RichText.Plain("windows line " + i)); }
            guide.AddHeading(RichText.Plain("Usage"), 2);
            guide.AddParagraph(new RichTextBuilder().Append("Go to ").AppendLink("Install", "#install").Build());
            return guide;
        }

        private static HelpView Help() {
            var topic = new HelpTopic("Get-Thing") { Synopsis = "Gets things from a store.", Module = "Demo", Version = "1.2.0", SupportsWhatIf = true };
            var syntax = new HelpSyntax("Get-Thing");
            syntax.Parameters.Add(new HelpSyntaxParameter("Path") { Type = "string[]", Required = true });
            topic.Syntax.Add(syntax);
            topic.Description.Add("Reads things.");
            topic.Parameters.Add(new HelpParameter("Path") { Type = "string[]", Required = true, Default = ".", Description = "Where things are.\nWildcards work." });
            topic.Parameters.Add(new HelpParameter("Force") { Description = "Includes hidden things." });
            topic.Examples.Add(new HelpExample("List things", "Get-Thing -Path .") { Remarks = "Lists every thing in the folder." });
            topic.Examples.Add(new HelpExample("Remove a thing", "Get-Thing -Path a | Remove-Thing") { Remarks = "Removes one." });
            topic.Links.Add(new HelpLink("Get-Other", "https://example.com/other"));
            return new HelpView(topic);
        }

        private static DocumentView View(Layout.IWidget content, Func<string, Layout.IWidget?>? open = null) =>
            new DocumentView(content, "Document", DocumentModel.From(content, "Document", Context, open != null)!, new PromptLabels(), open: open);

        private static string Draw(InteractiveView view, int width = 100, int height = 30) => DrawFrame(view, width, height).ToPlainText();

        private static CellBuffer DrawFrame(InteractiveView view, int width = 100, int height = 30) =>
            FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, width, height, false, TimeSpan.Zero)), width, height, Context);

        private static string[] Rows(string frame) => frame.Split('\n');

        private static string Row(string frame, string text) => Rows(frame).First(row => row.Contains(text));

        // The divider is the first vertical line of the body.
        private static int Divider(string frame) => Rows(frame).Select(row => row.IndexOfAny(new[] { '│', '|' })).First(at => at > 0);

        // The pinned row is the document pane's first row: the row under the strip (or title) with text after the divider.
        private static string Pinned(string frame) {
            string[] rows = Rows(frame);
            int divider = Divider(frame);
            string row = rows.First(line => line.Length > divider && (line[divider] == '│' || line[divider] == '|'));
            return row.Substring(divider + 1);
        }

        // The outline row marked as the current section.
        private static string Selected(string frame) => Rows(frame).First(row => row.StartsWith("▌", StringComparison.Ordinal) || row.StartsWith("›", StringComparison.Ordinal)).Substring(1, Divider(frame) - 1).Trim();

        private static void Press(InteractiveView view, ConsoleKey key, KeyModifiers modifiers = KeyModifiers.None) => view.Handle(InputEvent.Press(key, modifiers));

        private static void Type(InteractiveView view, string text) { foreach (char c in text) { view.Handle(InputEvent.Character(c)); } }

        private static void Click(InteractiveView view, int column, int row) {
            view.Handle(InputEvent.Pointer(column, row, MouseAction.Down, button: 1));
            view.Handle(InputEvent.Pointer(column, row, MouseAction.Up, button: 1));
        }
    }
}
