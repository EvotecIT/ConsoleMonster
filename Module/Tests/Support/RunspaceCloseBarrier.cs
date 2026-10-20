using System;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

// Holds a real asynchronous close in its Closing transition so ownership checks do not depend on a scheduler race.
public sealed class RunspaceCloseBarrier : IDisposable {
    private readonly Runspace _owner;
    private readonly ManualResetEventSlim _closing = new ManualResetEventSlim();
    private readonly ManualResetEventSlim _release = new ManualResetEventSlim();
    private readonly ManualResetEventSlim _closed = new ManualResetEventSlim();
    private Task _request;
    public RunspaceCloseBarrier(Runspace owner) { _owner = owner; owner.StateChanged += Changed; }
    public void BeginClose() { _request = Task.Run(new Action(_owner.CloseAsync)); }
    public bool WaitUntilClosing() { return _closing.Wait(15000); }
    public bool WaitUntilClosed() { return _closed.Wait(15000); }
    public void Release() { _release.Set(); }
    private void Changed(object sender, RunspaceStateEventArgs args) {
        if (args.RunspaceStateInfo.State == RunspaceState.Closing) {
            _closing.Set();
            _release.Wait(15000);
        } else if (args.RunspaceStateInfo.State == RunspaceState.Closed) {
            _closed.Set();
        }
    }
    public void Dispose() {
        _release.Set();
        if (_request != null) { _request.Wait(15000); _closed.Wait(15000); }
        _owner.StateChanged -= Changed;
        _closing.Dispose();
        _release.Dispose();
        _closed.Dispose();
    }
}
