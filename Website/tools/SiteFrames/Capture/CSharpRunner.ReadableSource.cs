using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
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
                return visited;
            }
        }
    }
}
