using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.TestHost {
    // detect-mode: a hidden console runs a parent that sets stdout's mode without VT, then a child sharing that console
    // runs capability detection and exits. The parent reads the console mode the child left behind.
    public static partial class Program {
        private static int LaunchDetectionMode(string receipt, string? runtime) =>
            LaunchHidden(Self(runtime, "detect-mode-parent", receipt, runtime ?? string.Empty));

        private static int DetectionModeParent(string receipt, string runtime) {
            IntPtr output = GetStdHandle(-11);
            if (!SetConsoleMode(output, 3)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
            List<string> child = Self(runtime.Length == 0 ? null : runtime, "detect-mode-child");
            ProcessStartInfo info = new ProcessStartInfo(child[0], string.Join(" ", child.Skip(1).Select(value => CommandLine.Quote(value, CommandStyle.Windows)))) {
                UseShellExecute = false
            };
            int exit;
            using (Process process = Process.Start(info)!) {
                if (!process.WaitForExit(30000)) { process.Kill(); throw new TimeoutException("The detection child did not exit."); }
                exit = process.ExitCode;
            }
            if (!GetConsoleMode(output, out uint after)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
            File.WriteAllText(receipt, "DETECT_MODE " + exit + " 3->" + after + "\n");
            return 0;
        }

        // Detection turns VT on for the rest of the process; exit 2 would mean it did not.
        private static int DetectionModeChild() {
            Terminal.GetCurrentProfile();
            return GetConsoleMode(GetStdHandle(-11), out uint mode) && (mode & 4) != 0 ? 0 : 2;
        }

        private static List<string> Self(string? runtime, params string[] arguments) {
            List<string> command = new List<string> { Process.GetCurrentProcess().MainModule!.FileName! };
#if !NETFRAMEWORK
            command.Add("exec");
            command.Add("--runtimeconfig");
            command.Add(runtime ?? throw new InvalidOperationException("No host runtime configuration."));
            command.Add(typeof(Program).Assembly.Location);
#endif
            command.AddRange(arguments);
            return command;
        }
    }
}
