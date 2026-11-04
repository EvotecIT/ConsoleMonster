using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.TestHost {
    // A fresh hidden process is essential: Terminal's static capability initialization runs only once.
    internal static class AppOutputModeProbe {
        internal static void Run(string scenario, string receipt) {
            IntPtr original = GetStdHandle(-11);
            IntPtr buffer = CreateConsoleScreenBuffer(3221225472u, 3, IntPtr.Zero, 1, IntPtr.Zero);
            if (buffer == new IntPtr(-1)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
            try {
                if (!SetStdHandle(-11, buffer) || !SetConsoleMode(buffer, 3)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
                if (scenario == "nested-prompt-signal") {
                    bool observed = false;
                    Menu menuProbe = new Menu("Prompt signal probe");
                    menuProbe.Item("Run", item => item.Action = context => {
                        context.Run(new Widgets.TextBlock("Working"), run => {
                            try {
                                new ConfirmPrompt("Nested", true).Show(new InteractiveOptions {
                                    Input = new SignalCancellationInput(), Mode = InteractionMode.Rich, Theme = Theme.Dark
                                });
                            } catch (OperationCanceledException) { observed = run.CancellationToken.IsCancellationRequested; }
                            return new MenuRunResult("Stopped");
                        }, new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false });
                        context.Exit();
                    });
                    menuProbe.Run(new MenuOptions { ConfirmExit = false, Interactive = new InteractiveOptions {
                        Input = new ScriptedInput("Enter"), Mode = InteractionMode.Rich, Theme = Theme.Dark
                    } });
                    if (!observed || !GetConsoleMode(buffer, out uint after) || after != 3) {
                        throw new InvalidOperationException("The nested prompt did not cancel with its owner's native Ctrl+C signal.");
                    }
                    File.WriteAllText(receipt, "APP_MODE_OK " + scenario + " 3->3\n");
                    return;
                }
                if (scenario == "guard-overlap" || scenario == "guard-reentrant") {
                    using (TerminalGuard outer = new TerminalGuard(() => { }, false)) {
                        Terminal.GetCurrentProfile(); // The actual first-use transition from mode 3 to mode 7.
                        if (scenario == "guard-reentrant") {
                            using TerminalGuard inner = new TerminalGuard(() => {
                                outer.RequestCancellation(); outer.RequestCancellation();
                            }, false);
                            inner.Dispose();
                        } else {
                            using ManualResetEventSlim entered = new ManualResetEventSlim();
                            using ManualResetEventSlim release = new ManualResetEventSlim();
                            using TerminalGuard inner = new TerminalGuard(() => {
                                entered.Set();
                                if (!release.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException("Inner restoration was not released."); }
                            }, false);
                            Task disposing = Task.Run(inner.Dispose);
                            if (!entered.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException("Inner restoration did not start."); }
                            Task emergency = Task.Run(() => { outer.RequestCancellation(); outer.RequestCancellation(); });
                            try {
                                // Hold the inner mode long enough for emergency cleanup to reach it. This arranges the
                                // overlap; correctness is the native mode after both operations, not elapsed time.
                                emergency.Wait(TimeSpan.FromMilliseconds(250));
                            } finally { release.Set(); }
                            if (!Task.WaitAll(new[] { disposing, emergency }, TimeSpan.FromSeconds(10))) {
                                throw new TimeoutException("Concurrent restoration did not finish.");
                            }
                        }
                        if (!GetConsoleMode(buffer, out uint restored) || restored != 3) {
                            throw new InvalidOperationException("Overlapping emergency output mode was not restored: 3 -> " + restored + ".");
                        }
                        File.WriteAllText(receipt, "APP_MODE_OK " + scenario + " 3->3\n");
                    }
                    return;
                }
                void ExitWithReceipt() {
                    AppDomain.CurrentDomain.ProcessExit += (_, _) => {
                        string result = GetConsoleMode(buffer, out uint restored) && restored == 3
                            ? "APP_MODE_OK " + scenario + " 3->3\n"
                            : "Emergency output mode was not restored: 3 -> " + restored + ".\n";
                        File.WriteAllText(receipt, result);
                    };
                    Environment.Exit(0);
                }
                bool menu = scenario.StartsWith("menu-", StringComparison.Ordinal);
                bool prompt = scenario.StartsWith("prompt-", StringComparison.Ordinal);
                string path = menu ? scenario.Substring(5) : prompt ? scenario.Substring(7) : scenario;
                using (ScriptedInput input = new ScriptedInput(path == "exception" ? "wait:0" : prompt || (menu && path == "terminate") ? "Enter" : menu ? "Ctrl+Q" : "Escape"))
                using (VtOutput output = new VtOutput(new StringTerminalWriter(), TerminalProfile.Fixed(80, 24)))
                using (CancellationTokenSource cancel = new CancellationTokenSource()) {
                    ConsoleAppOptions options = new ConsoleAppOptions {
                        Mouse = false,
                        // No Profile/Target: resolving capabilities must occur inside Run, for its first use.
                        Interactive = new InteractiveOptions { Input = input, Mode = InteractionMode.Rich, Theme = Theme.Dark },
                        Live = new LiveSessionOptions { Output = output, CaptureConsole = false, TaskbarProgress = false }
                    };
                    if (path == "terminate" && !menu) { options.ProcessEvents = _ => ExitWithReceipt(); }
                    if (path == "cancel") { cancel.Cancel(); }
                    InvalidOperationException expected = new InvalidOperationException("Synthetic application startup failure.");
                    // Throw only when the real event loop reads, after the initial view has been drawn.
                    if (path == "exception") { input.SkippedWait = _ => throw expected; }
                    try {
                        if (menu) {
                            Menu nativeMenu = new Menu("Output mode probe");
                            nativeMenu.Item("Quit", item => {
                                item.Action = context => {
                                    if (path == "terminate") {
                                        context.Run(new Widgets.TextBlock("Active nested run"), _ => {
                                            ExitWithReceipt();
                                            return new MenuRunResult("Unreachable");
                                        }, new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false });
                                    } else { context.Exit(); }
                                };
                            });
                            if (path == "startup") {
                                nativeMenu.KeyBindings.Add(KeyBinding.Numbered("probe", () => throw expected, _ => { }));
                            }
                            nativeMenu.Run(new MenuOptions { ConfirmExit = false, Layout = MenuLayout.Shell, Interactive = options.Interactive }, cancel.Token);
                        } else if (prompt) {
                            SelectPrompt<string> nativePrompt = new SelectPrompt<string>("Output mode probe", new[] { "answer" });
                            if (path == "startup") {
                                nativePrompt.Choices[0].Preview = new Widgets.ContextWidget(_ => throw expected);
                            }
                            if (nativePrompt.Show(options.Interactive, cancel.Token) != "answer") {
                                throw new InvalidOperationException("The native prompt returned an unexpected answer.");
                            }
                        } else {
                            ConsoleApp.Run(scenario == "startup" ? (Layout.IWidget)new AppBinding(_ => throw expected) : new TextInput("query"),
                                options: options, cancellationToken: cancel.Token);
                        }
                        if (path != "normal") { throw new InvalidOperationException("The failure path did not execute."); }
                    } catch (OperationCanceledException) when (path == "cancel" && cancel.IsCancellationRequested) {
                    } catch (InvalidOperationException error) when ((path == "startup" || path == "exception") && ReferenceEquals(error, expected)) {
                    }
                    if (!GetConsoleMode(buffer, out uint after) || after != 3) {
                        throw new InvalidOperationException("First-use output mode was not restored: 3 -> " + after + ".");
                    }
                    File.WriteAllText(receipt, "APP_MODE_OK " + scenario + " 3->3\n");
                }
            } finally {
                SetStdHandle(-11, original);
                CloseHandle(buffer);
            }
        }

        private sealed class SignalCancellationInput : IInputSource {
            public string Name => "native Ctrl+C during prompt";
            public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
                if (!GenerateConsoleCtrlEvent(0, 0)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
                if (!cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5))) {
                    throw new TimeoutException("Owner cancellation did not reach the nested prompt.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }
            public void Dispose() { }
        }

        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GenerateConsoleCtrlEvent(uint signal, uint group);
        [DllImport("kernel32.dll")] private static extern IntPtr GetStdHandle(int id);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetStdHandle(int id, IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr CreateConsoleScreenBuffer(uint access, uint share, IntPtr security, uint flags, IntPtr reserved);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetConsoleMode(IntPtr handle, out uint mode);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetConsoleMode(IntPtr handle, uint mode);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    }
}
