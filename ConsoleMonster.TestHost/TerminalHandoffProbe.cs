using System;
using System.IO;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using Grid = ConsoleMonster.Interactive.Grid;

namespace ConsoleMonster.TestHost {
    internal static class TerminalHandoffProbe {
        internal static int Run(string resultPath) {
            // Establish the CLR's console initialization before the outer PTY records the app baseline.
            using (IInputSource? source = ConsoleInput.OpenConsole()) { }
            Console.WriteLine("HANDOFF_BASELINE");
            WaitFor(resultPath + ".start");
            var listing = new Grid("TTY handoff"); listing.AddColumn("Name");
            listing.AddRow("first", "first"); listing.AddRow("second", "second");
            using var grid = new DataGrid("Rows", listing);
            var handoff = new TerminalHandoff();
            var profile = TerminalProfile.Fixed(80, 24);
            var options = new ConsoleAppOptions {
                Interactive = new InteractiveOptions { Profile = profile, Mode = InteractionMode.Rich },
                SignalKeys = false, TerminalHandoff = handoff,
                HandleInput = (state, input) => {
                    if (input.Is(ConsoleKey.F5)) {
                        var child = new TerminalChildOptions { FileName = "/bin/sh", Timeout = TimeSpan.FromSeconds(20) };
                        child.Arguments.Add("-c");
                        child.Arguments.Add("test -t 0 && test -t 1 && test -t 2 || exit 19; printf '\\033[32mCHILD_READY\\033[0m\\n'; IFS= read -r answer; printf 'CHILD_ANSWER:%s\\n' \"$answer\"; exit 7");
                        int exit = handoff.SuspendForChild(child);
                        File.WriteAllText(resultPath, "exit=" + exit + "\nfocus=" + grid.CurrentRow?.Item + "\n" + handoff.LastOutput);
                        grid.Label = "RESUMED";
                        return true;
                    }
                    if (input.Is(ConsoleKey.O, KeyModifiers.Control)) { handoff.ShowOutput(); return true; }
                    if (input.IsText && input.Char == 'q') { state.RequestExit(); return true; }
                    return false;
                }
            };
            ConsoleApp.Run(grid, options: options);
            Console.WriteLine("HANDOFF_RESTORED");
            WaitFor(resultPath + ".finish");
            return 0;
        }

        private static void WaitFor(string path) {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (!File.Exists(path)) {
                if (DateTime.UtcNow >= deadline) { throw new TimeoutException("Native handoff driver did not acknowledge " + Path.GetFileName(path)); }
                Thread.Sleep(10);
            }
        }
    }
}
