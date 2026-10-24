using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleMonster.Demos;
using ConsoleMonster.Export;
using ConsoleMonster.Sessions;
using Xunit;

namespace ConsoleMonster.Tests.Demos {
    public sealed class DemoTests {
        [Fact]
        public void GroupsPowerShellAndPresentationDirectivesWithoutExecutingThem() {
            var script = DemoScript.Parse("# demo: wpm 180\n# demo: chapter Build\n# demo: setup\nfunction prompt { 'λ> ' }\n# demo: narrate A report\n@(\n  'one',\n  'two'\n) |\n  ForEach-Object { $_ }\n# demo: pause 0.5\n# demo: type-only\nWrite-Host 'never run'\n# demo: wait complete");
            Assert.Equal(7, script.Steps.Count);
            Assert.True(script.Steps[1].Hidden);
            Assert.Equal(180, script.Steps[3].WordsPerMinute);
            Assert.Contains("ForEach-Object", script.Steps[3].Text);
            Assert.True(script.Steps[5].TypeOnly);
            Assert.Equal(DemoStepKind.Wait, script.Steps[6].Kind);
            var here = DemoScript.Parse("$value = @'\n# demo: chapter literal, not a directive\n'@\nWrite-Host $value");
            Assert.Equal(2, here.Steps.Count);
            Assert.Contains("literal", here.Steps[0].Text);
            var branches = DemoScript.Parse("if ($true) { 'yes' }\nelse { 'no' }\ntry { 'work' }\ncatch { 'error' }\nfinally { 'cleanup' }");
            Assert.Equal(2, branches.Steps.Count);
            Assert.Contains("else", branches.Steps[0].Text);
            Assert.Contains("finally", branches.Steps[1].Text);
            var nextLine = DemoScript.Parse("function Get-DemoValue\n{\n  42\n}\nGet-DemoValue");
            Assert.Equal(2, nextLine.Steps.Count);
            Assert.Contains("42", nextLine.Steps[0].Text);
        }
        [Theory]
        [InlineData("# demo: wpm NaN")]
        [InlineData("# demo: pause -1")]
        [InlineData("# demo: typo x")]
        [InlineData("# demo: setup")]
        [InlineData("Write-Host ('unfinished'")]
        public void RejectsUnusableScripts(string text) => Assert.Throws<FormatException>(() => DemoScript.Parse(text));
        [Fact]
        public void SemanticPromptsHandleAllChunkBoundariesAndBothTerminators() {
            const string stream = "not a prompt PS C:\\demo> \u001b]133;B\a\u001b]133;A\aλ 任意> \u001b]133;B\u001b\\\u001b]133;C\aoutput\u001b]133;D;0\a\u001b]133;A;k=i\u001b\\other> \u001b]133;B\a";
            for (int split = 0; split <= stream.Length; split++) {
                var detector = new SemanticPromptDetector();
                detector.Feed(stream.Substring(0, split)); detector.Feed(stream.Substring(split));
                Assert.Equal(2, detector.PromptCount);
            }
            var characters = new SemanticPromptDetector();
            foreach (char c in stream) { characters.Feed(c.ToString()); }
            Assert.Equal(2, characters.PromptCount);
        }
        [Theory]
        [InlineData('\r', ConsoleKey.Enter, DemoPresenterAction.Run)]
        [InlineData(' ', ConsoleKey.Spacebar, DemoPresenterAction.Type)]
        [InlineData('b', ConsoleKey.B, DemoPresenterAction.Back)]
        [InlineData('s', ConsoleKey.S, DemoPresenterAction.Skip)]
        [InlineData('Q', ConsoleKey.Q, DemoPresenterAction.Quit)]
        [InlineData('x', ConsoleKey.X, DemoPresenterAction.None)]
        public void PresenterKeysHaveStableActions(char character, ConsoleKey key, DemoPresenterAction expected) =>
            Assert.Equal(expected, DemoPresenter.Action(new ConsoleKeyInfo(character, key, false, false, false)));
        [Fact]
        public void SeededTypingHasARepeatableBoundedSchedule() {
            var first = new DemoTypingClock(42); var second = new DemoTypingClock(42); var other = new DemoTypingClock(43);
            bool differs = false;
            for (int i = 0; i < 100; i++) {
                var delay = first.Next(120); Assert.Equal(delay, second.Next(120));
                Assert.InRange(delay.TotalMilliseconds, 80, 120); differs |= delay != other.Next(120);
            }
            Assert.True(differs);
        }
        [Fact]
        public void TypedDemoUsesRealPowerShellAndKeepsHiddenSetupAndTypeOnlyOutOfExecution() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; } // ConPTY needs an attached Windows host.
            string folder = Path.Combine(Path.GetTempPath(), "cm-demo-proof-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            try {
                string demoSource = "# demo: setup\nfunction prompt { '任意 λ> ' }; $value = 'hidden-fixture'; Write-Output $value\n# demo: wpm 1000\n# demo: chapter First\n$command = 'real'; $reader = 42; Write-Output ($command + '-' + $reader), 'line-two'\n# demo: type-only\nthrow 'must never run'\n# demo: chapter Last\nGet-Content (Join-Path $PSScriptRoot 'fixture.txt')\nWrite-Output 'done'\n# demo: wait done";
                string demoPath = Path.Combine(folder, "demo.ps1");
                File.WriteAllText(demoPath, demoSource);
                File.WriteAllText(Path.Combine(folder, "fixture.txt"), "fixture-row");
                var script = DemoScript.Read(demoPath);
                string cast = Path.Combine(folder, "demo.cast");
                StringBuilder shown = new StringBuilder();
                ConsoleDemo.Record(script, new DemoOptions {
                    Recording = new SessionRecordingOptions { Shell = "pwsh", Path = cast, Width = 50, Height = 10 },
                    Output = value => { lock (shown) { shown.Append(value); } }
                });
                Assert.DoesNotContain("hidden-fixture", shown.ToString());
                string text = ConsoleExporter.ToText(SessionRecording.Read(cast).Transcript());
                Assert.Contains("real-42", text); Assert.Contains("done", text); Assert.DoesNotContain("Exception", text);
                Assert.Contains("\nline-two\n", text);
                Assert.Contains("\nfixture-row\n", text);
                Assert.DoesNotContain("hidden-fixture", File.ReadAllText(cast));
                Assert.Equal(2, SessionRecording.Read(cast).Chapters.Count);
            } finally { Directory.Delete(folder, true); }
        }
        [Fact]
        public void PresenterTypeThenSkipAndBackDoNotExecuteDiscardedCommands() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }
            string folder = Path.Combine(Path.GetTempPath(), "cm-presenter-proof-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            try {
                var script = DemoScript.Parse("# demo: wpm 1000\nWrite-Output 'FIRST-RAN'\nWrite-Output 'SECOND-RAN'");
                var keys = new[] { Key(' ', ConsoleKey.Spacebar), Key('s', ConsoleKey.S), Key('b', ConsoleKey.B), Key('s', ConsoleKey.S), Key('\r', ConsoleKey.Enter) };
                int index = 0;
                string path = ConsoleDemo.Present(script, new DemoOptions { Recording = new SessionRecordingOptions { Shell = "pwsh", Path = Path.Combine(folder, "live.cast"), Width = 50, Height = 10 } }, () => keys[index++]);
                var text = ConsoleExporter.ToText(SessionRecording.Read(path).Transcript());
                Assert.Contains("SECOND-RAN\n", text); Assert.DoesNotContain("\nFIRST-RAN\n", text);
            } finally { Directory.Delete(folder, true); }
        }
        private static ConsoleKeyInfo Key(char value, ConsoleKey key) => new ConsoleKeyInfo(value, key, false, false, false);
    }
}
