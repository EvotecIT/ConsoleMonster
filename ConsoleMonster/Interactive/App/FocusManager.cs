using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>Maintains one enabled input's focus in stable tab order, including explicit and mouse focus requests.</summary>
    public sealed class FocusManager : IDisposable {
        private readonly object _gate = new object();
        private readonly List<InputControl> _controls;
        private InputControl? _current;
        private bool _disposed;

        /// <summary>Discovers inputs in declaration order and applies their tab-order overrides.</summary>
        /// <param name="layout">The stable input layout.</param>
        public FocusManager(IWidget layout) {
            if (layout == null) { throw new ArgumentNullException(nameof(layout)); }
            _controls = Discover(layout).OrderBy(control => control.TabIndex).ToList();
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (InputControl control in _controls) {
                if (!names.Add(control.Key)) { throw new ArgumentException(PromptLabels.Format(AppMessages.Get("DuplicateInput"), control.Key), nameof(layout)); }
            }
            foreach (InputControl control in _controls) {
                control.FocusRequested += Focus;
                control.Focused = false;
                control.Invalidated += Reconcile;
            }
            MoveNext();
        }

        /// <summary>A stable copy of the controls in tab order.</summary>
        public IReadOnlyList<InputControl> Controls => _controls.AsReadOnly();

        /// <summary>The focused control, or null when every control is disabled.</summary>
        public InputControl? Current { get { lock (_gate) { return _current; } } }

        /// <summary>Moves to the next enabled control, wrapping at the end.</summary>
        public void MoveNext() => Move(1);

        /// <summary>Moves to the previous enabled control, wrapping at the beginning.</summary>
        public void MovePrevious() => Move(-1);

        /// <summary>Focuses a control belonging to this manager, if enabled.</summary>
        /// <param name="control">The target control.</param>
        public void Focus(InputControl control) {
            if (control == null) { throw new ArgumentNullException(nameof(control)); }
            lock (_gate) {
                if (_disposed) { throw new ObjectDisposedException(nameof(FocusManager)); }
                if (!_controls.Contains(control)) { throw new ArgumentException(nameof(control)); }
                if (!control.Enabled) { return; }
                InputControl? previous = _current;
                _current = control;
                if (previous != null && previous != control) { previous.Focused = false; }
                control.Focused = true;
            }
        }

        /// <summary>Focuses the enabled control at the zero-based frame coordinates.</summary>
        /// <param name="column">The frame column.</param>
        /// <param name="row">The frame row.</param>
        /// <returns>Whether a control was focused.</returns>
        public bool FocusAt(int column, int row) {
            lock (_gate) {
                foreach (InputControl control in _controls) {
                    Canvas.Rect bounds = control.Bounds;
                    if (control.Enabled && column >= bounds.X && column < bounds.Right && row >= bounds.Y && row < bounds.Bottom) {
                        Focus(control);
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>Detaches explicit focus requests. The final focused state remains available for snapshots.</summary>
        public void Dispose() {
            lock (_gate) {
                if (_disposed) { return; }
                _disposed = true;
                foreach (InputControl control in _controls) { control.FocusRequested -= Focus; control.Invalidated -= Reconcile; }
            }
        }

        private void Move(int delta) {
            lock (_gate) {
                if (_disposed) { throw new ObjectDisposedException(nameof(FocusManager)); }
                int start = _current == null ? (delta > 0 ? -1 : 0) : _controls.IndexOf(_current);
                for (int i = 1; i <= _controls.Count; i++) {
                    int index = (start + delta * i + _controls.Count * 2) % _controls.Count;
                    if (_controls[index].Enabled) { Focus(_controls[index]); return; }
                }
                InputControl? previous = _current;
                _current = null;
                if (previous != null) { previous.Focused = false; }
            }
        }

        private void Reconcile() {
            lock (_gate) {
                if (!_disposed && _current != null && !_current.Enabled) { MoveNext(); }
            }
        }

        internal static IEnumerable<InputControl> Discover(IWidget root) {
            if (root is InputControl input) { yield return input; }
            if (root is IContainer container) {
                foreach (IWidget child in container.Children) {
                    foreach (InputControl descendant in Discover(child)) { yield return descendant; }
                }
            }
        }
    }
}
