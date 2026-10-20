using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Hierarchy {
    /// <summary>Trees built from hierarchical objects: nesting, statuses, and the depth, width, size and cycle limits.</summary>
    public sealed class TreeHierarchyTests {
        private sealed class Item {
            public Item(string name, StatusKind? status = null, params Item[] children) {
                Name = name;
                Status = status;
                Children = new List<Item>(children);
            }

            public string Name { get; }

            public StatusKind? Status { get; }

            public List<Item> Children { get; }
        }

        private static string Render(Tree tree) => TestTerminal.RenderText(tree, 60, 40, GlyphTier.Full);

        [Fact]
        public void Add_NestedItems_BuildsTheHierarchyWithStatuses() {
            Item root = new Item("Domain Admins", StatusKind.Warn, new Item("Tier 0 operators", null, new Item("Anna")), new Item("Piotr", StatusKind.Pass));
            Tree tree = new Tree("[bold]Groups[/]");

            int added = TreeHierarchy.Add(tree.Root, new[] { root }, item => new TreeNode(item.Name, item.Status), item => item.Children);

            Assert.Equal(4, added);
            string text = Render(tree);
            Assert.Contains("Domain Admins", text);
            Assert.Contains("│   └── Anna", text);
            Assert.Contains("    └── " + GlyphSet.For(GlyphTier.Full).StatusMarker(StatusKind.Pass) + " Piotr", text);
        }

        [Fact]
        public void Add_BeyondDepth_MarksTheDepthLimitInsteadOfFollowing() {
            Item root = new Item("A", null, new Item("B", null, new Item("C", null, new Item("D"))));
            Tree tree = new Tree();

            TreeHierarchy.Add(tree.Root, new[] { root }, item => new TreeNode(item.Name), item => item.Children, new TreeHierarchyOptions { Depth = 1 });

            string text = Render(tree);
            Assert.Contains("B", text);
            Assert.DoesNotContain("C", text.Replace("depth limit", string.Empty));
            Assert.Contains("… (depth limit)", text);
        }

        [Fact]
        public void Add_ManyChildren_ListsMaxItemsAndCountsTheRest() {
            Item root = new Item("Share", null, Enumerable.Range(1, 30).Select(i => new Item("file" + i)).ToArray());
            Tree tree = new Tree();

            TreeHierarchy.Add(tree.Root, new[] { root }, item => new TreeNode(item.Name), item => item.Children, new TreeHierarchyOptions { MaxItems = 5 });

            string text = Render(tree);
            Assert.Contains("file5", text);
            Assert.DoesNotContain("file6", text);
            Assert.Contains("+25 more", text);
        }

        [Fact]
        public void Add_Cycle_ShowsTheRepeatedItemOnceWithACycleNote() {
            Item a = new Item("Group A");
            Item b = new Item("Group B", null, a);
            a.Children.Add(b);
            Tree tree = new Tree();

            int added = TreeHierarchy.Add(tree.Root, new[] { a }, item => new TreeNode(item.Name), item => item.Children);

            Assert.Equal(3, added);
            Assert.Contains("Group A  (cycle)", Render(tree));
        }

        [Fact]
        public void Add_NodeLimit_StopsTheWalkWithOneNote() {
            Item root = new Item("Root", null, Enumerable.Range(1, 10).Select(i => new Item("child" + i, null, new Item("grandchild" + i))).ToArray());
            Tree tree = new Tree();

            int added = TreeHierarchy.Add(tree.Root, new[] { root }, item => new TreeNode(item.Name), item => item.Children, new TreeHierarchyOptions { MaxNodes = 4 });

            Assert.Equal(4, added);
            string text = Render(tree);
            Assert.Single(text.Split('\n'), line => line.Contains("node limit reached"));
        }

        [Fact]
        public void Add_Identity_DetectsCyclesThroughWrappers() {
            Item group = new Item("Loop");
            group.Children.Add(group);
            Tree tree = new Tree();

            TreeHierarchy.Add(tree.Root, new object[] { new KeyValuePair<string, Item>("w", group) },
                wrapper => new TreeNode(((KeyValuePair<string, Item>)wrapper).Value.Name),
                wrapper => ((KeyValuePair<string, Item>)wrapper).Value.Children.Select(child => (object)new KeyValuePair<string, Item>("w", child)),
                new TreeHierarchyOptions { Identity = wrapper => ((KeyValuePair<string, Item>)wrapper).Value });

            Assert.Contains("Loop  (cycle)", Render(tree));
        }
    }
}
