using System;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Keeps one isolated operation per command alive until its worker has released its runspace.</summary>
    internal sealed class PSCommandRunner {
        private readonly string _source;
        private readonly string _arguments;
        private readonly string[] _modules;
        private int _active;

        internal PSCommandRunner(string source, string arguments, string[] modules) {
            _source = source;
            _arguments = arguments;
            _modules = modules;
        }

        internal async Task InvokeAsync(AppActionContext context, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            if (Interlocked.CompareExchange(ref _active, 1, 0) != 0) {
                throw new InvalidOperationException("The previous invocation of this command is still stopping.");
            }
            PSCommandOperation operation = new PSCommandOperation(context, _source, _arguments, _modules, token);
            Task work;
            try {
                // Only this worker disposes PowerShell resources. A native call may ignore BeginStop until it returns.
                work = Task.Run(() => {
                    try { operation.Run(); }
                    finally { Interlocked.Exchange(ref _active, 0); }
                });
            } catch { Interlocked.Exchange(ref _active, 0); throw; }
            TaskCompletionSource<bool> cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (token.Register(() => cancelled.TrySetResult(true))) {
                if (await Task.WhenAny(work, cancelled.Task).ConfigureAwait(false) == work) {
                    await work.ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    return;
                }
                // Never execute PowerShell stop callbacks on the UI cancellation caller.
                _ = Task.Run(operation.RequestStop).ContinueWith(stopped => { _ = stopped.Exception; }, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                if (await Task.WhenAny(work, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false) == work) {
                    try { await work.ConfigureAwait(false); }
                    catch (Exception) when (token.IsCancellationRequested) { throw new OperationCanceledException(token); }
                } else {
                    // Observe eventual faults without blocking the UI or disposing resources under the worker.
                    _ = work.ContinueWith(completed => { _ = completed.Exception; }, CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    context.Write("Cancellation requested; the native operation is still stopping. This command is unavailable until it exits.", "warn");
                }
                throw new OperationCanceledException(token);
            }
        }
    }
}
