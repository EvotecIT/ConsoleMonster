using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SiteFrames.Capture {
    /// <summary>
    /// The fluent methods PowerForge.Generators writes for ConsoleMonster: one <c>&lt;Type&gt;Fluent</c> class per model
    /// (handwritten helpers sit in the same class), a property-named setter, a declared verb for a delegate property and a
    /// singular-named child method for a collection.
    /// </summary>
    internal static class FluentApi {
        private const string FluentAttribute = "PowerForge.Fluent.FluentAttribute";
        internal const string NoFluentAttribute = "PowerForge.Fluent.NoFluentAttribute";

        /// <summary>The <c>&lt;Type&gt;Fluent</c> classes of a type and its base types, nearest first.</summary>
        internal static IEnumerable<INamedTypeSymbol> Classes(Compilation compilation, INamedTypeSymbol type) {
            for (INamedTypeSymbol? current = type; current != null; current = current.BaseType) {
                INamedTypeSymbol? fluent = compilation.GetTypeByMetadataName(current.ContainingNamespace.ToDisplayString() + "." + current.Name + "Fluent");
                if (fluent != null) { yield return fluent; }
            }
        }

        /// <summary>The fluent extension methods that apply to a type, reduced to its receiver.</summary>
        internal static IEnumerable<IMethodSymbol> Methods(Compilation compilation, INamedTypeSymbol type, string? name = null) =>
            Classes(compilation, type).SelectMany(fluent => name == null ? fluent.GetMembers() : fluent.GetMembers(name)).OfType<IMethodSymbol>()
                .Where(method => method.IsExtensionMethod).Select(method => method.ReduceExtensionMethod(type)).Where(method => method != null).Select(method => method!);

        /// <summary>The method that sets a property: its declared <c>[Fluent("Name")]</c>, else its name without a bool's Is prefix.</summary>
        internal static string SetterName(IPropertySymbol property) {
            string? declared = property.GetAttributes().Where(attribute => attribute.AttributeClass?.ToDisplayString() == FluentAttribute)
                .Select(attribute => attribute.ConstructorArguments.Length == 1 ? attribute.ConstructorArguments[0].Value as string : null).FirstOrDefault();
            if (declared != null) { return declared; }
            return property.Type.SpecialType == SpecialType.System_Boolean && property.Name.Length > 2 && property.Name.StartsWith("Is", System.StringComparison.Ordinal)
                && char.IsUpper(property.Name[2]) ? property.Name.Substring(2) : property.Name;
        }
    }
}
