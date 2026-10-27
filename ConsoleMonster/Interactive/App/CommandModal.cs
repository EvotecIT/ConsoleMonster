using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>A modal focus scope made from the same persistent controls used in the app body.</summary>
    internal sealed class CommandModal : Modal, IDisposable {
        private readonly IWidget _content;
        private readonly FocusManager _focus;
        private readonly PromptLabels _labels;
        private ViewFrame? _frame;

        internal CommandModal(string title, Action accept, PromptLabels labels) {
            _labels = labels;
            Button yes = new Button("Accept", labels.Yes, false);
            Button no = new Button("Cancel", labels.No, false);
            yes.Clicked += (_, __) => { Close(); accept(); };
            no.Clicked += (_, __) => Close();
            _content = DialogChrome.Frame(new Rows(TextBlock.Plain(title), new Columns().Add(yes, LayoutSize.Fixed(12)).Add(no, LayoutSize.Fixed(12))), labels.KeyConfirm);
            _focus = new FocusManager(_content);
            _focus.Focus(no);
        }

        internal CommandModal(IReadOnlyList<AppCommand> commands, Action<AppCommand> run, PromptLabels labels) {
            _labels = labels;
            Select select = new Select("Command", commands.Select(command => new Choice(command, command.Label))) { Label = labels.Commands, PageSize = 8 };
            select.Changed += (_, __) => { if (select.Value is AppCommand command) { Close(); run(command); } };
            _content = DialogChrome.Frame(select, labels.Commands);
            _focus = new FocusManager(_content);
        }

        public override IWidget Build(ViewFrame frame, CaretSlot caret) { _frame = frame; return _content; }
        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            bindings.Add(new KeyBinding(_labels.KeyNext, _focus.MoveNext, new KeyChord(ConsoleKey.Tab), new KeyChord(ConsoleKey.RightArrow)));
            bindings.Add(new KeyBinding(_labels.KeyBack, _focus.MovePrevious, new KeyChord(ConsoleKey.Tab, KeyModifiers.Shift), new KeyChord(ConsoleKey.LeftArrow)));
            bindings.Add(new KeyBinding(_labels.KeyCancel, Close, new KeyChord(ConsoleKey.Escape)));
        }
        public override bool HandleUnbound(InputEvent input) {
            if (_frame == null) { return false; }
            if (input.Kind == InputEventKind.Mouse && input.MouseAction == MouseAction.Down) { _focus.FocusAt(input.Column, input.Row); }
            return _focus.Current?.HandleInput(input, _frame.Context) ?? false;
        }
        public void Dispose() => _focus.Dispose();
        internal override void CancelPointer() => _focus.Current?.CancelPointer();
    }
}
