using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        /// <summary>Resolves configured properties from the same compilation used to execute a shown program.</summary>
        public static IReadOnlyCollection<string> ConfiguredMembers(string path, params string[] companions) {
            var compilation = CreateCompilation(path, Site.ReadText(path), companions: companions);
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (SyntaxTree tree in compilation.SyntaxTrees.Where(t => t.FilePath != "GlobalUsings.g.cs")) {
                SemanticModel model = compilation.GetSemanticModel(tree);
                foreach (SyntaxNode node in tree.GetRoot().DescendantNodes()) {
                    if (node is InvocationExpressionSyntax invocation && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol invoked) {
                        IMethodSymbol method = invoked.ReducedFrom ?? invoked;
                        if (method.ContainingNamespace.ToDisplayString().StartsWith("ConsoleMonster", StringComparison.Ordinal)) {
                            result.Add("M:" + method.ContainingType.ToDisplayString() + "." + method.Name);
                        }
                    }
                    if (node is AssignmentExpressionSyntax assignment && model.GetSymbolInfo(assignment.Left).Symbol is IPropertySymbol property) {
                        Add(result, property);
                    } else if (node is InvocationExpressionSyntax call && call.Expression is MemberAccessExpressionSyntax member
                        && model.GetSymbolInfo(call).Symbol is IMethodSymbol
                        && model.GetTypeInfo(member.Expression).Type is INamedTypeSymbol receiver) {
                        Add(result, Find(receiver, member.Name.Identifier.ValueText));
                    } else if (node is ArgumentSyntax argument && model.GetOperation(argument) is IArgumentOperation operation
                        && operation.Parameter is IParameterSymbol parameter && parameter.ContainingSymbol is IMethodSymbol constructor
                        && constructor.MethodKind == MethodKind.Constructor) {
                        Add(result, Find(constructor.ContainingType, parameter.Name));
                    }
                }
            }
            return result.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static IPropertySymbol? Find(INamedTypeSymbol type, string name) {
            for (INamedTypeSymbol? current = type; current != null; current = current.BaseType) {
                IPropertySymbol? property = current.GetMembers().OfType<IPropertySymbol>()
                    .FirstOrDefault(member => string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase));
                if (property != null) { return property; }
            }
            return null;
        }

        private static void Add(HashSet<string> result, IPropertySymbol? property) {
            if (property == null || !property.ContainingNamespace.ToDisplayString().StartsWith("ConsoleMonster", StringComparison.Ordinal)) { return; }
            var owner = property.ContainingType.OriginalDefinition;
            string name = owner.MetadataName;
            while (owner.ContainingType is INamedTypeSymbol parent) { name = parent.MetadataName + "+" + name; owner = parent; }
            result.Add(owner.ContainingNamespace.ToDisplayString() + "." + name + "." + property.Name);
        }
    }
}
