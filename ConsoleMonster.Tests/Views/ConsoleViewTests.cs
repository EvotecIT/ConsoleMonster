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
        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void Tree_HonorsRequestedDepth(int depth) {
            Folder root = new Folder { Name = "level1" };
            Folder current = root;
            for (int i = 2; i <= 12; i++) {
                Folder child = new Folder { Name = "level" + i };
                current.Children.Add(child);
                current = child;
            }
            Tree tree = ConsoleView.BuildTree(new object?[] { root }, new ConsoleViewOptions { MaxDepth = depth });
            string text = TestTerminal.RenderText(tree, 80, glyphs: GlyphTier.Ascii);
            Assert.Contains("level" + depth, text);
            Assert.DoesNotContain("level" + (depth + 1), text);
        }

        public enum ServiceState { Stopped, StartPending, Running }

        public enum Colour { Red, Green }

        public sealed class Server {
            public string Name { get; set; } = string.Empty;
            public ServiceState State { get; set; }
            public long Memory { get; set; }
            public DateTime Seen { get; set; }
            public TimeSpan Uptime { get; set; }
            public bool Healthy { get; set; }
        }

        public sealed class Folder {
            public string Name { get; set; } = string.Empty;
            public List<Folder> Children { get; } = new List<Folder>();
        }

        // A record with fields of its own and a collection of the same type: a release and its assets read as bags.
        public sealed class Release {
            public string Name { get; set; } = string.Empty;
            public string Tag { get; set; } = string.Empty;
            public List<Release> Assets { get; } = new List<Release>();
        }

        public sealed class Other {
            public int Value { get; set; }
        }

        private static Server Srv(string name, ServiceState state) => new Server { Name = name, State = state, Memory = 1536000, Seen = new DateTime(2026, 10, 1, 8, 30, 0), Uptime = TimeSpan.FromHours(5), Healthy = state == ServiceState.Running };

        [Fact]
        public void Decide_PicksTheViewForEachShape() {
            Folder root = new Folder { Name = "root" };
            root.Children.Add(new Folder { Name = "a" });
            Release release = new Release { Name = "v1.0", Tag = "v1.0" };
            release.Assets.Add(new Release { Name = "setup.zip" });
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
                Shape("hierarchies", root, new Folder { Name = "other" }),
                Shape("record with children", release),
                Shape("records with children", release, new Release { Name = "v0.9", Tag = "v0.9" })
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
            Assert.Equal(ConsoleViewKind.Chart, ConsoleView.Decide(servers, new ConsoleViewOptions { View = ConsoleViewKind.Chart }));
            Assert.Equal(ConsoleViewKind.Tree, ConsoleView.Decide(new object?[] { new Folder() }, new ConsoleViewOptions { ChildrenProperty = "Children" }));
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
            Assert.Same(CellPresenters.Bytes, CellPresenters.ForMember("WorkingSet64", typeof(long)));
            Assert.Null(CellPresenters.ForMember("Count", typeof(long)));
            Assert.Same(CellPresenters.Date, CellPresenters.ForMember("LastWriteTime", typeof(DateTime)));
            Assert.Same(CellPresenters.Duration, CellPresenters.ForMember("Uptime", typeof(TimeSpan)));
            Assert.Same(CellPresenters.Boolean, CellPresenters.ForMember("Enabled", typeof(bool?)));
            Assert.Same(CellPresenters.StatusWord, CellPresenters.ForMember("State", typeof(ServiceState)));
            Assert.Null(CellPresenters.ForMember("Colour", typeof(Colour)));
            Assert.Null(CellPresenters.ForMember("Name", typeof(string)));
        }

        [Theory]
        [InlineData("WorkingSet64")]
        [InlineData("PeakWorkingSet64")]
        [InlineData("PrivateMemorySize64")]
        [InlineData("SizeBytes")]
        [InlineData("SizeInBytes")]
        [InlineData("FileSize")]
        [InlineData("SizeOnDisk")]
        public void ForMember_ShowsByteNamesAsSizesFromAnySource(string name) {
            Assert.Same(CellPresenters.Bytes, CellPresenters.ForMember(name, typeof(long)));
        }

        [Theory]
        [InlineData("MinLength")]
        [InlineData("MaxLength")]
        [InlineData("BatchSize")]
        [InlineData("PageSize")]
        [InlineData("Length")]
        [InlineData("Size")]
        public void ForMember_KeepsLengthAndSizeCountsAsNumbers(string name) {
            Assert.Null(CellPresenters.ForMember(name, typeof(int)));
            Assert.Null(CellPresenters.ForMember(name, typeof(long), typeof(Server)));
        }

        [Theory]
        [InlineData(typeof(FileInfo))]
        [InlineData(typeof(DirectoryInfo))]
        [InlineData(typeof(MemoryStream))]
        public void ForMember_ShowsFileLengthAsSize(Type source) {
            Assert.Same(CellPresenters.Bytes, CellPresenters.ForMember("Length", typeof(long), source));
            Assert.Same(CellPresenters.Bytes, CellPresenters.ForMember("Size", typeof(long), source));
            Assert.Null(CellPresenters.ForMember("MaxLength", typeof(long), source));
        }

        [Theory]
        [InlineData("System.IO.FileInfo", true)]
        [InlineData("IO.FileInfo", true)]
        [InlineData("DirectoryInfo", true)]
        [InlineData("Selected.System.IO.FileInfo", true)]
        [InlineData("Deserialized.System.IO.FileInfo", true)]
        [InlineData("Deserialized.Selected.System.IO.FileInfo", true)]
        [InlineData("Selected.Deserialized.System.IO.FileInfo", true)]
        [InlineData("PSCustomObject", false)]
        [InlineData("Contoso.FileRule", false)]
        public void ByteSource_ReadsFileSourcesByTypeName(string typeName, bool file) {
            Type? source = CellPresenters.ByteSource(null, typeName);
            Assert.Equal(file, CellPresenters.ForLabel("Length", 1536L, source) == CellPresenters.Bytes);
            Assert.Null(CellPresenters.ForLabel("MaxLength", 260, source));
        }

        [Fact]
        public void Auto_TableShowsCharacterLimitsAsNumbersAndFileLengthsAsSizes() {
            IWidget limits = ConsoleView.Auto(new object?[] { new NameRule { Name = "storage", MinLength = 3, MaxLength = 24 }, new NameRule { Name = "vm", MinLength = 1, MaxLength = 260 } });
            string text = TestTerminal.RenderText(limits, 60, glyphs: GlyphTier.Ascii);
            Assert.Contains("260", text);
            Assert.DoesNotContain(" B ", text);

            string path = Path.GetTempFileName();
            try {
                File.WriteAllBytes(path, new byte[1536]);
                FileInfo file = new FileInfo(path);
                Table files = ConsoleView.BuildTable(new object?[] { file }, new ConsoleViewOptions { Property = new[] { "Name", "Length" } });
                Assert.Contains("1.5 KB", TestTerminal.RenderText(files, 100, glyphs: GlyphTier.Ascii));
                // One file is an inspector record; a list is a record sheet. Both keep its size.
                Assert.Contains("1.5 KB", TestTerminal.RenderText(ConsoleView.Auto(file), 100, glyphs: GlyphTier.Ascii));
                Assert.Contains("1.5 KB", TestTerminal.RenderText(ConsoleView.BuildList(new object?[] { file }, new ConsoleViewOptions { Property = new[] { "Name", "Length" } }), 100, glyphs: GlyphTier.Ascii));
                // A file in a mixed table does not make another object's count a size.
                Table mixed = ConsoleView.BuildTable(new object?[] { file, new Sized { Name = "rule", Length = 260 } }, new ConsoleViewOptions { Property = new[] { "Name", "Length" } });
                Assert.DoesNotContain("260 B", TestTerminal.RenderText(mixed, 100, glyphs: GlyphTier.Ascii));
            } finally {
                File.Delete(path);
            }
        }

        [Fact]
        public void InspectedStreams_KeepTheirLengthAsSize() {
            using MemoryStream stream = new MemoryStream(new byte[1536]);
            Assert.Contains("1.5 KB", TestTerminal.RenderText(InspectorTree.Build(ConsoleMonster.Data.ObjectInspector.Inspect(stream)), 100, glyphs: GlyphTier.Ascii));
            Assert.Contains("1.5 KB", TestTerminal.RenderText(new InspectorView(stream), 100, glyphs: GlyphTier.Ascii));
        }

        [Fact]
        public void InspectedFilesAndStreams_KeepTheirLengthAsSize_WhenIdentityIsAToken() {
            // A host's Identity can return a cycle token unrelated to the object; the object's own type still decides.
            ConsoleMonster.Data.InspectorOptions options = new ConsoleMonster.Data.InspectorOptions { Identity = value => new object() };
            using MemoryStream stream = new MemoryStream(new byte[1536]);
            Assert.Contains("1.5 KB", TestTerminal.RenderText(InspectorTree.Build(ConsoleMonster.Data.ObjectInspector.Inspect(stream, options)), 100, glyphs: GlyphTier.Ascii));
            string path = Path.GetTempFileName();
            try {
                File.WriteAllBytes(path, new byte[1536]);
                Assert.Contains("1.5 KB", TestTerminal.RenderText(new InspectorView(new FileInfo(path), options), 100, glyphs: GlyphTier.Ascii));
            } finally {
                File.Delete(path);
            }
        }

        public sealed class Sized {
            public string Name { get; set; } = string.Empty;
            public long Length { get; set; }
        }

        public sealed class NameRule {
            public string Name { get; set; } = string.Empty;
            public int MinLength { get; set; }
            public int MaxLength { get; set; }
        }

        private static KeyValuePair<string, IReadOnlyList<object?>> Shape(string name, params object?[] items) => new KeyValuePair<string, IReadOnlyList<object?>>(name, items);
    }
}
