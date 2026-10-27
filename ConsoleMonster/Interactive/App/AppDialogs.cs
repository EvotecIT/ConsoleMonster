using System;
using System.Threading;

namespace ConsoleMonster.Interactive {
    /// <summary>Modal form service attached to one application run. Dialog callbacks execute on the owning app thread.</summary>
    public sealed class AppDialogs {
        private Action<Modal>? _show;
        private Func<bool>? _open;
        private int _thread;
        /// <summary>Whether this app currently has a modal input scope.</summary>
        public bool Open => _open?.Invoke() ?? false;
        /// <summary>Opens a typed form; cancellation produces Accepted=false without evaluating the result.</summary>
        public void Show<T>(FormDialog<T> dialog, Action<DialogResult<T>> completed) {
            if (dialog == null) { throw new ArgumentNullException(nameof(dialog)); }
            if (completed == null) { throw new ArgumentNullException(nameof(completed)); }
            if (_show == null || _thread != Thread.CurrentThread.ManagedThreadId) { throw new InvalidOperationException("Dialogs must open on their owning app thread."); }
            if (Open) { throw new InvalidOperationException("Close the current modal before opening another form."); }
            _show(new FormDialogModal<T>(dialog, completed));
        }
        internal void Attach(Action<Modal> show, Func<bool> open) {
            if (_show != null) { throw new InvalidOperationException("This dialog service already belongs to an app."); }
            _show = show; _open = open; _thread = Thread.CurrentThread.ManagedThreadId;
        }
        internal void Detach() { _show = null; _open = null; _thread = 0; }
    }
}
