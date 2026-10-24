using System;
using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>The line editor and the choice list: editing, filtering, groups, selection rules and messages.</summary>
    public sealed class ControlTests {
        private static void Type(LineEditor editor, string script) {
            foreach (InputEvent e in ScriptedInput.ParseEvents(script)) {
                editor.Handle(e);
            }
        }

        [Fact]
        public void UnixMetaAndEmacsKeysEditTheLineThroughTheDecoder() {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) { return; }
            LineEditor editor = new LineEditor("alpha beta");
            VtInputDecoder decoder = new VtInputDecoder();
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed("\u001bb\u001b\u007f", events);
            foreach (InputEvent input in events) { editor.Handle(input); }
            Assert.Equal("beta", editor.Text);
            Assert.Equal(0, editor.Caret);
            editor.Handle(InputEvent.Press(ConsoleKey.E, KeyModifiers.Control));
            Assert.Equal(4, editor.Caret);
            editor.Handle(InputEvent.Press(ConsoleKey.A, KeyModifiers.Control));
            Assert.Equal(0, editor.Caret);
            editor.Handle(InputEvent.Character('f', KeyModifiers.Alt));
            Assert.Equal(4, editor.Caret);
        }

        [Fact]
        public void Handle_EditsAndMovesByWord() {
            LineEditor editor = new LineEditor();
            Type(editor, "\"copy files now\" Ctrl+Left Ctrl+Backspace");
            Assert.Equal("copy now", editor.Text);
            Assert.Equal(5, editor.Caret);
            Type(editor, "Home Delete End Backspace");
            Assert.Equal("opy no", editor.Text);
            Type(editor, "Ctrl+U");
            Assert.Equal(string.Empty, editor.Text);
        }

        [Fact]
        public void Handle_PasteJoinsLinesAndRespectsMaxLength() {
            LineEditor editor = new LineEditor { MaxLength = 10 };
            editor.Handle(InputEvent.Paste("first\r\nsecond\tthird"));
            Assert.Equal("first seco", editor.Text);
        }

        [Fact]
        public void HistoryStep_WalksBackAndRestoresTheDraft() {
            LineEditor editor = new LineEditor();
            editor.History.Add("one");
            editor.History.Add("two");
            Type(editor, "\"dr\"");
            editor.HistoryStep(-1);
            Assert.Equal("two", editor.Text);
            editor.HistoryStep(-1);
            Assert.Equal("one", editor.Text);
            editor.HistoryStep(1);
            editor.HistoryStep(1);
            Assert.Equal("dr", editor.Text);
        }

        private static List<Choice> Servers() {
            return new List<Choice> {
                new Choice("fs01", "FS01 file server") { Group = "Warsaw" },
                new Choice("web01", "WEB01") { Group = "Warsaw" },
                new Choice("prn11", "PRN11") { Group = "Berlin", DisabledReason = "offline" },
                new Choice("fs11", "FS11 file server") { Group = "Berlin" },
                new Choice("lab01", "LAB01") { Group = "Lab" }
            };
        }

        [Fact]
        public void HandleText_FiltersFuzzilyAndRanksTitlesFirst() {
            ChoiceList list = new ChoiceList(Servers(), multi: false);
            foreach (InputEvent e in ScriptedInput.ParseEvents("\"fs\"")) {
                list.HandleText(e, out _);
            }
            Assert.True(list.IsFiltering);
            Assert.Equal(2, list.VisibleItemCount);
            Assert.Equal("fs01", list.FocusedChoice!.Value);
            list.ClearFilter();
            Assert.Equal(5, list.VisibleItemCount);
        }

        [Fact]
        public void ToggleFocused_OnAGroup_SelectsItsEnabledMembersAndClearsThem() {
            ChoiceList list = new ChoiceList(Servers(), multi: true);
            list.Move(-1);
            while (list.FocusedGroup != "Berlin") {
                list.Move(1);
            }
            list.ToggleFocused();
            Assert.Equal(new object?[] { "fs11" }, list.SelectedChoices.ConvertAll(c => c.Value).ToArray());
            list.ToggleFocused();
            Assert.Equal(0, list.SelectedCount);
        }

        [Fact]
        public void SelectAll_RespectsTheFilterAndTheMaximum() {
            ChoiceList list = new ChoiceList(Servers(), multi: true) { Maximum = 3 };
            foreach (InputEvent e in ScriptedInput.ParseEvents("\"file\"")) {
                list.HandleText(e, out _);
            }
            list.SelectAll();
            Assert.Equal(2, list.SelectedCount);
            list.ClearFilter();
            list.SelectAll();
            Assert.Equal(2, list.SelectedCount);
            Assert.Equal("Select at most 3.", list.Message);
            list.Invert();
            Assert.Equal(new object?[] { "web01", "lab01" }, list.SelectedChoices.ConvertAll(c => c.Value).ToArray());
        }

        [Fact]
        public void ToggleFocused_OnADisabledChoice_ExplainsWhy() {
            ChoiceList list = new ChoiceList(Servers(), multi: true);
            list.FocusChoice(Servers()[2]);
            list.FocusChoice(list.Choices[2]);
            list.ToggleFocused();
            Assert.Equal(0, list.SelectedCount);
            Assert.Equal("Not available: offline", list.Message);
        }

        [Fact]
        public void ValidateSelection_ReportsTheMinimum() {
            ChoiceList list = new ChoiceList(Servers(), multi: true) { Minimum = 2 };
            Assert.Equal("Select at least 2.", list.ValidateSelection());
        }

        [Fact]
        public void ToggleSelectedOnly_ShowsOnlySelectedChoices() {
            ChoiceList list = new ChoiceList(Servers(), multi: true);
            list.SetSelected(list.Choices[4], true);
            list.ToggleSelectedOnly();
            Assert.Equal(1, list.VisibleItemCount);
            Assert.Equal("lab01", list.FocusedChoice!.Value);
        }

        [Fact]
        public void HandleText_WithHotkeys_PicksAndSlashStartsFiltering() {
            List<Choice> choices = new List<Choice> { new Choice(1, "Run") { Hotkey = 'r' }, new Choice(2, "Settings") { Hotkey = 's' } };
            ChoiceList list = new ChoiceList(choices, multi: false);
            Assert.True(list.HandleText(InputEvent.Character('s'), out Choice? picked));
            Assert.Equal(2, picked!.Value);
            Assert.False(list.HandleText(InputEvent.Character('x'), out _));
            list.FilterMode = true;
            Assert.True(list.HandleText(InputEvent.Character('x'), out picked));
            Assert.Null(picked);
        }

        [Fact]
        public void Move_WrapsAroundAndSkipsHeadingsInSingleSelection() {
            ChoiceList list = new ChoiceList(Servers(), multi: false);
            Assert.Equal("fs01", list.FocusedChoice!.Value);
            list.Move(-1);
            Assert.Equal("lab01", list.FocusedChoice!.Value);
            list.Move(1);
            Assert.Equal("fs01", list.FocusedChoice!.Value);
        }

        [Fact]
        public void RowWidth_FitsTheWidestTitleWithinLimits() {
            ChoiceList list = new ChoiceList(Servers(), multi: false);
            ConsoleMonster.Canvas.RenderContext context = Terminal.CreateContext(TerminalProfile.Fixed(120, 30, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            Assert.Equal(40, list.RowWidth(context, 120));
            Assert.Equal(30, list.RowWidth(context, 30));
        }
    }
}
