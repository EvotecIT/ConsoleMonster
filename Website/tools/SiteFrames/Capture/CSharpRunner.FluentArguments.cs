using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        private static bool HasArgumentDeclarations(BaseObjectCreationExpressionSyntax creation) =>
            creation.ArgumentList?.DescendantNodes(node => node is not AnonymousFunctionExpressionSyntax)
                .Any(node => node is DeclarationExpressionSyntax or DeclarationPatternSyntax) == true;

        private sealed partial class FluentFactoryRewriter {
            private static IParameterSymbol BoundParameter(IMethodSymbol constructor, ArgumentSyntax argument, int index) =>
                argument.NameColon == null ? constructor.Parameters[Math.Min(index, constructor.Parameters.Length - 1)]
                    : constructor.Parameters.Single(parameter => parameter.Name == argument.NameColon.Name.Identifier.ValueText);

            private static ExpressionSyntax? BoundArgument(BaseObjectCreationExpressionSyntax original, ObjectCreationExpressionSyntax visited, IMethodSymbol constructor, string name) {
                for (int index = 0; index < (original.ArgumentList?.Arguments.Count ?? 0); index++) {
                    if (BoundParameter(constructor, original.ArgumentList!.Arguments[index], index).Name == name) { return visited.ArgumentList!.Arguments[index].Expression; }
                }
                return null;
            }

            private bool NeedsArgumentStaging(BaseObjectCreationExpressionSyntax original, INamedTypeSymbol type, IMethodSymbol constructor, bool buildDefinition) {
                var arguments = original.ArgumentList?.Arguments ?? default;
                string[] names = arguments.Select((argument, index) => BoundParameter(constructor, argument, index).Name).ToArray();
                if (!buildDefinition) {
                    int primary = Array.IndexOf(names, "widgetId");
                    if (primary > 0 && !model.GetConstantValue(arguments[primary].Expression).HasValue
                        && arguments.Take(primary).Any(argument => !model.GetConstantValue(argument.Expression).HasValue)) { return true; }
                }
                if (type.Name is "SelectPrompt" or "MultiSelectPrompt" && names.Contains("values", StringComparer.Ordinal)) {
                    // Choice.From takes display/group in declaration order. Named constructor
                    // arguments are evaluated in source order, including method-group receivers.
                    string[] reordered = names.Where(name => name is not ("display" or "group")).SelectMany(name => name == "values"
                        ? new[] { "values" }.Concat(new[] { "display", "group" }.Where(names.Contains)) : new[] { name }).ToArray();
                    return !names.SequenceEqual(reordered, StringComparer.Ordinal);
                }
                return false;
            }

            private ExpressionSyntax ConstructorContents(BaseObjectCreationExpressionSyntax original, ObjectCreationExpressionSyntax visited, IMethodSymbol constructor, IParameterSymbol parameter) {
                string content = Unique("content", visited);
                var statements = new List<StatementSyntax>();
                for (int index = 0; index < visited.ArgumentList!.Arguments.Count; index++) {
                    ArgumentSyntax argument = original.ArgumentList!.Arguments[index];
                    if (BoundParameter(constructor, argument, index).Name != parameter.Name) { continue; }
                    var info = model.GetTypeInfo(argument.Expression);
                    var child = (SimpleLambdaExpressionSyntax)ConstructorContent(parameter, visited.ArgumentList.Arguments[index].Expression,
                        info.Type is IArrayTypeSymbol, info.Nullability.FlowState != NullableFlowState.NotNull, content);
                    if (child.Body is BlockSyntax block) { statements.AddRange(block.Statements); }
                    else { statements.Add(SyntaxFactory.ExpressionStatement((ExpressionSyntax)child.Body)); }
                }
                return SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier(content)), SyntaxFactory.Block(statements));
            }

            private ExpressionSyntax ConstructorContent(IParameterSymbol parameter, ExpressionSyntax value, bool array, bool nullable, string content) {
                string child = Unique("child", value);
                if (array) {
                    return SyntaxFactory.ParseExpression(content + " => { foreach (var " + child + " in (" + value + " ?? global::System.Array.Empty<global::ConsoleMonster.Layout.IWidget>())) { " + content + ".Add(" + child + "); } }");
                }
                if (nullable && parameter.Type.NullableAnnotation == NullableAnnotation.Annotated) {
                    return SyntaxFactory.ParseExpression(content + " => { global::ConsoleMonster.Layout.IWidget? " + child + " = " + value + "; if (" + child + " != null) { " + content + ".Add(" + child + "); } }");
                }
                if (RootFactory(value) is InvocationExpressionSyntax factory && factory.Expression is MemberAccessExpressionSyntax member
                    && member.Name.Identifier.ValueText is not ("Options" or "RichText")) {
                    string noun = member.Name.Identifier.ValueText;
                    if (value == factory && factory.ArgumentList.Arguments.Count == 1 && noun is "Text" or "Markup" or "Rule") {
                        return SyntaxFactory.ParseExpression(content + " => " + content + "." + noun + factory.ArgumentList);
                    }
                    if (noun == "Markup") { noun = "Text"; }

                    if (factory.ArgumentList.Arguments.Count == 0 && noun is not ("Chart" or "Progress" or "Select" or "TextInput" or "Button" or "Toggle" or "Binding")) {
                        string configured = Unique(noun, value);
                        ExpressionSyntax configuration = value.ReplaceNode(factory, SyntaxFactory.IdentifierName(configured));
                        if (configuration is IdentifierNameSyntax) { return SyntaxFactory.ParseExpression(content + " => " + content + "." + noun + "()"); }
                        return SyntaxFactory.ParseExpression(content + " => " + content + "." + noun + "(" + configured + " => " + configuration + ")");
                    }
                    return SyntaxFactory.ParseExpression(content + " => " + content + "." + noun + "(" + value + ")");
                }
                return SyntaxFactory.ParseExpression(content + " => " + content + ".Add(" + value + ")");
            }

            // Only source-order-sensitive calls need staging. Ordinary demo chains stay
            // direct; an immediate typed lambda preserves each argument's target type and
            // single evaluation when moving a primary input or grouping optional callbacks.
            private ExpressionSyntax StageArguments(BaseObjectCreationExpressionSyntax original, ObjectCreationExpressionSyntax visited, INamedTypeSymbol type, IMethodSymbol constructor) {
                var statements = new List<StatementSyntax>();
                var replacements = new List<ArgumentSyntax>();
                var format = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
                for (int index = 0; index < visited.ArgumentList!.Arguments.Count; index++) {
                    ArgumentSyntax argument = visited.ArgumentList.Arguments[index];
                    IParameterSymbol parameter = BoundParameter(constructor, original.ArgumentList!.Arguments[index], index);
                    ITypeSymbol localType = parameter.Type;
                    if (parameter.IsParams && localType is IArrayTypeSymbol array && model.GetTypeInfo(original.ArgumentList.Arguments[index].Expression).Type is not IArrayTypeSymbol) { localType = array.ElementType; }
                    string name = Unique("argument" + index, visited);
                    statements.Add(SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(localType.ToDisplayString(format)),
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(name).WithInitializer(SyntaxFactory.EqualsValueClause(argument.Expression))))));
                    replacements.Add(argument.WithExpression(SyntaxFactory.IdentifierName(name)));
                }
                ExpressionSyntax result = Rewrite(original, visited.WithArgumentList(visited.ArgumentList.WithArguments(SyntaxFactory.SeparatedList(replacements))), stageArguments: false);
                statements.Add(SyntaxFactory.ReturnStatement(result));
                var lambda = SyntaxFactory.ParenthesizedLambdaExpression().WithBlock(SyntaxFactory.Block(statements));
                var callback = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("global::System.Func<" + type.ToDisplayString(format) + ">"), SyntaxFactory.ParenthesizedExpression(lambda));
                return SyntaxFactory.InvocationExpression(SyntaxFactory.ParenthesizedExpression(callback), SyntaxFactory.ArgumentList()).WithTriviaFrom(original);
            }
        }
    }
}
