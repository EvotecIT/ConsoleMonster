using System;
using System.Threading;

// A native worker holds the real host writer's lock without running a PowerShell callback off its pipeline thread.
public sealed class HeldMonitor : IDisposable {
    private readonly ManualResetEventSlim _entered = new ManualResetEventSlim();
    private readonly ManualResetEventSlim _release = new ManualResetEventSlim();
    private readonly Thread _worker;

    public HeldMonitor(object gate) {
        _worker = new Thread(() => {
            lock (gate) {
                _entered.Set();
                _release.Wait(TimeSpan.FromSeconds(30));
            }
        }) { IsBackground = true };
        _worker.Start();
        if (!_entered.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException("The writer lock was not acquired."); }
    }

    public void Dispose() {
        _release.Set();
        if (!_worker.Join(TimeSpan.FromSeconds(10))) { throw new TimeoutException("The writer lock was not released."); }
        _entered.Dispose();
        _release.Dispose();
    }
}
