using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        // An initializer program often keeps an input control in a local so a loop can fill one of its collections:
        //     ChartView load = new ChartView("Load", chart);
        //     foreach (string time in times) { load.PointLabels.Add(time); }
        //     Rows layout = new Rows(grid, load);
        // Its fluent twin reads as one definition in the content builder:
        //     c.ChartView(chartView => chartView.Key("Load").Chart(chart).PointLabels(times));
        // Only a local used once, in the statement right after its fills, moves: nothing can run between its creation
        // and its use, so the move cannot change what the program does.
        private sealed class FluentInlineRewriter(SemanticModel model) : CSharpSyntaxRewriter {
            private const string Builder = "ConsoleMonster.Layout.ContentBuilder";

            public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node) {
                var members = new List<MemberDeclarationSyntax>();
                int index = 0;
                while (index < node.Members.Count) {
                    if (node.Members[index] is not GlobalStatementSyntax) { members.Add(node.Members[index++]); continue; }
                    int end = index;
                    while (end < node.Members.Count && node.Members[end] is GlobalStatementSyntax) { end++; }
                    var statements = node.Members.Skip(index).Take(end - index).Cast<GlobalStatementSyntax>().Select(s => s.Statement).ToList();
                    members.AddRange(Inline(statements).Select(SyntaxFactory.GlobalStatement));
                    index = end;
                }
                return node.WithMembers(SyntaxFactory.List(members));
            }

            private List<StatementSyntax> Inline(List<StatementSyntax> statements) {
                var result = new List<StatementSyntax>();
                for (int i = 0; i < statements.Count; i++) {
                    if (Definition(statements[i]) is (ILocalSymbol local, ExpressionSyntax chain, INamedTypeSymbol options, string noun)) {
                        int next = i + 1;
                        while (next < statements.Count && Fill(statements[next], local, options) is (string setter, ExpressionSyntax source)) {
                            chain = Call(chain, setter, source);
                            next++;
                        }
                        if (next < statements.Count && UsedOnlyIn(statements, next, local)
                            && Use(statements[next], local, noun, chain, options) is StatementSyntax replaced) {
                            // Comments above the moved declaration stay above the statement that now holds it.
                            SyntaxTriviaList lead = statements[i].GetLeadingTrivia();
                            if (lead.Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))) {
                                replaced = replaced.WithLeadingTrivia(lead.AddRange(replaced.GetLeadingTrivia()));
                            }
                            result.Add(replaced);
                            i = next;
                            continue;
                        }
                    }
                    result.Add(statements[i]);
                }
                return result;
            }

            // T name = ((T)Ui.Noun()...Build()); with a Build that takes no arguments and returns T.
            private (ILocalSymbol, ExpressionSyntax, INamedTypeSymbol, string)? Definition(StatementSyntax statement) {
                if (statement is not LocalDeclarationStatementSyntax { Declaration.Variables: { Count: 1 } variables }
                    || variables[0].Initializer?.Value is not ExpressionSyntax value
                    || model.GetDeclaredSymbol(variables[0]) is not ILocalSymbol local) { return null; }
                while (value is ParenthesizedExpressionSyntax parentheses) { value = parentheses.Expression; }
                if (value is CastExpressionSyntax cast) { value = cast.Expression; }
                while (value is ParenthesizedExpressionSyntax parentheses) { value = parentheses.Expression; }
                if (value is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0, Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Build" } build }
                    || model.GetTypeInfo(build.Expression).Type is not INamedTypeSymbol options
                    || !options.Name.EndsWith("Options", StringComparison.Ordinal)
                    || Root(build.Expression) is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax root }) { return null; }
                return (local, build.Expression, options, root.Name.Identifier.ValueText);
            }

            // foreach (E item in source) { name.Collection.Add(item); } where the definition has a Collection(source) setter.
            private (string, ExpressionSyntax)? Fill(StatementSyntax statement, ILocalSymbol local, INamedTypeSymbol options) {
                if (statement is not ForEachStatementSyntax loop) { return null; }
                StatementSyntax body = loop.Statement is BlockSyntax { Statements.Count: 1 } block ? block.Statements[0] : loop.Statement;
                if (body is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { ArgumentList.Arguments: { Count: 1 } arguments } add }
                    || add.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Add", Expression: MemberAccessExpressionSyntax collection }
                    || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(collection.Expression).Symbol, local)
                    || arguments[0].Expression is not IdentifierNameSyntax item || item.Identifier.ValueText != loop.Identifier.ValueText
                    || model.GetTypeInfo(loop.Expression).Type is not ITypeSymbol sourceType) { return null; }
                string setter = collection.Name.Identifier.ValueText;
                bool accepts = Setters(options, setter).Any(parameter => model.Compilation.ClassifyConversion(sourceType, parameter).IsImplicit);
                return accepts ? (setter, loop.Expression) : null;
            }

            // The single-argument setters a definition type has for a member, as instance or fluent extension methods.
            private IEnumerable<ITypeSymbol> Setters(INamedTypeSymbol options, string name) {
                foreach (IMethodSymbol method in options.GetMembers(name).OfType<IMethodSymbol>().Where(m => m.Parameters.Length == 1)) { yield return method.Parameters[0].Type; }
                INamedTypeSymbol? fluent = model.Compilation.GetTypeByMetadataName(options.ContainingNamespace.ToDisplayString() + "." + options.ContainingNamespace.Name + "Fluent");
                foreach (IMethodSymbol method in fluent?.GetMembers(name).OfType<IMethodSymbol>() ?? Enumerable.Empty<IMethodSymbol>()) {
                    if (method.IsExtensionMethod && method.Parameters.Length == 2 && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, options)) { yield return method.Parameters[1].Type; }
                }
            }

            private bool UsedOnlyIn(List<StatementSyntax> statements, int index, ILocalSymbol local) {
                int Uses(StatementSyntax statement) => statement.DescendantNodes().OfType<IdentifierNameSyntax>()
                    .Count(name => name.Identifier.ValueText == local.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(name).Symbol, local));
                return Uses(statements[index]) == 1 && statements.Skip(index + 1).All(statement => Uses(statement) == 0);
            }

            // c.Add(name) becomes c.Noun(noun => ...); pane(name) becomes pane(c => c.Noun(noun => ...)) where the pane has a
            // content-builder overload, such as Splitter.First and Second.
            private StatementSyntax? Use(StatementSyntax statement, ILocalSymbol local, string noun, ExpressionSyntax chain, INamedTypeSymbol options) {
                IdentifierNameSyntax reference = statement.DescendantNodes().OfType<IdentifierNameSyntax>()
                    .First(name => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(name).Symbol, local));
                if (reference.Parent is not ArgumentSyntax { Parent: ArgumentListSyntax { Arguments.Count: 1, Parent: InvocationExpressionSyntax call } }
                    || call.Expression is not MemberAccessExpressionSyntax member
                    || model.GetTypeInfo(member.Expression).Type is not INamedTypeSymbol receiver) { return null; }
                INamedTypeSymbol builder = model.Compilation.GetTypeByMetadataName(Builder)!;
                if (!builder.GetMembers(noun).OfType<IMethodSymbol>().Any(method => method.Parameters.Length == 1
                    && method.Parameters[0].Type is INamedTypeSymbol { DelegateInvokeMethod: { Parameters.Length: 1 } invoke }
                    && SymbolEqualityComparer.Default.Equals(invoke.Parameters[0].Type, options))) { return null; }
                var names = reference.Ancestors().OfType<SimpleLambdaExpressionSyntax>().Select(lambda => lambda.Parameter.Identifier.ValueText)
                    .Concat(chain.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.ValueText)).ToHashSet(StringComparer.Ordinal);
                string Fresh(string stem) { string name = stem; for (int suffix = 2; names.Contains(name); suffix++) { name = stem + suffix; } names.Add(name); return name; }
                string parameter = Fresh(BuilderParameter(noun));
                ExpressionSyntax configured = chain.ReplaceNode(Root(chain)!, SyntaxFactory.IdentifierName(parameter));
                ExpressionSyntax helper(string content) => SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(content), SyntaxFactory.IdentifierName(noun)),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(
                        configured is IdentifierNameSyntax ? SyntaxFactory.ParseExpression(parameter + " => { }")
                            : SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameter)), configured)))));
                if (receiver.ToDisplayString() == Builder && member.Name.Identifier.ValueText == "Add" && member.Expression is IdentifierNameSyntax content) {
                    return statement.ReplaceNode(call, helper(content.Identifier.ValueText).WithTriviaFrom(call));
                }
                INamedTypeSymbol? fluent = model.Compilation.GetTypeByMetadataName(receiver.ContainingNamespace.ToDisplayString() + "." + receiver.ContainingNamespace.Name + "Fluent");
                bool pane = fluent?.GetMembers(member.Name.Identifier.ValueText).OfType<IMethodSymbol>().Any(method => method.IsExtensionMethod && method.Parameters.Length == 2
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, receiver) && method.Parameters[1].Type.ToDisplayString() == "System.Action<" + Builder + ">") == true;
                if (!pane) { return null; }
                string c = Fresh("c");
                var lambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier(c)), helper(c));
                return statement.ReplaceNode(reference, lambda.WithTriviaFrom(reference));
            }

            private static InvocationExpressionSyntax? Root(ExpressionSyntax expression) {
                while (true) {
                    if (expression is not InvocationExpressionSyntax call || call.Expression is not MemberAccessExpressionSyntax member) { return null; }
                    if (member.Expression is IdentifierNameSyntax { Identifier.ValueText: "Ui" }) { return call; }
                    expression = member.Expression;
                }
            }

            private static InvocationExpressionSyntax Call(ExpressionSyntax receiver, string member, ExpressionSyntax argument) =>
                SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName(member)),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(argument))));
        }
    }
}
