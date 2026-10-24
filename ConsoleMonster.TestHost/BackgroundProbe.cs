using System;
using System.IO;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Output;

namespace ConsoleMonster.TestHost {
    internal static class BackgroundProbe {
        internal static int Run(string receipt, bool hostInput) {
            string input = string.Empty;
            // Resolve ordinary output first: a later owned probe must refresh any cached fallback appearance.
            _ = Terminal.Theme;
            if (hostInput) {
                Console.Write("HOST_READY");
                Thread.Sleep(150);
                TerminalBackground.TryDetect(out _);
                Terminal.WriteMarkupLine("Host input remains available");
                input = Console.ReadLine() ?? string.Empty;
                File.WriteAllText(receipt + ".value", "native|" + input);
            } else using (InputLease lease = ConsoleInput.Acquire()) {
                TerminalBackground.TryDetect(out TerminalBackgroundInfo background);
                if (ReferenceEquals(Terminal.Theme, ConsoleMonster.Styling.Theme.Light) != background.IsLight) {
                    throw new InvalidOperationException("The input-startup background did not refresh the theme.");
                }
                DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                while (DateTime.UtcNow < deadline) {
                    InputEvent? next = lease.Source.Read(TimeSpan.FromMilliseconds(100), CancellationToken.None);
                    if (next == null) { continue; }
                    if (next.Is(ConsoleKey.Enter)) { break; }
                    if (next.IsText) { input += next.Char; }
                }
                File.WriteAllText(receipt + ".value", background.Source + "|" + background.IsLight + "|" + input);
            }
            File.WriteAllText(receipt + ".restored", "yes");
            for (int i = 0; i < 500 && !File.Exists(receipt + ".ack"); i++) { Thread.Sleep(10); }
            return 0;
        }
    }
}
