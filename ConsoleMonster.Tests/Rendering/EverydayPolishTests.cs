using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>The small things of everyday output: identifiers, error facts, finished progress, key/value alignment, bar thresholds.</summary>
    public sealed class EverydayPolishTests {
        private static string[] Render(IWidget widget, int width, ColorDepth depth = ColorDepth.None, GlyphTier tier = GlyphTier.Full) =>
            TableShowcaseGoldenTests.WithInvariantCulture(() => Terminal.RenderText(widget, TerminalProfile.Fixed(width, 40, depth, tier), Theme.Dark))
                .Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

        [Theory]
        [InlineData(29, 100, "29%")]
        [InlineData(57, 100, "57%")]
        [InlineData(999, 1000, "99%")]
        [InlineData(7, 7, "100%")]
        public void Percent_RoundsDownPastBinaryNoise(int part, int whole, string expected) {
            Assert.Equal(expected, NumberFormat.Percent(part / (double)whole));
        }

        [Fact]
        public void ExceptionView_NarrowWidth_KeepsEveryLineInsideAndTheFileOnItsOwnLine() {
            Exception error;
            try {
                throw new InvalidOperationException("The report template folder is missing.");
            } catch (InvalidOperationException ex) {
                error = ex;
            }
            ConsoleMonster.Canvas.RenderContext context = new ConsoleMonster.Canvas.RenderContext(TerminalProfile.Fixed(60, 40, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            System.Collections.Generic.List<ConsoleMonster.Text.StyledLine> lines = new ExceptionView(error) { ShowSource = false }.Lines(48, context);
            Assert.All(lines, line => Assert.True(line.Width <= 48, line.PlainText));
            // The method and its file are on separate lines once they do not fit together.
            int at = lines.FindIndex(line => line.PlainText.TrimStart().StartsWith("at ", StringComparison.Ordinal));
            Assert.True(at >= 0);
            Assert.True(lines.Skip(at + 1).Take(5).Any(line => line.PlainText.Contains("EverydayPolishTests.cs")), string.Join("\n", lines.Select(line => line.PlainText)));
        }

        [Fact]
        public void IdentifierLabels_AreNeverGrouped_InRecordsAndTrees() {
            RecordView record = new RecordView("server");
            record.AddField("Port", 8080);
            record.AddField("ProcessId", 127096);
            record.AddField("Count", 127096);
            string text = string.Join("\n", Render(record, 60));
            Assert.Contains("8080", text);
            Assert.Contains("127096", text);
            Assert.Contains("127,096", text);
        }

        [Fact]
        public void ErrorFacts_LeaveOutAnIdThatRepeatsTheMessageOrType() {
            ExceptionInfo thrown = new ExceptionInfo("RuntimeException", "custom failure") { ErrorId = "custom failure" };
            // The type and the message only: the id "custom failure" repeats the message.
            Assert.Equal(2, Render(new ExceptionView(thrown) { ShowStack = false }, 80).Length);
            ExceptionInfo missing = new ExceptionInfo("ItemNotFoundException", "Cannot find path.") { Category = "ObjectNotFound", ErrorId = "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand" };
            Assert.Contains(Render(new ExceptionView(missing) { ShowStack = false }, 80), line => line.Trim() == "ObjectNotFound · PathNotFound");
        }

        [Fact]
        public void ErrorFacts_AThrownMessageWithACommaIsNotRepeatedAsAnId() {
            ExceptionInfo thrown = new ExceptionInfo("System.Management.Automation.RuntimeException", "Cannot reach DC01, check the network") { ErrorId = "Cannot reach DC01, check the network" };
            Assert.Equal(2, Render(new ExceptionView(thrown) { ShowStack = false }, 80).Length);
        }

        [Fact]
        public void ErrorCause_WithTheSameMessage_ShowsItsTypeOnly() {
            ExceptionInfo info = new ExceptionInfo("RuntimeException", "Attempted to divide by zero.") { Inner = new ExceptionInfo("DivideByZeroException", "Attempted to divide by zero.") };
            string[] lines = Render(new ExceptionView(info) { ShowStack = false }, 80);
            Assert.Equal(1, lines.Count(line => line.Contains("Attempted to divide by zero.")));
            Assert.Contains(lines, line => line.Contains("DivideByZeroException"));
        }

        [Fact]
        public void ActivityAtOneHundredPercent_ShowsACheckMark() {
            ActivityList list = new ActivityList();
            list.Apply(new LiveEvent(ActivityList.ProgressTarget, "update", key: "1", text: "Copying", value: 100, total: -1));
            string line = Render(list, 60)[0];
            Assert.StartsWith("✓", line.TrimStart());
        }

        [Fact]
        public void KeyValue_MixedText_StaysLeftAligned_NumbersOnlyShareAColumn() {
            KeyValueList mixed = new KeyValueList().Add("Host", "server01").Add("Uptime", "3 days 4 hours").Add("Users", "2");
            string[] rows = Render(mixed, 40);
            int column = rows[0].IndexOf("server01", StringComparison.Ordinal);
            Assert.Equal(column, rows[1].IndexOf("3 days", StringComparison.Ordinal));
            Assert.Equal(column, rows[2].IndexOf('2'));
        }

        private sealed class Stalling {
            public string Name { get; set; } = "web01";
            public int Cpu { get; set; }
            public int Slow1 { get { System.Threading.Thread.Sleep(250); return 1; } }
            public int Slow2 { get { System.Threading.Thread.Sleep(250); return 2; } }
            public int Slow3 { get { System.Threading.Thread.Sleep(250); return 3; } }
        }

        [Fact]
        public void ChangeSet_ListsMembersNotCompared_AndTheViewSaysSo() {
            ConsoleMonster.Changes.DiffOptions options = new ConsoleMonster.Changes.DiffOptions { TimeBudget = TimeSpan.FromMilliseconds(100) };
            ConsoleMonster.Changes.ChangeSet set = ConsoleMonster.Changes.ObjectDiff.Compare(new Stalling { Cpu = 4 }, new Stalling { Cpu = 8 }, options);
            Assert.True(set.Changed == 1);
            Assert.NotEmpty(set.NotCompared);
            string text = string.Join("\n", Render(new ConsoleMonster.Changes.ChangeView(set, ConsoleMonster.Changes.ChangeViewMode.Summary), 100));
            Assert.Contains("not compared", text);
        }

        [Theory]
        [InlineData(5, 3)]
        [InlineData(12, 3)]
        [InlineData(13, 1)]
        [InlineData(60, 1)]
        public void TreeDepth_WideObjectsOpenOneLevel(int fields, int expected) {
            Assert.Equal(expected, ConsoleMonster.Data.InspectorOptions.DefaultDepthFor(fields));
        }

        [Theory]
        [InlineData(93, StatusKind.Fail)]
        [InlineData(81, StatusKind.Warn)]
        [InlineData(34, null)]
        public void BarChart_ThresholdsMarkHighValues(double value, StatusKind? expected) {
            BarChart chart = new BarChart { Warning = 80, Critical = 90 };
            Assert.Equal(expected, chart.Threshold(value));
        }

        [Fact]
        public void BarChart_LowerIsWorse_WhenCriticalIsBelowWarning() {
            BarChart chart = new BarChart { Warning = 20, Critical = 10 };
            Assert.Equal(StatusKind.Fail, chart.Threshold(7));
            Assert.Equal(StatusKind.Warn, chart.Threshold(19));
            Assert.Null(chart.Threshold(66));
        }

        [Fact]
        public void BarChart_WithoutColour_MarksCrossedThresholdsWithASymbol() {
            BarChart chart = new BarChart { Warning = 80, Critical = 90, Unit = "%" }.Add("C:", 93).Add("D:", 34);
            string[] rows = Render(chart, 40);
            Assert.Contains("93%", rows[0]);
            Assert.DoesNotContain("D:", rows[0]);
            Assert.True(rows[0].Contains("✗ 93%") || rows[0].Contains("x 93%"));
            Assert.DoesNotContain("✗", rows[1]);
        }
    }
}
