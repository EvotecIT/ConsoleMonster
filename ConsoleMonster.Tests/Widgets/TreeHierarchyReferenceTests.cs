using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Localization;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class TreeHierarchyReferenceTests {
        [Theory]
        [InlineData(TreeReferenceKind.Collapsed)]
        [InlineData(TreeReferenceKind.Cycle)]
        [InlineData(TreeReferenceKind.Repeat)]
        public void Add_StoppedIdentityThenOrdinaryRepeat_PreservesFollowingRootWithoutSelectingHiddenChildren(TreeReferenceKind stopping) {
            Tree tree = new Tree { DetectRepeatedNodes = true };
            var source = new[] {
                new ReferenceItem("first", "shared", stopping),
                new ReferenceItem("second", "shared", TreeReferenceKind.None),
                new ReferenceItem("following", "following", TreeReferenceKind.None)
            };
            int hiddenSelections = 0;
            int added = TreeHierarchy.Add(tree.Root, source,
                item => new TreeNode(item.Name) { Identity = item.Id, Reference = item.Reference },
                item => {
                    if (item.Name != "second") return null;
                    hiddenSelections++;
                    return Enumerable.Repeat(item, 10000);
                }, new TreeHierarchyOptions { DetectRepeatedNodes = true, Identity = item => ((ReferenceItem)item).Id, MaxNodes = 3 });
            Assert.Equal(0, hiddenSelections);
            Assert.Equal(3, added);
            Assert.Equal(new[] { "first", "second", "following" }, tree.Root.Children.Select(node => node.Label));
            Assert.True((tree.Root.Children[1].Reference & TreeReferenceKind.Repeat) != 0);
            Assert.Empty(tree.Root.Children[1].Children);
            string text = TestTerminal.RenderText(tree, 60);
            Assert.Contains("following", text);
            Assert.DoesNotContain("node limit", text);
        }

        [Theory]
        [InlineData(TreeReferenceKind.Collapsed)]
        [InlineData(TreeReferenceKind.Cycle)]
        [InlineData(TreeReferenceKind.Repeat)]
        [InlineData(TreeReferenceKind.Collapsed | TreeReferenceKind.Incomplete)]
        public void Add_FactoryStoppingReference_DoesNotSelectHiddenChildrenOrSpendFollowingRootBudget(TreeReferenceKind reference) {
            Tree tree = new Tree();
            int hiddenSelections = 0;
            int added = TreeHierarchy.Add(tree.Root, new[] { "A", "B" },
                name => new TreeNode(name) { Reference = name == "A" ? reference : TreeReferenceKind.None },
                name => {
                    if (name != "A") return null;
                    hiddenSelections++;
                    return Enumerable.Range(0, 10000).Select(index => "hidden-" + index);
                }, new TreeHierarchyOptions { MaxNodes = 2 });

            Assert.Equal(0, hiddenSelections);
            Assert.Equal(2, added);
            Assert.Equal(new[] { "A", "B" }, tree.Root.Children.Select(node => node.Label));
            Assert.Empty(tree.Root.Children[0].Children);
            string text = TestTerminal.RenderText(tree, 80);
            Assert.Contains("A", text);
            Assert.Contains("B", text);
            Assert.DoesNotContain("hidden-", text);
            Assert.DoesNotContain("node limit", text);
        }

        [Theory]
        [InlineData(TreeReferenceKind.Collapsed)]
        [InlineData(TreeReferenceKind.Cycle)]
        [InlineData(TreeReferenceKind.Repeat)]
        public void Add_StoppingParent_DoesNotEnumerateInputOrCreateHiddenNodes(TreeReferenceKind reference) {
            TreeNode parent = new TreeNode("parent") { Reference = reference };
            int enumerated = 0;
            IEnumerable<string> Input() { enumerated++; yield return "hidden"; }
            int added = TreeHierarchy.Add(parent, Input(), name => new TreeNode(name), name => null);
            Assert.Equal(0, added);
            Assert.Equal(0, enumerated);
            Assert.Empty(parent.Children);
        }

        [Fact]
        public void Add_IncompleteWithoutStoppingReference_StillBuildsVisibleDescendants() {
            Tree tree = new Tree();
            int added = TreeHierarchy.Add(tree.Root, new[] { "A", "B" },
                name => new TreeNode(name) { Reference = name == "A" ? TreeReferenceKind.Incomplete : TreeReferenceKind.None },
                name => name == "A" ? new[] { "child" } : null, new TreeHierarchyOptions { MaxNodes = 3 });
            Assert.Equal(3, added);
            Assert.Equal("child", Assert.Single(tree.Root.Children[0].Children).Label);
            Assert.Equal("B", tree.Root.Children[1].Label);
        }

        [Theory]
        [InlineData(GlyphTier.Full, true, "↺ cycle")]
        [InlineData(GlyphTier.Ascii, true, "@ cycle")]
        [InlineData(GlyphTier.Full, false, "↺ cycle")]
        [InlineData(GlyphTier.Ascii, false, "@ cycle")]
        public void Render_AutomaticHierarchyCycle_StaysStructuralInNarrowColumnsAndWrappedTrees(GlyphTier tier, bool columns, string marker) {
            Item a = Cycle();
            Tree tree = new Tree { WrapLabels = !columns };
            if (columns) tree.ValueColumns.Add(new TableColumn("Kind"));
            int selectedA = 0;
            int added = TreeHierarchy.Add(tree.Root, new[] { a }, item => new TreeNode(item.Name), item => {
                if (ReferenceEquals(item, a)) selectedA++;
                return item.Children;
            });
            TreeNode cycle = tree.Root.Children[0].Children[0].Children[0];
            Assert.Equal(3, added);
            Assert.Equal(1, selectedA);
            Assert.True((cycle.Reference & TreeReferenceKind.Cycle) != 0);
            string text = TestTerminal.RenderText(tree, 45, glyphs: tier);
            if (columns) {
                Assert.Contains(marker.Split(' ')[0] + " A  cycle", text);
            } else Assert.Contains(marker, text);
            Assert.Equal(2, text.Split('\n').Count(line => line.Contains("A")));
        }

        [Theory]
        [InlineData("en")]
        [InlineData("pl")]
        public void Add_AutomaticCycle_PreservesTheFactoryDetailLocalizationCallback(string language) {
            Tree tree = new Tree();
            TreeHierarchy.Add(tree.Root, new[] { Cycle() }, item => new TreeNode(item.Name) {
                LocalizedNote = labels => "source " + labels.TreeDepthLimit
            }, item => item.Children);
            CultureInfo culture = CultureInfo.GetCultureInfo(language);
            var context = Terminal.CreateContext(TestTerminal.Profile(culture: culture), Theme.Dark);
            WidgetLabels labels = context.Labels;
            TreeNode cycle = tree.Root.Children[0].Children[0].Children[0];
            Assert.Equal("source " + labels.TreeDepthLimit + " " + labels.TreeCycle, cycle.NoteFor(context));
        }

        private static Item Cycle() {
            Item a = new Item("A");
            Item b = new Item("B");
            a.Children.Add(b);
            b.Children.Add(a);
            return a;
        }

        private sealed class ReferenceItem {
            internal ReferenceItem(string name, string id, TreeReferenceKind reference) { Name = name; Id = id; Reference = reference; }
            internal string Name { get; }
            internal string Id { get; }
            internal TreeReferenceKind Reference { get; }
        }

        private sealed class Item {
            internal Item(string name) { Name = name; }
            internal string Name { get; }
            internal List<Item> Children { get; } = new List<Item>();
        }
    }
}
