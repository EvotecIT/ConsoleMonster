using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>Reuses the action host's overlay and modal input routing with ordinary retained fields.</summary>
    internal sealed class FormDialogModal<T> : Modal, IDisposable {
        private readonly FormDialog<T> _dialog;
        private readonly Action<DialogResult<T>> _completed;
        private readonly AppState _values = new AppState();
        private readonly FocusManager _focus;
        private readonly IWidget _content;
        private readonly List<InputControl> _fields;
        private ViewFrame? _frame;
        private string? _error;
        private readonly AppViewport _viewport;
        private InputControl? _capture;

        internal FormDialogModal(FormDialog<T> dialog, Action<DialogResult<T>> completed) {
            _dialog = dialog; _completed = completed;
            _fields = FocusManager.Discover(dialog.Content).ToList();
            var accept = new Button("$dialog.accept", dialog.AcceptText, false);
            var cancel = new Button("$dialog.cancel", dialog.CancelText, false);
            accept.Clicked += (_, __) => Accept();
            cancel.Clicked += (_, __) => Cancel();
            _content = new Rows().Add(dialog.Content).Add(new Columns().Add(accept, LayoutSize.Fixed(16)).Add(cancel, LayoutSize.Fixed(16)), LayoutSize.Fixed(1));
            _focus = new FocusManager(_content);
            _viewport = new AppViewport(_content, _focus);
            foreach (InputControl field in _fields) { _values[field.Key] = field.CurrentValue; field.ValueCommitted += Commit; }
            InputControl? initial = dialog.InitialFocus == null ? null : _fields.Find(field => string.Equals(field.Key, dialog.InitialFocus, StringComparison.OrdinalIgnoreCase));
            _focus.Focus(initial ?? (dialog.DefaultAccept ? accept : cancel));
        }
        public override int MaxWidth => _dialog.MaxWidth;
        public override IWidget Build(ViewFrame frame, CaretSlot caret) {
            _frame = frame;
            var body = new Rows().Add(_viewport, LayoutSize.Fraction());
            if (_error != null) { body.Add(TextBlock.Plain(_error, ThemeToken.Fail), LayoutSize.Fixed(1)); }
            return DialogChrome.Frame(body, _dialog.Title);
        }
        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            bindings.Add(new KeyBinding("next field", _focus.MoveNext, new KeyChord(ConsoleKey.Tab)));
            bindings.Add(new KeyBinding("previous field", _focus.MovePrevious, new KeyChord(ConsoleKey.Tab, KeyModifiers.Shift)) { InFooter = false });
            bindings.Add(new KeyBinding("cancel", Cancel, new KeyChord(ConsoleKey.Escape)));
            bindings.Add(new KeyBinding("accept", Accept, new KeyChord(ConsoleKey.Enter, KeyModifiers.Control)));
        }
        public override bool HandleUnbound(InputEvent input) {
            if (_frame == null) { return false; }
            if (input.Kind == InputEventKind.Mouse) {
                if (input.MouseAction == MouseAction.Down && input.MouseButton == 1 && _viewport.FocusAt(input.Column, input.Row)) { _capture = _focus.Current; }
                InputControl? target = _capture ?? _focus.Current;
                bool handled = target?.HandleInput(_viewport.TranslatePointer(target, input), _frame.Context) ?? false;
                if (input.MouseAction == MouseAction.Up) { _capture = null; }
                return handled;
            }
            return _focus.Current?.HandleInput(input, _frame.Context) ?? false;
        }
        private void Commit(AppValueChangedEventArgs change) { _values[change.Key] = _fields.Find(field => field.Key == change.Key)!.CurrentValue; }
        private void Accept() {
            _error = _dialog.Validate?.Invoke(_values);
            if (_error != null) { return; }
            T value = _dialog.Result(_values);
            Close(); _completed(new DialogResult<T>(true, value));
        }
        private void Cancel() { Close(); _completed(new DialogResult<T>(false, default)); }
        internal override void CancelPointer() { _capture?.CancelPointer(); _capture = null; }
        public void Dispose() {
            foreach (InputControl field in _fields) { field.ValueCommitted -= Commit; }
            _focus.Dispose(); _viewport.Reset();
        }
    }
}
