using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames.Capture;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Example C# reads like hand-written code: long lines break at their structure and only whitespace changes.</summary>
    [Collection("Interactive process defaults")]
    public sealed class SourceLayoutContracts {
        [Fact]
        public void LongStringSummariesWrapWithoutChangingValuesOrInterpolationOrder() {
            const string source = """
                using ConsoleMonster;
                using ConsoleMonster.Widgets;
                int reads = 0;
                int Next() => ++reads;
                bool retry = true;
                string summary = retry ? $"[pass]{Next()} passed[/]   [accent]{Next()} recovered on retry[/]   [muted]retention: 14 days and archive verification complete[/]" : "No jobs were retried and every archive remains available for fourteen days in the nightly backup destination directory.";
                Terminal.Write(new TextBlock(summary + " / reads=" + reads));
                """;
            string formatted = SourceLayout.Format(source);
            Assert.All(formatted.Split('\n'), line => Assert.True(line.Length <= SourceLayout.Width, line));
            Assert.Equal(formatted, SourceLayout.Format(formatted));
            string directory = Path.Combine(Path.GetTempPath(), "cm-layout-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try {
                string before = Path.Combine(directory, "before.cs");
                string after = Path.Combine(directory, "after.cs");
                File.WriteAllText(before, source);
                File.WriteAllText(after, formatted);
                var profile = CSharpRunner.Profile(100, 12, ColorDepth.TrueColor, GlyphTier.Full);
                Assert.Equal(CSharpRunner.Execute(before, profile, Theme.Dark, null).Output,
                    CSharpRunner.Execute(after, profile, Theme.Dark, null).Output);
            } finally { Directory.Delete(directory, recursive: true); }
        }

        [Fact]
        public void NonStringTargetsKeepTheirInterpolatedExpression() {
            const string source = "FormattableString summary = $\"{DateTime.Now} a long template which preserves its arguments and format string for a caller supplied format provider\";";
            string original = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InterpolatedStringExpressionSyntax>().Single().ToString();
            string formatted = SourceLayout.Format(source);
            Assert.Equal(original, CSharpSyntaxTree.ParseText(formatted).GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InterpolatedStringExpressionSyntax>().Single().ToString());
        }

        [Fact]
        public void WrappingArgumentsRetainsTheirInlineComment() {
            const string source = "Write(\"The first argument deliberately fills the available line\", /* keep the explanation */ \"The second argument wraps\");";
            string formatted = SourceLayout.Format(source);
            Assert.Contains("/* keep the explanation */", formatted, StringComparison.Ordinal);
            Assert.True(CSharpSyntaxTree.ParseText(formatted).GetRoot().IsEquivalentTo(CSharpSyntaxTree.ParseText(source).GetRoot(), topLevel: false));
            Assert.Equal(formatted, SourceLayout.Format(formatted));
        }

        [Fact]
        public void SourceFoldMarkersRemainStandaloneAfterNormalizingBraces() {
            const string source = "// <more>\nvoid Work() { } // </more>\n";
            string formatted = SourceLayout.Format(source);
            Assert.Contains("}\n// </more>\n", formatted, StringComparison.Ordinal);
            Assert.Equal(formatted, SourceLayout.Format(formatted));
            SourceSnippets.Split(formatted);
        }

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

        // Recorded in a Windows pseudo console, whose recordings are stamped with these sources; they are laid out with the
        // next Windows capture of the live and demos areas.
        private static readonly string[] WindowsRecorded = { "live/pipeline.cs", "live/replay.cs", "live/stress.cs", "demos/hosting/Program.cs", "demos/logging/Program.cs" };

        private static string[] Examples() {
            DirectoryInfo? root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "ConsoleMonster.sln"))) { root = root.Parent; }
            string examples = Path.Combine(root!.FullName, "Website", "examples");
            return Directory.GetFiles(examples, "*.cs", SearchOption.AllDirectories)
                .Where(path => !Path.GetRelativePath(examples, path).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
                .Where(path => !WindowsRecorded.Contains(Path.GetRelativePath(examples, path).Replace('\\', '/'), StringComparer.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
        }
    }
}
