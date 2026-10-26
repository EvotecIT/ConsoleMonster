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
                return new Overlay(content, new KeyMapWidget(bindings, Labels), dim: !frame.Inline) { Inline = frame.Inline };
            }
            return content;
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
