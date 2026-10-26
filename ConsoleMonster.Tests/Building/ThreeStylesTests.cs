using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Building {
    /// <summary>
    /// The same screen written three ways (object initializers, generated fluent methods, nested Ui factories) renders
    /// identically: the three styles are one object model. The README section "Three ways to write it" shows this screen.
    /// </summary>
    public sealed class ThreeStylesTests {
        public sealed class Server {
            public Server(string name, string role, long ram, StatusKind status) {
                Name = name;
                Role = role;
                Ram = ram;
                Status = status;
            }

            public string Name { get; }

            public string Role { get; }

            public long Ram { get; }

            public StatusKind Status { get; }
        }

        private static readonly Server[] Servers = {
            new Server("web01", "IIS", 8L << 30, StatusKind.Pass),
            new Server("app01", "App", 16L << 30, StatusKind.Warn),
            new Server("db01", "SQL", 64L << 30, StatusKind.Fail)
        };

        private static readonly double[] Cpu = { 12, 18, 35, 61, 44, 38, 52, 70, 66, 41 };

        // 1. Object initializers: the canonical object model.
        private static IWidget Initializers() {
            Table table = new Table { ShowTotals = true, AutoTotals = true };
            table.AddColumn(new TableColumn("Name") { IsKey = true });
            table.AddColumn(new TableColumn("Role"));
            table.AddColumn(new TableColumn("Ram") { Presenter = CellPresenters.Bytes });
            table.AddColumn(new TableColumn("Status"));
            foreach (Server server in Servers) {
                table.AddValues(server.Name, server.Role, server.Ram, server.Status);
            }
            KeyValueList summary = new KeyValueList();
            summary.AddValue("Servers", 3);
            summary.AddValue("Failed", 1);
            Sparkline cpu = new Sparkline { Title = "CPU" };
            cpu.AddRange(Cpu);
            return new Rows(
                new Columns(
                    new Panel(table, "Servers"),
                    new Panel(new Rows(summary, cpu), "Summary")) { Gap = 1 },
                new TextBlock("[muted]Updated 10:02[/]")) { Gap = 1 };
        }

        // 2. Fluent: the generated With/Add methods, and a typed table.
        private static IWidget Fluent() {
            return new Rows()
                .Gap(1)
                .Add(new Columns()
                    .Gap(1)
                    .Add(new Panel(Table.For(Servers)
                        .Column(s => s.Name, c => c.IsKey())
                        .Column(s => s.Role)
                        .Column(s => s.Ram, c => c.Presenter(CellPresenters.Bytes))
                        .Column(s => s.Status)
                        .Totals(), "Servers"))
                    .Add(new Panel(new Rows()
                        .Add(new KeyValueList().AddValue("Servers", 3).AddValue("Failed", 1))
                        .Add(new Sparkline().Title("CPU").AddRange(Cpu)), "Summary")))
                .Add(new TextBlock("[muted]Updated 10:02[/]"));
        }

        // 3. Nested composition: ThreeStylesNested.cs, with `using static ConsoleMonster.Ui;`.
        private static IWidget Nested() => ThreeStylesSample.NestedStyle.Screen(Servers, Cpu);

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void ThreeStyles_RenderTheSameScreen(GlyphTier glyphs) {
            string initializers = TestTerminal.RenderText(Initializers(), 84, 0, glyphs);
            Assert.Equal(initializers, TestTerminal.RenderText(Fluent(), 84, 0, glyphs));
            Assert.Equal(initializers, TestTerminal.RenderText(Nested(), 84, 0, glyphs));
            Golden.Match("building/three-styles." + glyphs.ToString().ToLowerInvariant() + ".txt", initializers);
        }

        [Fact]
        public void TypedTable_TakesHeadersFromTheExpressions() {
            Table<Server> table = Table.For(Servers).Column(s => s.Name).Column(s => s.Ram).Column("Memory role", s => s.Role);
            Assert.Equal(new[] { "Name", "Ram", "Memory role" }, new List<TableColumn>(table.Columns).ConvertAll(c => c.Header));
            Assert.Equal(3, table.Rows.Count);
        }

        [Fact]
        public void TypedTable_WithoutColumns_ShowsEveryPublicProperty() {
            Table<Server> table = Table.For(Servers);
            Assert.Equal(new[] { "Name", "Role", "Ram", "Status" }, new List<TableColumn>(table.Columns).ConvertAll(c => c.Header));
            Assert.Equal("db01", table.Rows[2][0].Value);
        }

        [Fact]
        public void TypedTable_RefreshReadsTheItemsAgain() {
            List<Server> list = new List<Server>(Servers);
            Table<Server> table = Table.For(list).Column(s => s.Name);
            Assert.Equal(3, table.Rows.Count);
            list.RemoveAt(0);
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(2, table.Refresh().Rows.Count);
        }

        public sealed class Reading {
            public Reading(string name, object? value) {
                Name = name;
                Value = value;
            }

            public string Name { get; }

            public object? Value { get; }
        }

        private static string TotalLine(Table table) {
            foreach (string line in TestTerminal.RenderText(table, 80, 0, GlyphTier.Ascii).Split('\n')) {
                if (line.Contains("All")) {
                    return line;
                }
            }
            return string.Empty;
        }

        [Fact]
        public void WithTotals_SumsNumericColumnsButNotKeysOrIdentifiersAndLeavesTheColumnsAlone() {
            Table<Server> table = Table.For(Servers).Column(s => s.Name).Column("Id", s => 7).Column("Disks", s => 2).Totals("All");
            string total = TotalLine(table);
            Assert.Contains("6", total);
            Assert.DoesNotContain("21", total);
            Assert.All(table.Columns, c => Assert.Equal(ColumnAggregate.None, c.Aggregate));
            table.AutoTotals = false;
            Assert.Equal(string.Empty, TotalLine(table));
        }

        [Fact]
        public void WithTotals_StopsSummingWhenAColumnNoLongerHoldsOnlyNumbers() {
            List<Reading> readings = new List<Reading> { new Reading("a", 1), new Reading("b", 2) };
            Table<Reading> table = Table.For(readings).Column(r => r.Name).Column(r => r.Value).Totals("All");
            Assert.Contains("3", TotalLine(table));
            readings.Add(new Reading("c", "n/a"));
            Assert.Equal(string.Empty, TotalLine(table.Refresh()));
        }

        [Fact]
        public void TypedTable_ExplicitColumnsReplaceColumnsInferredByAnEarlierRead() {
            Table<Server> table = Table.For(Servers);
            Assert.Equal(4, table.Columns.Count);
            table.Column(s => s.Name).Column(s => s.Role);
            Assert.Equal(new[] { "Name", "Role" }, new List<TableColumn>(table.Columns).ConvertAll(c => c.Header));
            Assert.Equal(2, table.Rows[0].Length);
        }

        [Fact]
        public void TypedTable_SimpleItemsAreOneColumn() {
            Assert.Single(Table.For(new[] { System.Guid.Empty }).Columns);
            Table<int?> numbers = Table.For(new int?[] { 1, null, 3 });
            Assert.Equal("Int32", Assert.Single(numbers.Columns).Header);
            Assert.Equal(3, numbers.Rows.Count);
            Assert.Contains("3", TestTerminal.RenderText(numbers, 40, 0, GlyphTier.Ascii));
        }

        [Fact]
        public void TypedTable_HeaderFromAnExpressionIsShownLiterally() {
            Table<Server> table = Table.For(Servers).Column(s => "[red]" + s.Name);
            Assert.Contains("\"[red]\" + s.Name", TestTerminal.RenderText(table, 80, 0, GlyphTier.Ascii));
        }
        [Theory]
        [InlineData("s => s.Name", "Name")]
        [InlineData("(s) => s.Address.City", "City")]
        [InlineData("s => s.Owner?.Name", "Name")]
        [InlineData("s => s.Ram * 2", "Ram * 2")]
        [InlineData("GetName", "GetName")]
        [InlineData(null, "Column 3")]
        public void TypedTable_HeaderFromExpression(string? expression, string header) {
            Assert.Equal(header, Table<Server>.HeaderFrom(expression, 2));
        }
    }
}
