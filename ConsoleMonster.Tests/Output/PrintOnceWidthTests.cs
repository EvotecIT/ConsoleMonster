using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    /// <summary>Print-once output re-reads the window size on every write while the rest of detection stays cached.</summary>
    [Collection("Live sessions")]
    public sealed class PrintOnceWidthTests {
        private sealed class CountingProvider : ICapabilityProvider {
            private readonly TerminalProfile _profile;

            public CountingProvider(TerminalProfile profile) {
                _profile = profile;
            }

            public int Detections { get; private set; }

            public TerminalProfile Detect() {
                Detections++;
                return _profile;
            }
        }

        private static int WidestLine(string text) {
            return text.Replace("\r\n", "\n").Split('\n').Max(line => line.TrimEnd().Length);
        }

        private static Rule FullWidthRule() => new Rule();

        [Fact]
        public void Write_AfterSimulatedResize_UsesTheNewWidth() {
            // Interactive without VT: line output, so the written text is the plain rendered lines.
            CountingProvider provider = new CountingProvider(new TerminalProfile(ColorDepth.None, GlyphTier.Ascii, OutputMode.Interactive, 100, 30));
            int[] size = { 100, 30 };
            try {
                Terminal.CapabilityProvider = provider;
                Terminal.WindowSizeSource = () => new[] { size[0], size[1] };
                StringTerminalWriter before = new StringTerminalWriter();
                Terminal.Write(FullWidthRule(), null, before);
                size[0] = 60;
                StringTerminalWriter after = new StringTerminalWriter();
                Terminal.Write(FullWidthRule(), null, after);
                size[0] = 140;
                StringTerminalWriter wider = new StringTerminalWriter();
                Terminal.Write(FullWidthRule(), null, wider);

                Assert.Equal(100, WidestLine(before.ToString()));
                Assert.Equal(60, WidestLine(after.ToString()));
                Assert.Equal(140, WidestLine(wider.ToString()));
                Assert.Equal(1, provider.Detections);
                Assert.Equal(100, Terminal.Profile.Width);
            } finally {
                Terminal.WindowSizeSource = null;
                Terminal.CapabilityProvider = new CapabilityDetector();
                Terminal.Refresh();
            }
        }

        [Fact]
        public void GetCurrentProfile_AssignedOrRedirectedProfile_KeepsItsSize() {
            try {
                Terminal.WindowSizeSource = () => new[] { 50, 20 };
                Terminal.Profile = TerminalProfile.Fixed(90, 30, ColorDepth.None, GlyphTier.Ascii);
                Assert.Equal(90, Terminal.GetCurrentProfile().Width);

                Terminal.CapabilityProvider = new CountingProvider(TerminalProfile.Fixed(90, 30, ColorDepth.None, GlyphTier.Ascii, OutputMode.Stream));
                Assert.Equal(90, Terminal.GetCurrentProfile().Width);
            } finally {
                Terminal.WindowSizeSource = null;
                Terminal.CapabilityProvider = new CapabilityDetector();
                Terminal.Refresh();
            }
        }
    }
}
