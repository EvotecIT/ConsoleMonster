using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Demos;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary>PowerShell host relay for the shared demo engine.</summary>
    public abstract class ConsoleDemoCommand : PSCmdlet {
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        /// <summary><para type="description">Plain PowerShell demo file with optional # demo: directives.</para></summary>
        [Parameter(Mandatory = true, Position = 0)] public string Path { get; set; } = string.Empty;
        /// <summary><para type="description">Terminal width in cells.</para></summary>
        [Parameter, ValidateRange(2, 1000)] public int Width { get; set; } = 100;
        /// <summary><para type="description">Terminal height in cells.</para></summary>
        [Parameter, ValidateRange(2, 1000)] public int Height { get; set; } = 28;
        /// <summary><para type="description">Seed for repeatable character timing.</para></summary>
        [Parameter] public int Seed { get; set; } = 42;
        /// <summary><para type="description">Maximum wait for a semantic prompt or output pattern.</para></summary>
        [Parameter, ValidateRange(1, 3600)] public int StepTimeoutSeconds { get; set; } = 30;
        /// <summary><para type="description">Longest recorded idle gap in seconds; zero preserves actual timing.</para></summary>
        [Parameter, ValidateRange(0, 86400)] public double IdleLimit { get; set; } = 2;
        /// <summary><para type="description">Output patterns redacted before recording.</para></summary>
        [Parameter] public string[] Redact { get; set; } = Array.Empty<string>();
        /// <summary><para type="description">Child PowerShell executable. Defaults to the current PowerShell.</para></summary>
        [Parameter] public string? Shell { get; set; }
        /// <summary>Runs and drains the child on the pipeline thread; no PowerShell scriptblock is invoked on a reader thread.</summary>
        protected void RunDemo(string? destination, bool presenter) {
            if (presenter && Console.IsInputRedirected) { throw new InvalidOperationException("Live presenter mode needs a terminal. Use Invoke-ConsoleDemo for unattended recording."); }
            string source = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
            DemoScript script = DemoScript.Read(source);
            bool temporary = destination == null;
            string output = destination == null ? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "consolemonster-demo-" + Guid.NewGuid().ToString("N") + ".cast") : SessionState.Path.GetUnresolvedProviderPathFromPSPath(destination);
            using (RecordingOutputRelay relay = new RecordingOutputRelay())
            using (BlockingCollection<ConsoleKeyInfo> keys = new BlockingCollection<ConsoleKeyInfo>())
            using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token, relay.Token)) {
                DemoOptions options = new DemoOptions {
                    Recording = new SessionRecordingOptions {
                        Path = output, Shell = Shell ?? Process.GetCurrentProcess().MainModule!.FileName,
                        Width = Width, Height = Height, WorkingDirectory = SessionState.Path.CurrentFileSystemLocation.Path,
                        IdleLimit = TimeSpan.FromSeconds(IdleLimit), Name = System.IO.Path.GetFileNameWithoutExtension(source)
                    },
                    Seed = Seed, StepTimeout = TimeSpan.FromSeconds(StepTimeoutSeconds), Output = relay.Receive
                };
                foreach (string pattern in Redact) { options.Recording.Redact.Add(pattern); }
                // The module is imported only inside this dedicated child, using its actual installed entrypoint.
                options.Recording.Environment["CM_DEMO_MODULE"] = PSModuleImport.PathFor(this);
                string? title = null;
                int step = 0, count = 0;
                Task<string>? run = null;
                try {
                    if (presenter) { try { title = Host.UI.RawUI.WindowTitle; } catch (NotImplementedException) { } }
                    run = Task.Run(() => presenter ? ConsoleDemo.Present(script, options, () => keys.Take(linked.Token),
                        (current, total) => { Volatile.Write(ref count, total); Volatile.Write(ref step, current); }, linked.Token) : ConsoleDemo.Record(script, options, linked.Token));
                    int last = 0;
                    bool quit = false;
                    while (!run.IsCompleted) {
                        relay.Drain(Host.UI.Write);
                        if (presenter) {
                            if (Console.KeyAvailable) {
                                ConsoleKeyInfo key = Console.ReadKey(true);
                                if (DemoPresenter.Action(key) == DemoPresenterAction.Quit) { quit = true; linked.Cancel(); }
                                else if (!linked.IsCancellationRequested) { keys.Add(key, linked.Token); }
                            }
                            int current = Volatile.Read(ref step);
                            if (current != last) {
                                try { Host.UI.RawUI.WindowTitle = "Demo " + current + "/" + Volatile.Read(ref count) + " · Enter run · Space type · b back · s skip · q quit"; }
                                catch (NotImplementedException) { }
                                last = current;
                            }
                        }
                        Thread.Sleep(10);
                    }
                    try { run.GetAwaiter().GetResult(); }
                    catch (OperationCanceledException) when (quit) { }
                    while (relay.Drain(Host.UI.Write)) { }
                    if (!temporary) { WriteObject(new FileInfo(output)); }
                } finally {
                    linked.Cancel(); relay.Stop();
                    FinishDemo(run,
                        () => { if (title != null) { try { Host.UI.RawUI.WindowTitle = title; } catch (NotImplementedException) { } } },
                        () => {
                            if (!temporary) { return; }
                            try { if (File.Exists(output)) { File.Delete(output); } }
                            finally { if (File.Exists(output + ".json")) { File.Delete(output + ".json"); } }
                        }, _stop);
                }
            }
        }
        // Every owned resource is released even when the task or an earlier cleanup fails.
        private static void FinishDemo(Task<string>? run, Action restoreTitle, Action removeRecording, IDisposable stop) {
            try { if (run != null) { try { run.GetAwaiter().GetResult(); } catch (OperationCanceledException) { } } }
            finally {
                try { restoreTitle(); }
                finally { try { removeRecording(); } finally { stop.Dispose(); } }
            }
        }
        /// <inheritdoc />
        protected override void StopProcessing() { try { _stop.Cancel(); } catch (ObjectDisposedException) { } }
    }
}
