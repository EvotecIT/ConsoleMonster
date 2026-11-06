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
            if (_menu != null) { content = new MenuLayer(content, _menu); }
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
            if (_menu != null) {
                AppKeyMap.Entry? chosen = _menu.HandlePointer(input, out bool close, out bool used);
                if (close) { _menu = null; }
                if (chosen != null) { Handle(chosen.Chord!.Value.ToInput()); return; }
                if (used) { return; }
            }
            int clicks = _clicks.Count(input);
            if (KeyMapOpen) {
                if (input.MouseAction == MouseAction.Down) { KeyMapOpen = false; }
                return;
            }
            // A right click opens the context menu of the keys that work here, as it does in an app.
            if (input.MouseAction == MouseAction.Down && input.MouseButton == 3) {
                _targets.Cancel();
                OpenMenu(input.Column, input.Row);
                return;
            }
            if (_targets.Route(input, clicks, out KeyChord? key)) {
                if (key.HasValue) { _clicks.Reset(); Handle(key.Value.ToInput()); }
                return;
            }
            // Views with pointer handling of their own (a grid, a date picker) get the rest.
            if (HandlePriorityInput(input) || HandleUnbound(input)) {
                // A double click on a row opens it, as Enter does (the app path does the same for its lists and grids).
                if (clicks == 2 && input.MouseButton == 1 && OpensOnDoubleClick) { _clicks.Reset(); Handle(InputEvent.Press(ConsoleKey.Enter)); }
                return;
            }
            ScrollWithArrows(input);
        }

        // Whether the pointer input the view just handled landed on a row that a double click opens.
        protected virtual bool OpensOnDoubleClick => false;

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
            // A press held across a resize or focus loss does not complete on the next release.
            if (input.Kind == InputEventKind.Resize || input.Kind == InputEventKind.FocusLost) { _targets.Cancel(); _clicks.Reset(); }
            if (KeyMapOpen) {
                if (input.Kind == InputEventKind.Key) {
                    KeyMapOpen = false;
                }
                return;
            }
            if (input.Kind == InputEventKind.Key && input.Is(ConsoleKey.C, KeyModifiers.Control)) {
                _menu = null;
                Cancel();
                return;
            }
            if (_menu != null && input.Kind == InputEventKind.Key) {
                AppKeyMap.Entry? chosen = _menu.HandleKey(input, out bool close, out bool used);
                if (close) { _menu = null; }
                if (chosen != null) { Handle(chosen.Chord!.Value.ToInput()); return; }
                if (used) { return; }
            }
            if (HandlePriorityInput(input)) { return; }
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, _frame);
            if (input.Kind == InputEventKind.Key && KeyFooter.Dispatch(bindings, input)) {
                return;
            }
            // The Menu key and Shift+F10 open the context menu, unless a binding took them.
            if (input.Kind == InputEventKind.Key && AppContextMenu.IsOpenKey(input)) {
                OpenMenu(1, 1);
                return;
            }
            HandleUnbound(input);
        }

        private AppContextMenu? _menu;

        /// <summary>Whether the context menu is open.</summary>
        public bool MenuOpen => _menu != null;

        // The labelled bindings a key reaches now, in dispatch order, each under the keys still free for it.
        private void OpenMenu(int column, int row) {
            if (_frame == null) { return; }
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, _frame);
            HashSet<KeyChord> claimed = new HashSet<KeyChord>();
            List<AppKeyMap.Entry> entries = new List<AppKeyMap.Entry>();
            foreach (KeyBinding binding in bindings) {
                List<KeyChord> claims = new List<KeyChord>(binding.Claims);
                List<KeyChord> free = claims.FindAll(chord => !claimed.Contains(chord));
                foreach (KeyChord chord in free) { claimed.Add(chord); }
                if (free.Count == 0 || string.IsNullOrEmpty(binding.Label)) { continue; }
                string keys = free.Count == claims.Count || binding.KeyText != null ? binding.DisplayKeys() : KeyBinding.Display(free[0]);
                entries.Add(new AppKeyMap.Entry(binding, keys, free[0]));
            }
            _menu = entries.Count == 0 ? null : new AppContextMenu(entries, column, row);
        }

        // Draws the view, then the menu at its anchor, kept inside the frame.
        private sealed class MenuLayer : IWidget {
            private readonly IWidget _content;
            private readonly AppContextMenu _menu;
            internal MenuLayer(IWidget content, AppContextMenu menu) { _content = content; _menu = menu; }
            public Size Measure(Constraints constraints, RenderContext context) => _content.Measure(constraints, context);
            public void Render(Region region, RenderContext context) {
                _content.Render(region, context);
                Size size = _menu.Measure(new Constraints(region.Width, region.Height), context);
                int width = Math.Min(size.Width, region.Width), height = Math.Min(size.Height, region.Height);
                if (width < 6 || height < 3) { _menu.Place(default); return; }
                int x = Math.Max(0, Math.Min(_menu.Column, region.Width - width));
                int y = _menu.Row + height <= region.Height ? _menu.Row : Math.Max(0, region.Height - height);
                Region area = region.Sub(x, y, width, height);
                area.Fill(0, 0, width, height, " ", Styling.Style.Plain);
                _menu.Render(area, context);
                _menu.Place(area.Bounds);
            }
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
