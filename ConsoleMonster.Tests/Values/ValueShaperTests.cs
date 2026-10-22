using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Xml;
using ConsoleMonster.Data;
using ConsoleMonster.Testing;
using ConsoleMonster.Values;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Values {
    /// <summary>The shared value rules: every input type has one shape, readable fields and safe one-line text.</summary>
    public sealed class ValueShaperTests {
        public sealed class Named {
            public string Name { get; set; } = "pwsh";

            public int Id { get; set; } = 90836;

            public bool Enabled { get; set; } = true;
        }

        public sealed class FlagFirst {
            public bool Enabled => true;

            public string Server => "web01";
        }

        public sealed class Thrower {
            public string Name => "ok";

            public string Broken => throw new InvalidOperationException("getter failed");
        }

        public sealed class Node {
            public string Name { get; set; } = "parent";

            public Node? Child { get; set; }

            public Node? Parent { get; set; }
        }

        private sealed class Slow {
            private readonly Action<string> _read;

            public Slow(Action<string> read) { _read = read; }

            public int A { get { _read(nameof(A)); return 1; } }

            public int B { get { _read(nameof(B)); return 2; } }

            public int C { get { _read(nameof(C)); return 3; } }

            public int D { get { _read(nameof(D)); return 4; } }
        }

        private static SecureString Secret() {
            SecureString secret = new SecureString();
            foreach (char c in "NotARealSecret-123") {
                secret.AppendChar(c);
            }
            secret.MakeReadOnly();
            return secret;
        }

        private static DataTable Table() {
            DataTable table = new DataTable("t");
            table.Columns.Add("Name");
            table.Columns.Add("Size", typeof(int));
            table.Rows.Add("a", 1);
            table.Rows.Add("b", DBNull.Value);
            return table;
        }

        public static IEnumerable<object?[]> Inputs() {
            Hashtable hashtable = new Hashtable { ["Name"] = "alpha", ["Count"] = 3 };
            OrderedDictionary ordered = new OrderedDictionary { ["Zulu"] = 1, ["Alpha"] = 2 };
            ConcurrentDictionary<string, int> concurrent = new ConcurrentDictionary<string, int>();
            concurrent.TryAdd("x", 1);
            XmlDocument xml = new XmlDocument();
            xml.LoadXml("<config><server name=\"web01\" port=\"80\"/><server name=\"web02\" port=\"81\"/></config>");
            yield return new object?[] { "Hashtable", hashtable, ValueShape.Record };
            yield return new object?[] { "Ordered", ordered, ValueShape.Record };
            yield return new object?[] { "Dictionary", new Dictionary<string, object> { ["Name"] = "alpha", ["Count"] = 3 }, ValueShape.Record };
            yield return new object?[] { "SortedDictionary", new SortedDictionary<string, int> { ["b"] = 2, ["a"] = 1 }, ValueShape.Record };
            yield return new object?[] { "ConcurrentDictionary", concurrent, ValueShape.Record };
            yield return new object?[] { "NonStringKeys", new Hashtable { [1] = "one", [DayOfWeek.Monday] = "start" }, ValueShape.Record };
            yield return new object?[] { "Array", new[] { 1, 2, 3 }, ValueShape.Collection };
            yield return new object?[] { "List", new List<int> { 1, 2, 3 }, ValueShape.Collection };
            yield return new object?[] { "ArrayList", new ArrayList { "a", "b" }, ValueShape.Collection };
            yield return new object?[] { "ArrayOfArrays", new[] { new[] { 1, 2 }, new[] { 3, 4 } }, ValueShape.Collection };
            yield return new object?[] { "Lazy", Enumerable.Range(1, 3), ValueShape.Collection };
            yield return new object?[] { "String", "hello", ValueShape.Scalar };
            yield return new object?[] { "Tuple", Tuple.Create(1, "a"), ValueShape.Record };
            yield return new object?[] { "DictionaryEntry", new DictionaryEntry("k", "v"), ValueShape.Record };
            yield return new object?[] { "KeyValuePair", new KeyValuePair<string, int>("k", 1), ValueShape.Record };
            yield return new object?[] { "DataRow", Table().Rows[0], ValueShape.Record };
            yield return new object?[] { "DataTable", Table(), ValueShape.Collection };
            yield return new object?[] { "DataView", Table().DefaultView, ValueShape.Collection };
            yield return new object?[] { "XmlDocument", xml, ValueShape.Record };
            yield return new object?[] { "XmlTextElement", xml.CreateElement("port"), ValueShape.Scalar };
            yield return new object?[] { "Enum", DayOfWeek.Monday, ValueShape.Scalar };
            yield return new object?[] { "Flags", FileAttributes.Hidden | FileAttributes.System, ValueShape.Scalar };
            yield return new object?[] { "DateTime", new DateTime(2026, 10, 19, 14, 5, 6), ValueShape.Scalar };
            yield return new object?[] { "DateTimeOffset", new DateTimeOffset(2026, 10, 19, 14, 5, 6, TimeSpan.FromHours(2)), ValueShape.Scalar };
            yield return new object?[] { "TimeSpan", TimeSpan.FromMinutes(95.5), ValueShape.Scalar };
            yield return new object?[] { "Guid", Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"), ValueShape.Scalar };
            yield return new object?[] { "Version", new Version(2, 0, 1), ValueShape.Scalar };
            yield return new object?[] { "IPAddress", IPAddress.Parse("192.0.2.10"), ValueShape.Scalar };
            yield return new object?[] { "Uri", new Uri("https://example.test/x"), ValueShape.Scalar };
            yield return new object?[] { "SecureString", Secret(), ValueShape.Scalar };
            yield return new object?[] { "Bytes", Enumerable.Range(1, 200).Select(i => (byte)i).ToArray(), ValueShape.Scalar };
            yield return new object?[] { "FileInfo", new FileInfo(typeof(ValueShaperTests).Assembly.Location), ValueShape.Record };
            yield return new object?[] { "DirectoryInfo", new DirectoryInfo(Path.GetDirectoryName(typeof(ValueShaperTests).Assembly.Location)!), ValueShape.Record };
            yield return new object?[] { "Null", null, ValueShape.Empty };
            yield return new object?[] { "DBNull", DBNull.Value, ValueShape.Empty };
            yield return new object?[] { "Object", new Named(), ValueShape.Record };
            yield return new object?[] { "Thrower", new Thrower(), ValueShape.Record };
#if NET8_0_OR_GREATER
            yield return new object?[] { "JsonElement", System.Text.Json.JsonDocument.Parse("{\"a\":1,\"b\":[1,2]}").RootElement, ValueShape.Record };
            yield return new object?[] { "JsonArray", System.Text.Json.JsonDocument.Parse("[1,2]").RootElement, ValueShape.Collection };
            yield return new object?[] { "JsonNode", System.Text.Json.Nodes.JsonNode.Parse("{\"a\":1,\"b\":[1,2]}"), ValueShape.Record };
#endif
        }

        [Theory]
        [MemberData(nameof(Inputs))]
        public void Shape_EveryInputType_HasOneShapeAndSafeText(string name, object? value, ValueShape expected) {
            Assert.Equal(expected, ValueShaper.ShapeOf(value));
            string text = ValueShaper.Text(value);
            object? cell = ValueShaper.Cell(value);
            string cellText = cell is TableCell marked ? marked.Markup : Convert.ToString(cell, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            foreach (string shown in new[] { text, cellText }) {
                Assert.DoesNotContain("System.Collections", shown);
                Assert.DoesNotContain("SyncRoot", shown);
                Assert.DoesNotContain("NotARealSecret", shown);
                Assert.DoesNotContain("[]", shown);
            }
            if (expected == ValueShape.Record) {
                Assert.NotEmpty(ValueShaper.Fields(ValueShaper.Prepare(value)!));
                Assert.False(string.IsNullOrEmpty(ValueShaper.Title(value!)), name + " has a title");
            }
        }

        [Fact]
        public void Fields_Hashtable_AreSortedAndOrderedDictionaryKeepsItsOrder() {
            Assert.Equal(new[] { "Count", "Name" }, ValueShaper.Fields(new Hashtable { ["Name"] = "alpha", ["Count"] = 3 }).Select(f => f.Name));
            OrderedDictionary ordered = new OrderedDictionary { ["Zulu"] = 1, ["Alpha"] = 2, ["Mike"] = 3 };
            Assert.Equal(new[] { "Zulu", "Alpha", "Mike" }, ValueShaper.Fields(ordered).Select(f => f.Name));
        }

        [Fact]
        public void Fields_DataRow_AreItsColumnsOnly() {
            IReadOnlyList<InspectMember> fields = ValueShaper.Fields(Table().Rows[1]);
            Assert.Equal(new[] { "Name", "Size" }, fields.Select(f => f.Name));
            Assert.Null(fields[1].Value);
        }

        [Fact]
        public void Fields_XmlElement_AreAttributesAndChildrenWithoutDomPlumbing() {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml("<config version=\"2\"><server name=\"web01\"/><server name=\"web02\"/><owner>Jan</owner></config>");
            object root = ValueShaper.Prepare(xml)!;
            IReadOnlyList<InspectMember> fields = ValueShaper.Fields(root);
            Assert.Equal(new[] { "version", "server", "owner" }, fields.Select(f => f.Name));
            Assert.Equal("web01, web02", ValueShaper.Text(fields[1].Value));
            Assert.Equal("Jan", fields[2].Value);
        }

        [Fact]
        public void Fields_Object_LeaveOutPlumbing() {
            Dictionary<string, object?> withPlumbing = new Dictionary<string, object?> { ["SyncRoot"] = 1 };
            Assert.Single(ValueShaper.Fields(withPlumbing));
            Assert.True(ValueShaper.IsPlumbing("CimInstanceProperties"));
            Assert.True(ValueShaper.IsPlumbing("PSParentPath"));
            Assert.False(ValueShaper.IsPlumbing("Name"));
        }

        [Fact]
        public void Text_Collections_FollowTheDisplayMode() {
            int[] items = Enumerable.Range(1, 8).ToArray();
            Assert.Equal("1, 2, 3, 4, 5 … +3", ValueShaper.Text(items));
            Assert.Equal("1, 2, 3 +5 more", ValueShaper.Text(items, new ShapeOptions { Collections = CollectionDisplay.FirstN, ItemLimit = 3 }));
            Assert.Equal("8 items", ValueShaper.Text(items, new ShapeOptions { Collections = CollectionDisplay.Count }));
            Assert.Equal("1 | 2", ValueShaper.Text(new[] { 1, 2 }, new ShapeOptions { Separator = " | " }));
            TableCell lines = Assert.IsType<TableCell>(ValueShaper.Cell(new[] { "a", "b", "c" }, new ShapeOptions { Collections = CollectionDisplay.Lines, ItemLimit = 2 }));
            Assert.Equal("a\nb\n+1 more", lines.Markup);
            Assert.Equal("[1, 2], [3, 4]", ValueShaper.Text(new[] { new[] { 1, 2 }, new[] { 3, 4 } }));
        }

        [Fact]
        public void Cell_EmptyCollectionIsAMutedDashAndNullStaysBlank() {
            TableCell empty = Assert.IsType<TableCell>(ValueShaper.Cell(Array.Empty<int>()));
            Assert.Equal("[muted]—[/]", empty.Markup);
            Assert.Null(ValueShaper.Cell(null));
            Assert.Null(ValueShaper.Cell(DBNull.Value));
        }

        [Fact]
        public void Text_SecretsAndBytesAreSafe() {
            Assert.Equal(ValueShaper.SecureText, ValueShaper.Text(Secret()));
            Assert.Equal(ValueShaper.SecureText, ValueShaper.Cell(Secret()));
            Assert.Equal("200 bytes", ValueShaper.Text(new byte[200], new ShapeOptions { Culture = System.Globalization.CultureInfo.InvariantCulture }));
        }

        [Fact]
        public void Text_Dictionary_IsKeyValuePairs() {
            Assert.Equal("Port=8080, Tls=yes", ValueShaper.Text(new OrderedDictionary { ["Port"] = 8080, ["Tls"] = true }, new ShapeOptions { Culture = System.Globalization.CultureInfo.InvariantCulture }));
            Assert.Equal("1=one", ValueShaper.Text(new Hashtable { [1] = "one" }));
        }

        [Fact]
        public void Title_PrefersNamesAddsIdAndNeverUsesBooleans() {
            Assert.Equal("pwsh (90836)", ValueShaper.Title(new Named()));
            Assert.Equal("pwsh", ValueShaper.Title(new Named(), withId: false));
            Assert.Equal("web01", ValueShaper.Title(new FlagFirst()));
            Assert.Equal("Hashtable", ValueShaper.Title(new Hashtable { ["Enabled"] = true, ["Count"] = 2 }));
            Assert.Equal("alpha", ValueShaper.Title(new Hashtable { ["Name"] = "alpha", ["Enabled"] = false }));
            Assert.Equal(1, ValueShaper.TitleIndex(new[] { "Id", "ProcessName", "Handles" }));
            Assert.Equal(-1, ValueShaper.TitleIndex(new[] { "Enabled", "Count" }));
        }

        [Fact]
        public void Read_FailingGetter_IsAFieldErrorAndAWarningCell() {
            InspectMember broken = ValueShaper.Fields(new Thrower()).Single(f => f.Name == "Broken");
            FieldError error = Assert.IsType<FieldError>(ValueShaper.Read(broken));
            Assert.Equal("getter failed", error.Message);
            TableCell cell = Assert.IsType<TableCell>(ValueShaper.Cell(error));
            Assert.Equal(Styling.StatusKind.Warn, cell.Status);
        }

        [Fact]
        public void Flatten_NestedRecords_UseDottedNamesAndStopAtCycles() {
            Node parent = new Node { Name = "parent" };
            Node child = new Node { Name = "child", Parent = parent };
            parent.Child = child;
            IReadOnlyList<KeyValuePair<string, object?>> flat = ValueShaper.Flatten(parent, 2);
            Assert.Contains(flat, pair => pair.Key == "Child.Name" && (string?)pair.Value == "child");
            Assert.Contains(flat, pair => pair.Key == "Child.Parent" && (string?)pair.Value == "(cycle)");
            Assert.Contains(flat, pair => pair.Key == "Name");
        }

        [Fact]
        public void Budget_SlowGetters_StopReadingInsteadOfStalling() {
            TimeSpan elapsed = TimeSpan.Zero;
            List<string> reads = new List<string>();
            Slow record = new Slow(name => { reads.Add(name); elapsed += TimeSpan.FromMilliseconds(120); });
            ShapeBudget budget = new ShapeBudget(TimeSpan.FromMilliseconds(150), int.MaxValue, () => elapsed);
            IReadOnlyList<InspectMember> fields = ValueShaper.Fields(record);
            Assert.Empty(reads);

            List<object?> values = fields.Select(field => ValueShaper.Read(field, budget)).ToList();

            Assert.Equal(new object?[] { 1, 2, BudgetExceeded.Instance, BudgetExceeded.Instance }, values);
            Assert.Equal(new[] { "A", "B" }, reads);
            Assert.Equal(TimeSpan.FromMilliseconds(240), elapsed);
            Assert.True(budget.WasExceeded);
        }

        [Theory]
        [InlineData(149, true)]
        [InlineData(150, true)]
        [InlineData(151, false)]
        public void Budget_Deadline_AllowsReadsThroughExactLimit(int milliseconds, bool allowed) {
            TimeSpan elapsed = TimeSpan.Zero;
            ShapeBudget budget = new ShapeBudget(TimeSpan.FromMilliseconds(150), 10, () => elapsed);
            int reads = 0;
            InspectMember field = new InspectMember("Value", () => { reads++; return 42; });
            elapsed = TimeSpan.FromMilliseconds(milliseconds);

            object? result = ValueShaper.Read(field, budget);

            Assert.Equal(allowed ? 1 : 0, reads);
            if (allowed) {
                Assert.Equal(42, result);
            } else {
                Assert.Same(BudgetExceeded.Instance, result);
            }
            Assert.Equal(!allowed, budget.WasExceeded);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Budget_NonpositiveTime_StillEnforcesNodeLimit(int milliseconds) {
            ShapeBudget budget = new ShapeBudget(TimeSpan.FromMilliseconds(milliseconds), 1, () => TimeSpan.FromDays(1));
            int reads = 0;
            InspectMember field = new InspectMember("Value", () => { reads++; return 42; });

            Assert.Equal(42, ValueShaper.Read(field, budget));
            Assert.False(budget.WasExceeded);
            Assert.Same(BudgetExceeded.Instance, ValueShaper.Read(field, budget));
            Assert.Equal(1, reads);
            Assert.True(budget.WasExceeded);
        }

        [Fact]
        public void Inspector_TimeBudget_StopsWithAMarker() {
            TimeSpan elapsed = TimeSpan.Zero;
            List<string> reads = new List<string>();
            Slow Record() => new Slow(name => { reads.Add(name); elapsed += TimeSpan.FromMilliseconds(120); });
            InspectorOptions options = new InspectorOptions { TimeBudget = TimeSpan.FromMilliseconds(150) };
            ShapeBudget budget = new ShapeBudget(options.TimeBudget, int.MaxValue, () => elapsed);
            InspectCollection root = Assert.IsType<InspectCollection>(ObjectInspector.InspectWithBudget(new[] { Record(), Record(), Record() }, options, budget));

            Assert.Equal(new[] { "A", "B" }, reads);
            Assert.Equal(TimeSpan.FromMilliseconds(240), elapsed);
            Assert.Equal(3, root.Items.Count);
            InspectObject first = Assert.IsType<InspectObject>(root.Items[0]);
            Assert.Equal(1, Assert.IsType<InspectScalar>(first.Members[0]).Value);
            Assert.All(root.Items.Skip(1), item => Assert.IsType<InspectLimit>(item));
            List<InspectLimit> limits = new List<InspectLimit>();
            void Walk(InspectNode node) {
                if (node is InspectLimit limit) {
                    limits.Add(limit);
                }
                foreach (InspectNode child in node is InspectObject obj ? obj.Members : node is InspectCollection list ? list.Items : (IReadOnlyList<InspectNode>)Array.Empty<InspectNode>()) {
                    Walk(child);
                }
            }
            Walk(root);
            Assert.Contains(limits, limit => limit.Summary.Contains("time budget"));
        }

        [Fact]
        public void Inspector_SecureStringAndPlumbing_AreSafe() {
            Dictionary<string, object?> root = new Dictionary<string, object?> { ["Secret"] = Secret() };
            InspectObject node = Assert.IsType<InspectObject>(ObjectInspector.Inspect(root));
            Assert.IsType<InspectScalar>(node.Members[0]);
            string text = TestTerminal.RenderText(InspectorTree.Build(node), 60);
            Assert.Contains(ValueShaper.SecureText, text);
            Assert.DoesNotContain("Length", text);
        }

        [Fact]
        public void InspectorTree_ScalarRoot_ShowsTheValue() {
            string text = TestTerminal.RenderText(InspectorTree.Build(ObjectInspector.Inspect("hello")), 40);
            Assert.Contains("hello", text);
        }

        [Fact]
        public void Register_ShaperByTypeAndAdapter_ApplyEverywhere() {
            try {
                ValueShaper.Register<Thrower>(value => new OrderedDictionary { ["Name"] = "shaped" });
                Assert.Equal("shaped", ValueShaper.Title(new Thrower()));
                Assert.Equal(new[] { "Name" }, ValueShaper.Fields(ValueShaper.Prepare(new Thrower())!).Select(f => f.Name));
            } finally {
                ValueShaper.Unregister(typeof(Thrower));
            }
            Assert.Equal("ok", ValueShaper.Title(new Thrower()));
        }

        [Fact]
        public void Table_ArrayAndDictionaryCells_RenderTheirItems() {
            Table table = new Table();
            table.AddColumn(new TableColumn("Tags"));
            table.AddColumn(new TableColumn("Settings"));
            table.AddValues(new[] { "a", "b" }, new OrderedDictionary { ["Port"] = 80 });
            table.AddValues(new[] { "c" }, new OrderedDictionary { ["Port"] = 81 });
            string text = TestTerminal.RenderText(table, 60);
            Assert.Contains("a, b", text);
            Assert.Contains("Port=80", text);
            Assert.DoesNotContain("System.", text);
        }
    }
}
