using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ConsoleMonster.Sessions;
using SiteFrames.Replay;
using Xunit;

namespace SiteFrames.Tests {
    public sealed class RecordedStepContracts {
        private static string Cast(params (string Kind, string Text)[] events) =>
            "{\"version\":2,\"width\":40,\"height\":6}\n" + string.Concat(events.Select(e => "[0," + JsonSerializer.Serialize(e.Kind) + "," + JsonSerializer.Serialize(e.Text) + "]\n"));

        [Fact]
        public void EqualTimestampStepsKeepTheirOriginalEventOrderAndReadingHolds() {
            string native = Cast(("m", "site:prompt"), ("o", "PS> example\r\n"),
                ("m", "site:output"), ("o", "\u001b[32mfinished 甲\u001b[0m\r\n"), ("m", "site:end"));
            string cast = RecordedSteps.ToCast(native, "Example", false);
            SessionRecording session = SessionRecording.Read(new StringReader(cast));
            Assert.Contains("PS> example", session.FrameAt(TimeSpan.Zero).ToPlainText());
            Assert.DoesNotContain("finished", session.FrameAt(TimeSpan.Zero).ToPlainText());
            Assert.DoesNotContain("finished", session.FrameAt(TimeSpan.FromMilliseconds(399)).ToPlainText());
            Assert.Contains("finished 甲", session.FrameAt(TimeSpan.FromMilliseconds(400)).ToPlainText());
            Assert.DoesNotContain("\u001b[?1049h", cast);
        }

        [Fact]
        public void StaticOutputKeepsScrolledHistoryAndLinksRatherThanOnlyTheLastViewport() {
            string text = string.Concat(Enumerable.Range(0, 12).Select(i => "row " + i + "\r\n"))
                + "\u001b]8;;https://example.test\u001b\\linked\u001b]8;;\u001b\\\r\n";
            string cast = RecordedSteps.ToCast(Cast(("o", text)), "Static", true);
            SessionRecording result = SessionRecording.Read(new StringReader(cast));
            Assert.Contains("row 0", result.Transcript().ToPlainText());
            Assert.Contains("row 11", result.Transcript().ToPlainText());
            var frame = result.Transcript();
            Assert.Contains(Enumerable.Range(0, frame.Height).SelectMany(y => Enumerable.Range(0, frame.Width).Select(x => frame[x, y])),
                cell => cell.Style.Link == "https://example.test");
        }

        [Fact]
        public void NativeSpaceForegroundAndExplicitDefaultBackgroundProduceIdenticalStaticCasts() {
            string a = Cast(("o", "a b\r\n"));
            string b = Cast(("o", "\u001b[48;2;12;12;12ma\u001b[31m \u001b[39mb\r\n"));
            Assert.Equal(RecordedSteps.ToCast(a, "Static", true), RecordedSteps.ToCast(b, "Static", true));
        }

        [Fact]
        public void MissingOrForeignStepBoundariesAndReplacementGlyphsFail() {
            Assert.Throws<InvalidDataException>(() => RecordedSteps.ToCast(Cast(("o", "text")), "Bad", false));
            Assert.Throws<InvalidDataException>(() => RecordedSteps.ToCast(Cast(("m", "foreign"), ("o", "text"), ("m", "site:end")), "Bad", false));
            Assert.Throws<InvalidDataException>(() => RecordedSteps.ToCast(Cast(("o", "\uFFFD")), "Bad", true));
        }
    }
}
