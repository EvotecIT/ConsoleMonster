using System;
using System.Collections.Generic;
using System.Threading;

namespace ConsoleMonster.Interactive {
    internal sealed partial class FormView {
        private readonly object _applyGate = new object();
        private readonly List<Dictionary<string, object?>> _pendingApplications = new List<Dictionary<string, object?>>();
        private bool _detached;

        /// <summary>Preserves accepted values on close, even if the host never polls its queued callbacks again.</summary>
        public void Detach() {
            lock (_applyGate) {
                _detached = true;
                foreach (Dictionary<string, object?> values in _pendingApplications) {
                    _form.ApplyDetached(values, false);
                }
                _pendingApplications.Clear();
                // Keep advertising this view until the older batches are flushed. Concurrent callers then wait on
                // _applyGate (including callers holding an old view reference) instead of overtaking the handoff.
                _form.Detach(this);
            }
        }

        /// <summary>Applies app values on the owner thread, or keeps them until its queued callback or detach runs.</summary>
        /// <exception cref="InvalidOperationException">The call comes from another thread and the host cannot queue it.</exception>
        public void ApplyFromApp(IDictionary<string, object?> values) {
            Dictionary<string, object?> copy = Form.Copy(values);
            lock (_applyGate) {
                if (_detached) {
                    _form.ApplyDetached(copy, false);
                    return;
                }
                if (Thread.CurrentThread.ManagedThreadId == _ownerThread) {
                    ApplyValues(copy, false, false);
                    return;
                }
                _pendingApplications.Add(copy);
                if (!_host.TryPost(() => ApplyPending(copy))) {
                    _pendingApplications.Remove(copy);
                    throw new InvalidOperationException("Form '" + _form.Title + "' is shown on another thread that cannot take values from this one; apply them from the thread that shows the form or from an action of the menu that shows it.");
                }
            }
        }

        private void ApplyPending(Dictionary<string, object?> values) {
            lock (_applyGate) {
                // Detach may already have applied this batch. A late callback must not overwrite newer values.
                if (!_pendingApplications.Remove(values)) {
                    return;
                }
                if (ReferenceEquals(_form.View, this)) {
                    ApplyValues(values, false, false);
                } else {
                    _form.ApplyDetached(values, false);
                }
            }
        }
    }
}
