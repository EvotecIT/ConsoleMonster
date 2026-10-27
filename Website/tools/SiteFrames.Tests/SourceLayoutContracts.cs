using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SiteFrames.Capture;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Example C# reads like hand-written code: long lines break at their structure and only whitespace changes.</summary>
    public sealed class SourceLayoutContracts {
        [Fact]
        public void LongChainsBreakOneCallPerLineAndNestedChainsIndentAgain() {
            string source = "using ConsoleMonster.Widgets;\nvar bar = new StackedBar().Add(\"Passed\", 412, ThemeToken.Pass).Add(\"Warnings\", 51, ThemeToken.Warn).Add(\"Failed\", 23, ThemeToken.Fail);\n";
            Assert.Equal("using ConsoleMonster.Widgets;\nvar bar = new StackedBar()\n    .Add(\"Passed\", 412, ThemeToken.Pass)\n    .Add(\"Warnings\", 51, ThemeToken.Warn)\n    .Add(\"Failed\", 23, ThemeToken.Fail);\n",
                SourceLayout.Format(source));
        }

        [Fact]
        public void LiteralArraysRejoinAndDataRowsFillTheirLines() {
            string source = "double[] rate =\n{\n    18,\n    21,\n    19\n};\nvar row = new Row(Name: \"Domain controllers\", Rule: \"LDAP signing required\", Reason: \"LDAPServerIntegrity is 1\", Weight: 1.0);\nrecord Row(string Name, string Rule, string Reason, double Weight);\n";
            string laid = SourceLayout.Format(source);
            Assert.StartsWith("double[] rate = { 18, 21, 19 };\n", laid, StringComparison.Ordinal);
            Assert.Contains("var row = new Row(Name: \"Domain controllers\", Rule: \"LDAP signing required\",\n    Reason: \"LDAPServerIntegrity is 1\", Weight: 1.0);\n", laid, StringComparison.Ordinal);
            Assert.Contains("\n\nrecord Row(", laid, StringComparison.Ordinal);
        }

        [Fact]
        public void LayoutSettlesAndChangesOnlyWhitespace() {
            foreach (string path in Examples()) {
                string text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
                string laid = SourceLayout.Format(text);
                Assert.True(laid == text, path + " is not laid out; run SiteFrames layout Website <file>.");
                Assert.True(CSharpSyntaxTree.ParseText(laid).GetRoot().IsEquivalentTo(CSharpSyntaxTree.ParseText(text).GetRoot(), topLevel: false), path);
            }
        }

        [Fact]
        public void ExampleLinesOverTheWidthAreStringsOrComments() {
            foreach (string path in Examples()) {
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
                var text = tree.GetText();
                foreach (var line in text.Lines.Where(line => line.Span.Length > SourceLayout.Width)) {
                    // A string literal (raw strings included) or a comment cannot break.
                    bool literal = tree.GetRoot().DescendantTokens(line.Span).Any(token => token.Span.IntersectsWith(line.Span)
                        && (token.IsKind(SyntaxKind.StringLiteralToken) || token.IsKind(SyntaxKind.InterpolatedStringTextToken) || token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken)
                            || token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken) || token.IsKind(SyntaxKind.InterpolatedRawStringEndToken) || token.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken)));
                    bool comment = tree.GetRoot().DescendantTrivia(line.Span).Any(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) && trivia.Span.IntersectsWith(line.Span));
                    Assert.True(literal || comment, path + ": " + line);
                }
            }
        }

        private static string[] Examples() {
            DirectoryInfo? root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "ConsoleMonster.sln"))) { root = root.Parent; }
            string examples = Path.Combine(root!.FullName, "Website", "examples");
            return Directory.GetFiles(examples, "*.cs", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
        }
    }
}
