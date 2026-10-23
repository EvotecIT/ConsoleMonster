using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.TestHost {
    // Invoked in a fresh, hidden Windows console before Terminal's first capability lookup.
    public static class GridOutputModeProbe {
        public static void Verify(Action action) {
            IntPtr original = GetStdHandle(-11);
            IntPtr buffer = CreateConsoleScreenBuffer(3221225472u, 3, IntPtr.Zero, 1, IntPtr.Zero);
            if (buffer == new IntPtr(-1)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
            try {
                if (!SetStdHandle(-11, buffer) || !SetConsoleMode(buffer, 3)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
                action();
                if (!GetConsoleMode(buffer, out uint after) || after != 3) {
                    throw new InvalidOperationException("First-use grid output mode was not restored: 3 -> " + after + ".");
                }
            } finally {
                SetStdHandle(-11, original);
                CloseHandle(buffer);
            }
        }

        internal static void Run(string scenario, string receipt) {
            Verify(() => {
                using (ScriptedInput input = new ScriptedInput(scenario == "accepted" ? "F10" : "Escape"))
                using (CancellationTokenSource cancel = new CancellationTokenSource()) {
                    Grid grid = new Grid("Mode restoration").AddColumn("Name").AddRow("alpha", "alpha");
                    InteractiveOptions interactive = new InteractiveOptions { Input = input, Mode = InteractionMode.Rich };
                    if (scenario == "unavailable") { interactive.Mode = InteractionMode.None; }
                    if (scenario == "cancelled") { cancel.Cancel(); }
                    try {
                        GridResult result = grid.Show(new GridOptions { Interactive = interactive }, cancel.Token);
                        if (scenario == "unavailable" || scenario == "cancelled") { throw new InvalidOperationException("The failure path did not execute."); }
                        if (result.Accepted != (scenario == "accepted")) { throw new InvalidOperationException("Unexpected grid result."); }
                    } catch (PromptUnavailableException) when (scenario == "unavailable") {
                    } catch (OperationCanceledException) when (scenario == "cancelled" && cancel.IsCancellationRequested) {
                    }
                    if (ConsoleInput.IsAcquired) { throw new InvalidOperationException("Grid input lease was not released."); }
                }
            });
            File.WriteAllText(receipt, "GRID_MODE_OK " + scenario + " 3->3\n");
        }

        [DllImport("kernel32.dll")] private static extern IntPtr GetStdHandle(int id);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetStdHandle(int id, IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr CreateConsoleScreenBuffer(uint access, uint share, IntPtr security, uint flags, IntPtr reserved);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetConsoleMode(IntPtr handle, out uint mode);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetConsoleMode(IntPtr handle, uint mode);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    }
}
