using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SiteFrames.Capture {
    /// <summary>
    /// Lays out example C# the way it is written by hand: a line longer than <see cref="Width"/> breaks at its outermost
    /// chain, argument list or conditional (then operators, then initializers), one level at a time; literal arrays stay on
    /// as few lines as fit; a statement led by a comment gets a blank line before it. Roslyn's NormalizeWhitespace puts a
    /// whole chain on one line and every array element on its own, which is what this undoes. Overlong string values in
    /// confirmed string targets can split into concatenated parts; other changes affect whitespace only.
    /// </summary>
    internal static partial class SourceLayout {
        public const int Width = 100;
        private const int Step = 4;
        private static readonly CSharpParseOptions Options = new CSharpParseOptions(LanguageVersion.CSharp12);
        private static readonly string[] Markers = { "<more>", "</more>", "<hidden>", "</hidden>" };

        public static string Format(string source, IEnumerable<TextSpan>? stringValues = null) {
            string text = source.Replace("\r\n", "\n", StringComparison.Ordinal);
            if (stringValues != null) { text = WrapStringValues(text, stringValues.ToHashSet()); }
            text = Spacing(text);
            text = Compact(text);
            text = Wrap(text);
            text = Wrap(WrapStringValues(text));
            return Separate(MarkerLines(text)).TrimEnd() + "\n";
        }

        // NormalizeWhitespace can attach a closing source-fold marker to the
        // preceding brace. The snippet reader requires each marker on its own line.
        private static string MarkerLines(string text) {
            SyntaxNode root = Parse(text);
            SourceText source = root.SyntaxTree.GetText();
            var edits = new List<(int, int, string)>();
            foreach (SyntaxTrivia trivia in root.DescendantTrivia().Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                && Markers.Any(marker => trivia.ToString().Trim() == "// " + marker))) {
                TextLine line = source.Lines.GetLineFromPosition(trivia.SpanStart);
                int start = trivia.SpanStart;
                while (start > line.Start && (source[start - 1] == ' ' || source[start - 1] == '\t')) { start--; }
                if (start > line.Start) { edits.Add((start, trivia.SpanStart, "\n" + new string(' ', Indent(source, line.Start)))); }
            }
            return Apply(text, edits);
        }

        // NormalizeWhitespace writes catch (...)when (...).
        private static string Spacing(string text) {
            SyntaxNode root = Parse(text);
            var edits = root.DescendantNodes().OfType<CatchFilterClauseSyntax>()
                .Where(filter => filter.WhenKeyword.LeadingTrivia.Count == 0 && filter.WhenKeyword.GetPreviousToken().TrailingTrivia.Count == 0)
                .Select(filter => (filter.WhenKeyword.SpanStart, filter.WhenKeyword.SpanStart, " "));
            return Apply(text, edits);
        }

        private static SyntaxNode Parse(string text) => CSharpSyntaxTree.ParseText(text, Options).GetRoot();

        private static int Indent(SourceText source, int position) {
            TextLine line = source.Lines.GetLineFromPosition(position);
            int i = line.Start;
            while (i < line.End && source[i] == ' ') { i++; }
            return i - line.Start;
        }

        private static string Apply(string text, IEnumerable<(int Start, int End, string Text)> edits) {
            var builder = new StringBuilder(text);
            foreach (var edit in edits.OrderByDescending(edit => edit.Start)) {
                builder.Remove(edit.Start, edit.End - edit.Start).Insert(edit.Start, edit.Text);
            }
            return builder.ToString();
        }

        // Arrays and collections of literals: one line when it fits, otherwise filled lines under the opening brace.
        private static string Compact(string text) {
            SyntaxNode root = Parse(text);
            SourceText source = root.SyntaxTree.GetText();
            var edits = new List<(int, int, string)>();
            foreach (InitializerExpressionSyntax initializer in root.DescendantNodes().OfType<InitializerExpressionSyntax>()) {
                if (!initializer.IsKind(SyntaxKind.ArrayInitializerExpression) && !initializer.IsKind(SyntaxKind.CollectionInitializerExpression)) { continue; }
                if (initializer.Expressions.Count < 2 || !initializer.Expressions.All(Literal)) { continue; }
                if (initializer.DescendantTrivia(descendIntoTrivia: true).Any(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))) { continue; }
                // Only one element per line (NormalizeWhitespace's layout) rejoins; filled or single lines stay as they are.
                if (initializer.Expressions.Select(item => source.Lines.GetLineFromPosition(item.SpanStart).LineNumber).Distinct().Count() < initializer.Expressions.Count) { continue; }
                SyntaxToken before = initializer.OpenBraceToken.GetPreviousToken();
                TextLine line = source.Lines.GetLineFromPosition(before.SpanStart);
                int rest = source.Lines.GetLineFromPosition(initializer.Span.End).End - initializer.Span.End;
                string[] items = initializer.Expressions.Select(item => item.ToString()).ToArray();
                string single = " { " + string.Join(", ", items) + " }";
                if (before.Span.End - line.Start + single.Length + rest <= Width) {
                    edits.Add((before.Span.End, initializer.Span.End, single));
                    continue;
                }
                string indent = new string(' ', Indent(source, before.SpanStart));
                string inner = indent + new string(' ', Step);
                var filled = new StringBuilder("\n" + indent + "{\n" + inner);
                int column = inner.Length;
                for (int i = 0; i < items.Length; i++) {
                    string item = items[i] + (i < items.Length - 1 ? "," : string.Empty);
                    if (column > inner.Length && column + 1 + item.Length > Width) { filled.Append('\n').Append(inner); column = inner.Length; }
                    else if (column > inner.Length) { filled.Append(' '); column++; }
                    filled.Append(item);
                    column += item.Length;
                }
                filled.Append('\n').Append(indent).Append('}');
                edits.Add((before.Span.End, initializer.Span.End, filled.ToString()));
            }
            return Apply(text, edits);
        }

        private static bool Literal(ExpressionSyntax expression) => expression is LiteralExpressionSyntax
            || expression is PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax } unary && (unary.IsKind(SyntaxKind.UnaryMinusExpression) || unary.IsKind(SyntaxKind.UnaryPlusExpression));

        private static string Wrap(string text) {
            int from = 0;
            for (int guard = 0; guard < 5000; guard++) {
                SyntaxNode root = Parse(text);
                SourceText source = root.SyntaxTree.GetText();
                TextLine? line = null;
                for (int i = from; i < source.Lines.Count; i++) {
                    if (source.Lines[i].Span.Length > Width) { line = source.Lines[i]; from = i; break; }
                }
                if (line == null) { return text; }
                string? broken = Break(text, root, source, line.Value);
                if (broken == null) { from++; } else { text = broken; }
            }
            throw new InvalidOperationException("Source layout did not settle.");
        }

        private enum Kind { Single, Chain, Arguments, Parameters, Literals, Collection, Embedded, Conditional, Binary, Initializer }

        private static string? Break(string text, SyntaxNode root, SourceText source, TextLine line) {
            int lineIndent = line.Start + Indent(source, line.Start);
            var candidates = new List<(int Tier, SyntaxNode Node, Kind Kind, List<SyntaxToken> Points, bool Whole)>();
            foreach (SyntaxNode node in root.DescendantNodes(node => node.FullSpan.IntersectsWith(line.Span)).Where(node => node.Span.IntersectsWith(line.Span))) {
                (Kind kind, List<SyntaxToken> points)? found = Points(node);
                if (found == null || node.Ancestors().Any(ancestor => ancestor is InterpolatedStringExpressionSyntax)) { continue; }
                // Literals that span lines (verbatim and raw strings) cannot move.
                if (node.DescendantTokens().Any(token => token.Span.Length > 0 && source.Lines.GetLineFromPosition(token.SpanStart).LineNumber != source.Lines.GetLineFromPosition(token.Span.End).LineNumber)) { continue; }
                List<SyntaxToken> open = found.Value.points.Where(token => !Broken(source, token)).ToList();
                if (!open.Any(token => token.SpanStart > lineIndent && token.SpanStart < line.End)) { continue; }
                // An initializer that is still on one line opens as a whole, from its brace.
                if (found.Value.kind == Kind.Initializer && !Broken(source, found.Value.points[0])) { open = found.Value.points.Take(1).ToList(); }
                int tier = found.Value.kind switch {
                    Kind.Binary or Kind.Literals => 1,
                    // Last resort: a single argument too wide for its line starts on the next one.
                    Kind.Single => 3,
                    Kind.Initializer => ((InitializerExpressionSyntax)node).Expressions.Count >= 2 ? 0 : 2,
                    _ => 0
                };
                candidates.Add((tier, node, found.Value.kind, open, open.Count == found.Value.points.Count));
            }
            foreach (var candidate in candidates.OrderBy(candidate => candidate.Tier)) {
                string? broken = Apply(text, source, candidate.Node, candidate.Kind, candidate.Points, candidate.Whole);
                if (broken != null) { return broken; }
            }
            return null;
        }

        private static string? Apply(string text, SourceText source, SyntaxNode node, Kind kind, List<SyntaxToken> points, bool whole) {
            var chosen = (Node: node, Kind: kind, Points: points, Whole: whole);
            int indent = Indent(source, chosen.Node.SpanStart);
            string inner = new string(' ', indent + Step);
            string outer = new string(' ', indent);
            var edits = new List<(int, int, string)>();
            void Before(SyntaxToken token, string indentation) {
                int start = token.GetPreviousToken().Span.End;
                string gap = text.Substring(start, token.SpanStart - start);
                string comment = string.IsNullOrWhiteSpace(gap) ? string.Empty : gap.TrimEnd();
                edits.Add((start, token.SpanStart, comment + "\n" + indentation));
            }
            if (chosen.Kind == Kind.Initializer && Broken(source, ((InitializerExpressionSyntax)chosen.Node).OpenBraceToken)) {
                // Already open: every element that shares a line with the one before it takes its own line.
                foreach (SyntaxToken token in chosen.Points) { Before(token, new string(' ', Indent(source, token.GetPreviousToken().SpanStart))); }
                return edits.Count == 0 ? null : Apply(text, edits);
            } else if (chosen.Kind == Kind.Initializer) {
                var initializer = (InitializerExpressionSyntax)chosen.Node;
                Before(initializer.OpenBraceToken, outer);
                Before(initializer.Expressions.First().GetFirstToken(), inner);
                foreach (SyntaxToken comma in initializer.Expressions.GetSeparators().Where(comma => !comma.GetNextToken().IsKind(SyntaxKind.CloseBraceToken))) {
                    Before(comma.GetNextToken(), inner);
                }
                Before(initializer.CloseBraceToken, outer);
            } else if (chosen.Kind == Kind.Embedded) {
                Before(chosen.Points[0], inner);
            } else if (chosen.Kind == Kind.Collection) {
                foreach (SyntaxToken token in chosen.Points) { Before(token, token.IsKind(SyntaxKind.CloseBracketToken) ? outer : inner); }
            } else if (chosen.Whole && Fill(chosen.Node) is { } segments) {
                // Values fill their lines; a first value that does not fit after the opening starts the next line.
                int column = segments[0].Start - source.Lines.GetLineFromPosition(segments[0].Start).Start;
                if (segments[0].Optional && column + segments[0].Length > Width && column > inner.Length) { Before(segments[0].Token, inner); column = inner.Length; }
                column += segments[0].Length;
                // The last part carries whatever follows it on the line (closing parentheses, a semicolon).
                var last = segments[^1];
                segments[^1] = (last.Token, last.Start, source.Lines.GetLineFromPosition(last.Start + last.Length).End - last.Start, last.Optional);
                foreach (var segment in segments.Skip(1)) {
                    if (column + 1 + segment.Length > Width) { Before(segment.Token, inner); column = inner.Length + segment.Length; }
                    else { column += 1 + segment.Length; }
                }
                if (edits.Count == 0) { return null; }
            } else {
                foreach (SyntaxToken token in chosen.Points) { Before(token, inner); }
            }
            // Lines already inside the node (a lambda's block, an earlier break) move with the part they belong to.
            int first = edits.Min(edit => edit.Item1);
            for (int i = first; i < chosen.Node.Span.End; i++) {
                if (text[i] == '\n' && i + 1 < text.Length && text[i + 1] != '\n') { edits.Add((i + 1, i + 1, new string(' ', Step))); }
            }
            return Apply(text, edits);
        }

        // The fillable parts of an argument list of values or of a + / && / || chain: the token a break goes before and the
        // width up to the next part. Lists holding lambdas, constructions or collections keep one argument per line.
        private static List<(SyntaxToken Token, int Start, int Length, bool Optional)>? Fill(SyntaxNode node) {
            var segments = new List<(SyntaxToken, int, int, bool)>();
            if (node is BaseArgumentListSyntax list) {
                if (list.Arguments.Any(argument => argument.Expression.DescendantNodesAndSelf().Any(Structural))) { return null; }
                for (int i = 0; i < list.Arguments.Count; i++) {
                    int end = i + 1 < list.Arguments.Count ? list.Arguments[i + 1].SpanStart - 1 : list.GetLastToken().Span.End;
                    segments.Add((list.Arguments[i].GetFirstToken(), list.Arguments[i].SpanStart, end - list.Arguments[i].SpanStart, i == 0));
                }
                return segments;
            }
            if (node is InitializerExpressionSyntax literals && literals.Expressions.All(Literal)) {
                for (int i = 0; i < literals.Expressions.Count; i++) {
                    int end = i + 1 < literals.Expressions.Count ? literals.Expressions[i + 1].SpanStart - 1 : literals.CloseBraceToken.Span.End;
                    segments.Add((literals.Expressions[i].GetFirstToken(), literals.Expressions[i].SpanStart, end - literals.Expressions[i].SpanStart, false));
                }
                return segments;
            }
            if (node is ParameterListSyntax parameters) {
                for (int i = 0; i < parameters.Parameters.Count; i++) {
                    int end = i + 1 < parameters.Parameters.Count ? parameters.Parameters[i + 1].SpanStart - 1 : parameters.GetLastToken().Span.End;
                    segments.Add((parameters.Parameters[i].GetFirstToken(), parameters.Parameters[i].SpanStart, end - parameters.Parameters[i].SpanStart, i == 0));
                }
                return segments;
            }
            if (node is BinaryExpressionSyntax binary) {
                var operators = new List<SyntaxToken>();
                ExpressionSyntax current = binary;
                while (current is BinaryExpressionSyntax same && same.Kind() == binary.Kind() && !binary.IsKind(SyntaxKind.CoalesceExpression)) { operators.Add(same.OperatorToken); current = same.Left; }
                if (operators.Count == 0) { return null; }
                operators.Reverse();
                segments.Add((current.GetFirstToken(), current.SpanStart, current.Span.Length, false));
                for (int i = 0; i < operators.Count; i++) {
                    int end = i + 1 < operators.Count ? operators[i + 1].SpanStart - 1 : binary.Span.End;
                    segments.Add((operators[i], operators[i].SpanStart, end - operators[i].SpanStart, false));
                }
                return segments;
            }
            return null;
        }

        private static bool Structural(SyntaxNode node) => node is AnonymousFunctionExpressionSyntax or BaseObjectCreationExpressionSyntax
            or CollectionExpressionSyntax or InitializerExpressionSyntax;

        private static bool Broken(SourceText source, SyntaxToken token) {
            SyntaxToken previous = token.GetPreviousToken();
            return source.Lines.GetLineFromPosition(previous.Span.End).LineNumber != source.Lines.GetLineFromPosition(token.SpanStart).LineNumber;
        }

        private static (Kind, List<SyntaxToken>)? Points(SyntaxNode node) {
            switch (node) {
                case InvocationExpressionSyntax invocation when invocation.Parent is not MemberAccessExpressionSyntax: {
                    // a.B().C().D(): break before every call made on the result of another call or a construction.
                    var points = new List<SyntaxToken>();
                    ExpressionSyntax current = invocation;
                    while (current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member }) {
                        ExpressionSyntax target = member.Expression is PostfixUnaryExpressionSyntax postfix ? postfix.Operand : member.Expression;
                        if (target is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax or ElementAccessExpressionSyntax) { points.Add(member.OperatorToken); }
                        current = target;
                    }
                    points.Reverse();
                    return points.Count > 0 ? (Kind.Chain, points) : null;
                }
                case BaseArgumentListSyntax list when list.Arguments.Count >= 2:
                    return (Kind.Arguments, list.Arguments.Select(argument => argument.GetFirstToken()).ToList());
                case BaseArgumentListSyntax list when list.Arguments.Count == 1:
                    return (Kind.Single, new List<SyntaxToken> { list.Arguments[0].GetFirstToken() });
                case InitializerExpressionSyntax literals when literals.Expressions.Count >= 2 && literals.Expressions.All(Literal):
                    return (Kind.Literals, literals.Expressions.Select(expression => expression.GetFirstToken()).ToList());
                case ParameterListSyntax parameters when parameters.Parameters.Count >= 2:
                    return (Kind.Parameters, parameters.Parameters.Select(parameter => parameter.GetFirstToken()).ToList());
                case CatchFilterClauseSyntax filter:
                    return (Kind.Embedded, new List<SyntaxToken> { filter.WhenKeyword });
                case IfStatementSyntax { Statement: not BlockSyntax } conditional:
                    return (Kind.Embedded, new List<SyntaxToken> { conditional.Statement.GetFirstToken() });
                case CollectionExpressionSyntax collection when collection.Elements.Count >= 2:
                    return (Kind.Collection, collection.Elements.Select(element => element.GetFirstToken()).Append(collection.CloseBracketToken).ToList());
                case ConditionalExpressionSyntax conditional when conditional.Span.Length >= Width / 2 && conditional.Parent is not ParenthesizedExpressionSyntax:
                    return (Kind.Conditional, new List<SyntaxToken> { conditional.QuestionToken, conditional.ColonToken });
                case BinaryExpressionSyntax binary when (binary.IsKind(SyntaxKind.AddExpression) || binary.IsKind(SyntaxKind.LogicalAndExpression)
                        || binary.IsKind(SyntaxKind.LogicalOrExpression) || binary.IsKind(SyntaxKind.CoalesceExpression))
                        && binary.Span.Length >= Width / 2
                        && !(binary.Parent is BinaryExpressionSyntax parent && parent.Kind() == binary.Kind()): {
                    var points = new List<SyntaxToken>();
                    ExpressionSyntax current = binary;
                    while (current is BinaryExpressionSyntax same && same.Kind() == binary.Kind()) {
                        points.Add(same.OperatorToken);
                        current = binary.IsKind(SyntaxKind.CoalesceExpression) ? same.Right : same.Left;
                    }
                    points.Reverse();
                    return (Kind.Binary, points);
                }
                case InitializerExpressionSyntax initializer when initializer.Expressions.Count > 0 && !initializer.Parent.IsKind(SyntaxKind.ComplexElementInitializerExpression)
                        && !Literal(initializer.Expressions[0]):
                    return (Kind.Initializer, new List<SyntaxToken> { initializer.OpenBraceToken }.Concat(initializer.Expressions.Skip(1).Select(expression => expression.GetFirstToken())).ToList());
            }
            return null;
        }

        // A blank line before a statement, member or type that opens with a comment, and between type declarations.
        private static string Separate(string text) {
            SyntaxNode root = Parse(text);
            SourceText source = root.SyntaxTree.GetText();
            var starts = new SortedSet<int>();
            foreach (SyntaxNode node in root.DescendantNodes(node => node is not ExpressionSyntax)) {
                if (node is not (StatementSyntax or MemberDeclarationSyntax) || node is GlobalStatementSyntax) { continue; }
                SyntaxToken first = node.GetFirstToken();
                if (first.GetPreviousToken().IsKind(SyntaxKind.None)) { continue; }
                SyntaxTrivia comment = first.LeadingTrivia.FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia));
                bool type = node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax && node.Parent is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax;
                bool commented = comment.IsKind(SyntaxKind.SingleLineCommentTrivia) || comment.IsKind(SyntaxKind.MultiLineCommentTrivia);
                if (!commented && !type) { continue; }
                if (commented && Markers.Any(marker => comment.ToString().Contains(marker, StringComparison.Ordinal))) { continue; }
                int lineNumber = source.Lines.GetLineFromPosition(commented ? comment.SpanStart : first.SpanStart).LineNumber;
                if (lineNumber == 0) { continue; }
                string previous = source.Lines[lineNumber - 1].ToString().Trim();
                if (previous.Length == 0 || previous.EndsWith('{') || previous.StartsWith("//", StringComparison.Ordinal) || Markers.Any(marker => previous.Contains(marker, StringComparison.Ordinal))) { continue; }
                starts.Add(source.Lines[lineNumber].Start);
            }
            return Apply(text, starts.Select(start => (start, start, "\n")));
        }
    }
}
