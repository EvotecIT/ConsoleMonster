using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management.Automation.Runspaces;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

// Controls a real noncooperative runspace call through the public input boundary, without product hooks.
public sealed class NativeActionInput : IInputSource {
    private static readonly ConcurrentDictionary<string, NativeActionInput> Gates = new ConcurrentDictionary<string, NativeActionInput>();
    private readonly string _id;
    private readonly ScriptedInput _script;
    private readonly ScreenCapture _capture;
    private readonly object _lifecycle = new object();
    private readonly ManualResetEventSlim _entered = new ManualResetEventSlim();
    private readonly ManualResetEventSlim _release = new ManualResetEventSlim();
    private readonly ManualResetEventSlim _exited = new ManualResetEventSlim();
    private readonly ManualResetEventSlim _closed = new ManualResetEventSlim();
    private Runspace _runspace;
    private InputEvent _pendingCancel;
    private Stopwatch _cancelWait;
    private bool _recovering;
    private bool _disposed;
    private CellBuffer _attemptFrame;

    public NativeActionInput(string id, ScriptedInput script, ScreenCapture capture) {
        _id = id;
        _script = script;
        _capture = capture;
        if (!Gates.TryAdd(id, this)) { throw new InvalidOperationException("Duplicate native action gate."); }
    }

    public string Name { get { return "native-action-script"; } }
    public bool HeldThroughRejectedRestart { get; private set; }
    public bool RunspaceClosed { get; private set; }

    // Called by the isolated command. Cancellation cannot interrupt this native wait.
    public static void Hold(string id) {
        NativeActionInput gate = Gates[id];
        lock (gate._lifecycle) {
            if (gate._disposed) { throw new ObjectDisposedException("NativeActionInput"); }
            gate._runspace = Runspace.DefaultRunspace;
            gate._runspace.StateChanged += gate.StateChanged;
            gate._entered.Set();
        }
        try {
            if (!gate._release.Wait(TimeSpan.FromSeconds(60))) {
                throw new TimeoutException("The UI never released the native action gate.");
            }
        } finally { gate._exited.Set(); }
    }

    private void StateChanged(object sender, RunspaceStateEventArgs args) {
        if (args.RunspaceStateInfo.State == RunspaceState.Closed) { _closed.Set(); }
    }

    public InputEvent Read(TimeSpan timeout, CancellationToken cancellationToken) {
        if (timeout == TimeSpan.Zero) { return _script.Read(timeout, cancellationToken); }
        if (_pendingCancel != null) {
            if (_cancelWait.Elapsed >= TimeSpan.FromSeconds(30)) { throw new TimeoutException("The action never entered its native wait."); }
            if (!_entered.Wait(timeout, cancellationToken)) { return null; }
            InputEvent cancel = _pendingCancel;
            _pendingCancel = null;
            return cancel;
        }

        InputEvent next = _script.Read(timeout, cancellationToken);
        if (next != null && next.Is(ConsoleKey.F6) && !_entered.IsSet) {
            _pendingCancel = next;
            _cancelWait = Stopwatch.StartNew();
            return null;
        }
        var frame = _capture.LastFrame;
        string text = frame == null ? string.Empty : frame.ToPlainText();
        if (next != null && next.Is(ConsoleKey.DownArrow)) {
            HeldThroughRejectedRestart = _entered.IsSet && !_exited.IsSet &&
                text.Contains("previous invocation of this command is still stopping");
            if (!HeldThroughRejectedRestart) { throw new InvalidOperationException("Native work escaped before restart rejection."); }
            _release.Set();
            _recovering = true;
        } else if (_recovering && text.Contains("Second row completed")) {
            _recovering = false;
        } else if (_recovering && next == null && _exited.IsSet && text.Contains("Action output") && !ReferenceEquals(frame, _attemptFrame)) {
            // Native return precedes runner disposal. Retry only on a newly rendered idle screen,
            // until the same command visibly succeeds; the queued output wait bounds this recovery.
            _attemptFrame = frame;
            return InputEvent.Press(ConsoleKey.F5);
        }
        return next;
    }

    public void Dispose() {
        bool entered;
        lock (_lifecycle) {
            if (_disposed) { return; }
            _disposed = true;
            _release.Set();
            entered = _entered.IsSet;
        }
        try {
            if (entered) {
                RunspaceClosed = _closed.Wait(TimeSpan.FromSeconds(30));
                if (!RunspaceClosed) { throw new TimeoutException("The released native action did not close its runspace."); }
                _runspace.StateChanged -= StateChanged;
            }
        } finally {
            NativeActionInput removed;
            Gates.TryRemove(_id, out removed);
            // If cleanup timed out, the worker still owns these signals.
            if (!entered || RunspaceClosed) {
                _entered.Dispose();
                _release.Dispose();
                _exited.Dispose();
                _closed.Dispose();
            }
        }
    }
}
