using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class TreeValueColumnTests {
        [Fact]
        public void Render_ValuesAtDifferentDepths_ShareRightAlignedColumnEnds() {
            Tree tree = new Tree("Inventory");
            tree.ValueColumns.Add(new TableColumn("Count") { Key = "count", Priority = 20 });
            TreeNode a = tree.Add("parent");
            a.Values["count"] = TableCell.FromValue(7);
            a.Add("grandchild").Values["count"] = TableCell.FromValue(123);
            string[] rows = TestTerminal.RenderText(tree, 120).Split('\n');
            string parent = Assert.Single(rows, row => row.Contains("parent"));
            string child = Assert.Single(rows, row => row.Contains("grandchild"));
            Assert.EndsWith("7", parent);
            Assert.EndsWith("123", child);
            Assert.Equal(parent.Length, child.Length);
        }

        [Fact]
        public void Render_InsufficientWidth_DropsLowPriorityColumnAndReportsItsName() {
            Tree tree = new Tree();
            tree.ValueColumns.Add(new TableColumn("Important") { Width = 20, Priority = 10 });
            tree.ValueColumns.Add(new TableColumn("Optional") { Width = 25, Priority = 0 });
            TreeNode node = tree.Add("label requiring room for a useful identity");
            node.Values["Important"] = TableCell.FromValue("retain-me");
            node.Values["Optional"] = TableCell.FromValue("drop-me");
            string text = TestTerminal.RenderText(tree, 60);
            Assert.Contains("retain-me", text);
            Assert.DoesNotContain("drop-me", text);
            Assert.Contains("+1 columns (Optional)", text);
        }

        [Theory]
        [InlineData(GlyphTier.Full, "↺ cycle", "↪ shown above")]
        [InlineData(GlyphTier.Ascii, "@ cycle", "@ shown above")]
        public void Render_IdentityReferences_StopsCyclesAndSuppressesRepeatedSubtrees(GlyphTier tier, string cycle, string repeat) {
            Tree tree = new Tree { DetectRepeatedNodes = true, WrapLabels = true };
            TreeNode first = tree.Add("first");
            first.Identity = "shared";
            first.Add("child").Add(first);
            TreeNode second = tree.Add("second");
            second.Identity = "shared";
            second.Add("must-not-expand");
            string text = TestTerminal.RenderText(tree, 80, glyphs: tier);
            Assert.Contains(cycle, text);
            Assert.Contains(repeat, text);
            Assert.DoesNotContain("must-not-expand", text);
            Assert.Equal(2, text.Split('\n').Count(row => row.Contains("first")));
        }

        [Fact]
        public void Render_CollapsedIncompleteNode_UsesAggregateWithoutVisitingChildren() {
            Tree tree = new Tree { WrapLabels = true, CollapsedSummary = node => node.Children.Count + " descendants" };
            TreeNode collapsed = tree.Add("collapsed source");
            collapsed.Reference = TreeReferenceKind.Collapsed | TreeReferenceKind.Incomplete;
            collapsed.Add("hidden");
            string text = TestTerminal.RenderText(tree, 80);
            Assert.Contains("▸ collapsed", text);
            Assert.Contains("? incomplete", text);
            Assert.Contains("1 descendants", text);
            Assert.DoesNotContain("hidden", text);
        }

        [Fact]
        public void Render_NarrowLongLabel_KeepsReferenceMarkersAheadOfTruncatedIdentity() {
            Tree tree = new Tree();
            tree.Add(new string('x', 100)).Reference = TreeReferenceKind.Cycle | TreeReferenceKind.Incomplete;
            string text = TestTerminal.RenderText(tree, 45, glyphs: GlyphTier.Ascii);
            Assert.Contains("@ cycle ? incomplete", text);
            Assert.Contains("...", text);
        }

        [Fact]
        public void Render_MediumWidth_UsesMutedValueNotesInsteadOfColumnHeadings() {
            Tree tree = new Tree();
            tree.ValueColumns.Add(new TableColumn("Category"));
            tree.Add("item").Values["Category"] = TableCell.FromValue("retained-note");
            string text = TestTerminal.RenderText(tree, 80);
            Assert.Contains("item  retained-note", text);
            Assert.DoesNotContain("Category", text);
        }

        [Fact]
        public void Hierarchy_DepthCollapse_UsesCallbackAndIncompleteMarker() {
            Tree tree = new Tree { WrapLabels = true };
            TreeHierarchy.Add(tree.Root, new[] { "root" }, name => new TreeNode(name), name => new[] { "hidden" },
                new TreeHierarchyOptions { Depth = 0, CollapsedSummary = item => "24 descendants", IsIncomplete = item => true });
            string text = TestTerminal.RenderText(tree, 80);
            Assert.Contains("24 descendants", text);
            Assert.Contains("incomplete", text);
            Assert.DoesNotContain("hidden", text);
        }
    }
}
