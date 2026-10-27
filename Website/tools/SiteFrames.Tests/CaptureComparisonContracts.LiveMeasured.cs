using System;
using System.IO;
using System.Text.Json.Nodes;
using SiteFrames.Capture;
using Xunit;

namespace SiteFrames.Tests {
    public sealed partial class CaptureComparisonContracts {
        private static string MeasuredCast(string text, double duration = 0.1, string source = "0123456789abcdef") =>
            new JsonObject { ["version"] = 2, ["width"] = 20, ["height"] = 5, ["cm_source"] = source }.ToJsonString()
            + "\n[0,\"o\",\"begin\\r\\n\"]\n[" + duration.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ",\"o\"," + JsonValue.Create(text)!.ToJsonString() + "]\n";

        private static void DeclareMeasured(string path, string reason = "Actual concurrent completion order.", int maxFrames = 100) {
            JsonObject policy = new CaptureClasses.LiveMeasured(reason, 2, maxFrames, 0.05, 1).Json();
            File.WriteAllText(Path.Combine(path, CaptureClasses.FileName), new JsonObject {
                ["version"] = 1, ["captures"] = new JsonObject { ["sample.cast"] = policy.DeepClone() }
            }.ToJsonString());
            Change(path, m => m["files"]!["sample.cast"]!["comparison"] = policy.DeepClone());
        }

        [Fact]
        public void DeclaredLiveMeasurementsAllowRealValuesAndFrameCountsButUnclassifiedOutputStaysStrict() {
            string a = Run("a", "macos", MeasuredCast("12 ms"), "sample.cast");
            string b = Run("b", "macos", MeasuredCast("13 ms\r\ncompleted"), "sample.cast");
            Assert.Equal(1, Compare(a, b, out _));
            DeclareMeasured(a); DeclareMeasured(b);
            Assert.Equal(0, Compare(a, b, out JsonObject report));
            JsonNode allowed = Assert.Single(report["allowed"]!.AsArray())!;
            Assert.Equal("live-measured", allowed["class"]!.GetValue<string>());
            Assert.Equal("Actual concurrent completion order.", allowed["reason"]!.GetValue<string>());
            Assert.NotEqual(allowed["leftFrames"]!.GetValue<int>(), allowed["rightFrames"]!.GetValue<int>());
            Assert.False(allowed["bytesEqual"]!.GetValue<bool>());
            // The class has no filename skip list: any explicit declaration uses the same contract.
            Assert.Equal(0, Compare(a, RunWithDeclaration("c", "windows", "14 ms"), out _));
        }

        private string RunWithDeclaration(string name, string platform, string text) {
            string result = Run(name, platform, MeasuredCast(text), "sample.cast");
            DeclareMeasured(result);
            return result;
        }

        [Theory]
        [InlineData("\uFFFD")]
        [InlineData("┌─┐\r\n┘─└")]
        [InlineData("\u001b[?2026hpartial")]
        [InlineData("\u001b[31")]
        [InlineData("\u001b]8;;https://example.test")]
        public void LiveMeasuredNeverWaivesGarbledGlyphsBrokenBordersOrTruncatedFrames(string text) {
            Assert.Equal(1, Compare(RunWithDeclaration("a", "macos", "12 ms"), RunWithDeclaration("b", "macos", text), out _));
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(2)]
        public void LiveMeasuredDurationMustRemainInsideItsDeclaredBounds(double duration) {
            string a = RunWithDeclaration("a", "macos", "12 ms");
            string b = Run("b", "macos", MeasuredCast("13 ms", duration), "sample.cast");
            DeclareMeasured(b);
            Assert.Equal(1, Compare(a, b, out _));
        }

        [Fact]
        public void LiveMeasuredStillRequiresMatchingSourcesReceiptsReasonsAndFrameBounds() {
            string a = RunWithDeclaration("a", "macos", "12 ms");
            string b = Run("b", "macos", MeasuredCast("13 ms", source: "1111111111111111"), "sample.cast");
            DeclareMeasured(b);
            Assert.Equal(1, Compare(a, b, out _));
            b = RunWithDeclaration("c", "macos", "13 ms");
            DeclareMeasured(b, reason: "A different classification.");
            Assert.Equal(1, Compare(a, b, out _));
            DeclareMeasured(b, reason: "");
            Assert.Equal(1, Compare(a, b, out _));
            DeclareMeasured(b);
            File.Delete(Path.Combine(b, CaptureClasses.FileName));
            Assert.Equal(1, Compare(a, b, out _));
            b = RunWithDeclaration("d", "macos", "13 ms\r\nmore\r\nlast");
            DeclareMeasured(a, maxFrames: 2); DeclareMeasured(b, maxFrames: 2);
            Assert.Equal(1, Compare(a, b, out _));
        }
    }
}
