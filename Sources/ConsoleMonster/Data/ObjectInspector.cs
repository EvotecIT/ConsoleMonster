using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
#endif

namespace ConsoleMonster.Data {
    /// <summary>Builds a bounded, neutral tree from arbitrary objects for a presentation layer.</summary>
    public static class ObjectInspector {
        private static readonly IMemberSource Dictionaries = new DictionaryMemberSource();
        private static readonly IMemberSource Reflection = new ReflectionMemberSource();

#if NET8_0_OR_GREATER
        /// <summary>Inspects a value without allowing member getter failures to escape.</summary>
        /// <param name="value">The root value.</param>
        /// <param name="options">Inspection limits and filters.</param>
        /// <param name="name">Optional root label.</param>
        /// <returns>A root inspection node.</returns>
        /// <remarks>Trimmed or AOT applications should supply an <see cref="IMemberSource"/> or set <see cref="InspectorOptions.UseReflection"/> to false.</remarks>
        [RequiresUnreferencedCode(
            "Object inspection reflects over public properties. Supply an IMemberSource or set UseReflection=false for trimmed or AOT applications."
        )]
#else
        /// <summary>Inspects a value without allowing member getter failures to escape.</summary>
        /// <param name="value">The root value.</param>
        /// <param name="options">Inspection limits and filters.</param>
        /// <param name="name">Optional root label.</param>
        /// <returns>A root inspection node.</returns>
#endif
        public static InspectNode Inspect(
            object? value,
            InspectorOptions? options = null,
            string? name = null
        ) {
            InspectorOptions actual = options ?? new InspectorOptions();
            Walk walk = new Walk(Math.Max(1, actual.MaxNodes));
            return Build(value, name, "$", 0, actual, walk);
        }

        internal static IMemberSource? FindSource(object value, InspectorOptions options) {
            foreach (IMemberSource source in options.Sources)
                if (source.CanRead(value))
                    return source;
            if (value is IDictionary || IsPairDictionary(value))
                return Dictionaries;
#if NET8_0_OR_GREATER
            if (value is JsonElement)
                return JsonMemberSource.Instance;
#endif
            return options.UseReflection ? Reflection : null;
        }

        internal static object? UnwrapValue(object? value, InspectorOptions options) {
            if (value == null || options.Unwrap == null)
                return value;
            return options.Unwrap(value);
        }

        internal static bool IsScalarValue(object? value) {
            if (value == null)
                return true;
            Type type = value.GetType();
            return type.IsPrimitive
                || type.IsEnum
                || value is string
                || value is decimal
                || value is DateTime
                || value is DateTimeOffset
                || value is TimeSpan
                || value is Guid
                || value is Uri
                || value is Version
                || value is Type
                || value is IPAddress
                || value is byte[]
                || value is DBNull;
        }

        internal static bool MemberIncluded(string name, InspectorOptions options) {
            if (
                options.Include.Count > 0
                && !options.Include.Any(pattern => Wildcard(name, pattern))
            )
                return false;
            return !options.Exclude.Any(pattern => Wildcard(name, pattern));
        }

        internal static void ApplyOrder(List<string> names, IList<string> order) {
            List<string> sorted = new List<string>();
            foreach (string preferred in order) {
                string? match = names.FirstOrDefault(n =>
                    string.Equals(n, preferred, StringComparison.OrdinalIgnoreCase)
                );
                if (match != null && !sorted.Contains(match, StringComparer.OrdinalIgnoreCase))
                    sorted.Add(match);
            }
            foreach (string item in names)
                if (!sorted.Contains(item, StringComparer.OrdinalIgnoreCase))
                    sorted.Add(item);
            names.Clear();
            names.AddRange(sorted);
        }

        internal static string FriendlyTypeName(Type type) {
            if (type == typeof(object))
                return "Object";
            if (type.IsGenericType) {
                string baseName = type.Name.Substring(0, type.Name.IndexOf('`'));
                return baseName
                    + "<"
                    + string.Join(", ", type.GetGenericArguments().Select(FriendlyTypeName))
                    + ">";
            }
            if (type == typeof(string))
                return "String";
            if (type == typeof(int))
                return "Int32";
            if (type == typeof(long))
                return "Int64";
            if (type == typeof(short))
                return "Int16";
            if (type == typeof(bool))
                return "Boolean";
            if (type == typeof(double))
                return "Double";
            if (type == typeof(float))
                return "Single";
            if (type == typeof(byte))
                return "Byte";
            if (type == typeof(char))
                return "Char";
            string name = type.Name;
            if (name == "ExpandoObject")
                return "PSCustomObject";
            return name;
        }

        private static InspectNode Build(
            object? value,
            string? name,
            string path,
            int depth,
            InspectorOptions options,
            Walk walk
        ) {
            try {
                value = UnwrapValue(value, options);
            }
            catch (Exception ex) when (!IsCancellation(ex)) {
                return new InspectError(name, "Error", path, ex.Message);
            }
            if (walk.Remaining <= 0) {
                return NodeLimit(name, path, options);
            }
            walk.Remaining--;
            if (depth > Math.Max(0, options.MaxDepth)) {
                string limitedType =
                    value == null
                        ? "null"
                        : (
                            FindSource(value, options)?.GetTypeName(value)
                            ?? FriendlyTypeName(value.GetType())
                        );
                return new InspectLimit(
                    name,
                    limitedType,
                    path,
                    value == null ? "null" : Summary(value)
                );
            }
            if (value == null)
                return new InspectScalar(name, "null", path, null);
            if (IsScalarValue(value))
                return new InspectScalar(name, FriendlyTypeName(value.GetType()), path, value);
#if NET8_0_OR_GREATER
            if (value is JsonElement element) {
                if (
                    element.ValueKind == JsonValueKind.Null
                    || element.ValueKind == JsonValueKind.Undefined
                )
                    return new InspectScalar(name, "null", path, null);
                if (element.ValueKind == JsonValueKind.String)
                    return new InspectScalar(name, "String", path, element.GetString());
                if (
                    element.ValueKind == JsonValueKind.True
                    || element.ValueKind == JsonValueKind.False
                )
                    return new InspectScalar(name, "Boolean", path, element.GetBoolean());
                if (element.ValueKind == JsonValueKind.Number) {
                    if (element.TryGetInt64(out long integer))
                        return new InspectScalar(name, "Int64", path, integer);
                    return new InspectScalar(name, "Double", path, element.GetDouble());
                }
            }
#endif
            object current = value!;
            string typeName =
                FindSource(current, options)?.GetTypeName(current)
                ?? FriendlyTypeName(current.GetType());
            if (TryDescribePending(current, out string? pending))
                // An incomplete Task/ValueTask or an uncreated Lazy<T>: reading Result or Value would block or force it.
                return new InspectScalar(name, typeName, path, pending);
            if (
                !current.GetType().IsValueType && walk.Ancestors.TryGetValue(current, out string? target)
            )
                return new InspectCycle(name, typeName, path, target);
            bool track = !current.GetType().IsValueType;
            if (track)
                walk.Ancestors[current] = path;
            try {
                if (TryGetEnumerable(current, out IEnumerable? enumerable))
                    return BuildCollection(
                        current,
                        enumerable!,
                        name,
                        typeName,
                        path,
                        depth,
                        options,
                        walk
                    );
                IMemberSource? source = FindSource(current, options);
                if (source == null)
                    return new InspectScalar(name, typeName, path, current);
                List<InspectNode> members = new List<InspectNode>();
                foreach (InspectMember member in source.GetMembers(current)) {
                    if (!MemberIncluded(member.Name, options))
                        continue;
                    string childPath = MemberPath(path, member.Name);
                    if (walk.Remaining <= 0) {
                        // Out of nodes: one marker instead of reading (and listing) every remaining member.
                        members.Add(NodeLimit(member.Name, childPath, options));
                        break;
                    }
                    try {
                        members.Add(
                            Build(
                                member.Value,
                                member.Name,
                                childPath,
                                depth + 1,
                                options,
                                walk
                            )
                        );
                    }
                    catch (Exception ex) when (!IsCancellation(ex)) {
                        members.Add(new InspectError(member.Name, "Error", childPath, ex.Message));
                    }
                }
                List<string> memberNames = members.Select(m => m.Name ?? string.Empty).ToList();
                ApplyOrder(memberNames, options.Order);
                members = memberNames
                    .Select(n =>
                        members.First(m =>
                            string.Equals(m.Name, n, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    .ToList();
                return new InspectObject(name, typeName, path, members.AsReadOnly());
            }
            finally {
                if (track)
                    walk.Ancestors.Remove(current);
            }
        }

#if NET8_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
            "Trimming",
            "IL2026",
            Justification = "Collection inference is reached only through ObjectInspector.Inspect, which carries RequiresUnreferencedCode."
        )]
#endif
        private static InspectNode BuildCollection(
            object value,
            IEnumerable enumerable,
            string? name,
            string typeName,
            string path,
            int depth,
            InspectorOptions options,
            Walk walk
        ) {
            List<object?> values = new List<object?>();
            int max = Math.Max(0, options.MaxItems);
            bool more = false;
            Exception? enumerationError = null;
            IEnumerator? iterator;
            try {
                iterator = enumerable.GetEnumerator();
            }
            catch (Exception ex) when (!IsCancellation(ex)) {
                return new InspectError(name, typeName, path, ex.Message);
            }
            try {
                while (values.Count < max && iterator.MoveNext())
                    values.Add(iterator.Current);
                if (values.Count == max && iterator.MoveNext())
                    more = true;
            }
            catch (Exception ex) when (!IsCancellation(ex)) {
                enumerationError = ex;
            }
            try {
                (iterator as IDisposable)?.Dispose();
            }
            catch (Exception ex) when (!IsCancellation(ex)) {
                enumerationError ??= ex;
            }
            if (enumerationError != null)
                return new InspectError(name, typeName, path, enumerationError.Message);
            List<InspectNode> nodes = new List<InspectNode>();
            for (int index = 0; index < values.Count && walk.Remaining > 0; index++)
                nodes.Add(
                    Build(
                        values[index],
                        null,
                        path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                        depth + 1,
                        options,
                        walk
                    )
                );
            // Items the node budget left out count as omitted ("+N more").
            more |= nodes.Count < values.Count;
            int? count = TryCount(value);
            int omitted = count.HasValue
                ? Math.Max(0, count.Value - nodes.Count)
                : (more ? -1 : 0);
            int hidden = 0;
            IReadOnlyList<string>? columns = null;
            if (values.Count > 0) {
                IReadOnlyList<string> inferred = ColumnInference.Infer(values, options, out hidden);
                if (inferred.Count > 0)
                    columns = inferred;
            }
            return new InspectCollection(
                name,
                typeName,
                path,
                nodes.AsReadOnly(),
                count,
                omitted,
                columns,
                hidden
            );
        }

        private static bool TryGetEnumerable(object value, out IEnumerable? enumerable) {
#if NET8_0_OR_GREATER
            if (value is JsonElement json && json.ValueKind == JsonValueKind.Array) {
                enumerable = JsonMemberSource.ArrayValues(json);
                return true;
            }
#endif
            if (value is IDictionary || IsPairDictionary(value) || value is string) {
                enumerable = null;
                return false;
            }
            enumerable = value as IEnumerable;
            return enumerable != null;
        }

#if NET8_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
            "Trimming",
            "IL2075",
            Justification = "The Count property is read from an implemented IReadOnlyCollection<T> interface, whose contract includes the property."
        )]
#endif
        private static int? TryCount(object value) {
#if NET8_0_OR_GREATER
            if (value is JsonElement json && json.ValueKind == JsonValueKind.Array)
                return json.GetArrayLength();
#endif
            try {
                if (value is ICollection collection)
                    return collection.Count;
                foreach (Type type in value.GetType().GetInterfaces()) {
                    if (
                        type.IsGenericType
                        && type.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)
                    )
                        return (int)type.GetProperty("Count")!.GetValue(value, null)!;
                }
            }
            catch {
                return null;
            }
            return null;
        }

        private static bool IsPairDictionary(object value) => Dictionaries.CanRead(value);

        private static InspectLimit NodeLimit(string? name, string path, InspectorOptions options) =>
            new InspectLimit(
                name,
                "Limit",
                path,
                "node limit reached (MaxNodes = " + options.MaxNodes.ToString(CultureInfo.InvariantCulture) + ")"
            );

        // Cancellation (Ctrl+C in PowerShell surfaces as PipelineStoppedException) must stop the walk, not become a node.
        internal static bool IsCancellation(Exception ex) =>
            ex is OperationCanceledException
            || ex.GetType().FullName == "System.Management.Automation.PipelineStoppedException";

        // Task<T>.Result, ValueTask<T>.Result and Lazy<T>.Value block or force evaluation; only completed values are read.
        private static bool TryDescribePending(object value, out string? description) {
            description = null;
            if (value is System.Threading.Tasks.Task task) {
                if (task.IsCompleted)
                    return false;
                description = "(" + task.Status.ToString() + ", not completed)";
                return true;
            }
            Type type = value.GetType();
            if (type.Namespace == "System.Threading.Tasks" && type.Name.StartsWith("ValueTask", StringComparison.Ordinal)) {
                if (ReadFlag(value, "IsCompleted"))
                    return false;
                description = "(not completed)";
                return true;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>)) {
                if (ReadFlag(value, "IsValueCreated"))
                    return false;
                description = "(value not created)";
                return true;
            }
            return false;
        }

#if NET8_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
            "Trimming",
            "IL2075",
            Justification = "IsCompleted and IsValueCreated are public properties of ValueTask and Lazy<T>, which are part of the framework."
        )]
#endif
        private static bool ReadFlag(object value, string property) {
            try {
                return value.GetType().GetProperty(property)?.GetValue(value, null) is bool flag && flag;
            }
            catch {
                return false;
            }
        }

        private sealed class Walk {
            public Walk(int maxNodes) {
                Remaining = maxNodes;
            }

            public Dictionary<object, string> Ancestors { get; } = new Dictionary<object, string>(ReferenceComparer.Instance);

            public int Remaining { get; set; }
        }

        private static string MemberPath(string path, string name) =>
            name.All(c => char.IsLetterOrDigit(c) || c == '_')
                ? path + "." + name
                : path + "['" + name.Replace("'", "\\'") + "']";

        private static string Summary(object value) {
            if (TryGetEnumerable(value, out _)) {
                int? count = TryCount(value);
                return count.HasValue
                    ? count.Value.ToString(CultureInfo.InvariantCulture) + " items"
                    : "items";
            }
            return FriendlyTypeName(value.GetType());
        }

        private static bool Wildcard(string text, string pattern) {
            int t = 0,
                p = 0,
                star = -1,
                mark = -1;
            while (t < text.Length) {
                if (
                    p < pattern.Length
                    && (
                        pattern[p] == '?'
                        || char.ToUpperInvariant(pattern[p]) == char.ToUpperInvariant(text[t])
                    )
                ) {
                    t++;
                    p++;
                }
                else if (p < pattern.Length && pattern[p] == '*') {
                    star = p++;
                    mark = t;
                }
                else if (star >= 0) {
                    p = star + 1;
                    t = ++mark;
                }
                else
                    return false;
            }
            while (p < pattern.Length && pattern[p] == '*')
                p++;
            return p == pattern.Length;
        }

        private sealed class ReferenceComparer : IEqualityComparer<object> {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

            public int GetHashCode(object obj) =>
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
