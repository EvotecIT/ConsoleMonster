using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        // Authoring only: options reach the existing operation owner through its
        // configure overload, and adjacent fluent setters stay one ordered chain.
        private sealed class FluentErgonomicsRewriter(SemanticModel model) : CSharpSyntaxRewriter {
            public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node) {
                var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
                if (model.GetSymbolInfo(node).Symbol is not IMethodSymbol method) { return visited; }
                var args = visited.ArgumentList.Arguments;
                for (int index = 0; index < node.ArgumentList.Arguments.Count; index++) {
                    if (model.GetOperation(node.ArgumentList.Arguments[index]) is not Microsoft.CodeAnalysis.Operations.IArgumentOperation binding
                        || binding.Parameter?.Type is not INamedTypeSymbol options || !options.Name.EndsWith("Options", StringComparison.Ordinal)) { continue; }
                    var original = node.ArgumentList.Arguments[index].Expression;
                    // A configure callback runs inside the operation. Keep expressions
                    // in the caller when moving them would cross a later argument,
                    // an await boundary or a caller-scope declaration.
                    if (index != node.ArgumentList.Arguments.Count - 1 || original.DescendantNodesAndSelf().Any(value => value is AwaitExpressionSyntax or DeclarationExpressionSyntax or DeclarationPatternSyntax)) { continue; }
                    var creation = RootCreation(original);
                    if (creation == null || model.GetTypeInfo(creation).Type is not INamedTypeSymbol created || !SymbolEqualityComparer.Default.Equals(created, options)) { continue; }
                    var overload = method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>().FirstOrDefault(candidate => candidate.Parameters.Any(parameter =>
                        parameter.Type is INamedTypeSymbol action && action.OriginalDefinition.ToDisplayString() == "System.Action<T>" && SymbolEqualityComparer.Default.Equals(action.TypeArguments[0], options)));
                    if (overload == null) { continue; }
                    string name = "options";
                    var names = model.SyntaxTree.GetRoot().DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.ValueText).ToHashSet(StringComparer.Ordinal);
                    for (int suffix = 2; names.Contains(name); suffix++) { name = "options" + suffix; }
                    var value = args[index].Expression;
                    var newRoot = RootCreation(value)!;
                    var configured = value.ReplaceNode(newRoot, SyntaxFactory.IdentifierName(name));
                    var statements = configured is IdentifierNameSyntax ? Array.Empty<StatementSyntax>() : new[] { SyntaxFactory.ExpressionStatement(configured) };
                    var lambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)), SyntaxFactory.Block(statements));
                    // Named arguments also handle overloads whose required configure
                    // parameter precedes the original optional state/format parameters.
                    for (int other = 0; other < args.Count; other++) {
                        if (other == index) { args = args.Replace(args[other], args[other].WithExpression(lambda).WithNameColon(SyntaxFactory.NameColon("configure"))); }
                        else if (model.GetOperation(node.ArgumentList.Arguments[other]) is Microsoft.CodeAnalysis.Operations.IArgumentOperation argument && argument.Parameter != null) {
                            args = args.Replace(args[other], args[other].WithNameColon(SyntaxFactory.NameColon(argument.Parameter.Name)));
                        }
                    }
                }
                return visited.WithArgumentList(visited.ArgumentList.WithArguments(args));
            }

            private static BaseObjectCreationExpressionSyntax? RootCreation(ExpressionSyntax value) {
                while (value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member }) { value = member.Expression; }
                return value as BaseObjectCreationExpressionSyntax;
            }

            public override SyntaxNode? VisitBlock(BlockSyntax node) {
                var visited = (BlockSyntax)base.VisitBlock(node)!;
                return visited.WithStatements(SyntaxFactory.List(Join(node.Statements.ToArray(), visited.Statements.ToArray())));
            }
            public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node) {
                var visited = (CompilationUnitSyntax)base.VisitCompilationUnit(node)!;
                var members = new List<MemberDeclarationSyntax>();
                int index = 0;
                while (index < node.Members.Count) {
                    if (node.Members[index] is not GlobalStatementSyntax) { members.Add(visited.Members[index++]); continue; }
                    int end = index;
                    while (end < node.Members.Count && node.Members[end] is GlobalStatementSyntax) { end++; }
                    var originals = node.Members.Skip(index).Take(end - index).Cast<GlobalStatementSyntax>().Select(s => s.Statement).ToArray();
                    var changed = visited.Members.Skip(index).Take(end - index).Cast<GlobalStatementSyntax>().Select(s => s.Statement).ToArray();
                    members.AddRange(Join(originals, changed).Select(SyntaxFactory.GlobalStatement));
                    index = end;
                }
                return visited.WithMembers(SyntaxFactory.List(members));
            }

            private IEnumerable<StatementSyntax> Join(StatementSyntax[] originals, StatementSyntax[] changed) {
                for (int index = 0; index < originals.Length; index++) {
                    LocalDeclarationStatementSyntax? local = originals[index] as LocalDeclarationStatementSyntax;
                    ExpressionSyntax? value = null;
                    string? name = null;
                    bool declaration = local?.Declaration.Variables.Count == 1 && changed[index] is LocalDeclarationStatementSyntax { Declaration.Variables.Count: 1 };
                    if (declaration) {
                        name = local!.Declaration.Variables[0].Identifier.ValueText;
                        value = ((LocalDeclarationStatementSyntax)changed[index]).Declaration.Variables[0].Initializer?.Value;
                    } else if (originals[index] is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax first }
                        && changed[index] is ExpressionStatementSyntax expression && ReturnsReceiver(first)
                        && RootReceiver(first) is IdentifierNameSyntax receiver) {
                        name = receiver.Identifier.ValueText;
                        value = expression.Expression;
                    }
                    if (value == null) { yield return changed[index]; continue; }
                    int end = index + 1;
                    while (end < originals.Length && originals[end] is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax call }
                        && changed[end] is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax updated }
                        && RootReceiver(call) is IdentifierNameSyntax receiver && receiver.Identifier.ValueText == name
                        && ReturnsReceiver(call)
                        && !call.DescendantNodes().OfType<ArgumentSyntax>().Any(arg => arg.DescendantTokens().Any(token => token.ValueText == name))) {
                        var comments = changed[end].GetLeadingTrivia().Where(trivia => !trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia));
                        value = updated.ReplaceNode(RootReceiver(updated)!, value);
                        if (comments.Any()) { value = value.WithLeadingTrivia(comments.Concat(new[] { SyntaxFactory.ElasticCarriageReturnLineFeed })); }
                        end++;
                    }
                    if (declaration) {
                        var rewritten = (LocalDeclarationStatementSyntax)changed[index];
                        yield return rewritten.ReplaceNode(rewritten.Declaration.Variables[0].Initializer!.Value, value);
                    } else { yield return ((ExpressionStatementSyntax)changed[index]).WithExpression(value); }
                    index = end - 1;
                }
            }
            private bool ReturnsReceiver(InvocationExpressionSyntax call) {
                if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method || method.ReturnsVoid
                    || !SymbolEqualityComparer.Default.Equals(method.ReturnType, model.GetTypeInfo(RootReceiver(call)!).Type)) { return false; }
                // Equal types alone do not imply identity: TreeNode.Add returns a
                // child. Prove every link, including links before a generated setter.
                ExpressionSyntax value = call;
                while (value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } link) {
                    if (model.GetSymbolInfo(link).Symbol is not IMethodSymbol operation || !SameInstance(operation)) { return false; }
                    value = member.Expression;
                }
                return true;
            }
            private static bool SameInstance(IMethodSymbol method) {
                var owner = (method.ReducedFrom ?? method).ContainingType;
                if (owner.ContainingAssembly.Name != "ConsoleMonster") { return false; }
                // Generated setters/adders and the hand-written fluent helpers have
                // the same-instance contract. Native builders are explicit below.
                if (method.IsExtensionMethod && owner.Name.EndsWith("Fluent", StringComparison.Ordinal)) { return true; }
                return (owner.ContainingNamespace.ToDisplayString(), owner.Name, method.Name) switch {
                    ("ConsoleMonster.Widgets", "Table", "Column" or "AddColumn" or "AddRow" or "Refresh") => true,
                    ("ConsoleMonster.Widgets", "KeyValueList", "Add") => true,
                    ("ConsoleMonster.Interactive", "Menu", "Add" or "Item" or "Separator" or "Exit" or "Preset") => true,
                    ("ConsoleMonster.Interactive", "ReviewModel", "Row" or "Problem" or "Command") => true,
                    _ => false
                };
            }
            private static ExpressionSyntax? RootReceiver(InvocationExpressionSyntax call) {
                ExpressionSyntax value = call;
                while (value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member }) { value = member.Expression; }
                return value;
            }
        }
    }
}
