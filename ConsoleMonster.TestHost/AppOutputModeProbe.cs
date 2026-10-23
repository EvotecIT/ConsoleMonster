using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
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
                using (ScriptedInput input = new ScriptedInput("Escape"))
                using (VtOutput output = new VtOutput(new StringTerminalWriter(), TerminalProfile.Fixed(80, 24)))
                using (CancellationTokenSource cancel = new CancellationTokenSource()) {
                    ConsoleAppOptions options = new ConsoleAppOptions {
                        Mouse = false,
                        // No Profile/Target: resolving capabilities must occur inside Run, for its first use.
                        Interactive = new InteractiveOptions { Input = input, Mode = InteractionMode.Rich, Theme = Theme.Dark },
                        Live = new LiveSessionOptions { Output = output, CaptureConsole = false, TaskbarProgress = false }
                    };
                    if (scenario == "cancel") { cancel.Cancel(); }
                    InvalidOperationException expected = new InvalidOperationException("Synthetic application startup failure.");
                    try {
                        ConsoleApp.Run(scenario == "startup" ? (Layout.IWidget)new AppBinding(_ => throw expected) : new TextInput("query"),
                            options: options, cancellationToken: cancel.Token);
                        if (scenario != "normal") { throw new InvalidOperationException("The failure path did not execute."); }
                    } catch (OperationCanceledException) when (scenario == "cancel" && cancel.IsCancellationRequested) {
                    } catch (InvalidOperationException error) when (scenario == "startup" && ReferenceEquals(error, expected)) {
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

        [DllImport("kernel32.dll")] private static extern IntPtr GetStdHandle(int id);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetStdHandle(int id, IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr CreateConsoleScreenBuffer(uint access, uint share, IntPtr security, uint flags, IntPtr reserved);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetConsoleMode(IntPtr handle, out uint mode);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetConsoleMode(IntPtr handle, uint mode);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    }
}
