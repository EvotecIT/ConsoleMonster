using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        /// <summary>Resolves configured properties from the same compilation used to execute a shown program.</summary>
        public static IReadOnlyCollection<string> ConfiguredMembers(string path, params string[] companions) {
            return ConfiguredMembersFromSource(path, Site.ReadText(path), companions);
        }

        /// <summary>Resolves the generated variant without writing a temporary source file.</summary>
        public static IReadOnlyCollection<string> ConfiguredMembersFromSource(string path, string code, params string[] companions) {
            return ConfiguredMembers(CreateCompilation(path, code, companions: companions));
        }

        public static IReadOnlyCollection<string> ConfiguredProjectMembers(string path, string project, string code) =>
            ConfiguredMembers(CreateCompilation(path, code, references: ProjectReferences(project)));

        /// <summary>Resolves direct construction in a fluent program, so data records remain ordinary C# while UI construction uses factories.</summary>
        public static IReadOnlyCollection<string> ConstructedConsoleTypes(string path, string code) {
            CSharpCompilation compilation = CreateCompilation(path, code);
            var types = new HashSet<string>(StringComparer.Ordinal);
            foreach (SyntaxTree tree in compilation.SyntaxTrees.Where(tree => tree.FilePath == path)) {
                SemanticModel model = compilation.GetSemanticModel(tree);
                foreach (BaseObjectCreationExpressionSyntax node in tree.GetRoot().DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>()) {
                    if (model.GetTypeInfo(node).Type is INamedTypeSymbol type && type.ContainingAssembly.Name.StartsWith("ConsoleMonster", StringComparison.Ordinal)) { types.Add(type.ToDisplayString()); }
                }
            }
            return types;
        }

        private static IReadOnlyCollection<string> ConfiguredMembers(Compilation compilation) {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (SyntaxTree tree in compilation.SyntaxTrees.Where(t => t.FilePath != "GlobalUsings.g.cs")) {
                SemanticModel model = compilation.GetSemanticModel(tree);
                foreach (SyntaxNode node in tree.GetRoot().DescendantNodes()) {
                    if (node is BaseObjectCreationExpressionSyntax created && model.GetTypeInfo(created).Type is INamedTypeSymbol createdType && createdType.ContainingAssembly.Name.StartsWith("ConsoleMonster", StringComparison.Ordinal)) { result.Add("C:" + createdType.ToDisplayString()); }
                    if (node is InvocationExpressionSyntax invocation && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol invoked) {
                        IMethodSymbol method = invoked.ReducedFrom ?? invoked;
                        if (method.ContainingNamespace.ToDisplayString().StartsWith("ConsoleMonster", StringComparison.Ordinal) || method.ContainingType.ToDisplayString() == "System.Console") {
                            result.Add("M:" + method.ContainingType.ToDisplayString() + "." + method.Name);
                        }
                    }
                    if (node is AssignmentExpressionSyntax assignment && model.GetSymbolInfo(assignment.Left).Symbol is IPropertySymbol property) {
                        Add(result, property);
                    } else if (node is InvocationExpressionSyntax call && call.Expression is MemberAccessExpressionSyntax member
                        && model.GetSymbolInfo(call).Symbol is IMethodSymbol
                        && model.GetTypeInfo(member.Expression).Type is INamedTypeSymbol receiver) {
                        string name = member.Name.Identifier.ValueText;
                        var method = (IMethodSymbol)model.GetSymbolInfo(call).Symbol!;
                        var parameters = (method.ReducedFrom ?? method).Parameters;
                        bool boolean = parameters.Length > 1 && parameters[1].Type.SpecialType == SpecialType.System_Boolean || parameters.Length == 1 && method.IsExtensionMethod;
                        var flag = boolean ? Find(receiver, "Is" + name) : null;
                        Add(result, flag?.Type.SpecialType == SpecialType.System_Boolean ? flag : Find(receiver, name));
                    } else if (node is ArgumentSyntax argument && model.GetOperation(argument) is IArgumentOperation operation
                        && operation.Parameter is IParameterSymbol parameter && parameter.ContainingSymbol is IMethodSymbol owner) {
                        if (owner.ContainingNamespace.ToDisplayString().StartsWith("ConsoleMonster", StringComparison.Ordinal)) {
                            string methodName = owner.MethodKind == MethodKind.Constructor ? ".ctor" : owner.Name;
                            result.Add("A:" + owner.ContainingType.ToDisplayString() + "." + methodName + "." + parameter.Name);
                        }
                        if (owner.MethodKind == MethodKind.Constructor) { Add(result, Find(owner.ContainingType, parameter.Name)); }
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
