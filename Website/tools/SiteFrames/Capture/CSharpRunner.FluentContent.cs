using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        private sealed partial class FluentSourceRewriter {
            private const string ContentHelperAnnotation = "ContentBuilderHelper";

            private ExpressionSyntax AnnotateContentHelper(BaseObjectCreationExpressionSyntax original, ExpressionSyntax rewritten) {
                if (rewritten is ObjectCreationExpressionSyntax { Initializer: null }
                    && original.ArgumentList?.Arguments.Count == 1
                    && model.GetTypeInfo(original).Type is INamedTypeSymbol type
                    && type.ContainingNamespace.ToDisplayString() == "ConsoleMonster.Widgets" && type.Name is "Rule" or "TextBlock"
                    && model.GetTypeInfo(original.ArgumentList.Arguments[0].Expression).Type?.SpecialType == SpecialType.System_String) {
                    return rewritten.WithAdditionalAnnotations(new SyntaxAnnotation(ContentHelperAnnotation, type.Name == "Rule" ? "Rule" : "Text"));
                }
                return rewritten;
            }
            // The initializer remains the maintained source; its fluent twin teaches the
            // same ordered children through the public ContentBuilder grammar.
            private ExpressionSyntax RewriteContent(BaseObjectCreationExpressionSyntax original, ExpressionSyntax rewritten) {
                if (HasArgumentDeclarations(original)) { return rewritten; }
                if (model.GetTypeInfo(original).Type is not INamedTypeSymbol type
                    || !type.AllInterfaces.Any(i => i.ToDisplayString() == "ConsoleMonster.Layout.IContentOwner")
                    || model.GetSymbolInfo(original).Symbol is not IMethodSymbol constructor) { return rewritten; }
                var originalArguments = original.ArgumentList?.Arguments;
                if (originalArguments == null || originalArguments.Value.Count == 0) { return rewritten; }
                ObjectCreationExpressionSyntax? created = rewritten.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
                if (created?.ArgumentList == null || created.Initializer != null) { return rewritten; }
                var content = new List<ExpressionSyntax>();
                var sequences = new HashSet<ExpressionSyntax>();
                var retained = new List<ArgumentSyntax>();
                var retainedTypes = new Dictionary<string, ITypeSymbol>(StringComparer.Ordinal);
                for (int i = 0; i < originalArguments.Value.Count; i++) {
                    ArgumentSyntax argument = originalArguments.Value[i];
                    IParameterSymbol parameter = argument.NameColon == null
                        ? constructor.Parameters[Math.Min(i, constructor.Parameters.Length - 1)]
                        : constructor.Parameters.Single(p => p.Name == argument.NameColon.Name.Identifier.ValueText);
                    ExpressionSyntax value = created.ArgumentList.Arguments[i].Expression;
                    if (parameter.Name is "content" or "children" or "layers") {
                        if (model.GetTypeInfo(argument.Expression).Nullability.FlowState == NullableFlowState.MaybeNull) { return rewritten; }
                        bool sequence = model.GetTypeInfo(argument.Expression).Type is IArrayTypeSymbol;
                        content.Add(value);
                        if (sequence) { sequences.Add(value); }
                    } else {
                        // Reordering a dynamic title/column expression around child creation
                        // cannot be made equivalent by a fluent setter alone.
                        if (!model.GetConstantValue(argument.Expression).HasValue) { return rewritten; }
                        retained.Add(created.ArgumentList.Arguments[i].WithNameColon(SyntaxFactory.NameColon(parameter.Name)));
                        retainedTypes[parameter.Name] = model.GetTypeInfo(argument.Expression).Type ?? parameter.Type;
                    }
                }
                if (content.Count == 0) { return rewritten; }
                bool constructible = type.InstanceConstructors.Any(candidate => candidate.DeclaredAccessibility == Accessibility.Public
                    && candidate.Parameters.All(parameter => parameter.IsOptional || parameter.IsParams || retainedTypes.ContainsKey(parameter.Name))
                    && retainedTypes.All(pair => candidate.Parameters.Any(parameter => parameter.Name == pair.Key && compilation.ClassifyConversion(pair.Value, parameter.Type).IsImplicit)));
                if (!constructible) { return rewritten; }
                Namespaces.Add("ConsoleMonster.Layout");
                var empty = created.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(retained)));
                if (original.Initializer != null && original.Initializer.Expressions.Any(expression => expression is not AssignmentExpressionSyntax assignment
                    || !model.GetConstantValue(assignment.Right).HasValue)) {
                    return rewritten.ReplaceNode(created, ContentCall(empty, content, sequences)).WithTriviaFrom(original);
                }
                return ContentCall(rewritten.ReplaceNode(created, empty), content, sequences).WithTriviaFrom(original);
            }

            private ExpressionSyntax ContentCall(ExpressionSyntax receiver, IEnumerable<ExpressionSyntax> content, ISet<ExpressionSyntax>? sequences = null) {
                // Different nesting levels need distinct names: C# forbids reusing an
                // enclosing lambda's parameter in a nested Content block.
                var used = new HashSet<string>(model.SyntaxTree.GetRoot().DescendantTokens().Concat(receiver.DescendantTokens())
                    .Concat(content.SelectMany(value => value.DescendantTokens()))
                    .Where(token => token.IsKind(SyntaxKind.IdentifierToken)).Select(token => token.ValueText), StringComparer.Ordinal);
                string name = "c";
                for (int suffix = 2; used.Contains(name); suffix++) { name = "c" + suffix; }
                var statements = content.Select<ExpressionSyntax, StatementSyntax>(value => {
                    if (sequences?.Contains(value) == true) {
                        string child = name + "Widget";
                        for (int suffix = 2; used.Contains(child); suffix++) { child = name + "Widget" + suffix; }
                        return SyntaxFactory.ParseStatement("foreach (var " + child + " in (" + value + " ?? global::System.Array.Empty<global::ConsoleMonster.Layout.IWidget>())) { " + name + ".Add(" + child + "); }");
                    }
                    string operation = "Add";
                    ArgumentListSyntax arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(value)));
                    if (value is ObjectCreationExpressionSyntax widget && value.GetAnnotations(ContentHelperAnnotation).SingleOrDefault() is SyntaxAnnotation helper) {
                        operation = helper.Data!;
                        arguments = widget.ArgumentList!.WithArguments(SyntaxFactory.SeparatedList(widget.ArgumentList.Arguments.Select(argument => argument.WithNameColon(null))));
                        if (operation == "Text") {
                            string text = name + "Text";
                            for (int suffix = 2; used.Contains(text); suffix++) { text = name + "Text" + suffix; }
                            arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(
                                SyntaxFactory.ParseExpression(text + " => " + text + ".Content(" + arguments.Arguments[0].Expression + ").IsMarkup()"))));
                        }
                    }
                    return SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(name), SyntaxFactory.IdentifierName(operation)), arguments));
                });
                var lambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)), SyntaxFactory.Block(statements));
                return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName("Content")),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(lambda))));
            }
        }
    }
}
