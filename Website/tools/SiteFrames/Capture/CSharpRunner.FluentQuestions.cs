using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        // Typed prompt classes are the initializer style; the fluent tab asks the same questions through the
        // definitions Read-ConsoleText and Read-ConsoleSelect map to: Ui.TextQuestion() and Ui.SelectQuestion().
        private sealed partial class FluentFactoryRewriter {
            private static string? QuestionNoun(INamedTypeSymbol type) => type.ContainingNamespace.ToDisplayString() != "ConsoleMonster.Interactive" ? null : type.Name switch {
                "TextPrompt" or "SecretPrompt" or "TextAreaPrompt" => "TextQuestion",
                "SelectPrompt" or "MultiSelectPrompt" => "SelectQuestion",
                _ => null
            };

            private static string? QuestionVariant(INamedTypeSymbol type) => type.Name switch {
                "SecretPrompt" => "AsSecret", "TextAreaPrompt" => "Multiline", "MultiSelectPrompt" => "Multiple", _ => null
            };

            private static string? QuestionType(ITypeSymbol? type) => type is INamedTypeSymbol named ? QuestionNoun(named) switch {
                "TextQuestion" => "TextQuestionOptions", "SelectQuestion" => "SelectQuestionOptions", _ => null
            } : null;

            public override SyntaxNode? VisitGenericName(GenericNameSyntax node) {
                if (node.Parent is not MemberAccessExpressionSyntax && node.Parent is not ObjectCreationExpressionSyntax
                    && QuestionType(model.GetSymbolInfo(node).Symbol as ITypeSymbol) is string name) { return SyntaxFactory.IdentifierName(name).WithTriviaFrom(node); }
                return base.VisitGenericName(node);
            }

            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) {
                if (node.Parent is not MemberAccessExpressionSyntax && node.Parent is not ObjectCreationExpressionSyntax
                    && model.GetSymbolInfo(node).Symbol is ITypeSymbol type && QuestionType(type) is string name) { return SyntaxFactory.IdentifierName(name).WithTriviaFrom(node); }
                return base.VisitIdentifierName(node);
            }

            // SelectPrompt<T>.Show() returns T; the definition's typed endings keep that type.
            private ExpressionSyntax? QuestionShow(InvocationExpressionSyntax node, InvocationExpressionSyntax visited) {
                if (node.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Show" } || visited.Expression is not MemberAccessExpressionSyntax access
                    || model.GetTypeInfo(((MemberAccessExpressionSyntax)node.Expression).Expression).Type is not INamedTypeSymbol { TypeArguments.Length: 1 } receiver
                    || QuestionNoun(receiver) != "SelectQuestion") { return null; }
                string method = receiver.Name == "MultiSelectPrompt" ? "ShowMany" : "Show";
                var typed = SyntaxFactory.GenericName(SyntaxFactory.Identifier(method)).WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.ParseTypeName(receiver.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))));
                return visited.WithExpression(access.WithName(typed));
            }

            // A typed preview becomes a callback over the definition's untyped value.
            public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node) {
                var visited = (AssignmentExpressionSyntax)base.VisitAssignmentExpression(node)!;
                if (node.Left is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Preview" } member
                    || model.GetTypeInfo(member.Expression).Type is not INamedTypeSymbol { TypeArguments.Length: 1 } receiver || QuestionNoun(receiver) != "SelectQuestion") { return visited; }
                string item = receiver.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                string value = Unique("value", node);
                ExpressionSyntax preview = visited.Right is SimpleLambdaExpressionSyntax { ExpressionBody: ExpressionSyntax body } lambda
                    ? SyntaxFactory.ParseExpression(value + " => " + value + " is " + item + " " + lambda.Parameter.Identifier.ValueText + " ? " + body + " : null")
                    : SyntaxFactory.ParseExpression(value + " => " + value + " is " + item + " " + Unique("item", node) + " ? " + Callable(visited.Right, item) + "(" + Unique("item", node) + ") : null");
                return visited.WithRight(preview);
            }

            // A name or member access is invoked directly; anything else is typed first so "(x)(y)" never reads as a cast.
            private static string Callable(ExpressionSyntax callback, string item) => callback is IdentifierNameSyntax or MemberAccessExpressionSyntax
                ? callback.ToString() : "((global::System.Func<" + item + ", global::ConsoleMonster.Layout.IWidget?>)(" + callback + "))";
        }
    }
}
