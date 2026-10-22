using System;
using System.Linq;
using ConsoleMonster.Changes;
using Xunit;

namespace ConsoleMonster.Tests.Changes {
    public sealed class TextDiffTests {
        [Theory]
        [InlineData("default")]
        [InlineData("empty")]
        [InlineData("custom")]
        public void Unified_UsesDefaultOrCustomSideLabels(string scenario) {
            TextDiffOptions options = new TextDiffOptions { Context = 0 };
            if (scenario == "empty") {
                options.ReferenceLabel = string.Empty;
                options.DifferenceLabel = string.Empty;
            } else if (scenario == "custom") {
                options.ReferenceLabel = "Deployed";
                options.DifferenceLabel = "Proposed";
            }
            string expected = scenario == "custom" ? "--- Deployed\n+++ Proposed\n" : "--- Before\n+++ After\n";
            Assert.Equal(expected + "@@ -1,1 +1,1 @@\n-old\n+new\n", TextDiff.Compare("old\n", "new\n", options).ToUnifiedText());
        }

        [Theory]
        [InlineData("insert")]
        [InlineData("delete")]
        [InlineData("replace")]
        [InlineData("repeated-lines")]
        [InlineData("empty")]
        [InlineData("blank-line")]
        [InlineData("delete-document")]
        public void Compare_PreservesEditOrderContentAndOriginalLineNumbers(string scenario) {
            (string before, string after, string expected) = scenario switch {
                "insert" => ("a\nc\n", "a\nb\nc\n", "Unchanged:a:1:1|Added:b::2|Unchanged:c:2:3"),
                "delete" => ("a\nb\nc\n", "a\nc\n", "Unchanged:a:1:1|Removed:b:2:|Unchanged:c:3:2"),
                "replace" => ("a\nold\nz\n", "a\nnew\nz\n", "Unchanged:a:1:1|Removed:old:2:|Added:new::2|Unchanged:z:3:3"),
                "repeated-lines" => ("a\nb\na\n", "a\na\nb\n", "Unchanged:a:1:1|Removed:b:2:|Unchanged:a:3:2|Added:b::3"),
                "empty" => ("", "", ""),
                "blank-line" => ("", "\n", "Added:::1"),
                _ => ("x\n", "", "Removed:x:1:")
            };
            TextDiffResult result = TextDiff.Compare(before, after);
            Assert.Equal(expected, string.Join("|", result.Lines.Select(line => line.Kind + ":" + line.Text + ":" + line.ReferenceLineNumber + ":" + line.DifferenceLineNumber)));
            Assert.Equal(before, Reconstruct(result, true));
            Assert.Equal(after, Reconstruct(result, false));
        }

        [Theory]
        [InlineData("crlf")]
        [InlineData("cr")]
        [InlineData("null")]
        public void Compare_NormalizesLineEndingsAndTreatsNullAsEmpty(string scenario) {
            string? before = scenario == "crlf" ? "a\r\nb\r\n" : scenario == "cr" ? "a\rb\r" : null;
            string after = scenario == "null" ? "" : "a\nb\n";
            TextDiffResult result = TextDiff.Compare(before, after);
            Assert.False(result.HasChanges);
            Assert.Equal(string.Empty, result.ToUnifiedText());
        }

        [Fact]
        public void Unified_RecordsFinalNewlineOnlyChangesWithoutPhantomBlankLines() {
            TextDiffResult result = TextDiff.Compare("hello", "hello\n");
            Assert.Equal(2, result.Lines.Count);
            Assert.False(result.Lines[0].HasLineEnding);
            Assert.True(result.Lines[1].HasLineEnding);
            Assert.Equal("--- Before\n+++ After\n@@ -1,1 +1,1 @@\n-hello\n\\ No newline at end of file\n+hello\n", result.ToUnifiedText());
        }

        [Fact]
        public void Compare_IgnoreWhitespacePreservesOriginalTextAndStillComparesLineBoundaries() {
            TextDiffOptions options = new TextDiffOptions { IgnoreWhitespace = true };
            TextDiffResult equal = TextDiff.Compare(" key = 1\t\n", "key=1\n", options);
            Assert.False(equal.HasChanges);
            Assert.Equal(" key = 1\t", equal.Lines.Single().Text);
            Assert.True(TextDiff.Compare("a b\n", "ab\n").HasChanges);
            Assert.True(TextDiff.Compare("a\nb\n", "ab\n", options).HasChanges);
            Assert.True(TextDiff.Compare("a", "a\n", options).HasChanges);
        }

        [Theory]
        [InlineData("insert")]
        [InlineData("delete")]
        [InlineData("replace")]
        public void Unified_UsesExactZeroContextHunkCoordinates(string scenario) {
            (string before, string after, string expectedHunk) = scenario switch {
                "insert" => ("", "x\n", "@@ -0,0 +1,1 @@\n+x\n"),
                "delete" => ("x\n", "", "@@ -1,1 +0,0 @@\n-x\n"),
                _ => ("a\nb\nc\n", "a\nx\nc\n", "@@ -2,1 +2,1 @@\n-b\n+x\n")
            };
            TextDiffResult result = TextDiff.Compare(before, after, new TextDiffOptions { Context = 0 });
            Assert.Equal("--- Before\n+++ After\n" + expectedHunk, result.ToUnifiedText());
        }

        [Fact]
        public void Unified_SplitsSeparatedHunksAndMergesTouchingContext() {
            const string before = "1\n2\n3\n4\n5\n6\n7\n8\n9\n";
            const string after = "1\na\n3\n4\n5\n6\n7\nb\n9\n";
            string split = TextDiff.Compare(before, after, new TextDiffOptions { Context = 1 }).ToUnifiedText();
            Assert.Equal("--- Before\n+++ After\n@@ -1,3 +1,3 @@\n 1\n-2\n+a\n 3\n@@ -7,3 +7,3 @@\n 7\n-8\n+b\n 9\n", split);
            string merged = TextDiff.Compare(before, after, new TextDiffOptions { Context = 3 }).ToUnifiedText();
            Assert.Equal(1, merged.Split(new[] { "@@ -" }, StringSplitOptions.None).Length - 1);
            Assert.Contains("@@ -1,9 +1,9 @@", merged);
        }

        [Fact]
        public void Compare_LargeUnrelatedRangeRetainsSharedEdgesAndReconstructsBothInputs() {
            string before = "head\n" + string.Join("\n", Enumerable.Range(0, 5000).Select(i => "old" + i)) + "\ntail\n";
            string after = "head\n" + string.Join("\n", Enumerable.Range(0, 5000).Select(i => "new" + i)) + "\ntail\n";
            TextDiffResult result = TextDiff.Compare(before, after);
            Assert.Equal(5000, result.Added);
            Assert.Equal(5000, result.Removed);
            Assert.Equal(ChangeKind.Unchanged, result.Lines[0].Kind);
            Assert.Equal(ChangeKind.Unchanged, result.Lines[result.Lines.Count - 1].Kind);
            Assert.Equal(before, Reconstruct(result, true));
            Assert.Equal(after, Reconstruct(result, false));
        }

        [Fact]
        public void Options_AreValidatedAndCopiedAndUnifiedLabelsCannotInjectLines() {
            TextDiffOptions options = new TextDiffOptions { Context = 0, ReferenceLabel = "old\nfile", DifferenceLabel = "new\tfile" };
            Assert.Throws<ArgumentOutOfRangeException>(() => options.Context = -1);
            TextDiffResult result = TextDiff.Compare("", "new\n", options);
            options.Context = int.MaxValue;
            options.ReferenceLabel = "changed";
            Assert.Equal(0, result.Context);
            Assert.Equal("--- old file\n+++ new file\n@@ -0,0 +1,1 @@\n+new\n", result.ToUnifiedText());
            Assert.Contains("@@ -0,0 +1,1 @@", TextDiff.Compare("", "new\n", options).ToUnifiedText());
        }

        [Fact]
        public void ExistingObjectPreview_UsesSharedTextComparison() {
            Assert.Equal(new[] { ChangeKind.Unchanged, ChangeKind.Removed, ChangeKind.Added, ChangeKind.Unchanged, ChangeKind.Added },
                ChangeView.TextDiff("one\ntwo\nthree", "one\n2\nthree\nfour").Select(line => line.Kind));
            Assert.Empty(ChangeView.TextDiff("", ""));
        }

        private static string Reconstruct(TextDiffResult result, bool reference) => string.Concat(result.Lines
            .Where(line => line.Kind != (reference ? ChangeKind.Added : ChangeKind.Removed))
            .Select(line => line.Text + (line.HasLineEnding ? "\n" : string.Empty)));
    }
}
