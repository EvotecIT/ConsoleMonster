using System;
using System.Collections.Generic;
using ConsoleMonster.Data;
using ConsoleMonster.Testing;
using ConsoleMonster.Text;
using ConsoleMonster.Values;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class InspectorTreeTests {
        [Theory]
        [InlineData(24)]
        [InlineData(40)]
        public void Render_AlignmentLeavesRoomForValuesAndBranchDetails(int width) {
            var root = new InspectObject(null, "Record", "$", new InspectNode[] {
                new InspectScalar("A", "String", "$.A", "visible"),
                new InspectObject("B", "Child", "$.B", Array.Empty<InspectNode>()),
                new InspectCollection("C", "Array", "$.C", Array.Empty<InspectNode>(), 3, 0, null, 0),
                new InspectScalar("LongPropertyName", "String", "$.LongPropertyName", "")
            });
            string[] rows = TestTerminal.RenderText(InspectorTree.Build(root), width).Split('\n');
            Assert.Contains(rows, row => row.Contains("A ") && row.EndsWith("visible", StringComparison.Ordinal));
            Assert.Contains(rows, row => row.Contains("B ") && row.EndsWith("Child", StringComparison.Ordinal));
            Assert.Contains(rows, row => row.Contains("C ") && row.EndsWith("3 items", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(80)]
        [InlineData(120)]
        public void AutomaticIdentity_OmitsOnlyHeaderMembersAndKeepsExplicitTitleMembers(int width) {
            var record = new { Name = "worker", Id = 42, Status = "Running" };
            var represented = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string title = ValueShaper.Title(record, null, null, true, represented);
            InspectNode inspection = ObjectInspector.Inspect(record);
            string text = TestTerminal.RenderText(InspectorTree.Build(InspectorTree.OmitIdentity(inspection, represented), title), width);
            Assert.Contains("worker (42)", text);
            Assert.Contains(text.Split('\n'), row => row.Contains("Status") && row.EndsWith("Running", StringComparison.Ordinal));
            Assert.DoesNotContain("Name  ", text);
            Assert.DoesNotContain("Id  ", text);

            string explicitTitle = TestTerminal.RenderText(InspectorTree.Build(inspection, "Workers"), width);
            Assert.Contains("Name", explicitTitle);
            Assert.Contains("worker", explicitTitle);
            Assert.Contains("Id", explicitTitle);
            Assert.Contains("42", explicitTitle);
        }

        [Theory]
        [InlineData(80)]
        [InlineData(120)]
        public void Render_AlignsEachObjectsKeysByDisplayCellsWithoutPaddingPastSixteen(int width) {
            InspectScalar Scalar(string name, string value) => new InspectScalar(name, "String", "$." + name, value);
            var nested = new InspectObject("Nested", "Child", "$.Nested", new InspectNode[] {
                Scalar("Alpha", "child-first"), Scalar("B", "child-second"),
                new InspectScalar("+100000 empty", "InspectionEmptySummary", "$.Nested", string.Empty)
            });
            var root = new InspectObject(null, "Record", "$", new InspectNode[] {
                Scalar("短", "root-first"), Scalar("e\u0301", "root-second"),
                Scalar("LongPropertyNameBeyondTheCap", "long-value"), nested
            });
            Tree tree = InspectorTree.Build(root);
            Assert.Equal("短", tree.Root.Children[0].Label);
            Assert.Equal("e\u0301", tree.Root.Children[1].Label);
            string[] rows = TestTerminal.RenderText(tree, width).Split('\n');
            int ValueColumn(string marker) {
                string row = Assert.Single(rows, line => line.Contains(marker));
                return DisplayWidth.Measure(row.Substring(0, row.IndexOf(marker, StringComparison.Ordinal)));
            }
            Assert.Equal(ValueColumn("root-first"), ValueColumn("root-second"));
            Assert.Equal(22, ValueColumn("root-first"));
            Assert.Equal(ValueColumn("child-first"), ValueColumn("child-second"));
            Assert.Equal(15, ValueColumn("child-first"));
            Assert.Contains(rows, row => row.Contains("LongPropertyNameBeyondTheCap  long-value"));
        }
    }
}
