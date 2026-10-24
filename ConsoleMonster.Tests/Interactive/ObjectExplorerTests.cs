using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Data;
using ConsoleMonster.Canvas;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>Observable lazy navigation, selection, bounded reads and captured explorer screens.</summary>
    public sealed class ObjectExplorerTests {
        [Fact]
        public void CollapsedChildren_DoNotReadGrandchildGetters() {
            Counting child = new Counting();
            ObjectExplorer explorer = new ObjectExplorer(new { Owner = child });
            ExplorerNode owner = Assert.Single(explorer.Root.Children);
            Assert.True(owner.CanExpand);
            Assert.Equal(0, child.Reads);
            ExplorerNode value = Assert.Single(owner.Children);
            Assert.Equal(0, child.Reads);
            Assert.Equal("hello", value.Value);
            Assert.Equal("hello", value.Value);
            Assert.Equal(1, child.Reads);
        }

        [Fact]
        public void CyclesDepthAndCollections_AreBounded() {
            Hashtable cycle = new Hashtable();
            cycle["Self"] = cycle;
            ExplorerNode self = Assert.Single(new ObjectExplorer(cycle).Root.Children);
            Assert.True(self.IsCycle);
            Assert.Empty(self.Children);
            ObjectExplorer shallow = new ObjectExplorer(new { Owner = new { Name = "Ada" } }, new InspectorOptions { MaxDepth = 1 });
            Assert.False(Assert.Single(shallow.Root.Children).CanExpand);
            ObjectExplorer huge = new ObjectExplorer(Enumerable.Range(0, int.MaxValue), new InspectorOptions { MaxItems = 3 });
            Assert.Equal(3, huge.Root.Children.Count);
            Assert.True(huge.Root.HasMore);
        }

        [Fact]
        public void QuotedPaths_KeepOriginalKeysAndValues() {
            Hashtable values = new Hashtable { ["O'Brien"] = 42 };
            ExplorerNode member = Assert.Single(new ObjectExplorer(values, rootPath: "$config").Root.Children);
            Assert.Equal("$config['O''Brien']", member.Path);
            Assert.Equal(42, member.Value);
        }

        [Fact]
        public void DrillAndBack_ReturnTheOriginalSelectedValueAndPath() {
            object group = new { Name = "Operators" };
            ObjectExplorer explorer = new ObjectExplorer(new { Owner = new { Groups = new[] { group } } });
            ExplorerNode? chosen = Show(explorer, "Enter Enter F10", out _);
            Assert.NotNull(chosen);
            Assert.Same(group, chosen!.Value);
            Assert.Equal("$x.Owner.Groups[0]", chosen.Path);
            ExplorerNode? owner = Show(new ObjectExplorer(new { Owner = new { Groups = new[] { group } } }), "Enter Backspace F10", out _);
            Assert.Equal("$x.Owner", owner!.Path);
        }

        [Fact]
        public void SearchNamesAndValues_AndCancellationWork() {
            ExplorerNode? selected = Show(new ObjectExplorer(new { First = "alpha", Second = "beta" }), "/ \"beta\" Enter F10", out _);
            Assert.Equal("$x.Second", selected!.Path);
            Assert.Null(Show(new ObjectExplorer(new { Name = "alpha" }), "Escape", out _));
        }

        [Fact]
        public void SessionBudget_BoundsCachedNodesAcrossCollapsedBranches() {
            ObjectExplorer explorer = new ObjectExplorer(new { A = new { Leaf = 1 }, B = new { Leaf = 2 } }, new InspectorOptions { MaxNodes = 4 });
            Assert.Equal(2, explorer.Root.Children.Count);
            Assert.Single(explorer.Root.Children[0].Children);
            Assert.Empty(explorer.Root.Children[1].Children);
            Assert.True(explorer.Root.Children[1].HasMore);
        }

        [Fact]
        public void TypedDictionaryKeys_HaveDistinctPaths() {
            Hashtable values = new Hashtable { [1] = "integer", ["1"] = "string", [false] = "false" };
            IReadOnlyList<ExplorerNode> nodes = new ObjectExplorer(values).Root.Children;
            Assert.Equal("$x[[System.Int32]'1']", nodes.Single(n => Equals(n.Value, "integer")).Path);
            Assert.Equal("$x['1']", nodes.Single(n => Equals(n.Value, "string")).Path);
            Assert.Equal("$x[$false]", nodes.Single(n => Equals(n.Value, "false")).Path);
        }

        [Fact]
        public void UnpacedCollapseAndMove_CannotChooseHiddenDescendants() {
            RecordingSurface capture = new RecordingSurface(80, 20, false);
            InteractiveOptions options = PromptHarness.Options("Right Left Down F10", capture);
            ((ConsoleMonster.Input.ScriptedInput)options.Input!).Paced = false;
            ExplorerNode? result = new ObjectExplorer(new { A = new { Leaf = 1 }, Z = 2 }).Show(options);
            Assert.Equal("$x.Z", result!.Path);
            Assert.Equal(2, result.Value);
        }

        [Fact]
        public void EmptySearch_CannotAcceptTheRoot() {
            Assert.Null(Show(new ObjectExplorer(new { Name = "web-01" }), "/ \"absent\" Enter Enter F10 Escape", out _));
        }

        [Fact]
        public void EmptySearch_DrillDoesNotAddNavigationHistory() {
            ObjectExplorer explorer = new ObjectExplorer(new { Parent = new { Child = new { Name = "web-01" } } });
            ExplorerNode? selected = Show(explorer, "Enter / \"absent\" Enter Enter Backspace F10", out _);
            Assert.Equal("$x.Parent", selected!.Path);
        }

        [Theory]
        [InlineData(60, false)]
        [InlineData(120, false)]
        [InlineData(60, true)]
        [InlineData(120, true)]
        public void SearchPreviewOmitsHiddenRowsAndReturnsAfterTheFilterIsCleared(int width, bool clearFilter) {
            var records = new[] {
                new { Name = "alpha", Payload = "hidden-first" },
                new { Name = "beta", Payload = "wanted-payload" },
                new { Name = "gamma", Payload = "hidden-third" }
            };
            string keys = clearFilter
                ? "/ \"beta\" Enter / Backspace Backspace Backspace Backspace Enter F10"
                : "/ \"beta\" Enter F10";
            ExplorerNode? selected = Show(new ObjectExplorer(records), keys, out RecordingSurface surface, width);
            Assert.Contains("hidden-first", surface.Frames.First().ToPlainText());
            string final = surface.Frames.Last().ToPlainText();
            Golden.Match("interactive/explorer.search." + (clearFilter ? "cleared." : "filtered.") + width + ".txt", final.TrimEnd() + "\n");
            if (clearFilter) {
                Assert.Contains("hidden-first", final);
                Assert.Contains("hidden-third", final);
                Assert.Same(records[0], selected!.Value);
            } else {
                Assert.DoesNotContain("hidden-first", final);
                Assert.DoesNotContain("hidden-third", final);
                Assert.Contains("beta", final);
                Assert.Same(records[1], selected!.Value);
            }
        }

        [Fact]
        public void LimitedCollection_KeepsTheFocusedBottomMemberVisible() {
            ExplorerNode? result = Show(new ObjectExplorer(Enumerable.Range(0, 50), new InspectorOptions { MaxItems = 20 }), "PageDown PageDown F10", out RecordingSurface surface);
            Assert.NotNull(result);
            string last = surface.Frames.Last().ToPlainText();
            Assert.Contains(">   " + result!.Name + "  " + result.Value, last);
            Assert.Contains("more items", last);
        }

#if NET8_0_OR_GREATER
        [Fact]
        public void JsonArrays_KeepOriginalIdentityAndBoundTheirPage() {
            var items = new System.Text.Json.Nodes.JsonArray();
            for (int i = 0; i < 1000; i++) { items.Add(i); }
            var root = new System.Text.Json.Nodes.JsonObject { ["Items"] = items };
            ExplorerNode node = Assert.Single(new ObjectExplorer(root, new InspectorOptions { MaxItems = 3 }).Root.Children);
            Assert.Same(items, node.Value);
            Assert.Equal(3, node.Children.Count);
            Assert.True(node.HasMore);
            Assert.Equal("2", node.Children[2].Summary);
        }
#endif

        [Theory]
        [InlineData(120, GlyphTier.Full, ColorDepth.TrueColor)]
        [InlineData(60, GlyphTier.Full, ColorDepth.TrueColor)]
        [InlineData(60, GlyphTier.Ascii, ColorDepth.None)]
        public void ExplorerScreen_MatchesGolden(int width, GlyphTier glyphs, ColorDepth depth) {
            ObjectExplorer explorer = new ObjectExplorer(new { Name = "web-01", Owner = new { Name = "Ada", Groups = new[] { "Operators", "Readers" } }, Enabled = true });
            Show(explorer, "Down Right F10", out RecordingSurface surface, width, glyphs, depth);
            string frame = surface.Frames.Last().ToPlainText().TrimEnd() + "\n";
            Golden.Match("interactive/explorer." + width + "." + depth + ".txt", frame);
            Assert.Contains("Owner", frame);
            Assert.Contains("Ada", frame);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(120)]
        public void MemberValues_AlignAndMuteTypeAndCountDescriptors(int width) {
            var value = new { Short = "alpha", MuchLonger = "omega", Owner = new { Name = "Ada" }, Items = new[] { 1, 2 } };
            Show(new ObjectExplorer(value), "F10", out RecordingSurface surface, width);
            CellBuffer frame = surface.Frames.Last();
            string[] lines = frame.ToPlainText().Split('\n');
            int alpha = Array.FindIndex(lines, line => line.Contains("alpha"));
            int omega = Array.FindIndex(lines, line => line.Contains("omega"));
            int owner = Array.FindIndex(lines, line => line.Contains("Owner"));
            int items = Array.FindIndex(lines, line => line.Contains("2 items"));
            int column = lines[alpha].IndexOf("alpha", StringComparison.Ordinal);
            Assert.Equal(column, lines[omega].IndexOf("omega", StringComparison.Ordinal));
            Assert.Equal(column, lines[owner].IndexOf("Object", StringComparison.Ordinal));
            Assert.Equal(column, lines[items].IndexOf("2 items", StringComparison.Ordinal));
            var context = new RenderContext(TerminalProfile.Fixed(width, 20, ColorDepth.TrueColor, GlyphTier.Full), Theme.Dark);
            Assert.Equal(context.Style(ThemeToken.Muted).Color, frame[column, owner].Style.Color);
            Assert.Equal(context.Style(ThemeToken.Muted).Color, frame[column, items].Style.Color);
        }

        [Fact]
        public void SearchField_AppearsOnlyDuringSearch() {
            ExplorerNode? selected = Show(new ObjectExplorer(new { Name = "alpha" }), "/ \"alp\" Enter F10", out RecordingSurface surface);
            Assert.Equal("alpha", selected!.Value);
            Assert.Contains(surface.Frames, frame => frame.ToPlainText().Split('\n').Any(line => line.TrimStart().StartsWith("/ alp", StringComparison.Ordinal)));
            Assert.DoesNotContain(surface.Frames.Last().ToPlainText().Split('\n'), line => line.TrimStart().StartsWith("/ ", StringComparison.Ordinal));
            Assert.DoesNotContain("Search /", surface.Frames.First().ToPlainText());
        }

        [Theory]
        [InlineData(60)]
        [InlineData(120)]
        public void DrilledCollection_LabelsKeepIdentityAndPreviewImmediatelyFollowsMembers(int width) {
            var services = new[] { new { Name = "web", Status = "Running" }, new { Name = "metrics", Status = "Running" } };
            ExplorerNode? selected = Show(new ObjectExplorer(new { Services = services }), "Enter F10", out RecordingSurface surface, width);
            Assert.Same(services[0], selected!.Value);
            Assert.Equal("$x.Services[0]", selected.Path);
            string[] lines = surface.Frames.Last().ToPlainText().Split('\n');
            Assert.Contains(lines, line => System.Text.RegularExpressions.Regex.IsMatch(line, @"\[0\]\s+web"));
            int lastMember = Array.FindIndex(lines, line => line.Contains("[1]") && line.Contains("metrics"));
            int header = Array.FindIndex(lines, line => line.Contains("Name") && line.Contains("Status"));
            Assert.True(lastMember >= 0 && header > lastMember);
            Assert.InRange(header - lastMember, 2, 3);
            Assert.Empty(lines[lastMember + 1].Trim(' ', '│', '|'));
        }

        [Fact]
        public void CollectionLabel_UsesTheCachedIdentifyingGetterAcrossFrames() {
            var first = new CountingIdentifier();
            var second = new CountingIdentifier();
            Show(new ObjectExplorer(new[] { first, second }), "Down Up F10", out _);
            Assert.Equal(1, first.Reads);
            Assert.Equal(1, second.Reads);
        }

        [Fact]
        public void CollectionLabels_LeaveTheNodeBudgetAvailableForExplicitExpansion() {
            var records = Enumerable.Range(0, 65).Select(i => {
                var record = new System.Collections.Specialized.OrderedDictionary { ["Name"] = "host-" + i };
                for (int member = 0; member < 79; member++) { record.Add("Field" + member, member); }
                return record;
            }).ToArray();
            var explorer = new ObjectExplorer(records, new InspectorOptions { MaxItems = 65, MaxNodes = 200 });
            foreach (ExplorerNode node in explorer.Root.Children) {
                Assert.Equal("host-" + Array.IndexOf(records, node.Value), node.DisplayValue().Text);
            }
            ExplorerNode last = explorer.Root.Children.Last();
            Assert.Equal(80, last.Children.Count);
            Assert.False(last.HasMore);
            Assert.Equal("host-64", last.Children[0].Value);
        }

        [Fact]
        public void CollectionLabel_UsesIdentifierPriorityAndHonorsExcludedGetters() {
            var record = new IdentifierRecord();
            var options = new InspectorOptions();
            options.Exclude.Add("Name");
            ExplorerNode node = Assert.Single(new ObjectExplorer(new[] { record }, options).Root.Children);
            Assert.Equal("identity", node.DisplayValue().Text);
            Assert.Equal(0, record.NameReads);
            Assert.Equal("identity", node.Children.Single(n => n.Name == "Id").Value);
            Assert.Equal(1, record.IdReads);
            Assert.Equal(0, record.DisplayReads);
            Assert.Equal(0, record.OtherReads);
        }

        [Fact]
        public void CollectionLabel_CachesThrowingGettersWhenFallingBackToAnotherIdentifier() {
            var record = new IdentifierRecord { ThrowName = true };
            ExplorerNode node = Assert.Single(new ObjectExplorer(new[] { record }).Root.Children);
            Assert.Equal("identity", node.DisplayValue().Text);
            Assert.Equal("identity", node.DisplayValue().Text);
            ExplorerNode name = node.Children.Single(n => n.Name == "Name");
            Assert.Null(name.Value);
            Assert.Equal("name unavailable", name.Error);
            Assert.Equal(1, record.NameReads);
            Assert.Equal(1, record.IdReads);
        }

        [Fact]
        public void CollectionLabel_DoesNotAliasDictionaryKeysWithTheSameDisplayName() {
            var record = new System.Collections.Specialized.OrderedDictionary { ["Name"] = "primary", [new NamedKey()] = "other" };
            ExplorerNode node = Assert.Single(new ObjectExplorer(new[] { record }).Root.Children);
            Assert.Equal("primary", node.DisplayValue().Text);
            Assert.Equal("primary", node.Children[0].Value);
            Assert.Equal("other", node.Children[1].Value);
            Assert.NotEqual(node.Children[0].Path, node.Children[1].Path);
        }

        private static ExplorerNode? Show(ObjectExplorer explorer, string keys, out RecordingSurface surface, int width = 90, GlyphTier glyphs = GlyphTier.Full, ColorDepth depth = ColorDepth.TrueColor) {
            RecordingSurface capture = new RecordingSurface(width, 20, false);
            surface = capture;
            InteractiveOptions options = PromptHarness.Options(keys, capture);
            options.Profile = TerminalProfile.Fixed(width, 20, depth, glyphs);
            return PromptHarness.Guard(capture, () => explorer.Show(options));
        }

        private sealed class Counting {
            internal int Reads;
            public string Text { get { Reads++; return "hello"; } }
        }

        private sealed class CountingIdentifier {
            internal int Reads;
            public string Name { get { Reads++; return "web"; } }
        }

        private sealed class IdentifierRecord {
            internal int NameReads;
            internal int IdReads;
            internal int DisplayReads;
            internal int OtherReads;
            internal bool ThrowName;
            public string Name { get { NameReads++; if (ThrowName) { throw new InvalidOperationException("name unavailable"); } return "primary"; } }
            public string Id { get { IdReads++; return "identity"; } }
            public string DisplayName { get { DisplayReads++; return "display"; } }
            public string Other { get { OtherReads++; return "other"; } }
        }

        private sealed class NamedKey {
            public override string ToString() => "Name";
        }
    }
}
