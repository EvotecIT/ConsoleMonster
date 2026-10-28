using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell.Display;

/// <summary>Validated data-only schema 1 or 2 document. Parsing never executes a view expression.</summary>
public sealed class ConsoleViewDocument {
    private readonly IReadOnlyList<DeclarativeView> _views;
    private ConsoleViewDocument(int schemaVersion, IReadOnlyList<DeclarativeView> views) { SchemaVersion = schemaVersion; _views = views; }
    /// <summary>Source schema version.</summary>
    public int SchemaVersion { get; }
    /// <summary>Exact types declared by this file, suitable for an explicitly reviewed module whitelist.</summary>
    public IReadOnlyList<string> TypeNames => _views.Select(view => view.TypeName).ToArray();
    /// <summary>Validates and snapshots the complete document, including nested values.</summary>
    public static ConsoleViewDocument Parse(IDictionary document) {
        IDictionary data = Snapshot(document, 0) as IDictionary ?? throw new ArgumentException("A data hashtable is required.");
        Keys(data, "SchemaVersion", "Views");
        int schema = Number(data, "SchemaVersion", 1);
        if (schema != 1 && schema != 2) { throw new ArgumentException("SchemaVersion must be 1 or 2."); }
        List<DeclarativeView> views = new List<DeclarativeView>();
        foreach (object value in Sequence(data["Views"])) {
            if (!(value is IDictionary view)) { throw new ArgumentException("Each view must be a data hashtable."); }
            Keys(view, schema == 1 ? V1Keys : V2Keys);
            views.Add(new DeclarativeView(view));
        }
        return new ConsoleViewDocument(schema, views.AsReadOnly());
    }
    /// <summary>Parses one literal data hashtable, for example text from an embedded resource, without executing a script.</summary>
    /// <param name="text">Complete schema 1 or 2 data document.</param>
    /// <returns>The same validated data snapshot used by <see cref="Parse(IDictionary)"/>.</returns>
    public static ConsoleViewDocument ParseText(string text) {
        if (text == null) { throw new ArgumentNullException(nameof(text)); }
        ScriptBlockAst script = Parser.ParseInput(text, out Token[] tokens, out ParseError[] errors);
        if (errors.Length != 0 || script.ParamBlock != null || script.EndBlock == null || !script.EndBlock.Unnamed
            || script.EndBlock.Statements.Count != 1 || script.EndBlock.Traps?.Count > 0
            || script.FindAll(node => node is ScriptBlockExpressionAst || node is UsingStatementAst
                || node is NamedBlockAst && !ReferenceEquals(node, script.EndBlock), true).Any()
            || !(script.EndBlock.Statements[0] is PipelineAst pipeline) || pipeline.PipelineElements.Count != 1
            || !(pipeline.PipelineElements[0] is CommandExpressionAst expression) || expression.Redirections.Count != 0
            || !(expression.Expression is HashtableAst hashtable)) {
            throw new ArgumentException("The text must contain one literal data hashtable and no other statements.", nameof(text));
        }
        // The parameterless overload is the common PowerShell 5.1/7 safe-value contract.
        // It evaluates only permitted constant data; the snapshot rejects all non-data values recursively.
        object value;
        try { value = hashtable.SafeGetValue(); }
        catch (Exception error) { throw new ArgumentException("The text must contain only constant data values.", nameof(text), error); }
        return Parse(value as IDictionary ?? throw new ArgumentException("A data hashtable is required.", nameof(text)));
    }
    /// <summary>Loads through PowerShell's restricted data-file reader, with no module discovery or script evaluation.</summary>
    public static ConsoleViewDocument Read(string path) {
        if (path == null || !path.EndsWith(".consoleviews.psd1", StringComparison.OrdinalIgnoreCase)) { throw new ArgumentException("A .consoleviews.psd1 file is required.", nameof(path)); }
        using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)) {
            var result = ps.AddCommand("Microsoft.PowerShell.Utility\\Import-PowerShellDataFile").AddParameter("LiteralPath", path).AddParameter("ErrorAction", ActionPreference.Stop).Invoke();
            if (ps.HadErrors || result.Count != 1 || !(result[0].BaseObject is IDictionary document)) { throw new ArgumentException("The file must contain one restricted data hashtable.", nameof(path)); }
            return Parse(document);
        }
    }
    /// <summary>Resolves all builders and ownership before installing any registration.</summary>
    public IReadOnlyList<DefaultDisplayRegistration> Register(ModuleLease lease, Version? version = null) => Register(lease, version, null);

    /// <summary>Registers a data document with exact CLR identities supplied by the importing module.</summary>
    /// <param name="lease">The importing module's lease.</param><param name="version">Optional view version.</param>
    /// <param name="runtimeTypes">Exact type-name to CLR-type bindings for compiled builders, or null for data-only views.</param>
    /// <returns>The installed registrations.</returns>
    public IReadOnlyList<DefaultDisplayRegistration> Register(ModuleLease lease, Version? version, IReadOnlyDictionary<string, Type>? runtimeTypes) {
        if (lease == null) { throw new ArgumentNullException(nameof(lease)); }
        var builders = _views.Select(view => view.BuilderName == null ? view.Build : lease.State.Builder(lease, view.BuilderName)).ToArray();
        foreach (DeclarativeView view in _views) {
            lease.ValidateOwnedType(view.TypeName);
            if (runtimeTypes != null && runtimeTypes.TryGetValue(view.TypeName, out Type? runtimeType) && runtimeType.FullName != view.TypeName)
                throw new ArgumentException("A runtime type must match its exact declared name.", nameof(runtimeTypes));
        }
        List<DefaultDisplayRegistration> result = new List<DefaultDisplayRegistration>();
        for (int i = 0; i < _views.Count; i++) {
            DeclarativeView view = _views[i];
            Type? runtimeType = null;
            runtimeTypes?.TryGetValue(view.TypeName, out runtimeType);
            result.Add(lease.Register(view.TypeName, view.ViewId, version ?? lease.Version, view.Batch, view.ScopeKeys, builders[i], view.Budget, runtimeType: runtimeType));
        }
        return result.AsReadOnly();
    }
    internal static readonly string[] V1Keys = { "TypeName", "View", "Property", "GroupBy", "Group" };
    internal static readonly string[] V2Keys = V1Keys.Concat(new[] { "ViewId", "Batch", "ScopeKey", "Children", "Id", "ParentId", "Cycle", "Repeat", "Title", "Summary", "Tones", "Priority", "Budget", "OverBudget", "Builder" }).ToArray();
    internal static string? String(IDictionary data, string name) {
        object? value = data[name];
        if (value == null) { return null; }
        return value as string ?? throw new ArgumentException(name + " must be a string.");
    }
    internal static int Number(IDictionary data, string name, int fallback) => data[name] == null ? fallback : Convert.ToInt32(data[name], CultureInfo.InvariantCulture);
    internal static IEnumerable Sequence(object? value) => value == null ? Array.Empty<object>() : value is string || value is IDictionary ? new[] { value } : value as IEnumerable ?? new[] { value };
    internal static void Keys(IDictionary data, params string[] allowed) {
        foreach (object key in data.Keys) {
            if (!(key is string name) || !allowed.Contains(name, StringComparer.OrdinalIgnoreCase)) { throw new ArgumentException("Unknown console-view key: " + key); }
        }
    }
    private static object? Snapshot(object? value, int depth) {
        if (depth > 32) { throw new ArgumentException("View data exceeds the nesting budget."); }
        if (value == null || value is string || value is bool || value is char || value is byte || value is sbyte || value is short || value is ushort
            || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal) { return value; }
        if (value is IDictionary dictionary) {
            if (dictionary.Count > 4096) { throw new ArgumentException("View data exceeds the entry budget."); }
            Hashtable copy = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in dictionary) {
                if (!(entry.Key is string name)) { throw new ArgumentException("View data keys must be strings."); }
                copy.Add(name, Snapshot(entry.Value, depth + 1));
            }
            return copy;
        }
        if (value is Array || value is ArrayList) {
            List<object?> copy = new List<object?>();
            foreach (object? item in (IEnumerable)value) {
                if (copy.Count >= 4096) { throw new ArgumentException("View data exceeds the entry budget."); }
                copy.Add(Snapshot(item, depth + 1));
            }
            return copy.ToArray();
        }
        throw new ArgumentException("Views are data only; script blocks, delegates and executable objects are forbidden.");
    }
}
