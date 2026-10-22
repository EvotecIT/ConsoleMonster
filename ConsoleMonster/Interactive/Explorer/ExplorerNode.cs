using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using ConsoleMonster.Data;

namespace ConsoleMonster.Interactive {
    /// <summary>A lazily read member of an object explorer; values are read at most once.</summary>
    public sealed class ExplorerNode {
        private readonly Func<object?> _read;
        private readonly InspectorOptions _options;
        private readonly ExplorerBudget _budget;
        private readonly bool _collectionItem;
        private object? _value;
        private bool _readValue;
        private bool _prepared;
        private object? _preparedValue;
        private List<ExplorerNode>? _children;

        internal ExplorerNode(string name, string path, Func<object?> read, InspectorOptions options, ExplorerNode? parent, bool collectionItem = false) {
            Name = name;
            Path = path;
            _read = read;
            _options = options;
            Parent = parent;
            _collectionItem = collectionItem;
            _budget = parent?._budget ?? new ExplorerBudget(options.MaxNodes);
        }

        /// <summary>The member name or collection index shown in the tree.</summary>
        public string Name { get; }

        /// <summary>The quoted PowerShell expression locating this member from the root.</summary>
        public string Path { get; }

        /// <summary>The containing member, or null at the root.</summary>
        public ExplorerNode? Parent { get; }

        /// <summary>The original member value, read once when first displayed or selected.</summary>
        public object? Value {
            get {
                if (!_readValue) {
                    _readValue = true;
                    try { _value = _read(); }
                    catch (Exception ex) when (!ObjectInspector.IsCancellation(ex)) { Error = ex.Message; }
                }
                return _value;
            }
        }

        /// <summary>The getter or enumeration failure, if a read failed.</summary>
        public string? Error { get; private set; }

        /// <summary>Whether this member points back to an ancestor.</summary>
        public bool IsCycle {
            get {
                object? value = Prepared;
                if (value == null || value.GetType().IsValueType) { return false; }
                object identity = _options.Identity?.Invoke(value) ?? value;
                for (ExplorerNode? p = Parent; p != null; p = p.Parent) {
                    object? ancestor = p.Prepared;
                    if (ancestor != null && ReferenceEquals(identity, _options.Identity?.Invoke(ancestor) ?? ancestor)) { return true; }
                }
                return false;
            }
        }

        /// <summary>Whether the node can be opened; cycles, depth limits and scalar values stay leaves.</summary>
        public bool CanExpand => Error == null && Depth < Math.Max(0, _options.MaxDepth) && !IsCycle && !ObjectInspector.IsScalarValue(Prepared) && !Pending(out _);

        /// <summary>A plain, bounded summary suitable for search and display.</summary>
        public string Summary {
            get {
                object? value = Prepared;
                if (Error != null) { return "error: " + Error; }
                if (IsCycle) { return "(cycle)"; }
                if (value == null) { return "null"; }
                if (Pending(out string? pending)) { return pending ?? "pending"; }
                if (value is ICollection collection && !(value is IDictionary) && !(value is string)) {
                    return collection.Count.ToString(CultureInfo.InvariantCulture) + " items" + (Depth >= _options.MaxDepth ? " (depth limit)" : "");
                }
                if (!ObjectInspector.IsScalarValue(value)) {
                    string type = ObjectInspector.FindSource(value, _options)?.GetTypeName(value) ?? value.GetType().Name;
                    if (type.IndexOf("AnonymousType", StringComparison.Ordinal) >= 0) { type = "Object"; }
                    return type + (Depth >= _options.MaxDepth ? " (depth limit)" : "");
                }
                string text = Values.ValueShaper.ScalarText(value);
                return text.Length > 240 ? text.Substring(0, 240) + "…" : text;
            }
        }

        // Collection rows keep their index/path while showing an identifying string from the same bounded children.
        // Reading a label uses the ordinary cached getters, exclusions and node/depth budgets.
        internal (string Text, bool Descriptor) DisplayValue() {
            string summary = Summary;
            bool descriptor = Error == null && Prepared != null && !ObjectInspector.IsScalarValue(Prepared);
            if (descriptor && _collectionItem && CanExpand) {
                foreach (string identifier in new[] { "Name", "Id", "DisplayName" }) {
                    foreach (ExplorerNode child in Children) {
                        if (string.Equals(child.Name, identifier, StringComparison.OrdinalIgnoreCase) &&
                            child.Prepared is string text && !string.IsNullOrWhiteSpace(text)) {
                            return (text.Length > 240 ? text.Substring(0, 240) + "…" : text, false);
                        }
                    }
                }
            }
            return (summary, descriptor);
        }

        /// <summary>Whether collection or node limits left more children undisplayed.</summary>
        public bool HasMore { get; private set; }

        /// <summary>Reads only this level, caching a bounded page without visiting grandchildren.</summary>
        public IReadOnlyList<ExplorerNode> Children {
            get {
                if (_children != null) { return _children; }
                _children = new List<ExplorerNode>();
                if (!CanExpand) { return _children; }
                object? value = Prepared;
                if (value == null) { return _children; }
                try {
                    if (ObjectInspector.TryGetEnumerable(value, out IEnumerable? items)) {
                        ReadItems(items!);
                    } else {
                        IMemberSource? source = ObjectInspector.FindSource(value, _options);
                        if (source != null) {
                            if (source is DictionaryMemberSource && value is IDictionary map && map.Count > _budget.Remaining) { HasMore = true; }
                            IEnumerable<InspectMember> members = source is DictionaryMemberSource dictionary ? dictionary.GetMembers(value, _budget.Remaining) : source.GetMembers(value);
#if NET8_0_OR_GREATER
                            if (source is Values.JsonNodeMemberSource json) { members = json.GetMembers(value, true); }
#endif
                            Stopwatch elapsed = Stopwatch.StartNew();
                            foreach (InspectMember member in members) {
                                if (_options.TimeBudget > TimeSpan.Zero && elapsed.Elapsed >= _options.TimeBudget) { HasMore = true; break; }
                                if (!ObjectInspector.MemberIncluded(member.Name, _options)) { continue; }
                                if (!_budget.Take()) { HasMore = true; break; }
                                string name = member.Name;
                                string quoted = name.Replace("'", "''");
                                bool dictionaryMember = value is IDictionary || source is DictionaryMemberSource;
                                string path = member.DictionaryIndex.HasValue ? DictionaryPath(Path, member) : dictionaryMember ? Path + "['" + quoted + "']" : Path + ".'" + quoted + "'";
                                if (!dictionaryMember && IsIdentifier(name)) { path = Path + "." + name; }
                                _children.Add(new ExplorerNode(name, path, member.ReadValue, _options, this));
                            }
                        }
                    }
                } catch (Exception ex) when (!ObjectInspector.IsCancellation(ex)) { Error = ex.Message; }
                return _children;
            }
        }

        private object? Prepared {
            get {
                if (!_prepared) {
                    _prepared = true;
                    try {
                        object? value = ObjectInspector.UnwrapValue(Value, _options);
#if NET8_0_OR_GREATER
                        _preparedValue = value is System.Text.Json.Nodes.JsonArray ? value : Values.ValueShaper.Prepare(value);
#else
                        _preparedValue = Values.ValueShaper.Prepare(value);
#endif
                    }
                    catch (Exception ex) when (!ObjectInspector.IsCancellation(ex)) { Error = ex.Message; }
                }
                return _preparedValue;
            }
        }

        private int Depth => Parent == null ? 0 : Parent.Depth + 1;

        private bool Pending(out string? description) {
            object? value = Prepared;
            description = null;
            return value != null && ObjectInspector.TryDescribePending(_options.Identity?.Invoke(value) ?? value, out description);
        }

        private void ReadItems(IEnumerable items) {
            IEnumerator iterator = items.GetEnumerator();
            try {
                int limit = Math.Min(Math.Max(0, _options.MaxItems), Math.Max(1, _options.MaxNodes));
                Stopwatch elapsed = Stopwatch.StartNew();
                while (_children!.Count < limit && iterator.MoveNext()) {
                    if (_options.TimeBudget > TimeSpan.Zero && elapsed.Elapsed >= _options.TimeBudget) { HasMore = true; return; }
                    int index = _children.Count;
                    object? item = iterator.Current;
                    string name = "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                    if (!_budget.Take()) { HasMore = true; return; }
                    _children.Add(new ExplorerNode(name, Path + name, () => item, _options, this, true));
                }
                HasMore = iterator.MoveNext();
            } finally { (iterator as IDisposable)?.Dispose(); }
        }

        private static bool IsIdentifier(string name) {
            if (name.Length == 0 || !(char.IsLetter(name[0]) || name[0] == '_')) { return false; }
            foreach (char c in name) { if (!(char.IsLetterOrDigit(c) || c == '_')) { return false; } }
            return true;
        }

        private static string DictionaryPath(string path, InspectMember member) {
            object? key = member.DictionaryKey;
            if (key == null) { return path + "[$null]"; }
            if (key is bool boolean) { return path + (boolean ? "[$true]" : "[$false]"); }
            string text = (Convert.ToString(key, CultureInfo.InvariantCulture) ?? string.Empty).Replace("'", "''");
            if (key is string) { return path + "['" + text + "']"; }
            Type type = key.GetType();
            if (type.IsPrimitive || type.IsEnum || key is decimal || key is Guid) {
                return path + "[[" + type.FullName + "]'" + text + "']";
            }
            return path + "[(@([System.Collections.IDictionary].GetProperty('Keys').GetValue((" + path + "), $null)))[" + member.DictionaryIndex!.Value.ToString(CultureInfo.InvariantCulture) + "]]";
        }
    }
}
