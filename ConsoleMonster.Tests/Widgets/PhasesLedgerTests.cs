using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class PhasesLedgerTests {
        private static string Render(Phases phases, int width, GlyphTier tier) => Terminal.RenderText(
            new Panel(phases, "Connection ledger"), TerminalProfile.Fixed(width, 100, ColorDepth.None, tier), Theme.Dark);

        // A build ledger in a short inline area: the window follows the furthest started step, keeps the next one in
        // view, and does not go back to an older step that is still running while the next step has not started yet.
        [Fact]
        public void AShortLedger_FollowsTheFurthestStartedStepAndNeverGoesBack() {
            var phases = new Phases { StepCounter = true };
            for (int i = 1; i <= 12; i++) { phases.Set("s" + i, StatusKind.Pending, "Step " + i); }
            RenderContext context = Terminal.CreateContext(TerminalProfile.Fixed(40, 6, ColorDepth.None, GlyphTier.Ascii), Theme.Dark);
            string[] Window() {
                CellBuffer buffer = new CellBuffer(40, 6);
                phases.Render(new Region(buffer), context);
                return Enumerable.Range(0, buffer.Height).Select(y => buffer.GetRowText(y).TrimEnd()).ToArray();
            }
            Assert.Contains("Step 1", Window()[0]);
            phases.Set("s1", StatusKind.Pass).Set("s2", StatusKind.Pass).Set("s3", StatusKind.Running);
            phases.Set("s4", StatusKind.Running).Set("s4", StatusKind.Pass).Set("s5", StatusKind.Running).Set("s6", StatusKind.Running);
            string[] running = Window();
            Assert.Contains(running, row => row.Contains("Step 6"));
            Assert.Contains(running, row => row.Contains("Step 7"));
            Assert.StartsWith("+3", running[0]);
            Assert.StartsWith("+5", running[5]);
            // Steps 5 and 6 finished and step 7 has not started; step 3 is still running further up.
            phases.Set("s5", StatusKind.Pass).Set("s6", StatusKind.Pass);
            string[] gap = Window();
            Assert.Contains(gap, row => row.Contains("Step 6"));
            Assert.Contains(gap, row => row.Contains("Step 7"));
            Assert.DoesNotContain(gap, row => row.Contains("Step 3"));
            phases.Set("s7", StatusKind.Running);
            Assert.Contains(Window(), row => row.Contains("Step 8"));
        }
        [Theory]
        [InlineData(48, GlyphTier.Full)] [InlineData(40, GlyphTier.Full)]
        [InlineData(48, GlyphTier.Ascii)] [InlineData(40, GlyphTier.Ascii)]
        public void LabelAndResultDoNotSqueezeEachOther(int width, GlyphTier tier) {
            var phases = new Phases { StepCounter = true };
            phases.Set("chain", StatusKind.Fail, "Validate chain", "expired intermediate");
            phases.Set("expiry", StatusKind.Skip, "Expiry and revocation", "skipped");
            string[] lines = Render(phases, width, tier).Split('\n');
            int label = Array.FindIndex(lines, line => line.Contains("Validate chain"));
            int detail = Array.FindIndex(lines, line => line.Contains("expired intermediate"));
            Assert.True(label >= 0, "The full label should fit before the result is allocated space.");
            Assert.True(detail > label, "A result that cannot share the label's row belongs below it.");
            Assert.Contains(lines, line => line.Contains("Expiry and revocation"));
            Assert.Contains(lines, line => line.Contains("skipped"));
        }

        [Theory]
        [InlineData(48, GlyphTier.Full)] [InlineData(40, GlyphTier.Full)]
        [InlineData(48, GlyphTier.Ascii)] [InlineData(40, GlyphTier.Ascii)]
        public void HangingLabelsAndResultRowsMatchNarrowGoldens(int width, GlyphTier tier) {
            var phases = new Phases { StepCounter = true };
            phases.Set("dns", StatusKind.Pass, "Resolve DNS", "203.0.113.42");
            phases.Set("connect", StatusKind.Pass, "Connect :443", "connected through fallback route");
            phases.Set("handshake", StatusKind.Pass, "TLS handshake", "TLS 1.3");
            phases.Set("chain", StatusKind.Fail, "Validate certificate chain and issuer trust", "expired intermediate");
            phases.Set("expiry", StatusKind.Skip, "Expiry and revocation", "skipped");
            Golden.Match("phases-ledger." + width + "." + tier.ToString().ToLowerInvariant() + ".txt", Render(phases, width, tier));
        }
    }
}
