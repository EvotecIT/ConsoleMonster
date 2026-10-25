using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ConsoleMonster.Generators {
    /// <summary>
    /// Writes the fluent methods of every public widget (a class implementing <c>ConsoleMonster.Layout.IWidget</c>) and of
    /// the option types they hold (column definitions, items, presentation options) from their public properties:
    /// <list type="bullet">
    /// <item><c>X(value)</c> for every settable property (<c>X()</c> sets a bool to true);</item>
    /// <item><c>X(Action&lt;T&gt;)</c> for a property holding an option object, configuring it in place;</item>
    /// <item><c>AddX(item)</c>, <c>AddXs(params items)</c> and <c>AddXs(IEnumerable)</c> for every mutable collection.</item>
    /// </list>
    /// The methods are extension methods in the widget's namespace, return the same instance (typed as the receiver, so a
    /// chain on a derived type keeps that type) and use no reflection. Members marked <c>[NoFluent]</c> are skipped.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class FluentGenerator : IIncrementalGenerator {
        private const string WidgetInterface = "ConsoleMonster.Layout.IWidget";
        private const string NoFluentAttribute = "ConsoleMonster.NoFluentAttribute";

        private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context) {
            context.RegisterSourceOutput(context.CompilationProvider, (output, compilation) => {
                if (compilation.AssemblyName != "ConsoleMonster" && compilation.AssemblyName != "ConsoleMonster.Graphics" && compilation.AssemblyName != "ConsoleMonster.Markdown" && compilation.AssemblyName != "ConsoleMonster.PowerShell" && compilation.AssemblyName != "ConsoleMonster.Hosting" && compilation.AssemblyName != "ConsoleMonster.Extensions.Logging") {
                    return;
                }
                INamedTypeSymbol? widget = compilation.GetTypeByMetadataName(WidgetInterface);
                if (widget == null) {
                    return;
                }
                foreach (INamedTypeSymbol type in FluentTypes(compilation.Assembly, widget)) {
                    string? source = Generate(type, compilation);
                    if (source != null) {
                        output.AddSource(type.ToDisplayString().Replace("ConsoleMonster.", string.Empty).Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_') + ".Fluent.g.cs", source);
                    }
                }
            });
        }

        /// <summary>The widgets, then the option types reachable from their properties (with cycle detection).</summary>
        internal static List<INamedTypeSymbol> FluentTypes(IAssemblySymbol assembly, INamedTypeSymbol widget) {
            List<INamedTypeSymbol> widgets = AllTypes(assembly.GlobalNamespace)
                .Where(t => IsCandidate(t) && t.AllInterfaces.Contains(widget, SymbolEqualityComparer.Default))
                .OrderBy(t => t.ToDisplayString(), StringComparer.Ordinal)
                .ToList();
            foreach (string name in new[] { "ConsoleMonster.Interactive.MenuItem", "ConsoleMonster.Interactive.MenuRunResult", "ConsoleMonster.Interactive.ReviewModel", "ConsoleMonster.Interactive.FormPreset", "ConsoleMonster.Interactive.MenuPreset", "ConsoleMonster.Interactive.ObjectExplorer", "ConsoleMonster.Interactive.Grid", "ConsoleMonster.Interactive.SelectQuestionOptions", "ConsoleMonster.Interactive.SelectInputOptions", "ConsoleMonster.Interactive.TextInputOptions", "ConsoleMonster.Interactive.TextQuestionOptions", "ConsoleMonster.Widgets.ProgressOptions", "ConsoleMonster.Widgets.ChartOptions", "ConsoleMonster.Live.WidgetUpdate", "ConsoleMonster.Live.RunUpdate", "ConsoleMonster.Live.LiveRunOptions", "ConsoleMonster.Live.LiveSessionOptions", "ConsoleMonster.Export.ExportOptions", "ConsoleMonster.Export.RecordingExportOptions" }) {
                INamedTypeSymbol? option = AllTypes(assembly.GlobalNamespace).FirstOrDefault(t => t.ToDisplayString() == name);
                if (option != null && IsCandidate(option)) { widgets.Add(option); }
            }
            // Standalone operation options are roots too, including the add-ons' own options.
            foreach (INamedTypeSymbol option in AllTypes(assembly.GlobalNamespace).Where(t => IsCandidate(t) && t.Name.EndsWith("Options", StringComparison.Ordinal))) {
                if (!widgets.Contains(option, SymbolEqualityComparer.Default)) { widgets.Add(option); }
            }
            foreach (INamedTypeSymbol prompt in AllTypes(assembly.GlobalNamespace).Where(t => IsCandidate(t) && IsPrompt(t))) {
                if (!widgets.Contains(prompt, SymbolEqualityComparer.Default)) { widgets.Add(prompt); }
            }
            HashSet<INamedTypeSymbol> seen = new HashSet<INamedTypeSymbol>(widgets, SymbolEqualityComparer.Default);
            List<INamedTypeSymbol> result = new List<INamedTypeSymbol>(widgets);
            List<INamedTypeSymbol> frontier = widgets;
            while (frontier.Count > 0) {
                List<INamedTypeSymbol> next = new List<INamedTypeSymbol>();
                foreach (INamedTypeSymbol type in frontier) {
                    foreach (IPropertySymbol property in Properties(type)) {
                        foreach (ITypeSymbol? reached in new[] { property.Type, CollectionItem(property.Type) }) {
                            if (reached is INamedTypeSymbol annotated && annotated.OriginalDefinition is INamedTypeSymbol named && IsOptionType(named, assembly) && seen.Add(named)) {
                                next.Add(named);
                            }
                        }
                    }
                }
                foreach (INamedTypeSymbol type in frontier) {
                    // Items added through methods (Table.AddColumn(TableColumn)) are configured fluently too.
                    foreach (IMethodSymbol method in type.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary && !m.IsStatic && m.DeclaredAccessibility == Accessibility.Public
                        && (m.Name.StartsWith("Add", StringComparison.Ordinal) || m.Name.StartsWith("Set", StringComparison.Ordinal)))) {
                        foreach (IParameterSymbol parameter in method.Parameters) {
                            if (parameter.Type.OriginalDefinition is INamedTypeSymbol named && IsOptionType(named, assembly) && seen.Add(named)) {
                                next.Add(named);
                            }
                        }
                    }
                }
                next.Sort((a, b) => string.CompareOrdinal(a.ToDisplayString(), b.ToDisplayString()));
                result.AddRange(next);
                frontier = next;
            }
            return result;
        }

        private static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceSymbol ns) {
            foreach (INamespaceOrTypeSymbol member in ns.GetMembers()) {
                if (member is INamespaceSymbol child) {
                    foreach (INamedTypeSymbol type in AllTypes(child)) {
                        yield return type;
                    }
                } else if (member is INamedTypeSymbol type) {
                    yield return type;
                }
            }
        }

        private static bool IsPrompt(INamedTypeSymbol type) {
            for (INamedTypeSymbol? current = type.BaseType; current != null; current = current.BaseType) {
                if (current.OriginalDefinition.ToDisplayString() == "ConsoleMonster.Interactive.Prompt<T>") { return true; }
            }
            return false;
        }

        private static bool IsCandidate(INamedTypeSymbol type) =>
            type.TypeKind == TypeKind.Class && !type.IsAbstract && !type.IsStatic && (!type.IsGenericType || type.IsSealed && IsPrompt(type))
            && type.DeclaredAccessibility == Accessibility.Public && type.ContainingType == null && !HasNoFluent(type);

        // A public class of this assembly with something to set or add.
        private static bool IsOptionType(INamedTypeSymbol type, IAssemblySymbol assembly) =>
            SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, assembly) && IsCandidate(type)
            && type.TypeKind != TypeKind.Delegate && Properties(type).Any(p => IsSettable(p) || CollectionItem(p.Type) != null);

        /// <summary>Public instance properties, declared or inherited, without indexers or [NoFluent] members.</summary>
        internal static IEnumerable<IPropertySymbol> Properties(INamedTypeSymbol type) {
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            for (INamedTypeSymbol? current = type; current != null && current.SpecialType != SpecialType.System_Object; current = current.BaseType) {
                foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>()) {
                    if (property.IsStatic || property.IsIndexer || property.DeclaredAccessibility != Accessibility.Public || HasNoFluent(property)
                        || property.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute") || !names.Add(property.Name)) {
                        continue;
                    }
                    yield return property;
                }
            }
        }

        internal static bool IsSettable(IPropertySymbol property) =>
            property.SetMethod != null && property.SetMethod.DeclaredAccessibility == Accessibility.Public && !property.SetMethod.IsInitOnly;

        /// <summary>The item type of a mutable collection (ICollection&lt;T&gt;, not a dictionary, array or string), or null.</summary>
        internal static ITypeSymbol? CollectionItem(ITypeSymbol type) {
            if (type.SpecialType == SpecialType.System_String || type is IArrayTypeSymbol) {
                return null;
            }
            IEnumerable<INamedTypeSymbol> interfaces = type is INamedTypeSymbol named && named.TypeKind == TypeKind.Interface ? new[] { named }.Concat(type.AllInterfaces) : type.AllInterfaces;
            INamedTypeSymbol? collection = null;
            foreach (INamedTypeSymbol candidate in interfaces) {
                string definition = candidate.OriginalDefinition.ToDisplayString();
                if (definition == "System.Collections.Generic.IDictionary<TKey, TValue>") {
                    return null;
                }
                if (definition == "System.Collections.Generic.ICollection<T>") {
                    collection = candidate;
                }
            }
            return collection?.TypeArguments[0];
        }

        private static bool HasNoFluent(ISymbol symbol) => symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == NoFluentAttribute);

        private static string? Generate(INamedTypeSymbol type, Compilation compilation) {
            string typeName = type.ToDisplayString(TypeFormat);
            string docId = type.GetDocumentationCommentId() ?? type.Name;
            bool open = !type.IsSealed;
            // A type that can be derived from gets generic methods (TSelf : Type), so a chain keeps the derived type.
            string self = open ? "TSelf" : typeName;
            string generic = open ? "<TSelf>" : type.TypeParameters.Length == 0 ? string.Empty : "<" + string.Join(", ", type.TypeParameters.Select(parameter => parameter.Name)) + ">";
            string constraint = open ? " where TSelf : " + typeName : string.Empty;
            string receiver = Camel(type.Name);
            // Compare complete value-parameter signatures, including inherited instance methods.
            var existing = new List<IMethodSymbol>();
            for (INamedTypeSymbol? current = type; current != null; current = current.BaseType) { existing.AddRange(current.GetMembers().OfType<IMethodSymbol>()); }
            INamedTypeSymbol? handWritten = compilation.GetTypeByMetadataName(type.ContainingNamespace.ToDisplayString() + "." + type.Name + "Fluent");
            if (handWritten != null) { existing.AddRange(handWritten.GetMembers().OfType<IMethodSymbol>()); }
            bool Taken(string name, params ITypeSymbol[] parameters) => existing.Any(m => {
                int offset = m.IsExtensionMethod ? 1 : 0;
                return m.Name == name && m.Parameters.Length == parameters.Length + offset
                    && parameters.Select((parameter, index) => SymbolEqualityComparer.Default.Equals(parameter, m.Parameters[index + offset].Type)).All(equal => equal);
            });
            HashSet<string> generated = new HashSet<string>(StringComparer.Ordinal);
            StringBuilder body = new StringBuilder();
            int methods = 0;
            bool ownsContent = type.AllInterfaces.Any(i => i.ToDisplayString() == "ConsoleMonster.Layout.IContentOwner");
            if (ownsContent) {
                foreach (string parameterType in new[] { "global::ConsoleMonster.Layout.IWidget", "global::System.Action<global::ConsoleMonster.Layout.ContentBuilder>" }) {
                    body.Append(Doc("Configures this container's content before session ownership.", receiver, "The widget or nested content builder.", "content"));
                    body.Append("        public static ").Append(self).Append(" Content").Append(generic).Append("(this ").Append(self).Append(' ').Append(receiver)
                        .Append(", ").Append(parameterType).Append(" content)").Append(constraint).Append(" {\n")
                        .Append("            global::ConsoleMonster.Layout.ContentBuilder.Replace(").Append(receiver).Append(", content);\n")
                        .Append("            return ").Append(receiver).Append(";\n        }\n\n");
                    methods++;
                }
            }
            foreach (IPropertySymbol property in Properties(type)) {
                if (ownsContent && property.Name == "Content") { continue; }
                // An inherited property of a base that can be derived from is covered by the base's generic methods;
                // generating it again would make every call ambiguous.
                if (!SymbolEqualityComparer.Default.Equals(property.ContainingType, type) && !property.ContainingType.IsSealed && IsCandidate(property.ContainingType)
                    && SymbolEqualityComparer.Default.Equals(property.ContainingType.ContainingAssembly, compilation.Assembly)) {
                    continue;
                }
                string propertyType = property.Type.ToDisplayString(TypeFormat);
                string propertyDoc = property.GetDocumentationCommentId() ?? property.Name;
                string summary = Summary(property);
                string with = property.Name;
                ITypeSymbol? item = CollectionItem(property.Type);
                if (IsSettable(property) && !Taken(with, property.Type) && generated.Add(with + "(" + propertyType + ")")) {
                    bool flag = property.Type.SpecialType == SpecialType.System_Boolean;
                    body.Append(Doc("Sets <see cref=\"" + propertyDoc + "\"/>" + summary, receiver, flag ? "The value (default true)." : "The value.", "value"));
                    if (property.Type.TypeKind != TypeKind.Delegate) { body.Append(PropertyExample(type, property, receiver)); }
                    body.Append("        public static ").Append(self).Append(' ').Append(with).Append(generic).Append("(this ").Append(self).Append(' ').Append(receiver)
                        .Append(", ").Append(propertyType).Append(" value").Append(flag ? " = true" : string.Empty).Append(')').Append(constraint).Append(" {\n")
                        .Append("            ").Append(receiver).Append('.').Append(property.Name).Append(" = value;\n")
                        .Append("            return ").Append(receiver).Append(";\n        }\n\n");
                    methods++;
                }
                if (property.Type is INamedTypeSymbol option && IsOptionType(option, compilation.Assembly) && item == null && !Taken(with, compilation.GetTypeByMetadataName("System.Action`1")!.Construct(option)) && generated.Add(with + "(Action)")) {
                    body.Append(Configure(property, option, receiver, self, generic, constraint, with, propertyDoc, summary));
                    methods++;
                }
                if (item != null) {
                    string singular = Singular(property.Name);
                    string plural = property.Name;
                    string itemType = item.ToDisplayString(TypeFormat);
                    string add = "Add" + singular;
                    string addAll = "Add" + plural;
                    if (!Taken(add, item) && generated.Add(add + "(" + itemType + ")")) {
                        body.Append(Doc("Adds an item to <see cref=\"" + propertyDoc + "\"/>" + summary, receiver, "The item.", "item"));
                        body.Append("        public static ").Append(self).Append(' ').Append(add).Append(generic).Append("(this ").Append(self).Append(' ').Append(receiver)
                            .Append(", ").Append(itemType).Append(" item)").Append(constraint).Append(" {\n")
                            .Append(Ensure(property, receiver, itemType))
                            .Append("            ").Append(receiver).Append('.').Append(property.Name).Append(".Add(item);\n")
                            .Append("            return ").Append(receiver).Append(";\n        }\n\n");
                        methods++;
                    }
                    if (!Taken(addAll, compilation.CreateArrayTypeSymbol(item)) && generated.Add(addAll + "(params " + itemType + ")")) {
                        body.Append(Doc("Adds items to <see cref=\"" + propertyDoc + "\"/>" + summary, receiver, "The items, in order.", "items"));
                        body.Append("        public static ").Append(self).Append(' ').Append(addAll).Append(generic).Append("(this ").Append(self).Append(' ').Append(receiver)
                            .Append(", params ").Append(itemType).Append("[] items)").Append(constraint).Append(" {\n")
                            .Append("            if (items == null) { throw new global::System.ArgumentNullException(nameof(items)); }\n")
                            .Append(Ensure(property, receiver, itemType))
                            .Append("            foreach (var item in items) { ").Append(receiver).Append('.').Append(property.Name).Append(".Add(item); }\n")
                            .Append("            return ").Append(receiver).Append(";\n        }\n\n");
                        methods++;
                    }
                    if (!Taken(addAll, compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1")!.Construct(item)) && generated.Add(addAll + "(IEnumerable<" + itemType + ">)")) {
                        body.Append(Doc("Adds items to <see cref=\"" + propertyDoc + "\"/>" + summary, receiver, "The items, in order.", "items"));
                        body.Append("        public static ").Append(self).Append(' ').Append(addAll).Append(generic).Append("(this ").Append(self).Append(' ').Append(receiver)
                            .Append(", global::System.Collections.Generic.IEnumerable<").Append(itemType).Append("> items)").Append(constraint).Append(" {\n")
                            .Append("            if (items == null) { throw new global::System.ArgumentNullException(nameof(items)); }\n")
                            .Append(Ensure(property, receiver, itemType))
                            .Append("            foreach (").Append(itemType).Append(" item in items) {\n")
                            .Append("                ").Append(receiver).Append('.').Append(property.Name).Append(".Add(item);\n            }\n")
                            .Append("            return ").Append(receiver).Append(";\n        }\n\n");
                        methods++;
                    }
                }
            }
            if (methods == 0) {
                return null;
            }
            StringBuilder source = new StringBuilder();
            source.Append("// <auto-generated>\n// Generated by ConsoleMonster.Generators.FluentGenerator from the public properties of ")
                .Append(type.ToDisplayString()).Append(".\n// Do not edit: change the properties instead.\n// </auto-generated>\n")
                .Append("#nullable enable\n\n")
                .Append("namespace ").Append(type.ContainingNamespace.ToDisplayString()).Append(" {\n")
                .Append("    /// <summary>The fluent methods of <see cref=\"").Append(docId).Append("\"/>, generated from its public properties: A property-named method sets a property and <c>Add...</c> adds to a collection, both returning the same instance.</summary>\n")
                .Append("    [global::System.CodeDom.Compiler.GeneratedCode(\"ConsoleMonster.Generators\", \"1.0.0\")]\n")
                .Append("    public static partial class ").Append(type.Name).Append("Fluent {\n")
                .Append(body.ToString().TrimEnd('\n')).Append('\n')
                .Append("    }\n}\n");
            return source.ToString();
        }

        // X(Action<T>): configures the option object in place, creating it first when it is not set and can be.
        private static string Configure(IPropertySymbol property, INamedTypeSymbol option, string receiver, string self, string generic, string constraint, string with, string propertyDoc, string summary) {
            string optionType = option.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(TypeFormat);
            bool canCreate = IsSettable(property) && option.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
            StringBuilder text = new StringBuilder();
            text.Append(Doc("Configures <see cref=\"" + propertyDoc + "\"/> in place" + (canCreate ? " (creating it when not set)" : string.Empty) + summary, receiver, "Configures the value.", "configure"));
            text.Append("        public static ").Append(self).Append(' ').Append(with).Append(generic).Append("(this ").Append(self).Append(' ').Append(receiver)
                .Append(", global::System.Action<").Append(optionType).Append("> configure)").Append(constraint).Append(" {\n")
                .Append("            if (configure == null) { throw new global::System.ArgumentNullException(nameof(configure)); }\n")
                .Append("            ").Append(optionType).Append(" value = ").Append(receiver).Append('.').Append(property.Name).Append(" ?? ")
                .Append(canCreate ? "new " + optionType + "()" : "throw new global::System.InvalidOperationException(\"" + property.Name + " is not set.\")").Append(";\n")
                .Append("            configure(value);\n");
            if (IsSettable(property)) {
                text.Append("            ").Append(receiver).Append('.').Append(property.Name).Append(" = value;\n");
            }
            text.Append("            return ").Append(receiver).Append(";\n        }\n\n");
            return text.ToString();
        }

        // A nullable collection property is created on the first Add when a List<T> fits it; otherwise Add needs it set.
        private static string Ensure(IPropertySymbol property, string receiver, string itemType) {
            if (property.Type.NullableAnnotation != NullableAnnotation.Annotated) {
                return string.Empty;
            }
            string definition = property.Type.OriginalDefinition.ToDisplayString();
            bool fits = definition == "System.Collections.Generic.IList<T>" || definition == "System.Collections.Generic.ICollection<T>" || definition == "System.Collections.Generic.List<T>";
            if (IsSettable(property) && fits) {
                return "            " + receiver + "." + property.Name + " ??= new global::System.Collections.Generic.List<" + itemType + ">();\n";
            }
            return "            if (" + receiver + "." + property.Name + " == null) { throw new global::System.InvalidOperationException(\"" + property.Name + " is not set.\"); }\n";
        }

        /// <summary>Shows both configuration forms in the generated API reference, without assuming a parameterless constructor.</summary>
        private static string PropertyExample(INamedTypeSymbol type, IPropertySymbol property, string receiver) {
            bool constructible = type.InstanceConstructors.Any(ctor => ctor.DeclaredAccessibility == Accessibility.Public && ctor.Parameters.All(parameter => parameter.IsOptional));
            string creation = "new " + type.ToDisplayString(TypeFormat);
            string initializer = constructible ? creation + " { " + property.Name + " = value }" : receiver + "." + property.Name + " = value;";
            string fluent = (constructible ? creation + "()" : receiver) + "." + property.Name + "(value)";
            return "        /// <example><list type=\"table\"><listheader><term>C# initializer or property</term><description>C# fluent</description></listheader>\n"
                + "        /// <item><term><code>" + new XText(initializer) + "</code></term><description><code>" + new XText(fluent) + "</code></description></item></list></example>\n";
        }

        private static string Doc(string summary, string receiver, string parameter, string parameterName) {
            return "        /// <summary>" + summary + "</summary>\n"
                + "        /// <param name=\"" + receiver.TrimStart('@') + "\">The instance.</param>\n"
                + "        /// <param name=\"" + parameterName + "\">" + parameter + "</param>\n"
                + "        /// <returns>The same instance.</returns>\n";
        }

        // ": " and the property's own summary (crefs are already resolved to IDs; parameter references become code), or "."
        private static string Summary(IPropertySymbol property) {
            string? xml = property.GetDocumentationCommentXml();
            if (string.IsNullOrWhiteSpace(xml)) {
                return ".";
            }
            try {
                XElement? summary = XDocument.Parse(xml).Descendants("summary").FirstOrDefault();
                if (summary == null) {
                    return ".";
                }
                foreach (XElement reference in summary.Descendants().Where(e => e.Name.LocalName == "paramref" || e.Name.LocalName == "typeparamref").ToList()) {
                    reference.ReplaceWith(new XElement("c", (string?)reference.Attribute("name") ?? string.Empty));
                }
                string inner = string.Concat(summary.Nodes().Select(n => n.ToString(SaveOptions.DisableFormatting)));
                inner = string.Join(" ", inner.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim())).Trim();
                return inner.Length == 0 ? "." : ": " + inner;
            } catch (System.Xml.XmlException) {
                return ".";
            }
        }

        private static string Camel(string name) {
            string camel = char.ToLowerInvariant(name[0]) + name.Substring(1);
            return SyntaxFacts.GetKeywordKind(camel) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(camel) != SyntaxKind.None ? "@" + camel : camel;
        }

        /// <summary>The singular of a collection property name: Items -> Item, Children -> Child, Series -> Series.</summary>
        internal static string Singular(string name) {
            if (name.EndsWith("Children", StringComparison.Ordinal)) {
                return name.Substring(0, name.Length - 3);
            }
            if (name.EndsWith("Series", StringComparison.Ordinal) || name.EndsWith("ss", StringComparison.Ordinal) || !name.EndsWith("s", StringComparison.Ordinal)) {
                return name;
            }
            if (name.EndsWith("ies", StringComparison.Ordinal)) {
                return name.Substring(0, name.Length - 3) + "y";
            }
            if (name.EndsWith("ches", StringComparison.Ordinal) || name.EndsWith("shes", StringComparison.Ordinal) || name.EndsWith("sses", StringComparison.Ordinal) || name.EndsWith("xes", StringComparison.Ordinal)) {
                return name.Substring(0, name.Length - 2);
            }
            return name.Substring(0, name.Length - 1);
        }
    }
}
