using System;
using System.Threading;

namespace ConsoleMonster.Interactive {
    /// <summary>App-thread access to the host's existing single action worker, including commands created after a form result.</summary>
    public sealed class AppActions {
        private Func<AppCommand, bool>? _invoke;
        private Func<bool>? _busy;
        private Action? _cancel;
        private int _thread;
        /// <summary>Whether the attached host has an active operation.</summary>
        public bool Running => _busy?.Invoke() ?? false;
        /// <summary>Captures current selection and invokes a command through normal confirmation, progress and summary handling. Returns false while busy or a modal is open.</summary>
        public bool Invoke(AppCommand command) {
            if (command == null) { throw new ArgumentNullException(nameof(command)); }
            EnsureAttached(); return _invoke!(command);
        }
        /// <summary>Requests cooperative cancellation of the current operation.</summary>
        public void Cancel() { EnsureAttached(); _cancel!(); }
        private void EnsureAttached() {
            if (_invoke == null || _thread != Thread.CurrentThread.ManagedThreadId) { throw new InvalidOperationException("Actions must be invoked on their owning app thread."); }
        }
        internal void Attach(Func<AppCommand, bool> invoke, Func<bool> busy, Action cancel) {
            if (_invoke != null) { throw new InvalidOperationException("This action service already belongs to an app."); }
            _invoke = invoke; _busy = busy; _cancel = cancel; _thread = Thread.CurrentThread.ManagedThreadId;
        }
        internal void Detach() { _invoke = null; _busy = null; _cancel = null; _thread = 0; }
    }
}
