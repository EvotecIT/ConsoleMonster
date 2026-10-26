using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        // Source indexes protect evaluation order. A newly created callback or a
        // compile-time constant can move without evaluating a caller expression.
        private static SeparatedSyntaxList<ArgumentSyntax>? PositionalArguments(IMethodSymbol target,
            IReadOnlyList<(string Parameter, int SourceIndex, ArgumentSyntax Value)> supplied) {
            var ordered = new List<ArgumentSyntax>();
            var evaluated = new List<int>();
            int last = supplied.Count == 0 ? -1 : target.Parameters.Where(parameter => supplied.Any(argument => argument.Parameter == parameter.Name))
                .Select(parameter => parameter.Ordinal).DefaultIfEmpty(-1).Max();
            if (supplied.Any(argument => !target.Parameters.Any(parameter => parameter.Name == argument.Parameter))) { return null; }
            foreach (IParameterSymbol parameter in target.Parameters.Take(last + 1)) {
                var values = supplied.Where(argument => argument.Parameter == parameter.Name).OrderBy(argument => argument.SourceIndex).ToArray();
                if (values.Length == 0) {
                    if (!parameter.IsOptional) { return null; }
                    ExpressionSyntax value = parameter.ExplicitDefaultValue == null
                        ? SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)
                        : SyntaxFactory.ParseExpression(SymbolDisplay.FormatPrimitive(parameter.ExplicitDefaultValue, quoteStrings: true, useHexadecimalNumbers: false));
                    if (parameter.Type.TypeKind == TypeKind.Enum && parameter.ExplicitDefaultValue != null) {
                        value = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)), value);
                    }
                    ordered.Add(SyntaxFactory.Argument(value));
                } else {
                    if (values.Length > 1 && !parameter.IsParams) { return null; }
                    ordered.AddRange(values.Select(argument => argument.Value.WithNameColon(null)));
                    evaluated.AddRange(values.Where(argument => argument.SourceIndex >= 0).Select(argument => argument.SourceIndex));
                }
            }
            // Omitted trailing parameters must all have defaults, too.
            if (target.Parameters.Skip(last + 1).Any(parameter => !parameter.IsOptional && !parameter.IsParams)
                || !evaluated.SequenceEqual(evaluated.OrderBy(index => index))) { return null; }
            return SyntaxFactory.SeparatedList(ordered);
        }

        private static IParameterSymbol? ParameterFor(SemanticModel model, IMethodSymbol method, ArgumentSyntax argument, int index) =>
            (model.GetOperation(argument) as Microsoft.CodeAnalysis.Operations.IArgumentOperation)?.Parameter
            ?? (argument.NameColon == null ? method.Parameters.ElementAtOrDefault(Math.Min(index, method.Parameters.Length - 1))
                : method.Parameters.FirstOrDefault(parameter => parameter.Name == argument.NameColon.Name.Identifier.ValueText));

        private static SeparatedSyntaxList<ArgumentSyntax> WithoutArgumentNames(SemanticModel model, SyntaxNode original,
            SeparatedSyntaxList<ArgumentSyntax> arguments, SeparatedSyntaxList<ArgumentSyntax> rewritten) {
            if (!arguments.Any(argument => argument.NameColon != null) || model.GetSymbolInfo(original).Symbol is not IMethodSymbol method) { return rewritten; }
            var supplied = new List<(string, int, ArgumentSyntax)>();
            for (int index = 0; index < arguments.Count; index++) {
                IParameterSymbol? parameter = ParameterFor(model, method, arguments[index], index);
                if (parameter == null) { return rewritten; }
                supplied.Add((parameter.Name, index, rewritten[index]));
            }
            return PositionalArguments(method, supplied) ?? rewritten;
        }
    }
}
