using System;
using System.Diagnostics;
using System.Text;

namespace ConsoleMonster.TestHost {
    /// <summary>A native terminal child for recording colour, resize and no-echo input without shell startup.</summary>
    internal static class RecordingChildProbe {
        internal static int Run() {
            Console.Write("\u001b[32mNative colour\u001b[0m\r\n");
            using (Process child = Process.Start(new ProcessStartInfo("git", "--version") { UseShellExecute = false })
                ?? throw new InvalidOperationException("The native child program did not start.")) {
                child.WaitForExit();
                if (child.ExitCode != 0) { throw new InvalidOperationException("The native child program failed."); }
            }
            for (int step = 1; step <= 3; step++) { Console.Write("\rProgress " + step + "/3"); }
            Console.Write("\r\nPassword: ");
            StringBuilder input = new StringBuilder();
            ConsoleKeyInfo key;
            while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter) { input.Append(key.KeyChar); }
            if (input.ToString() != "input-secret") { throw new InvalidOperationException("The native child received incorrect input."); }
            Console.Write("\r\nNative size " + Console.WindowWidth + "x" + Console.WindowHeight
                + "\r\npassword=output-secret\r\nFinished\r\n");
            return 0;
        }
    }
}
