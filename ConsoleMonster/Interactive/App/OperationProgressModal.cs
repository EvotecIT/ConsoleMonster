using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>A cancellable action view; Background releases the modal scope while work continues on the same runner.</summary>
    internal sealed class OperationProgressModal : Modal, IDisposable {
        private readonly AppActionSession _session;
        private readonly FocusManager _focus;
        private readonly IWidget _content;
        private ViewFrame? _frame;
        internal OperationProgressModal(AppActionSession session) {
            _session = session;
            var cancel = new Button("$operation.cancel", "Cancel", false);
            var background = new Button("$operation.background", "Background", false);
            cancel.Clicked += (_, __) => session.Cancel();
            background.Clicked += (_, __) => Close();
            _content = new Rows().Add(new ProgressBody(session)).Add(new Columns().Add(cancel, LayoutSize.Fixed(16)).Add(background, LayoutSize.Fixed(18)), LayoutSize.Fixed(1));
            _focus = new FocusManager(_content); _focus.Focus(cancel);
        }
        public override IWidget Build(ViewFrame frame, CaretSlot caret) { _frame = frame; return DialogChrome.Frame(_content, _session.OperationCommand?.Label ?? "Operation"); }
        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            bindings.Add(new KeyBinding("next", _focus.MoveNext, new KeyChord(ConsoleKey.Tab)));
            bindings.Add(new KeyBinding("background", Close, new KeyChord(ConsoleKey.Escape)));
        }
        public override bool HandleUnbound(InputEvent input) {
            if (_frame == null) { return false; }
            if (input.Kind == InputEventKind.Mouse && input.MouseAction == MouseAction.Down) { _focus.FocusAt(input.Column, input.Row); }
            return _focus.Current?.HandleInput(input, _frame.Context) ?? false;
        }
        internal override void CancelPointer() => _focus.Current?.CancelPointer();
        public void Dispose() => _focus.Dispose();
        private sealed class ProgressBody : IWidget {
            private readonly AppActionSession _session;
            internal ProgressBody(AppActionSession session) { _session = session; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(4, constraints.MaxHeight));
            public void Render(Region region, RenderContext context) {
                AppProgress? progress = _session.Progress;
                IWidget bar = Progress.Create(new ProgressOptions { Label = progress?.Message ?? "Working", Value = progress?.Completed ?? 0, Total = progress?.Total ?? 0 });
                bar.Render(region.Sub(0, 0, region.Width, Math.Min(2, region.Height)), context);
                if (progress?.Detail != null && region.Height > 2) { TextBlock.Plain(progress.Detail, ThemeToken.Muted).Render(region.Sub(0, 2, region.Width, region.Height - 2), context); }
            }
        }
    }
}
