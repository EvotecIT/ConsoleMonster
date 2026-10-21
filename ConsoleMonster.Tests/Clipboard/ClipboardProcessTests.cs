using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleMonster.Clipboard;
using Xunit;

namespace ConsoleMonster.Tests.Clipboard {
    /// <summary>Exercises the real process boundary with mock providers that only write a task-owned file or return a status.</summary>
    public sealed class ClipboardProcessTests {
        private static bool Windows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [Fact]
        public void MockExecutableReceivesExactUtf8StdinWithoutBomOrShellEvaluation() {
            string path = Path.Combine(Path.GetTempPath(), "consolemonster-clipboard-" + Guid.NewGuid().ToString("N") + ".txt");
            string text = "日本 zażółć <div>\n`$(do-not-run); & \"quotes\"\n";
            try {
                Command command = Windows
                    ? PowerShell("[Console]::InputEncoding = New-Object System.Text.UTF8Encoding($false); [IO.File]::WriteAllText('" + path.Replace("'", "''") + "', [Console]::In.ReadToEnd(), (New-Object System.Text.UTF8Encoding($false)))")
                    : Shell("cat > '" + path.Replace("'", "'\\''") + "'");
                // This fixture starts PowerShell, whose cold startup under CI load is slower than a clipboard utility.
                // The separate blocked-provider test protects the deadline contract; this case protects exact bytes.
                Assert.True(new ClipboardPlatform().Run(command.Executable, command.Arguments, text, 30000, out string? error), error);
                Assert.Equal(new UTF8Encoding(false).GetBytes(text), File.ReadAllBytes(path));
            } finally {
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void MockProviderNonzeroExitIsAFailure() {
            Command command = Windows ? PowerShell("[Console]::In.ReadToEnd() | Out-Null; exit 23") : Shell("cat >/dev/null; exit 23");
            Assert.False(new ClipboardPlatform().Run(command.Executable, command.Arguments, "synthetic", 30000, out string? error));
            Assert.Contains("23", error!);
        }

        [Fact]
        public void MissingExecutableDoesNotThrowOrEchoTheDocument() {
            string text = "private synthetic document";
            Assert.False(new ClipboardPlatform().Run(Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N")), string.Empty, text, out string? error));
            Assert.NotNull(error);
            Assert.DoesNotContain(text, error!);
        }

        [Fact]
        public void ProviderThatDoesNotReadInputIsBounded() {
            Command command = Windows ? PowerShell("Start-Sleep -Seconds 60") : Shell("while :; do :; done");
            Assert.False(new ClipboardPlatform().Run(command.Executable, command.Arguments, new string('x', 1000000), 200, out string? error));
            Assert.Contains("timed out", error!);
        }

        private static Command PowerShell(string script) => new Command(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
            "-NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));

        private static Command Shell(string script) => new Command("/bin/sh", "-c \"" + script.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");

        private sealed class Command {
            internal Command(string executable, string arguments) { Executable = executable; Arguments = arguments; }
            internal string Executable { get; }
            internal string Arguments { get; }
        }
    }
}
