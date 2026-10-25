using System;
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
