using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Views;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Views {
    /// <summary>The automatic view choice and the presenters picked by member name and type.</summary>
    public sealed class ConsoleViewTests {
        public enum ServiceState { Stopped, StartPending, Running }

        public enum Colour { Red, Green }

        public sealed class Server {
            public string Name { get; set; } = string.Empty;
            public ServiceState State { get; set; }
            public long Size { get; set; }
            public DateTime Seen { get; set; }
            public TimeSpan Uptime { get; set; }
            public bool Healthy { get; set; }
        }

        public sealed class Folder {
            public string Name { get; set; } = string.Empty;
            public List<Folder> Children { get; } = new List<Folder>();
        }

        public sealed class Other {
            public int Value { get; set; }
        }

        private static Server Srv(string name, ServiceState state) => new Server { Name = name, State = state, Size = 1536000, Seen = new DateTime(2026, 10, 1, 8, 30, 0), Uptime = TimeSpan.FromHours(5), Healthy = state == ServiceState.Running };

        [Fact]
        public void Decide_PicksTheViewForEachShape() {
            Folder root = new Folder { Name = "root" };
            root.Children.Add(new Folder { Name = "a" });
            List<KeyValuePair<string, IReadOnlyList<object?>>> shapes = new List<KeyValuePair<string, IReadOnlyList<object?>>> {
                Shape("nothing"),
                Shape("plain strings", "alpha", "beta"),
                Shape("json string", "{ \"name\": \"x\", \"count\": 3 }"),
                Shape("powershell string", "param($Name)\nGet-Item $Name | Where-Object { $_.Length -gt 0 }"),
                Shape("one number", 42),
                Shape("numbers", 3, 8.5, 5L),
                Shape("scalars", DateTime.MinValue, Guid.Empty),
                Shape("one object", Srv("srv01", ServiceState.Running)),
                Shape("objects of one shape", Srv("srv01", ServiceState.Running), Srv("srv02", ServiceState.Stopped)),
                Shape("objects of two shapes", Srv("srv01", ServiceState.Running), new Other()),
                Shape("hierarchy", root),
                Shape("hierarchies", root, new Folder { Name = "other" })
            };
            StringBuilder table = new StringBuilder();
            foreach (KeyValuePair<string, IReadOnlyList<object?>> shape in shapes) {
                table.Append(shape.Key.PadRight(24)).Append(ConsoleView.Decide(shape.Value)).Append('\n');
            }
            Golden.Match("views.decisions.txt", table.ToString());
        }

        [Fact]
        public void Decide_HonoursOverridesAndHints() {
            IReadOnlyList<object?> servers = new object?[] { Srv("a", ServiceState.Running), Srv("b", ServiceState.Stopped) };
            Assert.Equal(ConsoleViewKind.Grid, ConsoleView.Decide(servers, new ConsoleViewOptions { View = ConsoleViewKind.Grid }));
            Assert.Equal(ConsoleViewKind.Chart, ConsoleView.Decide(servers, new ConsoleViewOptions { Chart = true }));
            Assert.Equal(ConsoleViewKind.Tree, ConsoleView.Decide(new object?[] { new Folder() }, new ConsoleViewOptions { ChildProperty = "Children" }));
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void Auto_TableUsesPresentersByTypeAndName(GlyphTier tier) {
            IWidget view = ConsoleView.Auto(new object?[] { Srv("srv01", ServiceState.Running), Srv("srv02", ServiceState.Stopped), Srv("srv03", ServiceState.StartPending) });
            Golden.Match("views.table." + tier.ToString().ToLowerInvariant() + ".txt", TestTerminal.RenderText(view, 100, glyphs: tier));
        }

        [Fact]
        public void Auto_TreeAndChart() {
            Folder root = new Folder { Name = "root" };
            Folder b = new Folder { Name = "b" };
            b.Children.Add(new Folder { Name = "b1" });
            root.Children.Add(new Folder { Name = "a" });
            root.Children.Add(b);
            Assert.Equal("└── root\n    ├── a\n    └── b\n        └── b1", TestTerminal.RenderText(ConsoleView.Auto(root), 40).TrimEnd());
            string chart = TestTerminal.RenderText(ConsoleView.Auto(new object?[] { 1, 4 }), 30, glyphs: GlyphTier.Ascii);
            Assert.Contains("4", chart);
        }

        [Fact]
        public void ForMember_PicksPresentersByNameAndType() {
            Assert.Same(CellPresenters.Bytes, CellPresenters.ForMember("Length", typeof(long)));
            Assert.Same(CellPresenters.Bytes, CellPresenters.ForMember("WorkingSet64", typeof(long)));
            Assert.Null(CellPresenters.ForMember("Count", typeof(long)));
            Assert.Same(CellPresenters.Date, CellPresenters.ForMember("LastWriteTime", typeof(DateTime)));
            Assert.Same(CellPresenters.Duration, CellPresenters.ForMember("Uptime", typeof(TimeSpan)));
            Assert.Same(CellPresenters.Boolean, CellPresenters.ForMember("Enabled", typeof(bool?)));
            Assert.Same(CellPresenters.StatusWord, CellPresenters.ForMember("State", typeof(ServiceState)));
            Assert.Null(CellPresenters.ForMember("Colour", typeof(Colour)));
            Assert.Null(CellPresenters.ForMember("Name", typeof(string)));
        }

        private static KeyValuePair<string, IReadOnlyList<object?>> Shape(string name, params object?[] items) => new KeyValuePair<string, IReadOnlyList<object?>>(name, items);
    }
}
