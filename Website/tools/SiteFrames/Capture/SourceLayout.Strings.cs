using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SiteFrames.Capture {
    internal static partial class SourceLayout {
        // Without a semantic model, only an explicit string declaration supplies
        // a known target. Bound property and argument targets can opt in too;
        // FormattableString and handler arguments retain their interpolation.
        private static bool StringValue(ExpressionSyntax expression) {
            SyntaxNode value = expression;
            while (value.Parent is ParenthesizedExpressionSyntax
                || value.Parent is ConditionalExpressionSyntax conditional && conditional.Condition != value) {
                value = value.Parent;
            }
            return value.Parent is EqualsValueClauseSyntax {
                Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type: PredefinedTypeSyntax type } }
            } && type.Keyword.IsKind(SyntaxKind.StringKeyword);
        }

        private static string WrapStringValues(string text, HashSet<TextSpan>? knownStrings = null) {
            SyntaxNode root = Parse(text);
            var source = root.SyntaxTree.GetText();
            var edits = new List<(int, int, string)>();
            foreach (ExpressionSyntax value in root.DescendantNodes().OfType<ExpressionSyntax>()
                .Where(value => knownStrings == null ? StringValue(value) : knownStrings.Contains(value.Span))) {
                var line = source.Lines.GetLineFromPosition(value.SpanStart);
                if (line.Span.Length <= Width || source.Lines.GetLineFromPosition(value.Span.End).LineNumber != line.LineNumber) { continue; }
                string start;
                string end;
                IEnumerable<string> units;
                if (value is InterpolatedStringExpressionSyntax interpolated
                    && interpolated.StringStartToken.IsKind(SyntaxKind.InterpolatedStringStartToken)) {
                    start = interpolated.StringStartToken.Text;
                    end = interpolated.StringEndToken.Text;
                    units = interpolated.Contents.SelectMany(content => content is InterpolatedStringTextSyntax
                        ? Words(content.ToString()) : new[] { content.ToString() });
                } else if (value is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression)
                    && literal.Token.IsKind(SyntaxKind.StringLiteralToken)) {
                    start = literal.Token.Text.StartsWith('@') ? "@\"" : "\"";
                    end = "\"";
                    units = Words(literal.Token.Text.Substring(start.Length, literal.Token.Text.Length - start.Length - 1));
                } else { continue; }
                int room = Math.Max(24, Width - Indent(source, value.SpanStart) - Step - start.Length - end.Length - 5);
                var parts = new List<string>();
                var current = new StringBuilder();
                foreach (string unit in units) {
                    if (current.Length > 0 && current.Length + unit.Length > room) {
                        parts.Add(start + current + end);
                        current.Clear();
                    }
                    current.Append(unit);
                }
                if (parts.Count == 0) { continue; }
                if (current.Length > 0) { parts.Add(start + current + end); }
                edits.Add((value.SpanStart, value.Span.End, "(" + string.Join(" + ", parts) + ")"));
            }
            return Apply(text, edits);
        }

        // Break only at actual spaces: escaped characters and interpolation holes
        // stay intact, and every space belongs to exactly one output string.
        private static IEnumerable<string> Words(string text) =>
            Regex.Matches(text, "[^ ]+ *| +").Select(match => match.Value);
    }
}
