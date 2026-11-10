using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        // A second semantic pass sees the already-translated setters and containers.
        // Factory creation remains an authoring transformation, never a runtime adapter.
        private sealed partial class FluentFactoryRewriter(SemanticModel model) : CSharpSyntaxRewriter {
            public HashSet<string> Namespaces { get; } = new(StringComparer.Ordinal);
            private static readonly Dictionary<string, string> Names = new(StringComparer.Ordinal) {
                ["ViewFacts"] = "Facts", ["MarkdownView"] = "Markdown", ["TextBlock"] = "Text", ["KeyValueList"] = "KeyValue", ["RegionWidget"] = "Region", ["AppBinding"] = "Binding",
                ["AppCommand"] = "Command", ["Grid"] = "LayoutGrid", ["RichTextBuilder"] = "RichText", ["ReviewModel"] = "Review"
            };
            public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) => Rewrite(node, (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node)!);
            public override SyntaxNode? VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) {
                var visited = (ImplicitObjectCreationExpressionSyntax)base.VisitImplicitObjectCreationExpression(node)!;
                if (model.GetTypeInfo(node).Type is not INamedTypeSymbol type || !type.ContainingAssembly.Name.StartsWith("ConsoleMonster", StringComparison.Ordinal)) { return visited; }
                return Rewrite(node, SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)), visited.ArgumentList, visited.Initializer));
            }
            private ExpressionSyntax Rewrite(BaseObjectCreationExpressionSyntax original, ObjectCreationExpressionSyntax visited, bool stageArguments = true) {
                if (HasInitializerBoundaryTrivia(original.Initializer)) { return visited; }
                // Moving a declaration into Content or an argument-staging lambda would
                // change its caller scope. Keep this uncommon authoring form intact.
                if (HasArgumentDeclarations(original)) { return visited; }
                if (model.GetTypeInfo(original).Type is not INamedTypeSymbol type || !type.ContainingAssembly.Name.StartsWith("ConsoleMonster", StringComparison.Ordinal)
                    || model.GetSymbolInfo(original).Symbol is not IMethodSymbol ctor) { return original is ImplicitObjectCreationExpressionSyntax ? (ExpressionSyntax)original : visited; }
                if (type.ToDisplayString() == "ConsoleMonster.Layout.Sized"
                    && original.ArgumentList?.Arguments.Count == 2
                    && original.ArgumentList.Arguments[1].Expression is InvocationExpressionSyntax size
                    && model.GetSymbolInfo(size).Symbol is IMethodSymbol sizing
                    && sizing.ContainingType.ToDisplayString() == "ConsoleMonster.Layout.LayoutSize"
                    && sizing.Name is "Fixed" or "Fraction"
                    && visited.ArgumentList!.Arguments[1].Expression is InvocationExpressionSyntax rewrittenSize) {
                    ExpressionSyntax widget = visited.ArgumentList.Arguments[0].Expression;
                    if (widget is ConditionalExpressionSyntax or BinaryExpressionSyntax or AssignmentExpressionSyntax) { widget = SyntaxFactory.ParenthesizedExpression(widget); }
                    Namespaces.Add("ConsoleMonster");
                    return Call(widget, sizing.Name == "Fixed" ? "Fixed" : "Fill", rewrittenSize.ArgumentList.Arguments[0].Expression).WithTriviaFrom(original);
                }
                if (type.Name == "LiveSession") {
                    ExpressionSyntax root = visited.ArgumentList!.Arguments[0].Expression;
                    if (root is ConditionalExpressionSyntax or BinaryExpressionSyntax or AssignmentExpressionSyntax) { root = SyntaxFactory.ParenthesizedExpression(root); }
                    return Call(root, "CreateSession", visited.ArgumentList.Arguments.Skip(1).Select(argument => argument.Expression).ToArray()).WithTriviaFrom(original);
                }
                string noun = Names.GetValueOrDefault(type.Name, type.Name);
                if (type.ToDisplayString() == "ConsoleMonster.Interactive.Grid") { noun = "Grid"; }
                string? chartType = type.Name == "Histogram" ? "Histogram" : null;
                bool progress = false;
                if (type.Name == "Histogram") { noun = "Chart"; }

                bool options = type.Name.EndsWith("Options", StringComparison.Ordinal);
                bool input = type.Name is "TextInput" or "Select" or "MultiSelect" or "Toggle" or "Button" or "Collapsible" or "Outline" or "Breadcrumb" or "ChartView" or "DataGrid" or "DatePicker" or "TreeSelect";
                if (type.Name == "MultiSelect") { noun = "Select"; }
                string? question = QuestionNoun(type);
                if (question != null) { noun = question; }
                INamedTypeSymbol ui = model.Compilation.GetTypeByMetadataName("ConsoleMonster.Ui")!;
                IMethodSymbol? factory = ui.GetMembers(noun).OfType<IMethodSymbol>().FirstOrDefault(m => (question != null ? m.Arity == 0 : m.Arity == type.Arity) && m.Parameters.All(p => p.IsOptional));
                if (!options && factory == null) { return visited; } // data records retain ordinary C# construction
                Namespaces.Add(type.ContainingNamespace.ToDisplayString());
                if (factory != null) { Namespaces.Add(factory.ReturnType.ContainingNamespace.ToDisplayString()); }
                if (stageArguments && NeedsArgumentStaging(original, type, ctor, buildDefinition: input || progress || chartType != null || type.Name is "AppBinding" or "AppCommand")) {
                    return StageArguments(original, visited, type, ctor);
                }
                string typeArgs = type.IsGenericType && question == null ? "<" + string.Join(", ", type.TypeArguments.Select(t => t.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))) + ">" : string.Empty;
                ExpressionSyntax result = SyntaxFactory.ParseExpression(options ? "new " + type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) + "()" : "Ui." + noun + typeArgs + "()");
                if (chartType != null) { result = Call(result, "Type", SyntaxFactory.ParseExpression("ConsoleMonster.Widgets.ChartType." + chartType)); }

                if (question != null && QuestionVariant(type) is string variant) { result = Call(result, variant); }
                bool build = input || progress || chartType != null || type.Name is "AppBinding" or "AppCommand";
                var contentParameters = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < (visited.ArgumentList?.Arguments.Count ?? 0); i++) {
                    ArgumentSyntax arg = original.ArgumentList!.Arguments[i];
                    IParameterSymbol p = BoundParameter(ctor, arg, i);
                    ExpressionSyntax value = visited.ArgumentList!.Arguments[i].Expression;
                    string member = char.ToUpperInvariant(p.Name[0]) + p.Name.Substring(1);
                    // The primary input of a factory goes in its call: Ui.Region(widgetId), Ui.LogViewport(key), Ui.DashboardFrame(title).
                    if ((p.Name == "widgetId" || (p.Name == "key" && type.Name == "LogViewport") || (p.Name == "title" && type.Name == "DashboardFrame")) && !build) {
                        var primary = RootFactory(result)!;
                        result = result.ReplaceNode(primary, primary.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(value)))));
                        continue;
                    }
                    if (p.Name is "content" or "children" or "layers" && type.AllInterfaces.Any(contract => contract.ToDisplayString() == "ConsoleMonster.Layout.IContentOwner")) {
                        if (contentParameters.Add(p.Name)) { result = Call(result, "Content", ConstructorContents(original, visited, ctor, p)); }
                        continue;
                    }
                    if (type.Name == "TextBlock" && p.Name == "text") { result = SyntaxFactory.ParseExpression("Ui.Text(" + value + ")"); continue; }
                    if (type.Name == "Grid" && noun == "LayoutGrid" && p.Name == "columns") { member = "Columns"; }
                    if (type.Name == "Logo" && p.Name == "text") { member = "Text"; }
                    if (type.Name == "AppBinding" && p.Name == "content" || type.Name == "AppCommand" && p.Name == "action") {
                        result = Call(result, "Configure", SyntaxFactory.ParseExpression(Callback(member, value))); continue;
                    }
                    if ((type.Name is "SelectPrompt" or "MultiSelectPrompt") && p.Name is "values" or "choices") {
                        if (p.Name == "values") {
                            string display = BoundArgument(original, visited, ctor, "display")?.ToString() ?? "null";
                            string group = BoundArgument(original, visited, ctor, "group")?.ToString() ?? "null";
                            value = SyntaxFactory.ParseExpression("ConsoleMonster.Interactive.Choice.From(" + value + ", " + display + ", " + group + ")");
                        }
                        member = "AddChoices";
                    }
                    if ((type.Name is "SelectPrompt" or "MultiSelectPrompt") && p.Name is "display" or "group") { continue; }
                    if (type.Name == "Calendar" && p.Name is "marks" or "data") {
                        member = "Marks";
                        if (model.GetTypeInfo(arg.Expression).Type == null) {
                            var format = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
                            value = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(p.Type.ToDisplayString(format)), SyntaxFactory.ParenthesizedExpression(value));
                        }
                    }
                    if (type.Name is "Select" or "MultiSelect" && p.Name == "choices") { member = "AddChoices"; }
                    if (type.Name == "ConfirmPrompt" && p.Name == "defaultValue") { member = "Default"; }
                    if (type.Name == "Panel" && p.Name == "title" && p.Type.ToDisplayString().Contains("RichText", StringComparison.Ordinal)
                        || type.Name == "Rule" && p.Type.ToDisplayString().Contains("RichText", StringComparison.Ordinal)) { value = Call(value, "ToMarkup"); }
                    // A built input control in a pane (a splitter's First or Second) goes through the pane's content builder,
                    // the way a person writes it: .First(c => c.DataGrid(grid => ...)).
                    if (EndsWithBuild(value) && HasContentOverload(type, member)) {
                        value = ConstructorContent(p, value, false, false, LambdaName("c", original, value));
                    }
                    result = Call(result, member, value);
                }
                if (type.Name == "MultiSelect") { result = Call(result, "Multiple"); }
                if (build) {
                    result = Call(result, "Build");
                    if (progress || chartType != null) {
                        var built = (InvocationExpressionSyntax)result;
                        result = built.WithExpression(((MemberAccessExpressionSyntax)built.Expression).WithName(SyntaxFactory.GenericName("Build")
                            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))))));
                    }
                    if (input && type.Name != "Collapsible") { result = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)), result)); }
                }
                if (visited.Initializer != null) {
                    for (int i = 0; i < visited.Initializer.Expressions.Count; i++) {
                        if (original.Initializer!.Expressions[i] is not AssignmentExpressionSyntax originalAssignment
                            || model.GetSymbolInfo(originalAssignment.Left).Symbol is not IPropertySymbol property
                            || visited.Initializer.Expressions[i] is not AssignmentExpressionSyntax assignment) { return visited; }
                        if (assignment.Right is InitializerExpressionSyntax collection) {
                            INamedTypeSymbol? fluent = model.Compilation.GetTypeByMetadataName(type.ContainingNamespace.ToDisplayString() + "." + type.ContainingNamespace.Name + "Fluent");
                            ITypeSymbol? item = (property.Type as INamedTypeSymbol)?.AllInterfaces.Prepend(property.Type as INamedTypeSymbol).FirstOrDefault(t => t?.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.ICollection<T>")?.TypeArguments[0];
                            // Two collections can share an item type (a breadcrumb's segments and ancestors), so the adder
                            // named after this property wins over any other adder of the same item type.
                            IMethodSymbol? add = fluent?.GetMembers().OfType<IMethodSymbol>().Where(method => method.IsExtensionMethod)
                                .Select(method => method.ReduceExtensionMethod(type)).Where(method => method != null && method.Name.StartsWith("Add", StringComparison.Ordinal)
                                    && method.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, item))
                                .OrderBy(method => property.Name.StartsWith(method!.Name.Substring(3), StringComparison.Ordinal) ? 0 : 1).FirstOrDefault();
                            if (add == null || collection.Expressions.Any(value => value is InitializerExpressionSyntax)) { return visited; }
                            foreach (ExpressionSyntax value in collection.Expressions) { result = Call(result, add.Name, value); }
                        } else {
                            ExpressionSyntax value = property.Name == "Preview" && question == "SelectQuestion"
                                ? QuestionPreview(type, assignment.Right, originalAssignment) : assignment.Right;
                            result = property.Type.TypeKind == TypeKind.Delegate
                                ? Call(result, "Configure", SyntaxFactory.ParseExpression(Callback(property.Name, value)))
                                : Call(result, FluentSourceRewriter.FluentName(property), value);
                        }
                    }
                }
                return result.WithTriviaFrom(original);
            }
            private string Callback(string member, ExpressionSyntax value) {
                string name = Unique("definition", value);
                return name + " => " + name + "." + member + " = " + value;
            }

            private string Unique(string stem, params SyntaxNode[] scope) {
                var names = model.SyntaxTree.GetRoot().DescendantTokens().Concat(scope.SelectMany(node => node.DescendantTokens()))
                    .Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.ValueText).ToHashSet(StringComparer.Ordinal);
                string basis = BuilderParameter(stem);
                string name = basis;
                for (int suffix = 2; names.Contains(name); suffix++) { name = basis + suffix; }
                return name;
            }

            public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node) {
                var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
                if (QuestionShow(node, visited) is ExpressionSyntax typedShow) { return typedShow.WithTriviaFrom(node); }
                if (AppInputDefinition(node, visited) is ExpressionSyntax definitionBuild) { return definitionBuild.WithTriviaFrom(node); }
                if (node.Expression is MemberAccessExpressionSyntax originalSetter && visited.Expression is MemberAccessExpressionSyntax setter
                    && model.GetSymbolInfo(node).Symbol is IMethodSymbol setterMethod && setterMethod.Name.EndsWith(originalSetter.Name.Identifier.ValueText, StringComparison.Ordinal)
                    && (setterMethod.ReducedFrom ?? setterMethod).ContainingType.Name.EndsWith("Fluent", StringComparison.Ordinal)
                    && visited.ArgumentList.Arguments.All(argument => argument.Expression is not AnonymousFunctionExpressionSyntax)) {
                    ExpressionSyntax receiver = setter.Expression;
                    ExpressionSyntax unwrappedReceiver = receiver is ParenthesizedExpressionSyntax { Expression: CastExpressionSyntax receiverCast } ? receiverCast.Expression : receiver;
                    if (unwrappedReceiver is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Build" } buildMember } build
                        && RootFactory(buildMember.Expression)?.Expression is MemberAccessExpressionSyntax rootMember) {
                        ITypeSymbol? definitionType = model.Compilation.GetTypeByMetadataName("ConsoleMonster.Ui")!.GetMembers(rootMember.Name.Identifier.ValueText).OfType<IMethodSymbol>().FirstOrDefault()?.ReturnType;
                        if (definitionType is INamedTypeSymbol configurationType && configurationType.Name.EndsWith("Options", StringComparison.Ordinal)
                            && FindProperty(configurationType, originalSetter.Name.Identifier.ValueText) is IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Public } property
                            && property.Type.TypeKind != TypeKind.Delegate) {
                            ExpressionSyntax configured = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, buildMember.Expression, setter.Name), visited.ArgumentList);
                            return receiver.ReplaceNode(build, build.WithExpression(buildMember.WithExpression(configured))).WithTriviaFrom(node);
                        }
                    }
                }
                if (model.GetSymbolInfo(node).Symbol is IMethodSymbol { IsStatic: true } method) {
                    if (method.ContainingType.ToDisplayString() == "ConsoleMonster.Widgets.Table" && method.Name == "For") {
                        return SyntaxFactory.ParseExpression("Ui.Table(" + string.Join(", ", visited.ArgumentList.Arguments) + ")").WithTriviaFrom(node);
                    }
                    if (method.ContainingType.ToDisplayString() == "ConsoleMonster.Widgets.TextBlock" && method.Name is "Plain" or "FromMarkup") {
                        ExpressionSyntax text = SyntaxFactory.ParseExpression("Ui." + (method.Name == "Plain" ? "Text" : "Markup") + "(" + visited.ArgumentList.Arguments[0].Expression + ")");
                        if (visited.ArgumentList.Arguments.Count > 1) { text = Call(text, "Role", visited.ArgumentList.Arguments[1].Expression); }
                        return text.WithTriviaFrom(node);
                    }
                }
                if (node.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Add" } access
                    || model.GetTypeInfo(access.Expression).Type?.ToDisplayString() != "ConsoleMonster.Layout.ContentBuilder"
                    || visited.ArgumentList.Arguments.Count != 1) { return visited; }
                ExpressionSyntax value = visited.ArgumentList.Arguments[0].Expression;
                var factory = RootFactory(value);
                if (factory?.Expression is not MemberAccessExpressionSyntax factoryAccess) { return visited; }
                string noun = factoryAccess.Name.Identifier.ValueText;
                if (noun is "Options" or "RichText") { return visited; }
                if (factory.ArgumentList.Arguments.Count > 0 && value == factory && noun is "Text" or "Markup" or "Rule") {
                    return visited.WithExpression(((MemberAccessExpressionSyntax)visited.Expression).WithName(SyntaxFactory.IdentifierName(noun)))
                        .WithArgumentList(factory.ArgumentList);
                }

                if (factory.ArgumentList.Arguments.Count > 0) {
                    return visited.WithExpression(((MemberAccessExpressionSyntax)visited.Expression).WithName(SyntaxFactory.IdentifierName(noun)));
                }
                bool definition = noun is "Chart" or "Progress" or "Select" or "TextInput" or "Button" or "Toggle" or "Collapsible" or "Outline" or "Breadcrumb" or "Binding" or "ChartView" or "DataGrid" or "DatePicker" or "TreeSelect";
                ExpressionSyntax unwrapped = value is ParenthesizedExpressionSyntax { Expression: CastExpressionSyntax cast } ? cast.Expression : value;
                bool endsWithBuild = unwrapped is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Build" } };
                if (definition && !endsWithBuild) {
                    return visited.WithExpression(((MemberAccessExpressionSyntax)visited.Expression).WithName(SyntaxFactory.IdentifierName(noun)));
                }
                if (!definition && model.GetTypeInfo(node.ArgumentList.Arguments[0].Expression).Type is ITypeSymbol actualType) {
                    var factoryMethod = model.Compilation.GetTypeByMetadataName("ConsoleMonster.Ui")!.GetMembers(noun).OfType<IMethodSymbol>().FirstOrDefault(method => !method.IsGenericMethod && method.Parameters.All(parameter => parameter.IsOptional));
                    if (factoryMethod != null && !SymbolEqualityComparer.Default.Equals(factoryMethod.ReturnType, actualType)) { return visited; }
                }
                string name = Unique(noun, value);
                ExpressionSyntax configuration = value.ReplaceNode(factory, SyntaxFactory.IdentifierName(name));
                // Definition helpers perform Build once after configuration.
                if (configuration is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Build" } built }) { configuration = built.Expression; }
                if (configuration is ParenthesizedExpressionSyntax { Expression: CastExpressionSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Build" } castBuilt } } }) { configuration = castBuilt.Expression; }
                if (configuration is IdentifierNameSyntax) { return visited.WithExpression(((MemberAccessExpressionSyntax)visited.Expression).WithName(SyntaxFactory.IdentifierName(noun))).WithArgumentList(SyntaxFactory.ArgumentList()); }
                var lambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)), configuration);
                return visited.WithExpression(((MemberAccessExpressionSyntax)visited.Expression).WithName(SyntaxFactory.IdentifierName(noun)))
                    .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(lambda))));
            }

            // A lambda parameter only has to differ from the enclosing lambdas' parameters and the names its body uses.
            private static string LambdaName(string stem, SyntaxNode position, SyntaxNode body) {
                var names = position.Ancestors().SelectMany(node => node switch {
                    SimpleLambdaExpressionSyntax simple => new[] { simple.Parameter.Identifier.ValueText },
                    ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters.Select(parameter => parameter.Identifier.ValueText).ToArray(),
                    _ => Array.Empty<string>()
                }).Concat(body.DescendantTokens().Where(token => token.IsKind(SyntaxKind.IdentifierToken)).Select(token => token.ValueText)).ToHashSet(StringComparer.Ordinal);
                string name = stem;
                for (int suffix = 2; names.Contains(name); suffix++) { name = stem + suffix; }
                return name;
            }

            private static bool EndsWithBuild(ExpressionSyntax value) {
                ExpressionSyntax unwrapped = value is ParenthesizedExpressionSyntax { Expression: CastExpressionSyntax cast } ? cast.Expression : value;
                return unwrapped is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Build" } };
            }

            // Whether the type's fluent class has member(Action<ContentBuilder>), such as Splitter.First.
            private bool HasContentOverload(INamedTypeSymbol type, string member) {
                INamedTypeSymbol? fluent = model.Compilation.GetTypeByMetadataName(type.ContainingNamespace.ToDisplayString() + "." + type.ContainingNamespace.Name + "Fluent");
                return fluent?.GetMembers(member).OfType<IMethodSymbol>().Any(method => method.IsExtensionMethod && method.Parameters.Length == 2
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, type)
                    && method.Parameters[1].Type.ToDisplayString() == "System.Action<ConsoleMonster.Layout.ContentBuilder>") == true;
            }

            private static IPropertySymbol? FindProperty(INamedTypeSymbol type, string name) {
                for (INamedTypeSymbol? current = type; current != null; current = current.BaseType) {
                    if (current.GetMembers(name).OfType<IPropertySymbol>().FirstOrDefault() is IPropertySymbol property) { return property; }
                }
                return null;
            }

            // AppInput.Text(new TextInputOptions { ... }) is the initializer form of a control with no public class (a
            // multiline text area); its fluent form is the Ui definition with the same setters, ended by Build.
            private ExpressionSyntax? AppInputDefinition(InvocationExpressionSyntax node, InvocationExpressionSyntax visited) {
                if (model.GetSymbolInfo(node).Symbol is not IMethodSymbol { IsStatic: true } method
                    || method.ContainingType.ToDisplayString() != "ConsoleMonster.Interactive.AppInput"
                    || node.ArgumentList.Arguments.Count != 1 || visited.ArgumentList.Arguments.Count != 1
                    || model.GetTypeInfo(node.ArgumentList.Arguments[0].Expression).Type is not INamedTypeSymbol options) { return null; }
                IMethodSymbol? definition = model.Compilation.GetTypeByMetadataName("ConsoleMonster.Ui")!.GetMembers().OfType<IMethodSymbol>()
                    .FirstOrDefault(candidate => candidate.IsStatic && candidate.Arity == 0 && candidate.Parameters.Length == 0 && SymbolEqualityComparer.Default.Equals(candidate.ReturnType, options));
                if (definition == null) { return null; }
                ExpressionSyntax chain = visited.ArgumentList.Arguments[0].Expression;
                ObjectCreationExpressionSyntax? root = chain.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>()
                    .FirstOrDefault(creation => creation.ArgumentList?.Arguments.Count is null or 0 && creation.Initializer == null
                        && creation.Type.ToString() == options.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                if (root == null) { return null; }
                Namespaces.Add("ConsoleMonster");
                ExpressionSyntax configured = chain.ReplaceNode(root, SyntaxFactory.ParseExpression("Ui." + definition.Name + "()"));
                return Call(configured, "Build");
            }

            private static InvocationExpressionSyntax? RootFactory(ExpressionSyntax expression) {
                while (true) {
                    if (expression is ParenthesizedExpressionSyntax parentheses) { expression = parentheses.Expression; continue; }
                    if (expression is CastExpressionSyntax cast) { expression = cast.Expression; continue; }
                    if (expression is not InvocationExpressionSyntax call || call.Expression is not MemberAccessExpressionSyntax member) { return null; }
                    if (member.Expression is IdentifierNameSyntax { Identifier.ValueText: "Ui" }) { return call; }
                    expression = member.Expression;
                }
            }

            private static InvocationExpressionSyntax Call(ExpressionSyntax receiver, string member, params ExpressionSyntax[] args) =>
                SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName(member)),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(args.Select(SyntaxFactory.Argument))));
        }
    }
}
