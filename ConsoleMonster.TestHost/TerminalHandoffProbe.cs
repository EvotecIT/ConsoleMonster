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
        internal static int Windows(string receipt) {
            var handoff = new TerminalHandoff();
            var field = new TextInput("Value", "retained");
            bool ran = false;
            var options = new ConsoleAppOptions {
                TerminalHandoff = handoff,
                Interactive = new InteractiveOptions { Mode = InteractionMode.Rich, Profile = TerminalProfile.Fixed(80, 24) },
                ProcessEvents = state => {
                    if (ran) { return; }
                    ran = true;
                    var child = new TerminalChildOptions { FileName = "cmd.exe", Timeout = TimeSpan.FromSeconds(10) };
                    child.Arguments.Add("/d"); child.Arguments.Add("/c"); child.Arguments.Add("echo CHILD_FINAL & exit /b 7");
                    int exit = handoff.SuspendForChild(child);
                    if (exit != 7 || !handoff.LastOutput.Contains("CHILD_FINAL") || !field.Focused || field.Value != "retained") {
                        throw new InvalidOperationException("ConPTY state: exit=" + exit + ", focused=" + field.Focused + ", value=" + field.Value + ", output=" + handoff.LastOutput);
                    }
                    child.Arguments[2] = "pause >nul";
                    using (var cancellation = new CancellationTokenSource(150)) {
                        try { handoff.SuspendForChild(child, cancellation.Token); throw new InvalidOperationException("Child cancellation was lost."); }
                        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
                    }
                    state.RequestExit();
                }
            };
            ConsoleApp.Run(field, options: options);
            File.WriteAllText(receipt, "CONPTY_HANDOFF_OK\nexit=7\nfinal-output=retained\nfocus=retained\ncancel=restored\n");
            return 0;
        }
        internal static int Run(string resultPath) {
            // Establish the CLR's console initialization before the outer PTY records the app baseline.
            using (IInputSource? source = ConsoleInput.OpenConsole()) { }
            Console.WriteLine("HANDOFF_BASELINE");
            WaitFor(resultPath + ".start");
            var listing = new Grid("TTY handoff"); listing.AddColumn("Name");
            listing.AddRow("first", "first"); listing.AddRow("second", "second");
            using var grid = new DataGrid("Rows", listing);
            var handoff = new TerminalHandoff();
            using var appCancellation = new CancellationTokenSource();
            var profile = TerminalProfile.Fixed(80, 24);
            bool startupReady = false;
            var options = new ConsoleAppOptions {
                Interactive = new InteractiveOptions { Profile = profile, Mode = InteractionMode.Rich },
                SignalKeys = false, TerminalHandoff = handoff,
                ProcessEvents = _ => {
                    if (startupReady) { return; }
                    startupReady = true;
                    File.WriteAllText(resultPath + ".ready", "ready");
                    WaitFor(resultPath + ".continue");
                },
                HandleInput = (state, input) => {
                    if (input.Key == ConsoleKey.F2 || input.Key == ConsoleKey.F3 || input.Key == ConsoleKey.F4) {
                        string expected = input.Key == ConsoleKey.F2 ? "timeout" : input.Key == ConsoleKey.F3 ? "cancelled" : "invalid";
                        string retained = handoff.LastOutput;
                        var child = new TerminalChildOptions { FileName = "/bin/sh", Timeout = TimeSpan.FromMilliseconds(150) };
                        child.Arguments.Add("-c"); child.Arguments.Add("sleep 5");
                        using var cancellation = new CancellationTokenSource();
                        if (input.Key == ConsoleKey.F3) { child.Timeout = TimeSpan.FromSeconds(10); cancellation.CancelAfter(150); }
                        if (input.Key == ConsoleKey.F4) { child.FileName = "/consolemonster-missing-child"; }
                        try { handoff.SuspendForChild(child, cancellation.Token); throw new InvalidOperationException("Expected " + expected); }
                        catch (TimeoutException) when (expected == "timeout") { }
                        catch (OperationCanceledException) when (expected == "cancelled") { }
                        catch (System.ComponentModel.Win32Exception) when (expected == "invalid") {
                            if (handoff.LastOutput != retained) { throw new InvalidOperationException("Startup validation lost prior output."); }
                        }
                        File.AppendAllText(resultPath, "\nrestored=" + expected + ";focus=" + grid.CurrentRow?.Item);
                        grid.Label = "RESTORED_" + expected;
                        return true;
                    }
                    if (input.Is(ConsoleKey.F9)) {
                        var child = new TerminalChildOptions { FileName = "/bin/sh", Timeout = TimeSpan.FromSeconds(10) };
                        child.Arguments.Add("-c"); child.Arguments.Add("sleep 5");
                        appCancellation.CancelAfter(150);
                        handoff.SuspendForChild(child);
                        throw new InvalidOperationException("The app cancellation must cancel handoff.");
                    }
                    if (input.Is(ConsoleKey.F5)) {
                        var child = new TerminalChildOptions { FileName = "/bin/sh", Timeout = TimeSpan.FromSeconds(20) };
                        child.Arguments.Add("-c");
                        child.Arguments.Add("test -t 0 && test -t 1 && test -t 2 || exit 19; printf '\\033[32mCHILD_READY\\033[0m\\n'; IFS= read -r answer; printf 'CHILD_ANSWER:%s\\n' \"$answer\"; printf 'CHILD_SIZE:'; stty size; exit 7");
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
            try { ConsoleApp.Run(grid, options: options, cancellationToken: appCancellation.Token); }
            catch (OperationCanceledException) when (appCancellation.IsCancellationRequested) { File.AppendAllText(resultPath, "\napp-cancellation=restored"); }
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
