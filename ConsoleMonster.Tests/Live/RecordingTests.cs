using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleMonster.Live;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    [Collection("Live sessions")]
    public sealed class RecordingTests {
        [Fact]
        public void RecordingRoundTripsEscapesUnicodeAndInvariantNumbers() {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
                StringWriter writer = new StringWriter();
                string text = "quotes \" slash \\ newline\n\r\t\0 zażółć 日本 😀";
                LiveEvent expected = new LiveEvent("work", "detail", "key", text, "pass", 1.2345678901234567, double.Epsilon, 1234);
                using (LiveRecorder recorder = new LiveRecorder(writer)) {
                    recorder.Write(expected);
                    recorder.Write(new LiveEvent("work", "advance"));
                }
                Assert.StartsWith("{\"consolemonster\":\"recording\",\"version\":1}", writer.ToString());
                Assert.Contains("1.2345678901234567", writer.ToString());
                Assert.Contains("\\u000a", writer.ToString());
                LiveEvent[] events = LiveRecordingReader.Read(new StringReader("\n" + writer + "\n")).ToArray();
                Assert.Equal(2, events.Length);
                LiveEvent actual = events[0];
                Assert.Equal(expected.Time, actual.Time);
                Assert.Equal(expected.Target, actual.Target);
                Assert.Equal(expected.Operation, actual.Operation);
                Assert.Equal(expected.Key, actual.Key);
                Assert.Equal(expected.Text, actual.Text);
                Assert.Equal(expected.State, actual.State);
                Assert.Equal(expected.Value, actual.Value);
                Assert.Equal(expected.Total, actual.Total);
                Assert.Null(events[1].Text);
                Assert.Equal(0, events[1].Value);
            } finally { CultureInfo.CurrentCulture = previous; }
        }

        [Theory]
        [InlineData("{\"target\":\"work\"}")]
        [InlineData("{\"target\":\"work\",\"op\":\"advance\",\"value\":01}")]
        [InlineData("{\"target\":\"work\",\"op\":\"advance\",\"value\":\"1\"}")]
        [InlineData("{\"target\":\"work\",\"op\":\"advance\",\"text\":\"\\q\"}")]
        [InlineData("{\"target\":\"work\",\"op\":\"advance\",\"t\":1.5}")]
        [InlineData("{\"target\":\"work\",\"op\":\"advance\",}")]
        [InlineData("{\"consolemonster\":\"recording\",\"version\":2}")]
        public void MalformedRecordingReportsPhysicalLineNumber(string malformed) {
            FormatException error = Assert.Throws<FormatException>(() => LiveRecordingReader.Read(new StringReader("\n\n" + malformed)).ToArray());
            Assert.Contains("line 3", error.Message);
        }

        [Theory]
        [InlineData(2.0, 50, 100)]
        [InlineData(0.5, 200, 400)]
        [InlineData(0, 0, 0)]
        [InlineData(-1, 0, 0)]
        public void ReplayScalesOriginalTimestampsWithoutRealSleeps(double speed, int firstDelay, int secondDelay) {
            FakeTarget target = new FakeTarget();
            LiveSessionOptions options = LiveSessionTests.Options(new FakeOutput(false));
            List<TimeSpan> waits = new List<TimeSpan>();
            options.ReplayDelay = (delay, token) => { token.ThrowIfCancellationRequested(); waits.Add(delay); };
            string recording = "{\"t\":100,\"target\":\"work\",\"op\":\"advance\",\"value\":1}\n"
                + "{\"t\":300,\"target\":\"work\",\"op\":\"advance\",\"value\":2}\n";
            LiveSession.Replay(target, new StringReader(recording), options, speed);
            Assert.Equal(new[] { 1d, 2d }, target.Events.Select(e => e.Value));
            Assert.Equal(new long[] { 100, 300 }, target.Events.Select(e => e.Time));
            if (speed > 0) { Assert.Equal(new[] { TimeSpan.FromMilliseconds(firstDelay), TimeSpan.FromMilliseconds(secondDelay) }, waits); }
            else { Assert.Empty(waits); }
            Assert.Null(LiveSession.Current);
        }

        [Fact]
        public void ReplayCancellationRestoresAndDoesNotApplyPendingEvent() {
            FakeTarget target = new FakeTarget();
            FakeOutput output = new FakeOutput(false);
            LiveSessionOptions options = LiveSessionTests.Options(output);
            using (CancellationTokenSource cancellation = new CancellationTokenSource()) {
                options.ReplayDelay = (delay, token) => cancellation.Cancel();
                Assert.ThrowsAny<OperationCanceledException>(() => LiveSession.Replay(target,
                    new StringReader("{\"t\":100,\"target\":\"work\",\"op\":\"advance\"}"), options, ct: cancellation.Token));
                Assert.Empty(target.Events);
                Assert.Contains("end", output.Calls);
                Assert.Null(LiveSession.Current);
            }
        }

        [Fact]
        public void SessionRecordsOnlyAppliedEventsAndStampsZeroTimeFromClock() {
            FakeTarget target = new FakeTarget();
            StringWriter recording = new StringWriter();
            LiveSessionOptions options = LiveSessionTests.Options(new FakeOutput(false));
            long ticks = TimeSpan.FromSeconds(50).Ticks;
            options.Clock = () => TimeSpan.FromTicks(Interlocked.Read(ref ticks));
            options.RecordWriter = recording;
            using (LiveSession session = new LiveSession(target, options)) {
                session.Start();
                Interlocked.Add(ref ticks, TimeSpan.FromMilliseconds(123).Ticks);
                session.Post(new LiveEvent("work", "advance", value: 7));
                session.Post(new LiveEvent("unknown", "advance"));
                session.Stop();
                Assert.Equal(123, Assert.Single(LiveRecordingReader.Read(new StringReader(recording.ToString()))).Time);
                Assert.Equal(TimeSpan.FromMilliseconds(123), session.Elapsed);
            }
        }

        [Fact]
        public void RecorderRejectsNonFiniteValuesWithoutWritingMalformedJson() {
            StringWriter writer = new StringWriter();
            using (LiveRecorder recorder = new LiveRecorder(writer)) {
                Assert.Throws<ArgumentException>(() => recorder.Write(new LiveEvent("work", "advance", value: double.NaN)));
                recorder.Write(new LiveEvent("work", "advance", value: 1));
            }
            Assert.Single(LiveRecordingReader.Read(new StringReader(writer.ToString())));
        }
    }
}
