using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        /// <summary>Generates a fluent source variant from resolved public setters, preserving mixed/behavioral initializer order.</summary>
        public static string FluentSource(string path, params string[] companions) => FluentSources(path, companions)[path];

        /// <summary>Translates every file together, so a fluent tab never silently depends on initializer-only companions.</summary>
        public static IReadOnlyDictionary<string, string> FluentSources(string path, params string[] companions) => FluentSourcesCore(path, companions, null);

        private static IReadOnlyDictionary<string, string> FluentSourcesCore(string path, string[] companions, IReadOnlyList<MetadataReference>? references) {
            var inputs = companions.Prepend(path).ToDictionary(file => file, file => SourceSnippets.Split(Site.ReadText(file)).Full, StringComparer.Ordinal);
            CSharpCompilation compilation = CreateCompilation(path, inputs[path], companions: companions, sourceOverrides: inputs, references: references);
            var generated = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (SyntaxTree tree in compilation.SyntaxTrees.Where(tree => inputs.ContainsKey(tree.FilePath))) {
                SemanticModel model = compilation.GetSemanticModel(tree);
                var rewriter = new FluentSourceRewriter(model, compilation);
                var root = (CompilationUnitSyntax)rewriter.Visit(tree.GetRoot())!;
                foreach (string ns in rewriter.Namespaces.OrderBy(value => value, StringComparer.Ordinal)) {
                    if (!root.Usings.Any(directive => directive.Alias == null && directive.Name?.ToString() == ns)) {
                        root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)));
                    }
                }
                generated[tree.FilePath] = root.NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString() + "\n";
            }
            CSharpCompilation checkedProgram = CreateCompilation(path, generated[path], companions: companions, sourceOverrides: generated, references: references);
            foreach (SyntaxTree tree in checkedProgram.SyntaxTrees.Where(tree => generated.ContainsKey(tree.FilePath))) {
                var factoryRewriter = new FluentFactoryRewriter(checkedProgram.GetSemanticModel(tree));
                var rewritten = factoryRewriter.Visit(tree.GetRoot())!;
                var root = (CompilationUnitSyntax)rewritten;
                if (!root.Usings.Any(u => u.Name?.ToString() == "ConsoleMonster")) { root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("ConsoleMonster"))); }
                foreach (string ns in factoryRewriter.Namespaces.Where(ns => !string.IsNullOrEmpty(ns)).OrderBy(ns => ns, StringComparer.Ordinal)) {
                    if (!root.Usings.Any(usingDirective => usingDirective.Alias == null && usingDirective.Name?.ToString() == ns)) { root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns))); }
                }
                generated[tree.FilePath] = root.NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString() + "\n";
            }
            checkedProgram = CreateCompilation(path, generated[path], companions: companions, sourceOverrides: generated, references: references);
            foreach (SyntaxTree tree in checkedProgram.SyntaxTrees.Where(tree => generated.ContainsKey(tree.FilePath))) {
                var ergonomics = new FluentErgonomicsRewriter(checkedProgram.GetSemanticModel(tree));
                generated[tree.FilePath] = ergonomics.Visit(tree.GetRoot())!.NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString() + "\n";
            }
            checkedProgram = CreateCompilation(path, generated[path], companions: companions, sourceOverrides: generated, references: references);
            var diagnostics = checkedProgram.GetDiagnostics();
            foreach (SyntaxTree tree in checkedProgram.SyntaxTrees.Where(tree => generated.ContainsKey(tree.FilePath))) {
                var unused = diagnostics.Where(diagnostic => diagnostic.Id == "CS8019" && diagnostic.Location.SourceTree == tree)
                    .Select(diagnostic => tree.GetRoot().FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<UsingDirectiveSyntax>())
                    .Where(directive => directive != null).Cast<UsingDirectiveSyntax>().ToArray();
                if (unused.Length > 0) {
                    generated[tree.FilePath] = tree.GetRoot().RemoveNodes(unused, SyntaxRemoveOptions.KeepExteriorTrivia)!
                        .NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString() + "\n";
                }
            }
            checkedProgram = CreateCompilation(path, generated[path], companions: companions, sourceOverrides: generated, references: references);
            foreach (SyntaxTree tree in checkedProgram.SyntaxTrees.Where(tree => generated.ContainsKey(tree.FilePath))) {
                var names = new ExampleNames(checkedProgram.GetSemanticModel(tree));
                generated[tree.FilePath] = ReadableSource(names.Visit(tree.GetRoot())!);
            }
            checkedProgram = CreateCompilation(path, generated[path], companions: companions, sourceOverrides: generated, references: references);
            foreach (SyntaxTree tree in checkedProgram.SyntaxTrees.Where(tree => generated.ContainsKey(tree.FilePath))) {
                generated[tree.FilePath] = LayoutSource(tree, checkedProgram.GetSemanticModel(tree));
            }
            return generated;
        }

        private sealed partial class FluentSourceRewriter(SemanticModel model, CSharpCompilation compilation) : CSharpSyntaxRewriter {
            public HashSet<string> Namespaces { get; } = new HashSet<string>(StringComparer.Ordinal);
            public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node) {
                var visited = (ExpressionStatementSyntax)base.VisitExpressionStatement(node)!;
                if (node.Expression is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment
                    && assignment.Left is MemberAccessExpressionSyntax member
                    && model.GetSymbolInfo(member).Symbol is IPropertySymbol property
                    && property.SetMethod?.DeclaredAccessibility == Accessibility.Public
                    && model.GetTypeInfo(member.Expression).Type is INamedTypeSymbol receiver && Owned(receiver)
                    && HasSetter(receiver, property.Type.TypeKind == TypeKind.Delegate ? "Configure" : FluentName(property))
                    && visited.Expression is AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax rewrittenMember } rewritten) {
                    return visited.WithExpression(Setter(rewrittenMember.Expression, receiver, property, rewritten.Right));
                }
                return visited;
            }
            public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) {
                var visited = (SimpleLambdaExpressionSyntax)base.VisitSimpleLambdaExpression(node)!;
                if (node.ExpressionBody is AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax member } assignment
                    && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                    && model.GetTypeInfo(node).ConvertedType is INamedTypeSymbol { DelegateInvokeMethod.ReturnsVoid: true }
                    && model.GetSymbolInfo(member).Symbol is IPropertySymbol property
                    && model.GetTypeInfo(member.Expression).Type is INamedTypeSymbol receiver && Owned(receiver)
                    && HasSetter(receiver, property.Type.TypeKind == TypeKind.Delegate ? "Configure" : FluentName(property))
                    && visited.ExpressionBody is AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax rewrittenMember } rewritten) {
                    return visited.WithExpressionBody(Setter(rewrittenMember.Expression, receiver, property, rewritten.Right));
                }
                return visited;
            }

            public override SyntaxNode? VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) {
                if (model.GetTypeInfo(node).Type is not INamedTypeSymbol type || !Owned(type)) { return base.VisitImplicitObjectCreationExpression(node); }
                var visited = (ImplicitObjectCreationExpressionSyntax)base.VisitImplicitObjectCreationExpression(node)!;
                var created = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                    visited.ArgumentList, visited.Initializer).WithTriviaFrom(node);
                return AnnotateContentHelper(node, RewriteContent(node, Rewrite(node, created)));
            }
            public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) {
                return AnnotateContentHelper(node, RewriteContent(node, Rewrite(node, (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node)!)));
            }

            private ExpressionSyntax Rewrite(BaseObjectCreationExpressionSyntax original, ObjectCreationExpressionSyntax visited) {
                if (original.Initializer == null || model.GetTypeInfo(original).Type is not INamedTypeSymbol type
                    || !Owned(type)) { return visited; }
                // Folding markers are editorial boundaries. Retain their initializer rather than dropping
                // a closing marker attached to a brace while translating its setters.
                if (original.Initializer.DescendantTrivia().Any(trivia => trivia.ToString().Contains("<more>", StringComparison.Ordinal)
                    || trivia.ToString().Contains("</more>", StringComparison.Ordinal) || trivia.ToString().Contains("<hidden>", StringComparison.Ordinal)
                    || trivia.ToString().Contains("</hidden>", StringComparison.Ordinal))) { return visited; }
                var setters = new List<(IPropertySymbol Property, ExpressionSyntax Value)>();
                for (int i = 0; i < original.Initializer.Expressions.Count; i++) {
                    if (original.Initializer.Expressions[i] is not AssignmentExpressionSyntax assignment
                        || model.GetSymbolInfo(assignment.Left).Symbol is not IPropertySymbol property
                        || property.SetMethod?.DeclaredAccessibility != Accessibility.Public
                        || property.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == "ConsoleMonster.NoFluentAttribute")
                        || visited.Initializer!.Expressions[i] is not AssignmentExpressionSyntax converted
                        || converted.Right is InitializerExpressionSyntax || !HasSetter(type, property.Type.TypeKind == TypeKind.Delegate ? "Configure" : FluentName(property))) {
                        // Moving only some members would reorder callback/kind setters or collection initialization.
                        return visited;
                    }
                    Namespaces.Add(property.ContainingNamespace.ToDisplayString());
                    setters.Add((property, converted.Right));
                }
                ExpressionSyntax result = visited.WithInitializer(null).WithArgumentList(visited.ArgumentList ?? SyntaxFactory.ArgumentList());
                foreach (var setter in setters) {
                    result = Setter(result, type, setter.Property, setter.Value);
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

            private ExpressionSyntax Setter(ExpressionSyntax receiver, INamedTypeSymbol type, IPropertySymbol property, ExpressionSyntax value) {
                bool callback = property.Type.TypeKind == TypeKind.Delegate;
                string method = callback ? "Configure" : FluentName(property);
                Namespaces.Add(SetterNamespace(type, method)!);
                if (property.Name == "Content" && type.AllInterfaces.Any(i => i.ToDisplayString() == "ConsoleMonster.Layout.IContentOwner")) {
                    return ContentCall(receiver, new[] { value });
                }
                if (callback) {
                    var used = new HashSet<string>(model.SyntaxTree.GetRoot().DescendantTokens().Concat(value.DescendantTokens())
                        .Where(token => token.IsKind(SyntaxKind.IdentifierToken)).Select(token => token.ValueText), StringComparer.Ordinal);
                    string name = "configured";
                    for (int suffix = 2; used.Contains(name); suffix++) { name = "configured" + suffix; }
                    value = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)),
                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(name), SyntaxFactory.IdentifierName(property.Name)), value));
                }
                return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    receiver, SyntaxFactory.IdentifierName(method)), property.Type.SpecialType == SpecialType.System_Boolean && value.IsKind(SyntaxKind.TrueLiteralExpression)
                        ? SyntaxFactory.ArgumentList() : SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(value))));
            }

            internal static string FluentName(IPropertySymbol property) => property.Type.SpecialType == SpecialType.System_Boolean
                && property.Name.Length > 2 && property.Name.StartsWith("Is", StringComparison.Ordinal) && char.IsUpper(property.Name[2]) ? property.Name.Substring(2) : property.Name;

            private bool HasSetter(INamedTypeSymbol type, string member) => SetterNamespace(type, member) != null;

            private string? SetterNamespace(INamedTypeSymbol type, string member) {
                for (INamedTypeSymbol? current = type; current != null; current = current.BaseType) {
                    INamedTypeSymbol? fluent = compilation.GetTypeByMetadataName(current.ContainingNamespace.ToDisplayString() + "." + current.Name + "Fluent");
                    if (fluent?.GetMembers(member).OfType<IMethodSymbol>().Any(method => method.Parameters.Length >= 2) == true) { return current.ContainingNamespace.ToDisplayString(); }
                }
                return null;
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

            private static bool Owned(INamedTypeSymbol type) => type.ContainingAssembly.Name is "ConsoleMonster" or "ConsoleMonster.Markdown" or "ConsoleMonster.Graphics" or "ConsoleMonster.PowerShell" or "ConsoleMonster.Hosting" or "ConsoleMonster.Extensions.Logging";
        }
    }
}
