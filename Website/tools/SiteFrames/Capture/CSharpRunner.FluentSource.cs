using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        /// <summary>Generates a fluent source variant from resolved public setters, preserving mixed/behavioral initializer order.</summary>
        public static string FluentSource(string path, params string[] companions) {
            string code = SourceSnippets.Split(Site.ReadText(path)).Full;
            CSharpCompilation compilation = CreateCompilation(path, code, companions: companions);
            SyntaxTree tree = compilation.SyntaxTrees.First();
            SemanticModel model = compilation.GetSemanticModel(tree);
            var rewriter = new FluentSourceRewriter(model, compilation);
            var root = (CompilationUnitSyntax)rewriter.Visit(tree.GetRoot())!;
            foreach (string ns in rewriter.Namespaces.OrderBy(value => value, StringComparer.Ordinal)) {
                if (!root.Usings.Any(directive => directive.Alias == null && directive.Name?.ToString() == ns)) {
                    root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)));
                }
            }
            string generated = root.NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString() + "\n";
            var checkedProgram = CreateCompilation(path, generated, companions: companions);
            SyntaxTree checkedTree = checkedProgram.SyntaxTrees.First();
            var unused = checkedProgram.GetDiagnostics().Where(diagnostic => diagnostic.Id == "CS8019" && diagnostic.Location.SourceTree == checkedTree)
                .Select(diagnostic => checkedTree.GetRoot().FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<UsingDirectiveSyntax>())
                .Where(directive => directive != null).Cast<UsingDirectiveSyntax>().ToArray();
            if (unused.Length > 0) {
                generated = checkedTree.GetRoot().RemoveNodes(unused, SyntaxRemoveOptions.KeepExteriorTrivia)!
                    .NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString() + "\n";
            }
            return generated;
        }

        private sealed class FluentSourceRewriter(SemanticModel model, CSharpCompilation compilation) : CSharpSyntaxRewriter {
            public HashSet<string> Namespaces { get; } = new HashSet<string>(StringComparer.Ordinal);
            public override SyntaxNode? VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) {
                if (model.GetTypeInfo(node).Type is not INamedTypeSymbol type || !Owned(type)) { return base.VisitImplicitObjectCreationExpression(node); }
                var visited = (ImplicitObjectCreationExpressionSyntax)base.VisitImplicitObjectCreationExpression(node)!;
                var created = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                    visited.ArgumentList, visited.Initializer).WithTriviaFrom(node);
                return Rewrite(node, created);
            }
            public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) {
                return Rewrite(node, (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node)!);
            }

            private ExpressionSyntax Rewrite(BaseObjectCreationExpressionSyntax original, ObjectCreationExpressionSyntax visited) {
                if (original.Initializer == null || model.GetTypeInfo(original).Type is not INamedTypeSymbol type
                    || !Owned(type)) { return visited; }
                // Folding markers are editorial boundaries. Retain their initializer rather than dropping
                // a closing marker attached to a brace while translating its setters.
                if (original.Initializer.DescendantTrivia().Any(trivia => trivia.ToString().Contains("<more>", StringComparison.Ordinal)
                    || trivia.ToString().Contains("</more>", StringComparison.Ordinal) || trivia.ToString().Contains("<hidden>", StringComparison.Ordinal)
                    || trivia.ToString().Contains("</hidden>", StringComparison.Ordinal))) { return visited; }
                var setters = new List<(string Name, ExpressionSyntax Value)>();
                for (int i = 0; i < original.Initializer.Expressions.Count; i++) {
                    if (original.Initializer.Expressions[i] is not AssignmentExpressionSyntax assignment
                        || model.GetSymbolInfo(assignment.Left).Symbol is not IPropertySymbol property
                        || property.SetMethod?.DeclaredAccessibility != Accessibility.Public || property.Type.TypeKind == TypeKind.Delegate
                        || property.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == "ConsoleMonster.NoFluentAttribute")
                        || visited.Initializer!.Expressions[i] is not AssignmentExpressionSyntax converted
                        || converted.Right is InitializerExpressionSyntax || !HasSetter(type, property.Name)) {
                        // Moving only some members would reorder callback/kind setters or collection initialization.
                        return visited;
                    }
                    Namespaces.Add(property.ContainingNamespace.ToDisplayString());
                    setters.Add((property.Name, converted.Right));
                }
                ExpressionSyntax result = visited.WithInitializer(null).WithArgumentList(visited.ArgumentList ?? SyntaxFactory.ArgumentList());
                foreach (var setter in setters) {
                    result = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        result, SyntaxFactory.IdentifierName(setter.Name)), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(setter.Value))));
                }
                return result.WithTriviaFrom(original);
            }

            public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node) {
                var visited = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node)!;
                // Fluent setters do not carry object-initializer nullable flow into later member reads.
                // Keep only the non-null fact the compiler proved for the maintained source.
                if ((node.Parent is ArgumentSyntax || node.Parent is MemberAccessExpressionSyntax parent && parent.Expression == node)
                    && model.GetSymbolInfo(node).Symbol is IPropertySymbol property && Owned(property.ContainingType)
                    && property.NullableAnnotation == NullableAnnotation.Annotated
                    && model.GetTypeInfo(node).Nullability.FlowState == NullableFlowState.NotNull) {
                    return SyntaxFactory.PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, visited).WithTriviaFrom(node);
                }
                return visited;
            }

            private bool HasSetter(INamedTypeSymbol type, string member) {
                for (INamedTypeSymbol? current = type; current != null; current = current.BaseType) {
                    INamedTypeSymbol? fluent = compilation.GetTypeByMetadataName(current.ContainingNamespace.ToDisplayString() + "." + current.Name + "Fluent");
                    if (fluent?.GetMembers(member).OfType<IMethodSymbol>().Any(method => method.Parameters.Length >= 2) == true) { return true; }
                }
                return false;
            }

            public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node) {
                IMethodSymbol? method = model.GetSymbolInfo(node).Symbol as IMethodSymbol;
                var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
                if (method?.ContainingType.ToDisplayString() == "ConsoleMonster.Terminal" && method.Name == "Write" && visited.ArgumentList.Arguments.Count == 1) {
                    Namespaces.Add("ConsoleMonster");
                    return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        visited.ArgumentList.Arguments[0].Expression, SyntaxFactory.IdentifierName("Show"))).WithTriviaFrom(node);
                }
                return visited;
            }

            private static bool Owned(INamedTypeSymbol type) => type.ContainingAssembly.Name is "ConsoleMonster" or "ConsoleMonster.Markdown" or "ConsoleMonster.Graphics" or "ConsoleMonster.PowerShell";
        }
    }
}
