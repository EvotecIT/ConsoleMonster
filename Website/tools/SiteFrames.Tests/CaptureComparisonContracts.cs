using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using SiteFrames.Capture;
using Xunit;

namespace SiteFrames.Tests {
    public sealed class CaptureComparisonContracts : IDisposable {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "siteframes-compare-" + Guid.NewGuid().ToString("N"));

        [Fact]
        public void EquivalentTransportEncodingPassesAcrossPlatformsButFailsRepeatedRunBytes() {
            string a = Run("a", "windows", "ok\r\n");
            string b = Run("b", "macos", "ok\n");
            Assert.Equal(0, CaptureComparison.Run(a, b));
            b = Run("b", "windows", "ok\n");
            Assert.Equal(1, CaptureComparison.Run(a, b));
        }

        [Theory]
        [InlineData("different\r\n")]
        [InlineData("\u001b[31mok\u001b[0m\r\n")]
        [InlineData("\uFFFD\r\n")]
        [InlineData("┌─┐\r\n┘─└\r\n")]
        public void NeutralTextColourReplacementGlyphAndBrokenBordersFail(string changed) {
            Assert.Equal(1, CaptureComparison.Run(Run("a", "windows", "ok\r\n"), Run("b", "macos", changed)));
        }

        [Theory]
        [InlineData("sample.ps5.ansi")]
        [InlineData("live/pipeline-ps5.cast")]
        [InlineData("tails/ps5-120-dark.ansi")]
        [InlineData("help-formatting/native-ps5-120-dark.ansi")]
        [InlineData("help-formatting/get-help-ps5-120-dark.ansi")]
        [InlineData("help-formatting/command-help-ps5-120-dark.ansi")]
        [InlineData("help-formatting/help-ps5-120-dark.ansi")]
        [InlineData("help-formatting/show-consolehelp-ps5-120-dark.ansi")]
        public void EveryExistingPs5NamingContractIsWindowsOnly(string path) {
            Assert.True(CapturePlatforms.IsPs5(path));
            string a = Run("a", "windows", "ok");
            string extra = Path.Combine(a, path);
            Directory.CreateDirectory(Path.GetDirectoryName(extra)!);
            string output = path.EndsWith(".cast", StringComparison.Ordinal) ? Cast("ok") : "ok";
            File.WriteAllText(extra, output);
            Change(a, manifest => manifest["files"]![path] = new JsonObject {
                ["width"] = 20, ["height"] = 5, ["platform"] = "windows", ["category"] = "neutral",
                ["sha256"] = CaptureRun.Hash(Encoding.UTF8.GetBytes(output))
            });
            File.WriteAllText(Path.Combine(a, "sources.json"), new JsonObject { ["sample.ansi"] = "0123456789abcdef", [path] = "0123456789abcdef" }.ToJsonString());
            Assert.Equal(0, Compare(a, Run("b", "macos", "ok"), out JsonObject report));
            Assert.Single(report["allowed"]!.AsArray());
        }

        [Fact]
        public void AnsiCompletedAlternateFramesCannotDisappearAfterPrimaryRestoration() {
            string Prefix(string content) => "\u001b[?1049h\u001b[?2026h" + content + "\u001b[?2026l\u001b[?1049l";
            Assert.Equal(1, CaptureComparison.Run(Run("a", "windows", Prefix("ok")), Run("b", "macos", Prefix("different"))));
            Assert.Equal(1, CaptureComparison.Run(Run("a", "windows", Prefix("\uFFFD")), Run("b", "macos", Prefix("\uFFFD"))));
        }

        [Fact]
        public void AnsiProvenanceMustExistAndMatchEvenWhenCellsAreIdentical() {
            string a = Run("a", "windows", "ok"), b = Run("b", "macos", "ok");
            File.WriteAllText(Path.Combine(b, "sources.json"), new JsonObject { ["sample.ansi"] = "1111111111111111" }.ToJsonString());
            Assert.Equal(1, CaptureComparison.Run(a, b));
            File.Delete(Path.Combine(b, "sources.json"));
            Assert.Equal(1, CaptureComparison.Run(a, b));
        }

        [Fact]
        public void ChangedFileCannotReuseItsProducerHash() {
            string a = Run("a", "windows", "ok\r\n"), b = Run("b", "macos", "ok\r\n");
            File.WriteAllText(Path.Combine(b, "sample.ansi"), "other");
            Assert.Equal(1, CaptureComparison.Run(a, b));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(null)]
        public void PartialOrLegacyReceiptsCannotProveCompletedInvocation(bool? complete) {
            string a = Run("a", "windows", "ok\r\n"), b = Run("b", "windows", "ok\r\n");
            Change(b, m => m["complete"] = complete);
            Assert.Equal(1, CaptureComparison.Run(a, b));
        }

        [Fact]
        public void FocusedRunReportsOnlyItsActualCoverage() {
            string a = Run("a", "windows", "ok\r\n"), b = Run("b", "windows", "ok\r\n");
            Assert.Equal(0, Compare(a, b, out JsonObject report));
            Assert.False(report["coverage"]!["fullSuiteVerified"]!.GetValue<bool>());
            Assert.Equal("produced-only", report["coverage"]!["scope"]!.GetValue<string>());
            Assert.Equal("sample.ansi", report["coverage"]!["left"]!["paths"]![0]!.GetValue<string>());
        }

        [Fact]
        public void ProducerReceiptsPreserveCompletionAndIndividualProductionErrors() {
            string repo = Path.Combine(_root, "producer");
            Directory.CreateDirectory(Path.Combine(repo, "Website", "captures"));
            File.WriteAllText(Path.Combine(repo, "ConsoleMonster.sln"), string.Empty);
            var site = new Site(Path.Combine(repo, "Website"));
            string output = Path.Combine(site.Captures, "sample.ansi");
            byte[] bytes = Encoding.UTF8.GetBytes("ok");
            CaptureRun.Reset();
            try {
                File.WriteAllBytes(output, bytes);
                CaptureRun.Observe(bytes, 20, 5);
                CaptureRun.Record(output);
                CaptureRun.Write(site);
                JsonObject receipt = JsonNode.Parse(File.ReadAllText(Path.Combine(site.Captures, "run.json")))!.AsObject();
                Assert.True(receipt["complete"]!.GetValue<bool>());
                Assert.Single(receipt["files"]!.AsObject());
                string broken = Path.Combine(site.Captures, "broken.cast");
                File.WriteAllText(broken, "{");
                CaptureRun.Record(broken);
                CaptureRun.Write(site, complete: false);
                receipt = JsonNode.Parse(File.ReadAllText(Path.Combine(site.Captures, "run.json")))!.AsObject();
                Assert.False(receipt["complete"]!.GetValue<bool>());
                Assert.Equal(2, receipt["files"]!.AsObject().Count);
                Assert.NotNull(receipt["files"]!["broken.cast"]!["error"]);
                Assert.Equal(CaptureRun.Hash(bytes), receipt["files"]!["sample.ansi"]!["sha256"]!.GetValue<string>());
                CaptureRun.Observe(bytes, 30, 5);
                CaptureRun.Write(site);
                receipt = JsonNode.Parse(File.ReadAllText(Path.Combine(site.Captures, "run.json")))!.AsObject();
                Assert.False(receipt["complete"]!.GetValue<bool>());
                Assert.Contains("path-bound evidence", receipt["files"]!["sample.ansi"]!["error"]!.GetValue<string>());
            } finally { CaptureRun.Reset(); }
        }

        [Theory]
        [InlineData("docs/profile.windows.ansi", "docs/profile.macos.ansi", "ok\r\n")]
        [InlineData("docs/profile.ansi", "docs/profile.macos.ansi", "ok\r\n")]
        public void PlatformVariantsResolveByLogicalName(string leftFile, string rightFile, string text) {
            Assert.Equal(0, CaptureComparison.Run(Run("a", "windows", text, leftFile), Run("b", "macos", text, rightFile)));
        }

        [Theory]
        [InlineData("docs/profile.windows.ansi", "docs/profile.macos.ansi", "\u001b[31mok\u001b[0m\r\n")]
        [InlineData("docs/profile.windows.ansi", "docs/profile.macos.ansi", " ok\r\n")]
        [InlineData("live/stress.windows.cast", "live/stress.macos.cast", "other\r\n")]
        public void CategoryDoesNotWaiveStyleLayoutOrValueChanges(string leftFile, string rightFile, string changed) {
            string first = leftFile.EndsWith(".cast", StringComparison.Ordinal) ? Cast("ok\r\n") : "ok\r\n";
            string second = rightFile.EndsWith(".cast", StringComparison.Ordinal) ? Cast(changed) : changed;
            Assert.Equal(1, Compare(Run("a", "windows", first, leftFile), Run("b", "macos", second, rightFile), out JsonObject report));
            Assert.Empty(report["allowed"]!.AsArray());
            Assert.Contains(report["failures"]!.AsArray(), f => f is JsonObject o && o["changedRows"]!.AsArray().Count > 0);
        }

        [Fact]
        public void SamePlatformMeasurementDifferencesAlwaysFail() {
            Assert.Equal(1, CaptureComparison.Run(Run("a", "windows", Cast("12 ms\r\n"), "live/stress.windows.cast"),
                Run("b", "windows", Cast("13 ms\r\n"), "live/stress.windows.cast")));
        }

        [Fact]
        public void BlankCellStylesAndHyperlinksStillParticipateInNeutralParity() {
            string a = Run("a", "windows", "ok\r\n");
            Assert.Equal(1, CaptureComparison.Run(a, Run("b", "macos", "ok\r\n\u001b[31m \u001b[0m")));
            Assert.Equal(1, CaptureComparison.Run(a, Run("c", "macos", "\u001b]8;;https://example.test\u001b\\ok\u001b]8;;\u001b\\\r\n")));
        }

        [Fact]
        public void EveryChangedRowIncludesExactStyleAndCellDetails() {
            string a = Run("a", "windows", "one\r\ntwo\r\n"), b = Run("b", "macos", "ONE\r\n\u001b[31mtwo\u001b[0m\r\n");
            Assert.Equal(1, Compare(a, b, out JsonObject report));
            JsonArray rows = report["failures"]!.AsArray().OfType<JsonObject>().Single()["changedRows"]!.AsArray();
            Assert.Equal(2, rows.Count);
            Assert.Equal(0, rows[0]!["row"]!.GetValue<int>());
            Assert.Equal(1, rows[1]!["row"]!.GetValue<int>());
            Assert.Equal("t", rows[1]!["cells"]![0]!["left"]!["glyph"]!.GetValue<string>());
            Assert.NotEqual(rows[1]!["cells"]![0]!["left"]!["foreground"]!.GetValue<string>(), rows[1]!["cells"]![0]!["right"]!["foreground"]!.GetValue<string>());
        }

        [Fact]
        public void ScreenDescriptionsCompareSourceStyledHashesAndEveryScreenWithoutVtParsing() {
            const string text = "source 0123456789abcdef\f0123456789abcdef\nfirst\f1111111111111111\nlast";
            string a = Run("a", "windows", text, "sample.txt"), b = Run("b", "macos", text.Replace("\n", "\r\n"), "sample.txt");
            Assert.Equal(0, CaptureComparison.Run(a, b));
            b = Run("b", "macos", text.Replace("1111111111111111", "2222222222222222"), "sample.txt");
            Assert.Equal(1, Compare(a, b, out JsonObject report));
            JsonObject difference = report["failures"]!.AsArray().OfType<JsonObject>().Single();
            Assert.Empty(difference["changedRows"]!.AsArray());
            JsonNode screen = difference["changedScreenHashes"]![0]!;
            Assert.Equal(1, screen["frame"]!.GetValue<int>());
            Assert.Equal("1111111111111111", screen["left"]!.GetValue<string>());
            b = Run("b", "macos", text.Replace("source 0123456789abcdef", "source fedcba9876543210"), "sample.txt");
            Assert.Equal(1, CaptureComparison.Run(a, b));
        }

        [Theory]
        [InlineData("version")]
        [InlineData("producer")]
        [InlineData("platform")]
        [InlineData("category")]
        [InlineData("width")]
        [InlineData("sha256")]
        public void InvalidReceiptMetadataProducesFailureReport(string field) {
            string a = Run("a", "windows", "ok"), b = Run("b", "macos", "ok");
            Change(b, m => {
                if (field == "version") { m[field] = 2; }
                else if (field == "producer") { m[field] = "unknown"; }
                else { m["files"]!["sample.ansi"]![field] = field == "width" ? JsonValue.Create(0) : JsonValue.Create("invalid"); }
            });
            Assert.Equal(1, Compare(a, b, out JsonObject report));
            Assert.NotEmpty(report["failures"]!.AsArray());
        }

        [Fact]
        public void MissingOrMalformedReceiptReportsFailureInsteadOfThrowing() {
            string a = Run("a", "windows", "ok"), b = Run("b", "macos", "ok");
            File.WriteAllText(Path.Combine(b, "run.json"), "{");
            Assert.Equal(1, Compare(a, b, out _));
            File.Delete(Path.Combine(b, "run.json"));
            Assert.Equal(1, Compare(a, b, out _));
        }

        [Fact]
        public void DuplicateLogicalNamesAndWrongPlatformTagsFail() {
            string a = Run("a", "windows", "ok", "docs/profile.windows.ansi"), b = Run("b", "macos", "ok", "docs/profile.macos.ansi");
            Change(a, m => m["files"]!["docs/profile.ansi"] = m["files"]!["docs/profile.windows.ansi"]!.DeepClone());
            Assert.Equal(1, CaptureComparison.Run(a, b));
            a = Run("c", "windows", "ok", "docs/profile.macos.ansi");
            Assert.Equal(1, CaptureComparison.Run(a, b));
        }

        [Fact]
        public void NoncanonicalReceiptPathsCannotEscapeTheirDirectory() {
            string a = Run("a", "windows", "ok"), b = Run("b", "macos", "ok");
            File.WriteAllText(Path.Combine(_root, "outside.ansi"), "ok", new UTF8Encoding(false));
            Change(b, m => { m["files"]!["../outside.ansi"] = m["files"]!["sample.ansi"]!.DeepClone(); m["files"]!.AsObject().Remove("sample.ansi"); });
            Assert.Equal(1, Compare(a, b, out JsonObject report));
            Assert.Contains(report["failures"]!.AsArray(), f => f is JsonValue && f.GetValue<string>().Contains("canonical relative path", StringComparison.Ordinal));
        }

        [Fact]
        public void MissingPs5IsAllowedOnlyForWindowsAndStillValidatesRetainedFile() {
            string a = Run("a", "windows", "ok"), b = Run("b", "macos", "ok");
            File.WriteAllText(Path.Combine(a, "only.ps5.ansi"), "ok", new UTF8Encoding(false));
            Change(a, m => m["files"]!["only.ps5.ansi"] = m["files"]!["sample.ansi"]!.DeepClone());
            File.WriteAllText(Path.Combine(a, "sources.json"), new JsonObject { ["sample.ansi"] = "0123456789abcdef", ["only.ps5.ansi"] = "0123456789abcdef" }.ToJsonString());
            Assert.Equal(0, Compare(a, b, out JsonObject report));
            Assert.Single(report["allowed"]!.AsArray());
            File.WriteAllText(Path.Combine(a, "only.ps5.ansi"), "changed");
            Assert.Equal(1, CaptureComparison.Run(a, b));
            // A substring in an ordinary name is not a Windows PowerShell host identifier.
            Change(a, m => { m["files"]!.AsObject().Remove("only.ps5.ansi"); m["files"]!["ps5-not-host.ansi"] = m["files"]!["sample.ansi"]!.DeepClone(); });
            File.WriteAllText(Path.Combine(a, "ps5-not-host.ansi"), "ok", new UTF8Encoding(false));
            Assert.Equal(1, CaptureComparison.Run(a, b));
        }

        [Theory]
        [InlineData("sample.bin", "ok")]
        [InlineData("sample.txt", "not a styled description")]
        [InlineData("sample.ansi", "\u001b[?2026hpartial")]
        [InlineData("sample.cast", "{\"version\":2,\"width\":21,\"height\":5}\n[0,\"o\",\"ok\"]\n")]
        [InlineData("sample.cast", "{\"version\":2,\"width\":20,\"height\":5}\n[0,\"resize\",\"1x1\"]\n")]
        [InlineData("sample.cast", "{\"version\":2,\"width\":20,\"height\":5}\n[0,\"o\",\"\\u001b[?2026hpartial\"]\n")]
        public void UnsupportedOrMalformedCaptureContentFails(string file, string text) {
            Assert.Equal(1, CaptureComparison.Run(Run("a", "windows", text, file), Run("b", "macos", text, file)));
        }

        [Fact]
        public void CastPacketBoundariesAndInputMarkersAreNotOutputFrames() {
            string a = Run("a", "windows", Cast("😀ok\r\n"), "sample.cast");
            string split = "{\"version\":2,\"width\":20,\"height\":5}\n[0,\"i\",\"hidden input\"]\n[0,\"m\",\"chapter\"]\n[0.1,\"o\",\"😀\"]\n[0.2,\"o\",\"ok\\r\\n\"]\n";
            Assert.Equal(0, CaptureComparison.Run(a, Run("b", "macos", split, "sample.cast")));
            string stamped = Cast("😀ok\r\n").Replace("{", "{\"cm_source\":\"0123456789abcdef\",", StringComparison.Ordinal);
            Assert.Equal(1, CaptureComparison.Run(a, Run("c", "macos", stamped, "sample.cast")));
        }

        [Theory]
        [InlineData("界", 1, true)]
        [InlineData("界", 2, false)]
        [InlineData("\uFFFD", 1, true)]
        [InlineData("x", 1, false)]
        public void QualityRejectsReplacementAndWideGlyphsStoredWithoutTheirSecondCell(string glyph, int width, bool broken) {
            var frame = new CellBuffer(4, 1);
            frame.Set(0, 0, glyph, Style.Plain, width);
            Assert.Equal(broken, CaptureComparison.Quality(frame).Any());
        }

        [Theory]
        [InlineData("│─", false)]
        [InlineData("─\n│", false)]
        [InlineData("┌─┐\n└─┘\n┌─┐\n└─┘", false)]
        [InlineData("┌─┐\n┘─└", true)]
        public void BorderQualityDistinguishesSeparateLinesAndReportsExactBrokenEdge(string text, bool broken) {
            string[] rows = text.Split('\n');
            var frame = new CellBuffer(rows.Max(r => r.Length), rows.Length);
            for (int row = 0; row < rows.Length; row++) { frame.Write(0, row, rows[row], Style.Plain); }
            string[] problems = CaptureComparison.Quality(frame).ToArray();
            Assert.Equal(broken, problems.Length > 0);
            if (broken) { Assert.Contains(problems, p => p.Contains("from 0,1 (┘) to 1,1 (─)", StringComparison.Ordinal)); }
        }

        private string Run(string name, string platform, string text, string file = "sample.ansi") {
            string path = Path.Combine(_root, name);
            Directory.CreateDirectory(path);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(path, file))!);
            File.WriteAllText(Path.Combine(path, file), text, new UTF8Encoding(false));
            var manifest = new JsonObject {
                ["version"] = 1, ["producer"] = platform, ["complete"] = true, ["scope"] = "produced-only",
                ["files"] = new JsonObject { [file] = new JsonObject {
                    ["width"] = 20, ["height"] = 5, ["platform"] = platform,
                    ["category"] = CaptureRun.Category(file), ["sha256"] = CaptureRun.Hash(Encoding.UTF8.GetBytes(text))
                } }
            };
            File.WriteAllText(Path.Combine(path, "run.json"), manifest.ToJsonString());
            File.WriteAllText(Path.Combine(path, "sources.json"), new JsonObject { [file] = "0123456789abcdef" }.ToJsonString());
            return path;
        }

        private static void Change(string path, Action<JsonObject> change) {
            string file = Path.Combine(path, "run.json");
            JsonObject manifest = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
            change(manifest);
            File.WriteAllText(file, manifest.ToJsonString());
        }

        private int Compare(string a, string b, out JsonObject report) {
            string path = Path.Combine(_root, "report.json");
            int code = CaptureComparison.Run(a, b, path);
            report = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            return code;
        }

        private static string Cast(string text) => "{\"version\":2,\"width\":20,\"height\":5}\n[0,\"o\"," + JsonValue.Create(text)!.ToJsonString() + "]\n";

        public void Dispose() { if (Directory.Exists(_root)) { Directory.Delete(_root, true); } }
    }
}
