using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Tests.Rendering;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>The multi-line text prompt, its editor, and the text area form field.</summary>
    public sealed class TextAreaTests {
        private static string Ask(TextAreaPrompt prompt, string keys, out RecordingSurface surface, int width = 60, int height = 16) {
            RecordingSurface s = new RecordingSurface(width, height, false);
            surface = s;
            return PromptHarness.Guard(s, () => prompt.Show(PromptHarness.Options(keys, s)));
        }

        [Fact]
        public void Enter_AddsALine_CtrlEnterSaves() {
            string value = Ask(new TextAreaPrompt("Change reason"), "\"Patch Tuesday\" Enter \"KB5031354 on SRV01\" Ctrl+Enter", out _);
            Assert.Equal("Patch Tuesday\nKB5031354 on SRV01", value);
        }

        [Fact]
        public void CtrlS_AlsoSaves_AndTrailingLinesAreTrimmed() {
            Assert.Equal("one", Ask(new TextAreaPrompt("Note"), "\"one\" Enter Enter Ctrl+S", out _));
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void Frame_SoftWrapsWithTheCounterAndFooter_MatchesGolden(GlyphTier tier) {
            RecordingSurface surface = new RecordingSurface(60, 16, false);
            TextAreaPrompt prompt = new TextAreaPrompt("Change reason") { Description = "Shown in the change log.", MaxLines = 10, Height = 5 };
            InteractiveOptions options = PromptHarness.Options("\"Monthly patching of the web farm, approved in CAB on Tuesday; rollback plan attached.\" Enter \"Owner: ops\" Ctrl+Enter", surface);
            options.Profile = TerminalProfile.Fixed(60, 16, ColorDepth.TrueColor, tier);
            PromptHarness.Guard(surface, () => prompt.Show(options));
            string frame = surface.Frames.Last(f => f.ToPlainText().Contains("Owner: ops")).ToPlainText();
            Golden.Match("interactive/textarea." + tier.ToString().ToLowerInvariant() + ".txt", frame.TrimEnd() + "\n");
            Assert.Contains("2 of 10 lines", frame);
        }

        [Fact]
        public void Required_ShowsTheErrorUntilFixed() {
            string value = Ask(new TextAreaPrompt("Reason") { Required = true }, "Ctrl+Enter \"x\" Ctrl+Enter", out RecordingSurface surface);
            Assert.Equal("x", value);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("A value is required."));
        }

        [Fact]
        public void MaxLines_StopsNewLines_AndValidatorRuns() {
            Assert.Equal("a\nbc", Ask(new TextAreaPrompt("Two lines") { MaxLines = 2 }, "\"a\" Enter \"b\" Enter \"c\" Ctrl+Enter", out _));
            string value = Ask(new TextAreaPrompt("No secrets") { Validator = t => t.Contains("password") ? "Leave passwords out." : null }, "\"password 1\" Ctrl+Enter Ctrl+Backspace Ctrl+Backspace \"ok\" Ctrl+Enter", out RecordingSurface surface);
            Assert.Equal("ok", value);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Leave passwords out."));
        }

        [Fact]
        public void Editor_PasteKeepsLineBreaks_TabsBecomeSpaces_ControlsAreDropped() {
            TextAreaEditor editor = new TextAreaEditor();
            editor.Handle(InputEvent.Paste("first\r\nsecond\tthird\u0007\u001b"));
            Assert.Equal("first\nsecond    third", editor.Text);
            Assert.Equal(2, editor.LineCount);
        }

        [Fact]
        public void Editor_UpAndDown_FollowTheVisualRows() {
            TextAreaEditor editor = new TextAreaEditor("alpha beta gamma delta\nshort");
            TextAreaWidget widget = new TextAreaWidget(editor, null) { Height = 6 };
            ConsoleMonster.Testing.TestTerminal.Render(widget, 14);
            Assert.Equal(new[] { 0, 11 }, editor.Rows[0]);
            editor.Handle(InputEvent.Press(ConsoleKey.Home, KeyModifiers.Control));
            editor.Handle(InputEvent.Press(ConsoleKey.DownArrow));
            Assert.Equal(11, editor.Caret);
            editor.Handle(InputEvent.Press(ConsoleKey.DownArrow));
            Assert.Equal(23, editor.Caret);
        }

        [Fact]
        public void FormField_OpensADialog_AndTheValueReachesTheResult() {
            Form form = new Form("Change") { Command = "New-Change" };
            TextAreaField reason = form.Page("Details").TextArea("Reason", "Reason");
            reason.Parameter = "Reason";
            reason.Required = true;
            RecordingSurface surface = new RecordingSurface(80, 24, false);
            FormOptions options = new FormOptions { Interactive = PromptHarness.Options("Enter \"Patch\" Enter \"approved\" Ctrl+Enter PageDown Enter", surface) };
            FormResult result = PromptHarness.Guard(surface, () => form.Show(options));
            Assert.True(result.Submitted);
            Assert.Equal("Patch\napproved", result.Values["Reason"]);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("ctrl+enter save"));
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Patch (+1)"));
        }
    }
}
