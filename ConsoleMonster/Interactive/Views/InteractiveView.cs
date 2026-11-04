using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>The size and render context of the frame being built.</summary>
    internal sealed class ViewFrame {
        public ViewFrame(RenderContext context, int width, int height, bool inline, TimeSpan now, CaretSlot? caret = null) {
            Caret = caret ?? new CaretSlot();
            Context = context;
            Width = width;
            Height = height;
            Inline = inline;
            Now = now;
        }

        public RenderContext Context { get; }

        public int Width { get; }

        public int Height { get; }

        public bool Inline { get; }

        public TimeSpan Now { get; }

        /// <summary>Where the focused text box reports its caret.</summary>
        public CaretSlot Caret { get; }
    }

    /// <summary>
    /// A screen of the interactive layer (a prompt, a menu, a form). It builds a widget tree per frame, declares its key
    /// bindings (which also generate the footer and key map), and handles typing that no binding claims.
    /// </summary>
    internal abstract class InteractiveView {
        private ViewFrame? _frame;
        private Toast? _toast;

        protected InteractiveView(PromptLabels labels) {
            Labels = labels ?? throw new ArgumentNullException(nameof(labels));
        }

        public PromptLabels Labels { get; }

        public CaretSlot Caret { get; } = new CaretSlot();

        /// <summary>The last frame drawn (the backdrop of prompts asked by the view's actions).</summary>
        public CellBuffer? LastRendered { get; set; }

        public bool IsDone { get; protected set; }

        public bool IsCanceled { get; protected set; }

        /// <summary>Whether the full key map is open.</summary>
        public bool KeyMapOpen { get; set; }

        /// <summary>The toast to show, if any.</summary>
        public Toast? CurrentToast => _toast;

        /// <summary>The last frame's settings (bindings are resolved against them).</summary>
        protected ViewFrame? LastFrame => _frame;

        public virtual bool IsAnimating => false;

        public IWidget BuildFrame(ViewFrame frame) {
            _frame = frame;
            if (_toast != null && !_toast.ShownAt.HasValue) {
                _toast.ShownAt = frame.Now;
            }
            IWidget content = Build(frame);
            if (KeyMapOpen) {
                List<KeyBinding> bindings = new List<KeyBinding>();
                AddBindings(bindings, frame);
                content = new Overlay(content, new KeyMapWidget(bindings, Labels), dim: !frame.Inline) { Inline = frame.Inline };
            }
            return new Collecting(content, _targets);
        }

        private readonly PointerTargets _targets = new PointerTargets();
        private readonly ClickCounter _clicks = new ClickCounter();

        // Records the pointer targets (footer hints, lists) while the frame renders.
        private sealed class Collecting : IWidget {
            private readonly IWidget _content;
            private readonly PointerTargets _targets;
            internal Collecting(IWidget content, PointerTargets targets) { _content = content; _targets = targets; }
            public Size Measure(Constraints constraints, RenderContext context) => _content.Measure(constraints, context);
            public void Render(Region region, RenderContext context) { using (_targets.Collect()) { _content.Render(region, context); } }
        }

        /// <summary>
        /// A pointer event, in frame coordinates. A click on a footer hint presses its key, a list takes clicks, drags and
        /// the wheel on its rows, and a double click on a row presses the list's key (Enter, or Space where it toggles);
        /// anything else goes to the view's own handlers, then the wheel to the arrow keys. While the key map shows, a
        /// click closes it like a key.
        /// </summary>
        private void HandleMouse(InputEvent input) {
            int clicks = _clicks.Count(input);
            if (KeyMapOpen) {
                if (input.MouseAction == MouseAction.Down) { KeyMapOpen = false; }
                return;
            }
            if (_targets.Route(input, clicks, out KeyChord? key)) {
                if (key.HasValue) { _clicks.Reset(); Handle(key.Value.ToInput()); }
                return;
            }
            // Views with pointer handling of their own (a grid, a date picker) get the rest.
            if (HandlePriorityInput(input) || HandleUnbound(input)) { return; }
            ScrollWithArrows(input);
        }

        // The wheel goes the way the arrow keys do in views with nothing of their own to scroll.
        private bool ScrollWithArrows(InputEvent input) {
            if (input.MouseAction != MouseAction.Wheel || input.WheelDelta == 0 || _frame == null) { return false; }
            InputEvent arrow = InputEvent.Press(input.WheelDelta > 0 ? ConsoleKey.UpArrow : ConsoleKey.DownArrow);
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, _frame);
            bool moved = false;
            for (int i = 0; i < 3; i++) { moved |= KeyFooter.Dispatch(bindings, arrow); }
            return moved;
        }

        protected abstract IWidget Build(ViewFrame frame);

        /// <summary>Adds the key bindings that are active now.</summary>
        public abstract void AddBindings(List<KeyBinding> bindings, ViewFrame frame);

        /// <summary>Handles an event that no binding claimed (typing). Returns whether it changed anything.</summary>
        protected virtual bool HandleUnbound(InputEvent input) => false;

        /// <summary>Checks background work (async loading) and timers; returns true when the view changed.</summary>
        public virtual bool Poll(TimeSpan now) {
            if (_toast != null && _toast.IsExpired(now)) {
                _toast = null;
                return true;
            }
            return false;
        }

        public void ShowToast(Toast toast) {
            _toast = toast;
        }

        public void Handle(InputEvent input) {
            if (_frame == null) {
                return;
            }
            if (input.Kind == InputEventKind.Mouse) {
                HandleMouse(input);
                return;
            }
            if (KeyMapOpen) {
                if (input.Kind == InputEventKind.Key) {
                    KeyMapOpen = false;
                }
                return;
            }
            if (input.Kind == InputEventKind.Key && input.Is(ConsoleKey.C, KeyModifiers.Control)) {
                Cancel();
                return;
            }
            if (HandlePriorityInput(input)) { return; }
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, _frame);
            if (input.Kind == InputEventKind.Key && KeyFooter.Dispatch(bindings, input)) {
                return;
            }
            HandleUnbound(input);
        }

        /// <summary>Lets the active modal focus scope consume input before the view's ordinary bindings.</summary>
        protected virtual bool HandlePriorityInput(InputEvent input) => false;

        /// <summary>Ctrl+C: ends the view as cancelled.</summary>
        public virtual void Cancel() {
            IsCanceled = true;
            IsDone = true;
        }

        /// <summary>The standard key-map binding (F1 or ?).</summary>
        protected KeyBinding HelpBinding() {
            return new KeyBinding(Labels.KeyHelp, () => KeyMapOpen = true, new KeyChord(ConsoleKey.F1), new KeyChord('?')) {
                Order = KeyFooter.HelpOrder, Group = "Help",
                KeyText = "?"
            };
        }

        /// <summary>The summary lines left in the scrollback by an inline prompt.</summary>
        public virtual IReadOnlyList<StyledLine>? Summary(RenderContext context) => null;
    }
}
