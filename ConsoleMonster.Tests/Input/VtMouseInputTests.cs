using System;
using System.Collections.Generic;
using ConsoleMonster.Input;
using Xunit;

namespace ConsoleMonster.Tests.Input {
    /// <summary>SGR 1006 decoding, fragmented reads, ignored reports and keyboard continuity.</summary>
    public sealed class VtMouseInputTests {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 3)]
        public void Feed_MousePressAndRelease_UsesCellCoordinatesAndButton(int code, int button) {
            VtInputDecoder decoder = new VtInputDecoder();
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed("\u001b[<" + code + ";1;25M\u001b[<" + code + ";120;2m", events);
            Assert.Equal(2, events.Count);
            AssertMouse(events[0], 0, 24, button, true, KeyModifiers.None);
            AssertMouse(events[1], 119, 1, button, false, KeyModifiers.None);
            Assert.False(decoder.HasPending);
        }

        [Fact]
        public void Feed_MouseAtEverySplitBoundary_PreservesModifiersAndFollowingKeyboard() {
            AssertEverySplit("\u001b[<28;2147483647;2147483647M", true);
            AssertEverySplit("\u001b[<28;2147483647;2147483647m", false);
        }

        private static void AssertEverySplit(string report, bool pressed) {
            for (int split = 1; split < report.Length; split++) {
                VtInputDecoder decoder = new VtInputDecoder();
                List<InputEvent> events = new List<InputEvent>();
                decoder.Feed(report.Substring(0, split), events);
                Assert.Empty(events);
                Assert.True(decoder.HasPending);
                decoder.Feed(report.Substring(split) + "\u001b[Aa\t", events);
                Assert.Equal(4, events.Count);
                AssertMouse(events[0], int.MaxValue - 1, int.MaxValue - 1, 1, pressed,
                    KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Control);
                Assert.True(events[1].Is(ConsoleKey.UpArrow));
                Assert.True(events[2].IsChar('a'));
                Assert.True(events[3].Is(ConsoleKey.Tab));
                Assert.False(decoder.HasPending);
            }
        }

        [Theory]
        [InlineData(4, KeyModifiers.Shift)]
        [InlineData(8, KeyModifiers.Alt)]
        [InlineData(16, KeyModifiers.Control)]
        public void Feed_MouseRelease_KeepsEachModifier(int code, KeyModifiers modifier) {
            VtInputDecoder decoder = new VtInputDecoder();
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed("\u001b[<" + code + ";2;3m", events);
            AssertMouse(Assert.Single(events), 1, 2, 1, false, modifier);
        }

        [Fact]
        public void Feed_UnsupportedOrMalformedMouseReports_DropsOnlyReports() {
            // Values stay in data rather than parameterized test names, including the overlong report.
            string[] reports = {
                "<32;1;1M", "<35;1;1M", "<64;1;1M", "<65;1;1m", "<128;1;1M", "<3;1;1M",
                "<0;0;1M", "<0;1;0M", "<0;-1;1M", "<-1;1;1M", "<0;2147483648;1M",
                "<0;1;2147483648m", "<2147483648;1;1M", "<0;;1M", "<0;1M", "<0;1;1;1M",
                "<0;1;1 M", "<0;1;1A", "<0;" + new string('9', 80) + ";1M"
            };
            foreach (string report in reports) {
                VtInputDecoder decoder = new VtInputDecoder();
                List<InputEvent> events = new List<InputEvent>();
                decoder.Feed("\u001b[" + report + "\u001b[Zx", events);
                Assert.Equal(2, events.Count);
                Assert.True(events[0].Is(ConsoleKey.Tab, KeyModifiers.Shift));
                Assert.True(events[1].IsChar('x'));
                Assert.False(decoder.HasPending);
            }
        }

        [Fact]
        public void Feed_OverlongMouseAcrossReads_DropsTailAndResumesKeyboard() {
            VtInputDecoder decoder = new VtInputDecoder();
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed("\u001b[<0;" + new string('9', 80), events);
            Assert.Empty(events);
            Assert.True(decoder.HasPending);
            decoder.Feed(";1M\u001b[<0;2;3Mx", events);
            Assert.Equal(2, events.Count);
            AssertMouse(events[0], 1, 2, 1, true, KeyModifiers.None);
            Assert.True(events[1].IsChar('x'));
            Assert.False(decoder.HasPending);
        }

        [Fact]
        public void Flush_IncompleteMouse_DropsReportWithoutInventingAltKey() {
            VtInputDecoder decoder = new VtInputDecoder();
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed("\u001b[<0;12;", events);
            decoder.Flush(events);
            Assert.Empty(events);
            decoder.Feed("3Mx", events);
            Assert.True(Assert.Single(events).IsChar('x'));
            Assert.False(decoder.HasPending);
        }

        [Fact]
        public void Feed_MouseLikeTextInsideBracketedPaste_RemainsPaste() {
            VtInputDecoder decoder = new VtInputDecoder();
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed("\u001b[200~\u001b[<0;1;1M\u001b[201~\u001b[I", events);
            Assert.Equal(2, events.Count);
            Assert.Equal(InputEventKind.Paste, events[0].Kind);
            Assert.Equal("\u001b[<0;1;1M", events[0].Text);
            Assert.Equal(InputEventKind.FocusGained, events[1].Kind);
        }

        private static void AssertMouse(InputEvent actual, int column, int row, int button, bool pressed, KeyModifiers modifiers) {
            Assert.Equal(InputEventKind.Mouse, actual.Kind);
            Assert.Equal(column, actual.Column);
            Assert.Equal(row, actual.Row);
            Assert.Equal(button, actual.MouseButton);
            Assert.Equal(pressed, actual.MousePressed);
            Assert.Equal(modifiers, actual.Modifiers);
        }
    }
}
