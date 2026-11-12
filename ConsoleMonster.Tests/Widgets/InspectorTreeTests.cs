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
        public void Render_AlignsSiblingKeysToTheWidestAndPutsValuesOfKeysPastTheCapBelow(int width) {
            string beyondCap = new string('k', width);
            InspectScalar Scalar(string name, string value) => new InspectScalar(name, "String", "$." + name, value);
            var nested = new InspectObject("Nested", "Child", "$.Nested", new InspectNode[] {
                Scalar("Alpha", "child-first"), Scalar("B", "child-second"),
                new InspectScalar("+100000 empty", "InspectionEmptySummary", "$.Nested", string.Empty)
            });
            var root = new InspectObject(null, "Record", "$", new InspectNode[] {
                Scalar("短", "root-first"), Scalar("e\u0301", "root-second"),
                Scalar("received_events_url", "url-value"), Scalar("url", "wide" + new string((char)118, 3 * width)), Scalar(beyondCap, "long-value"), nested
            });
            Tree tree = InspectorTree.Build(root);
            Assert.Equal("短", tree.Root.Children[0].Label);
            Assert.Equal("e\u0301", tree.Root.Children[1].Label);
            string[] rows = TestTerminal.RenderText(tree, width).Split('\n');
            int ValueColumn(string marker) {
                string row = Assert.Single(rows, line => line.Contains(marker));
                return DisplayWidth.Measure(row.Substring(0, row.IndexOf(marker, StringComparison.Ordinal)));
            }
            // Siblings share the column of their widest key that fits the cap (two fifths of the room after the guides).
            Assert.Equal(ValueColumn("root-first"), ValueColumn("root-second"));
            Assert.Equal(ValueColumn("root-first"), ValueColumn("url-value"));
            // A value wider than the room is shortened in the value column; the key column never gives way to it.
            Assert.Equal(ValueColumn("root-first"), ValueColumn("widevvv"));
            Assert.Equal(4 + "received_events_url".Length + 2, ValueColumn("root-first"));
            Assert.Equal(ValueColumn("child-first"), ValueColumn("child-second"));
            Assert.Equal(15, ValueColumn("child-first"));
            // A key past the cap keeps its full name; its value starts on the next line in the shared value column.
            int keyRow = Array.FindIndex(rows, row => row.Contains(beyondCap.Substring(0, width - 8)));
            Assert.DoesNotContain("long-value", rows[keyRow]);
            Assert.Contains("long-value", rows[keyRow + 1]);
            Assert.Equal(ValueColumn("root-first"), ValueColumn("long-value"));
            Assert.StartsWith("│", rows[keyRow + 1]);
        }


        [Theory]
        [InlineData(80)]
        [InlineData(120)]
        public void NestedGenericObjects_ReadByIdentityOrFieldCountInTreesAndNestedTables(int width) {
            InspectScalar Scalar(string name, object? value) => new InspectScalar(name, "Object", "$." + name, value);
            var uploader = new InspectObject("uploader", "PSCustomObject", "$.uploader", new InspectNode[] {
                Scalar("login", "daxian-dbw"), Scalar("id", 127450), Scalar("type", "User") });
            var pool = new InspectObject("pool", "PSCustomObject", "$.pool", new InspectNode[] { Scalar("min", 2), Scalar("max", 20), Scalar("idle", 300) });
            var typed = new InspectObject("owner", "Account", "$.owner", new InspectNode[] { Scalar("enabled", true) });
            // A certificate in a chain: its Key is an algorithm, not its name, so the item reads by its field count.
            var chain = new InspectCollection("Chain", "Object[]", "$.Chain", new InspectNode[] {
                new InspectObject(null, "PSCustomObject", "$.Chain[0]", new InspectNode[] {
                    Scalar("Subject", "CN=ISRG Root X1"),
                    Scalar("Key", "ECDSA P-256"), Scalar("Revoked", false) })
            }, 1, 0, null, 0);
            var root = new InspectObject(null, "PSCustomObject", "$", new InspectNode[] { uploader, pool, typed, chain });
            string tree = TestTerminal.RenderText(InspectorTree.Build(root, "asset"), width);
            Assert.Contains(tree.Split('\n'), row => row.Contains("[0]") && row.TrimEnd().EndsWith("3 fields", StringComparison.Ordinal));
            Assert.Contains(tree.Split('\n'), row => row.Contains("uploader") && row.TrimEnd().EndsWith("daxian-dbw (127450)", StringComparison.Ordinal));
            Assert.Contains(tree.Split('\n'), row => row.Contains("pool") && row.TrimEnd().EndsWith("3 fields", StringComparison.Ordinal));
            Assert.Contains(tree.Split('\n'), row => row.Contains("owner") && row.TrimEnd().EndsWith("Account", StringComparison.Ordinal));
            Assert.DoesNotContain("PSCustomObject", tree);

            var assets = new InspectCollection("assets", "Object[]", "$.assets", new InspectNode[] {
                new InspectObject(null, "PSCustomObject", "$.assets[0]", new InspectNode[] { Scalar("name", "a.zip"), uploader }),
                new InspectObject(null, "PSCustomObject", "$.assets[1]", new InspectNode[] { Scalar("name", "b.zip"), new InspectObject("uploader", "PSCustomObject", "$.assets[1].uploader", pool.Members) })
            }, 2, 0, new[] { "name", "uploader" }, 0);
            string table = TestTerminal.RenderText(new InspectorView(new InspectObject(null, "PSCustomObject", "$", new InspectNode[] { assets })), width);
            Assert.Contains("daxian-dbw (127450)", table);
            Assert.Contains("3 fields", table);
            Assert.DoesNotContain("PSCustomObject", table.Substring(table.IndexOf('\n')));
        }
    }
}
