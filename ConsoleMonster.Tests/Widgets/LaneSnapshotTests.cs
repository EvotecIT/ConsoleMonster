using System;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    /// <summary>Importing a completed lane preserves the public state that its timing columns render.</summary>
    public sealed class LaneSnapshotTests {
        [Fact]
        public void AddCopiesCompleteLaneAndPreservesDestinationOrder() {
            Lanes lanes = new Lanes();
            lanes.Add("first");
            Lane source = new Lane("build", "Build") {
                LabelText = RichText.Plain("Build", new RichStyle(ThemeToken.Accent)),
                DetailText = RichText.Plain("Complete", new RichStyle(ThemeToken.Pass)),
                Total = 5, Done = 5, Passed = 3, Failed = 1, Warned = 1, Skipped = 2,
                Status = StatusKind.Warn, Started = TimeSpan.FromSeconds(2), Finished = TimeSpan.FromSeconds(9), Order = 99
            };
            Lane actual = lanes.Add(source);
            Assert.NotSame(source, actual);
            Assert.Equal(1, actual.Order);
            Assert.Same(source.LabelText, actual.LabelText);
            Assert.Same(source.DetailText, actual.DetailText);
            Assert.Equal((5d, 5d, 3, 1, 1, 2, StatusKind.Warn),
                (actual.Total, actual.Done, actual.Passed, actual.Failed, actual.Warned, actual.Skipped, actual.Status));
            Assert.Equal(source.Started, actual.Started);
            Assert.Equal(source.Finished, actual.Finished);
            source.Done = 1;
            Assert.Equal(5, actual.Done);
            Assert.Same(actual, lanes.Add(actual));
            Assert.NotNull(actual.LabelText);
            Lane replacement = lanes.Add(new Lane("build", "") { Status = StatusKind.Pending });
            Assert.Same(actual, replacement);
            Assert.Equal(2, lanes.Items.Count);
            Assert.Equal(0, replacement.Total);
            Assert.Null(replacement.Started);
            Assert.Null(replacement.Finished);
            Assert.Null(replacement.DetailText);
            Assert.Equal("", replacement.Label);
            Assert.Null(replacement.LabelText);
        }
    }
}
