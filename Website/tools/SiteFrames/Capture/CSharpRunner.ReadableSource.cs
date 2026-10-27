using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        /// <summary>Normalizes argument names through the same bound source pass used by fluent examples.</summary>
        internal static string PositionalSource(string path, string source) {
            var compilation = CreateCompilation(path, source);
            var tree = compilation.SyntaxTrees.Single(tree => tree.FilePath == path);
            if (!tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Any(argument => argument.NameColon != null)) { return source; }
            Check(path, source);
            string readable = ReadableSource(new ExampleNames(compilation.GetSemanticModel(tree)).Visit(tree.GetRoot())!);
            readable = LayoutSource(path, readable);
            Check(path, readable);
            return readable;
        }

        /// <summary>Wraps only expressions whose bound target is string, preserving handlers and FormattableString.</summary>
        internal static string LayoutSource(string path, string source) {
            source = source.Replace("\r\n", "\n", StringComparison.Ordinal);
            var compilation = CreateCompilation(path, source);
            var tree = compilation.SyntaxTrees.Single(tree => tree.FilePath == path);
            return LayoutSource(tree, compilation.GetSemanticModel(tree));
        }

        private static string LayoutSource(SyntaxTree tree, SemanticModel model) {
            var strings = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>()
                .Where(node => node is InterpolatedStringExpressionSyntax
                    || node is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                .Where(node => model.GetTypeInfo(node).ConvertedType?.SpecialType == SpecialType.System_String)
                .ToArray();
            var spans = strings.Where(node => !node.Ancestors().Any(parent => strings.Contains(parent)))
                .Select(node => node.Span);
            return SourceLayout.Format(tree.GetText().ToString(), spans);
        }

        /// <summary>Shares ordinary builder callback names between runnable programs and their translation notes.</summary>
        internal static string BuilderParameter(string noun) => noun switch {
            "definition" => "o", "Table" => "t", "Panel" => "p", "KeyValue" => "kv", "Chart" => "chart", "Rows" => "r",
            _ => char.ToLowerInvariant(noun[0]) + noun.Substring(1)
        };

        private static string ReadableSource(SyntaxNode root) =>
            SourceLayout.Format(root.NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString() + "\n");

        /// <summary>Uses names resolved in the program's imports, retaining qualification where a short name is ambiguous.</summary>
        private sealed class ExampleNames(SemanticModel model) : CSharpSyntaxRewriter {
            public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
                if (node.Ancestors().OfType<UsingDirectiveSyntax>().Any()) { return node; }
                if (model.GetSymbolInfo(node).Symbol is INamedTypeSymbol type) {
                    return SyntaxFactory.ParseTypeName(type.ToMinimalDisplayString(model, node.SpanStart)).WithTriviaFrom(node);
                }
                return base.VisitMemberAccessExpression(node);
            }

            public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node) {
                if (node.Ancestors().OfType<UsingDirectiveSyntax>().Any()) { return node; }
                if (model.GetSymbolInfo(node).Symbol is INamedTypeSymbol type) {
                    return SyntaxFactory.ParseTypeName(type.ToMinimalDisplayString(model, node.SpanStart)).WithTriviaFrom(node);
                }
                return base.VisitQualifiedName(node);
            }

            public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) {
                var visited = (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node)!;
                return node.ArgumentList == null ? visited : visited.WithArgumentList(visited.ArgumentList!.WithArguments(
                    WithoutArgumentNames(model, node, node.ArgumentList.Arguments, visited.ArgumentList.Arguments)));
            }

            public override SyntaxNode? VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) {
                var visited = (ImplicitObjectCreationExpressionSyntax)base.VisitImplicitObjectCreationExpression(node)!;
                return visited.WithArgumentList(visited.ArgumentList.WithArguments(
                    WithoutArgumentNames(model, node, node.ArgumentList.Arguments, visited.ArgumentList.Arguments)));
            }

            public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node) {
                var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
                if (node.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "Build" } } member
                    && model.GetSymbolInfo(node).Symbol is IMethodSymbol { ContainingType.Name: "UiConstruction" } method) {
                    // A kind-selected definition returns IWidget. A typed live
                    // handle needs a visible cast, not a generic teaching helper.
                    var call = visited.WithExpression(((MemberAccessExpressionSyntax)visited.Expression).WithName(SyntaxFactory.IdentifierName("Build")));
                    var cast = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(method.ReturnType.ToMinimalDisplayString(model, node.SpanStart)), call);
                    return node.Parent is MemberAccessExpressionSyntax ? SyntaxFactory.ParenthesizedExpression(cast).WithTriviaFrom(node) : cast.WithTriviaFrom(node);
                }
                return visited.WithArgumentList(visited.ArgumentList.WithArguments(
                    WithoutArgumentNames(model, node, node.ArgumentList.Arguments, visited.ArgumentList.Arguments)));
            }
        }
    }
}
