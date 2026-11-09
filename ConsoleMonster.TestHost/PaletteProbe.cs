using System;
using System.IO;
using System.Text;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.TestHost {
    internal static class PaletteProbe {
        internal static int Run(string receipt) {
            string input = string.Empty;
            Terminal.Theme = Theme.Terminal;
            _ = Terminal.Profile;
            using (InputLease lease = ConsoleInput.Acquire()) {
                TerminalScheme? scheme = Terminal.Palette;
                StringBuilder text = new StringBuilder(scheme == null ? "unknown" : scheme.Foreground + "|" + scheme.Background + "|" + string.Join(",", scheme.Palette));
                DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                while (DateTime.UtcNow < deadline) {
                    InputEvent? next = lease.Source.Read(TimeSpan.FromMilliseconds(100), CancellationToken.None);
                    if (next == null) { continue; }
                    if (next.Is(ConsoleKey.Enter)) { break; }
                    if (next.IsText) { input += next.Char; }
                }
                File.WriteAllText(receipt + ".value", text + "|" + input);
                File.WriteAllText(receipt + ".svg", ConsoleExporter.ToSvg(new TextBlock("[pass]pass[/]"), new ExportOptions { Theme = ExportTheme.Current, Width = 20 }));
                using (SessionRecorder recording = SessionRecorder.Start(new SessionRecordingOptions { InProcess = true, Path = receipt + ".cast", Width = 20, Height = 5 })) { recording.CaptureOutput("\u001b[32mpass\u001b[0m"); }
                File.WriteAllText(receipt + ".recording.svg", SessionRecording.Read(receipt + ".cast").Export(ExportFormat.Svg));
                File.WriteAllText(receipt + ".theme.json", ThemeFile.FromTheme(Terminal.Theme).ToJson());
                File.WriteAllText(receipt + ".decision", string.Join("\n", Terminal.Profile.Decisions));
                // Palette checks use the same cached exchange after the keyboard has started.
                ThemeCheckReport check = ThemeCheck.Run(Terminal.Theme);
                File.WriteAllText(receipt + ".check", string.Join(";", System.Linq.Enumerable.Select(check.Results, r => r.Outcome.ToString())));
            }
            File.WriteAllText(receipt + ".restored", "yes");
            for (int i = 0; i < 500 && !File.Exists(receipt + ".ack"); i++) { Thread.Sleep(10); }
            return 0;
        }
    }
}
