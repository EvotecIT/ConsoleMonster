using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    /// <summary>Targets: scoped and explicit output, live sessions, prompts and status helpers.</summary>
    public sealed class TerminalTargetTests {
        private static Table Servers() {
            return new Table().AddColumn("Server").AddColumn("State").AddValues("SRV01", StatusKind.Pass).AddValues("SRV02", StatusKind.Fail);
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void Use_RoutesStaticWritesToTheTarget(GlyphTier tier) {
            RecordingTarget target = TestTerminal.Target(40, glyphs: tier);
            using (Terminal.Use(target)) {
                Assert.Same(target, Terminal.Target);
                Assert.Equal(40, Terminal.GetCurrentProfile().Width);
                Terminal.Write(Servers());
                Terminal.WriteMarkup("[pass]done[/] ");
                Terminal.WriteMarkupLine("in [muted]3s[/]");
            }
            Assert.IsType<ConsoleTarget>(Terminal.Target);
            Golden.Match("targets.table." + tier.ToString().ToLowerInvariant() + ".txt", target.Text);
        }

        [Fact]
        public void Use_NestedScopesRestoreTheOuterTarget() {
            RecordingTarget outer = TestTerminal.Target();
            RecordingTarget inner = TestTerminal.Target();
            using (Terminal.Use(outer)) {
                using (Terminal.Use(inner)) {
                    Terminal.WriteMarkupLine("inner");
                }
                Terminal.WriteMarkupLine("outer");
            }
            Assert.Equal("inner\n", inner.Text);
            Assert.Equal("outer\n", outer.Text);
        }

        [Fact]
        public async Task Use_IsLocalToEachAsynchronousFlow() {
            RecordingTarget[] targets = Enumerable.Range(0, 8).Select(_ => TestTerminal.Target()).ToArray();
            await Task.WhenAll(targets.Select((target, i) => Task.Run(async () => {
                using (Terminal.Use(target)) {
                    for (int line = 0; line < 20; line++) {
                        Terminal.WriteMarkupLine("flow " + i);
                        await Task.Yield();
                    }
                }
            })));
            for (int i = 0; i < targets.Length; i++) {
                string[] lines = targets[i].Text.TrimEnd('\n').Split('\n');
                Assert.Equal(20, lines.Length);
                Assert.All(lines, line => Assert.Equal("flow " + i, line));
            }
        }

        [Fact]
        public void Extensions_WriteToAnExplicitTargetWithoutChangingTheAmbientOne() {
            RecordingTarget target = TestTerminal.Target(60);
            target.Write(Servers());
            target.WriteMarkupLine("[muted]2 servers[/]");
            target.Status("Checking", ctx => ctx.Update("Checking SRV02"));
            Assert.IsType<ConsoleTarget>(Terminal.Target);
            Assert.Contains("SRV02", target.Text);
            Assert.EndsWith("2 servers\nChecking\nChecking SRV02\n", target.Text);
        }

        [Fact]
        public void WriterTarget_WritesPlainAsciiAtTheChosenWidth() {
            StringWriter log = new StringWriter();
            WriterTarget target = new WriterTarget(log, width: 30);
            target.Write(new Rule("Log"));
            string text = log.ToString().Replace("\r\n", "\n");
            Assert.Equal(30, text.TrimEnd('\n').Length);
            Assert.False(text.IndexOf('\u001b') >= 0, text.Replace("\u001b", "<ESC>"));
            Assert.All(text, c => Assert.True(c < 128));
        }

        [Fact]
        public void NullTarget_DiscardsOutputAndPromptsUseTheirDefaults() {
            NullTarget.Instance.Write(Servers());
            using (Terminal.Use(NullTarget.Instance)) {
                Assert.True(Prompt.Confirm("Continue?", true));
                Assert.Equal("dc01", Prompt.Text("Server", "dc01"));
                Assert.Throws<PromptUnavailableException>(() => Prompt.Text("Server"));
            }
        }

        [Fact]
        public void RecordingTarget_ScriptedKeysAnswerPromptsAndScreensAreCaptured() {
            RecordingTarget target = TestTerminal.Target(60, 12, keys: "Down Enter");
            string chosen = Prompt.Select("Target", new[] { "dc01", "dc02", "dc03" }, options: new InteractiveOptions { Target = target });
            Assert.Equal("dc02", chosen);
            Assert.NotNull(target.Screens.FindFrame("dc03"));
        }

        [Fact]
        public void LiveSession_WithATarget_DrawsThereInStreamMode() {
            RecordingTarget target = TestTerminal.Target(70);
            Lanes lanes = new Lanes();
            using (LiveSession session = new LiveSession(lanes, new LiveSessionOptions { Target = target, CaptureConsole = false, HandleCancelKey = false, StreamTimestamps = false })) {
                session.Start();
                LanesHandle handle = lanes.Handle(session);
                handle.Add("srv01", "SRV01", 2);
                handle.Start("srv01");
                handle.Advance("srv01", StatusKind.Pass);
                handle.Complete("srv01");
            }
            Assert.Contains("SRV01 started", target.Text);
            Assert.Contains("SRV01 PASS", target.Text);
        }

        [Fact]
        public void MatchGolden_ComparesThePlainText() {
            RecordingTarget target = TestTerminal.Target();
            target.WriteMarkupLine("[pass]golden[/]");
            string path = Path.Combine(Path.GetTempPath(), "cm-target-" + Guid.NewGuid().ToString("N") + ".txt");
            try {
                File.WriteAllText(path, "golden\n");
                target.MatchGolden(path);
                File.WriteAllText(path, "other\n");
                Assert.Throws<GoldenFileMismatchException>(() => target.MatchGolden(path));
            } finally {
                File.Delete(path);
            }
        }
    }
}
