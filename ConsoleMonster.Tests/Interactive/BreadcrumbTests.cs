using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Interactive;
using ConsoleMonster.Testing;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>A breadcrumb never says the same step twice in a row: a field named like its page is named once.</summary>
    public sealed class BreadcrumbTests {
        [Fact]
        public void FormBreadcrumb_FieldNamedLikeItsPage_IsSaidOnce() {
            Form form = new Form("New check");
            form.Page("Checks").Select("Checks", "Checks", new[] { new Choice("DNS"), new Choice("Time") });
            form.Page("Options").Toggle("Notify", "Email the report");
            ScreenCapture capture = new ScreenCapture(90, 20);
            form.Show(new FormOptions { Interactive = TestTerminal.Interactive("Enter Escape Escape", capture) });

            // The field's list is open on the Checks page.
            CellBuffer? open = capture.Frames.FirstOrDefault(f => f.ToPlainText().Contains("Time") && f.GetRowText(0).Contains("Checks"));
            Assert.True(open != null, "No frame shows the open Checks field. Last frame:\n" + capture.LastFrame?.ToPlainText());
            string header = open!.GetRowText(0);
            Assert.Contains("New check › Checks", header);
            Assert.DoesNotContain("Checks › Checks", header);
        }
    }
}
