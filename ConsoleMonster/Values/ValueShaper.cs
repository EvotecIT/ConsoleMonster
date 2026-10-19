using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;
using ConsoleMonster.Data;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
#if NET8_0_OR_GREATER
using System.Text.Json;
#endif

namespace ConsoleMonster.Values {
    /// <summary>
    /// The one model of what a value is, shared by every view (tables, lists, cards, trees, JSON, the automatic view and the
    /// object comparison): whether it is a scalar, a record or a collection, which fields a record has, how a value is written
    /// inside a cell or a title, and how nested records flatten. A type is supported once here and works everywhere.
    /// <para>Records are read through adapters in this order: adapters registered with <see cref="RegisterAdapter"/>, the
    /// built-in data adapters (dictionaries, data rows, XML elements, JSON objects and nodes, files and folders, dictionary
    /// entries), <see cref="ShapeOptions.Sources"/>, then reflection over public properties when
    /// <see cref="ShapeOptions.UseReflection"/> allows it. Shapers registered with <see cref="Register(Type, Func{object, object})"/>
    /// or <see cref="Register(string, Func{object, object})"/> turn a value into another one first (for example an object into
    /// an ordered dictionary of the fields worth showing).</para>
    /// </summary>
    public static class ValueShaper {
        private static readonly IMemberSource Dictionaries = new DictionaryMemberSource();
        private static readonly IMemberSource Rows = new DataRowMemberSource();
        private static readonly IMemberSource Xml = new XmlMemberSource();
        private static readonly IMemberSource Entries = new DictionaryEntryMemberSource();
        private static readonly IMemberSource Files = new FileSystemMemberSource();
        private static readonly IMemberSource Reflection = new ReflectionMemberSource();
#if NET8_0_OR_GREATER
        private static readonly IMemberSource JsonNodes = new JsonNodeMemberSource();
#endif

        // Members that describe the object's plumbing rather than its data: provider paths, CIM metadata, collection
        // synchronisation, component sites and XML DOM links. Never default columns or tree members.
        private static readonly HashSet<string> PlumbingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "PSPath", "PSParentPath", "PSChildName", "PSDrive", "PSProvider", "PSIsContainer", "PSStandardMembers",
            "PSShowComputerName", "PSComputerName", "CimClass", "CimInstanceProperties", "CimSystemProperties", "SyncRoot",
            "IsSynchronized", "IsFixedSize", "Site", "Container", "OwnerDocument", "ParentNode", "SchemaInfo", "NamespaceURI",
            "Prefix", "PreviousSibling", "NextSibling", "BaseURI", "PropertyNames", "PropertyCount", "AddedProperties",
            "RemovedProperties", "ModifiedProperties"
        };

        private static readonly string[] TitleNames = { "Name", "DisplayName" };
        private static readonly string[] TitleFallbackNames = { "Title", "Label", "Caption", "Key", "Id", "Identity" };
        private static readonly string[] IdNames = { "Id", "ProcessId", "PID", "Key" };
        private static readonly HashSet<string> NotTitleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "FullName", "TypeName", "MachineName", "PSComputerName", "UserName", "ModuleName", "HostName", "BaseName", "DomainName"
        };

        private static readonly object Gate = new object();
        private static readonly List<KeyValuePair<Type, Func<object, object?>>> TypedShapers = new List<KeyValuePair<Type, Func<object, object?>>>();
        private static readonly List<KeyValuePair<string, Func<object, object?>>> NamedShapers = new List<KeyValuePair<string, Func<object, object?>>>();
        private static readonly List<IMemberSource> Adapters = new List<IMemberSource>();

        /// <summary>The text a secure string is shown as: never its content or length.</summary>
        public const string SecureText = "••••••";

        // ---------------------------------------------------------------- registration

        /// <summary>Registers a shaper for a type (and types derived from it): values of that type are replaced by what it returns before they are shown.</summary>
        /// <param name="type">The type.</param>
        /// <param name="shaper">Returns the value to show instead (for example an ordered dictionary), or the value itself.</param>
        public static void Register(Type type, Func<object, object?> shaper) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (shaper == null) {
                throw new ArgumentNullException(nameof(shaper));
            }
            lock (Gate) {
                TypedShapers.RemoveAll(entry => entry.Key == type);
                TypedShapers.Add(new KeyValuePair<Type, Func<object, object?>>(type, shaper));
            }
        }

        /// <summary>Registers a shaper for <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="shaper">Returns the value to show instead.</param>
        public static void Register<T>(Func<T, object?> shaper) {
            if (shaper == null) {
                throw new ArgumentNullException(nameof(shaper));
            }
            Register(typeof(T), value => shaper((T)value));
        }

        /// <summary>
        /// Registers a shaper for a type name: the full name of the value's type or a base type, or any name
        /// <see cref="ShapeOptions.TypeNames"/> reports (PowerShell type names, so deserialized objects can opt in).
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="shaper">Returns the value to show instead.</param>
        public static void Register(string typeName, Func<object, object?> shaper) {
            if (string.IsNullOrEmpty(typeName)) {
                throw new ArgumentException("A type name is required.", nameof(typeName));
            }
            if (shaper == null) {
                throw new ArgumentNullException(nameof(shaper));
            }
            lock (Gate) {
                NamedShapers.RemoveAll(entry => string.Equals(entry.Key, typeName, StringComparison.OrdinalIgnoreCase));
                NamedShapers.Add(new KeyValuePair<string, Func<object, object?>>(typeName, shaper));
            }
        }

        /// <summary>Registers a record adapter, tried before the built-in ones.</summary>
        /// <param name="adapter">The adapter.</param>
        public static void RegisterAdapter(IMemberSource adapter) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            lock (Gate) {
                if (!Adapters.Contains(adapter)) {
                    Adapters.Insert(0, adapter);
                }
            }
        }

        /// <summary>Removes the shaper registered for a type name.</summary>
        /// <param name="typeName">The type name.</param>
        /// <returns>True when one was removed.</returns>
        public static bool Unregister(string typeName) {
            lock (Gate) {
                return NamedShapers.RemoveAll(entry => string.Equals(entry.Key, typeName, StringComparison.OrdinalIgnoreCase)) > 0;
            }
        }

        /// <summary>Removes the shaper registered for a type.</summary>
        /// <param name="type">The type.</param>
        /// <returns>True when one was removed.</returns>
        public static bool Unregister(Type type) {
            lock (Gate) {
                return TypedShapers.RemoveAll(entry => entry.Key == type) > 0;
            }
        }

        /// <summary>Removes a registered record adapter.</summary>
        /// <param name="adapter">The adapter.</param>
        /// <returns>True when it was removed.</returns>
        public static bool UnregisterAdapter(IMemberSource adapter) {
            lock (Gate) {
                return Adapters.Remove(adapter);
            }
        }

        // ---------------------------------------------------------------- shapes

        /// <summary>
        /// The value to show for <paramref name="value"/>: wrappers unwrapped, registered shapers applied, an XML document as
        /// its root element, a text-only XML element as its text, JSON scalars as .NET scalars and <see cref="DBNull"/> as null.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The prepared value.</returns>
        public static object? Prepare(object? value, ShapeOptions? options = null) {
            if (value == null) {
                return null;
            }
            object start = value;
            if (options != null) {
                lock (options.PreparedCache) {
                    if (options.PreparedCache.TryGetValue(start, out object? cached)) {
                        return cached;
                    }
                }
            }
            bool shapedAny = false;
            for (int pass = 0; pass < 4 && value != null; pass++) {
                object original = value;
                Func<object, object?>? shaper = FindShaper(original, options);
                object? current = Unwrap(original, options);
                if (shaper != null) {
                    object? shaped = shaper(current ?? original);
                    object? shapedValue = shaped == null ? null : Unwrap(shaped, options);
                    // A shaper that returns its input (or a new wrapper around it) means "show as it is".
                    if (shaped != null && !ReferenceEquals(shapedValue, current) && !ReferenceEquals(shaped, original)) {
                        value = shaped;
                        shapedAny = true;
                        continue;
                    }
                }
                value = Normalize(current);
                if (ReferenceEquals(value, current)) {
                    break;
                }
            }
            // Whatever pass the loop stopped at, the result is unwrapped and normalised once more.
            value = value == null ? null : Normalize(Unwrap(value, options));
            object? result = value is DBNull ? null : value;
            if (options != null && shapedAny && !start.GetType().IsValueType) {
                // Script shapers make a new object on every call: one call per object and pass.
                lock (options.PreparedCache) {
                    options.PreparedCache[start] = result;
                }
            }
            return result;
        }

        private static object? Unwrap(object value, ShapeOptions? options) => options?.Unwrap != null ? options.Unwrap(value) : value;

        private static object? Normalize(object? value) {
            switch (value) {
                case DBNull _:
                    return null;
                case XmlDocument document:
                    return (object?)document.DocumentElement ?? string.Empty;
                case XmlElement element when XmlMemberSource.IsTextOnly(element):
                    return element.InnerText;
                case XmlAttribute attribute:
                    return attribute.Value;
#if NET8_0_OR_GREATER
                case JsonElement json:
                    switch (json.ValueKind) {
                        case JsonValueKind.String:
                            return json.GetString();
                        case JsonValueKind.Number:
                            return json.TryGetInt64(out long integer) ? integer : (object)json.GetDouble();
                        case JsonValueKind.True:
                            return true;
                        case JsonValueKind.False:
                            return false;
                        case JsonValueKind.Null:
                        case JsonValueKind.Undefined:
                            return null;
                        default:
                            return json;
                    }
                case System.Text.Json.Nodes.JsonValue jsonValue:
                    return JsonNodeMemberSource.ValueOf(jsonValue);
                case System.Text.Json.Nodes.JsonArray jsonArray:
                    return JsonNodeMemberSource.ValueOf(jsonArray);
#endif
                default:
                    return value;
            }
        }

        /// <summary>Whether a value is shown as one value: <see cref="ObjectInspector.IsScalarValue"/> plus secure strings.</summary>
        /// <param name="value">The value (already prepared).</param>
        /// <returns>True for a scalar.</returns>
        public static bool IsScalar(object? value) => ObjectInspector.IsScalarValue(value) || value is SecureString;

        /// <summary>What <paramref name="value"/> is for display: empty, scalar, record or collection.</summary>
        /// <param name="value">The value.</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The shape.</returns>
        public static ValueShape ShapeOf(object? value, ShapeOptions? options = null) {
            object? prepared = Prepare(value, options);
            if (prepared == null) {
                return ValueShape.Empty;
            }
            if (IsScalar(prepared)) {
                return ValueShape.Scalar;
            }
            if (FindStructuralAdapter(prepared) != null) {
                return ValueShape.Record;
            }
            if (TryGetItems(prepared, out _)) {
                return ValueShape.Collection;
            }
            return FindAdapter(prepared, options ?? new ShapeOptions()) != null ? ValueShape.Record : ValueShape.Scalar;
        }

        /// <summary>
        /// Whether a value is read by a built-in data adapter (a dictionary, data row, XML element, JSON object or node, file
        /// or folder, dictionary entry) or is a data table or view: its keys or columns are its fields, whatever wraps it.
        /// </summary>
        /// <param name="value">The value (unwrapped).</param>
        /// <returns>True for a structural value.</returns>
        public static bool IsStructural(object? value) =>
            value != null && (FindStructuralAdapter(value) != null
#if NET8_0_OR_GREATER
            || value is JsonElement || value is System.Text.Json.Nodes.JsonNode
#endif
            || value is XmlNode);

        internal static IMemberSource? RegisteredAdapter(object value) {
            lock (Gate) {
                foreach (IMemberSource adapter in Adapters) {
                    if (adapter.CanRead(value)) {
                        return adapter;
                    }
                }
            }
            return null;
        }

        internal static IMemberSource? FindStructuralAdapter(object value) {
            if (value is IDictionary || Dictionaries.CanRead(value)) {
                return Dictionaries;
            }
            if (Rows.CanRead(value)) {
                return Rows;
            }
            if (Xml.CanRead(value)) {
                return Xml;
            }
            if (Entries.CanRead(value)) {
                return Entries;
            }
            if (Files.CanRead(value)) {
                return Files;
            }
#if NET8_0_OR_GREATER
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Object) {
                return JsonMemberSource.Instance;
            }
            if (JsonNodes.CanRead(value)) {
                return JsonNodes;
            }
#endif
            return null;
        }

        /// <summary>The adapter that reads a record's fields, or null when the value has none.</summary>
        /// <param name="value">The value (prepared).</param>
        /// <param name="options">Options.</param>
        /// <returns>The adapter, or null.</returns>
        public static IMemberSource? FindAdapter(object value, ShapeOptions options) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            options ??= new ShapeOptions();
            lock (Gate) {
                foreach (IMemberSource adapter in Adapters) {
                    if (adapter.CanRead(value)) {
                        return adapter;
                    }
                }
            }
            IMemberSource? structural = FindStructuralAdapter(value);
            if (structural != null) {
                return structural;
            }
            foreach (IMemberSource source in options.Sources) {
                if (source.CanRead(value)) {
                    return source;
                }
            }
            if (IsScalar(value) || TryGetItems(value, out _)) {
                return null;
            }
            return options.UseReflection ? Reflection : null;
        }

        /// <summary>
        /// The items of a collection: arrays, lists and other sequences (never a string, a dictionary, a byte array or an XML
        /// node), a data table's rows, a data set's tables and a JSON array's values.
        /// </summary>
        /// <param name="value">The value (prepared).</param>
        /// <param name="items">The items.</param>
        /// <returns>True for a collection.</returns>
        public static bool TryGetItems(object? value, out IEnumerable? items) {
            items = null;
            switch (value) {
                case null:
                case string _:
                case byte[] _:
                case IDictionary _:
                case XmlNode _:
                    return false;
                case DataTable table:
                    items = table.Rows;
                    return true;
                case DataSet set:
                    items = set.Tables;
                    return true;
#if NET8_0_OR_GREATER
                case JsonElement json when json.ValueKind == JsonValueKind.Array:
                    items = JsonMemberSource.ArrayValues(json);
                    return true;
                case JsonElement _:
                    return false;
                case System.Text.Json.Nodes.JsonObject _:
                    return false;
#endif
            }
            if (Dictionaries.CanRead(value) || IsScalar(value)) {
                return false;
            }
            items = value as IEnumerable;
            return items != null;
        }

        /// <summary>Whether a member name is plumbing (provider paths, CIM metadata, collection synchronisation, DOM links).</summary>
        /// <param name="name">The member name.</param>
        /// <returns>True for plumbing.</returns>
        public static bool IsPlumbing(string name) => name != null && PlumbingNames.Contains(name);

        /// <summary>
        /// The fields of a record, in display order: a Hashtable's keys sorted, other dictionaries in their own order,
        /// columns of a data row, attributes and child elements of an XML element, properties of an object. Plumbing members
        /// of objects (<see cref="IsPlumbing"/>) are left out; dictionary keys and data columns are data and always kept.
        /// </summary>
        /// <param name="record">The record (prepared).</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The fields (values are read lazily), or an empty list when the value is not a record.</returns>
        public static IReadOnlyList<InspectMember> Fields(object record, ShapeOptions? options = null) {
            if (record == null) {
                throw new ArgumentNullException(nameof(record));
            }
            ShapeOptions o = options ?? new ShapeOptions();
            lock (o.FieldCache) {
                if (o.FieldCache.TryGetValue(record, out IReadOnlyList<InspectMember>? cached)) {
                    return cached;
                }
            }
            IMemberSource? adapter = FindAdapter(record, o);
            if (adapter == null) {
                return Array.Empty<InspectMember>();
            }
            bool data = FindStructuralAdapter(record) != null;
            List<InspectMember> fields = new List<InspectMember>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try {
                foreach (InspectMember member in adapter.GetMembers(record)) {
                    if ((!data && IsPlumbing(member.Name)) || !seen.Add(member.Name)) {
                        continue;
                    }
                    fields.Add(member);
                }
            } catch (Exception ex) when (!ObjectInspector.IsCancellation(ex)) {
                // An adapter that cannot enumerate (an unbound directory entry, a disposed COM object): the fields read so far.
            }
            if (!record.GetType().IsValueType) {
                lock (o.FieldCache) {
                    o.FieldCache[record] = fields;
                }
            }
            return fields;
        }

        /// <summary>Reads one field: the value, a <see cref="FieldError"/> when the getter throws, or <see cref="BudgetExceeded"/> once the budget is spent.</summary>
        /// <param name="member">The field.</param>
        /// <param name="budget">The pass budget, or null for none.</param>
        /// <returns>The value or a marker.</returns>
        public static object? Read(InspectMember member, ShapeBudget? budget = null) {
            if (member == null) {
                throw new ArgumentNullException(nameof(member));
            }
            if (budget != null && !budget.TryTake()) {
                return BudgetExceeded.Instance;
            }
            try {
                return member.Value;
            } catch (Exception ex) when (!ObjectInspector.IsCancellation(ex) && !(ex is OutOfMemoryException)) {
                return FieldError.From(ex);
            }
        }

        /// <summary>Reads a named field of a record (case-insensitive), or returns false when the record has no such field.</summary>
        /// <param name="record">The record (prepared).</param>
        /// <param name="name">The field name.</param>
        /// <param name="value">The value or a marker (see <see cref="Read"/>).</param>
        /// <param name="options">Options, or null.</param>
        /// <param name="budget">The pass budget, or null.</param>
        /// <returns>True when the field exists.</returns>
        public static bool TryGetField(object record, string name, out object? value, ShapeOptions? options = null, ShapeBudget? budget = null) {
            value = null;
            if (record == null || name == null) {
                return false;
            }
            if (record is IDictionary dictionary) {
                foreach (DictionaryEntry entry in dictionary) {
                    if (string.Equals(Convert.ToString(entry.Key, CultureInfo.InvariantCulture), name, StringComparison.OrdinalIgnoreCase)) {
                        if (budget != null && !budget.TryTake()) {
                            value = BudgetExceeded.Instance;
                        } else {
                            value = entry.Value;
                        }
                        return true;
                    }
                }
                return false;
            }
            foreach (InspectMember member in Fields(record, options)) {
                if (string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase)) {
                    value = Read(member, budget);
                    return true;
                }
            }
            return false;
        }

        /// <summary>A short type name for a value: the adapter's name, an XML element's tag, or the .NET type name.</summary>
        /// <param name="value">The value (prepared).</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The name.</returns>
        public static string TypeName(object value, ShapeOptions? options = null) {
            if (value == null) {
                return "null";
            }
            IMemberSource? adapter = IsScalar(value) ? null : FindAdapter(value, options ?? new ShapeOptions());
            try {
                return adapter?.GetTypeName(value) ?? ObjectInspector.FriendlyTypeName(value.GetType());
            } catch (Exception ex) when (!ObjectInspector.IsCancellation(ex)) {
                return ObjectInspector.FriendlyTypeName(value.GetType());
            }
        }

        // ---------------------------------------------------------------- titles

        /// <summary>
        /// The index of the field that titles a record, by name: <c>Name</c>, <c>DisplayName</c>, the first other
        /// <c>…Name</c> (ProcessName, ServiceName, SamAccountName), then Title, Label, Caption, Key, Id, Identity.
        /// </summary>
        /// <param name="names">The field names.</param>
        /// <returns>The index, or -1 when no name qualifies.</returns>
        public static int TitleIndex(IReadOnlyList<string> names) {
            if (names == null) {
                throw new ArgumentNullException(nameof(names));
            }
            foreach (int index in TitleCandidates(names)) {
                return index;
            }
            return -1;
        }

        private static IEnumerable<int> TitleCandidates(IReadOnlyList<string> names) {
            foreach (string preferred in TitleNames) {
                for (int i = 0; i < names.Count; i++) {
                    if (string.Equals(names[i], preferred, StringComparison.OrdinalIgnoreCase)) {
                        yield return i;
                    }
                }
            }
            for (int i = 0; i < names.Count; i++) {
                string name = names[i];
                if (name.Length > 4 && name.EndsWith("Name", StringComparison.OrdinalIgnoreCase) && !NotTitleNames.Contains(name)) {
                    yield return i;
                }
            }
            foreach (string preferred in TitleFallbackNames) {
                for (int i = 0; i < names.Count; i++) {
                    if (string.Equals(names[i], preferred, StringComparison.OrdinalIgnoreCase)) {
                        yield return i;
                    }
                }
            }
        }

        /// <summary>
        /// The title of a record: the first name-like field (see <see cref="TitleIndex"/>) with a short text value, then the
        /// first short text field, never a Boolean, date, number (except an Id or Key), collection or nested record; the type
        /// name when nothing qualifies. With <paramref name="withId"/>, an Id next to a name is added: <c>pwsh (90836)</c>.
        /// </summary>
        /// <param name="record">The record (prepared).</param>
        /// <param name="options">Options, or null.</param>
        /// <param name="budget">The pass budget, or null.</param>
        /// <param name="withId">Whether to add the Id after a name.</param>
        /// <returns>The title.</returns>
        public static string Title(object record, ShapeOptions? options = null, ShapeBudget? budget = null, bool withId = true) {
            if (record == null) {
                return string.Empty;
            }
            ShapeOptions o = options ?? new ShapeOptions();
            object prepared = Prepare(record, o) ?? record;
            if (IsScalar(prepared)) {
                return ScalarText(prepared, o);
            }
            IReadOnlyList<InspectMember> fields = Fields(prepared, o);
            List<string> names = fields.Select(field => field.Name).ToList();
            Dictionary<int, object?> read = new Dictionary<int, object?>();
            object? ReadAt(int index) {
                if (!read.TryGetValue(index, out object? value)) {
                    value = Prepare(Read(fields[index], budget), o);
                    read[index] = value;
                }
                return value;
            }
            string? title = null;
            bool fromName = false;
            foreach (int index in TitleCandidates(names)) {
                bool idLike = IdNames.Contains(names[index], StringComparer.OrdinalIgnoreCase) || TitleFallbackNames.Contains(names[index], StringComparer.OrdinalIgnoreCase);
                string? text = TitleText(ReadAt(index), idLike, o);
                if (text != null) {
                    title = text;
                    fromName = !IdNames.Contains(names[index], StringComparer.OrdinalIgnoreCase);
                    break;
                }
            }
            // Objects without a name-like field are titled by their first short text; a dictionary's values are data, so
            // one without a name-like key is titled by its type.
            if (title == null && !(prepared is IDictionary)) {
                for (int i = 0; i < fields.Count && i < 24; i++) {
                    if (ReadAt(i) is string text && text.Length > 0 && text.Length <= 60 && text.IndexOf('\n') < 0) {
                        title = text;
                        break;
                    }
                }
            }
            if (title == null) {
                return TypeName(prepared, o);
            }
            if (withId && fromName) {
                for (int i = 0; i < names.Count; i++) {
                    if (IdNames.Contains(names[i], StringComparer.OrdinalIgnoreCase)) {
                        object? id = ReadAt(i);
                        string? idText = id == null || !IsScalar(id) || id is bool ? null : IdentifierText(id);
                        if (!string.IsNullOrEmpty(idText) && !string.Equals(idText, title, StringComparison.OrdinalIgnoreCase)) {
                            return title + " (" + idText + ")";
                        }
                        break;
                    }
                }
            }
            return title;
        }

        private static string? TitleText(object? value, bool idLike, ShapeOptions options) {
            switch (value) {
                case null:
                case bool _:
                case DateTime _:
                case DateTimeOffset _:
                case TimeSpan _:
                case FieldError _:
                case BudgetExceeded _:
                case SecureString _:
                case byte[] _:
                    return null;
                case string text:
                    return text.Length > 0 && text.Length <= 80 && text.IndexOf('\n') < 0 ? text : null;
                case Enum _:
                case Guid _:
                case Version _:
                case Uri _:
                case System.Net.IPAddress _:
                    return ScalarText(value, options);
            }
            if (PresenterValues.TryGetNumber(value, out _)) {
                return idLike ? IdentifierText(value) : null;
            }
            return null;
        }

        private static string IdentifierText(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

        // ---------------------------------------------------------------- text and cells

        /// <summary>
        /// A value as one line of text, as it appears inside a cell, a title or a joined list: scalars in the culture,
        /// collections by <see cref="ShapeOptions.Collections"/>, a dictionary as <c>Key=Value</c> pairs, another record as
        /// its title, a secure string as <see cref="SecureText"/>, a byte array as its size.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="options">Options, or null.</param>
        /// <param name="budget">The pass budget, or null.</param>
        /// <returns>The text.</returns>
        public static string Text(object? value, ShapeOptions? options = null, ShapeBudget? budget = null) {
            ShapeOptions o = options ?? new ShapeOptions();
            return TextAt(Prepare(value, o), o, budget, 0, null);
        }

        private static string TextAt(object? value, ShapeOptions options, ShapeBudget? budget, int depth, HashSet<object>? visiting) {
            switch (value) {
                case null:
                    return string.Empty;
                case FieldError error:
                    return "⚠ " + error.Message;
                case BudgetExceeded _:
                    return "…";
            }
            if (IsScalar(value)) {
                return ScalarText(value, options);
            }
            visiting ??= new HashSet<object>(ReferenceIdentity.Instance);
            if (!value.GetType().IsValueType && !visiting.Add(value)) {
                return "(cycle)";
            }
            try {
                if (depth > 2) {
                    // Deeply nested values in one cell: their count or type, not another level of items.
                    int? count = FindStructuralAdapter(value) == null && TryGetItems(value, out IEnumerable? deep) ? CountOf(deep!) : null;
                    return count.HasValue ? ItemCount(count.Value, options.ResolvedCulture) : TypeName(value, options);
                }
                if (FindStructuralAdapter(value) == null && TryGetItems(value, out IEnumerable? items)) {
                    return CollectionText(items!, options, budget, depth, visiting, options.Collections == CollectionDisplay.Lines && depth == 0 ? "\n" : null);
                }
                if (value is IDictionary || (FindStructuralAdapter(value) == Dictionaries)) {
                    return DictionaryText(value, options, budget, depth, visiting);
                }
                return Title(value, options, budget);
            } finally {
                visiting.Remove(value);
            }
        }

        private static string DictionaryText(object dictionary, ShapeOptions options, ShapeBudget? budget, int depth, HashSet<object> visiting) {
            IReadOnlyList<InspectMember> fields = Fields(dictionary, options);
            if (fields.Count == 0) {
                return EmptyText;
            }
            int limit = Math.Max(1, options.ItemLimit);
            List<string> parts = new List<string>();
            for (int i = 0; i < fields.Count && i < limit; i++) {
                object? value = Prepare(Read(fields[i], budget), options);
                string text = value != null && !IsScalar(value) ? "{" + TextAt(value, options, budget, depth + 1, visiting) + "}" : TextAt(value, options, budget, depth + 1, visiting);
                parts.Add(fields[i].Name + "=" + text);
            }
            string joined = string.Join(options.Separator, parts);
            int more = fields.Count - parts.Count;
            return more > 0 ? joined + " … +" + more.ToString("N0", options.ResolvedCulture) : joined;
        }

        private static string CollectionText(IEnumerable items, ShapeOptions options, ShapeBudget? budget, int depth, HashSet<object> visiting, string? lineSeparator) {
            int limit = Math.Max(1, options.ItemLimit);
            CultureInfo culture = options.ResolvedCulture;
            if (options.Collections == CollectionDisplay.Count) {
                int? known = CountOf(items);
                int count = known ?? Enumerate(items, int.MaxValue, budget, out _).Count;
                return ItemCount(count, culture);
            }
            List<object?> taken = Enumerate(items, depth == 0 ? limit : Math.Min(limit, 3), budget, out bool more);
            if (taken.Count == 0) {
                return EmptyText;
            }
            List<string> parts = new List<string>(taken.Count);
            foreach (object? item in taken) {
                object? prepared = Prepare(item, options);
                string text = TextAt(prepared, options, budget, depth + 1, visiting);
                if (prepared != null && FindStructuralAdapter(prepared) == null && TryGetItems(prepared, out _)) {
                    text = "[" + text + "]";
                }
                parts.Add(text);
            }
            int? total = CountOf(items);
            int remaining = total.HasValue ? Math.Max(0, total.Value - taken.Count) : (more ? -1 : 0);
            string tail = remaining > 0 ? remaining.ToString("N0", culture) : "…";
            if (lineSeparator != null) {
                string lines = string.Join(lineSeparator, parts);
                return remaining != 0 ? lines + lineSeparator + "+" + tail + " more" : lines;
            }
            string joined = string.Join(options.Separator, parts);
            if (remaining == 0) {
                return joined;
            }
            return options.Collections == CollectionDisplay.FirstN ? joined + " +" + tail + " more" : joined + " … +" + tail;
        }

        private const string EmptyText = "—";

        private static string ItemCount(int count, CultureInfo culture) =>
            count == 1 ? "1 item" : count.ToString("N0", culture) + " items";

        private static List<object?> Enumerate(IEnumerable items, int limit, ShapeBudget? budget, out bool more) {
            List<object?> taken = new List<object?>();
            more = false;
            IEnumerator? iterator = null;
            try {
                iterator = items.GetEnumerator();
                while (iterator.MoveNext()) {
                    if (taken.Count >= limit || (budget != null && !budget.TryTake())) {
                        more = true;
                        break;
                    }
                    taken.Add(iterator.Current);
                }
            } catch (Exception ex) when (!ObjectInspector.IsCancellation(ex)) {
                taken.Add(FieldError.From(ex));
            } finally {
                (iterator as IDisposable)?.Dispose();
            }
            return taken;
        }

        private static int? CountOf(IEnumerable items) {
            try {
                return items is ICollection collection ? collection.Count : (int?)null;
            } catch (Exception ex) when (!ObjectInspector.IsCancellation(ex)) {
                return null;
            }
        }

        /// <summary>A scalar as text: secure strings masked, byte arrays as their size, dates as <c>yyyy-MM-dd HH:mm</c>, numbers and the rest in the culture.</summary>
        /// <param name="value">The scalar.</param>
        /// <param name="options">Options, or null.</param>
        /// <returns>The text.</returns>
        public static string ScalarText(object? value, ShapeOptions? options = null) {
            CultureInfo culture = (options ?? new ShapeOptions()).ResolvedCulture;
            switch (value) {
                case null:
                case DBNull _:
                    return string.Empty;
                case SecureString _:
                    return SecureText;
                case byte[] bytes:
                    return bytes.Length == 1 ? "1 byte" : bytes.Length.ToString("N0", culture) + " bytes";
                case string text:
                    return text;
                case bool flag:
                    return flag ? "yes" : "no";
                case DateTime date:
                    return date == DateTime.MinValue ? "never" : date.ToString(date.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                case DateTimeOffset offset:
                    return offset.ToString("yyyy-MM-dd HH:mm zzz", CultureInfo.InvariantCulture);
                case ConsoleMonster.Text.RichText rich:
                    return ConsoleMonster.Text.Markup.Strip(rich.ToMarkup());
            }
            return PresenterValues.ToText(value, culture).Replace("\r\n", " ").Replace('\n', ' ');
        }

        /// <summary>
        /// The value a table cell holds for <paramref name="value"/>: scalars stay typed (so presenters format numbers, dates and
        /// Booleans), secure strings and byte arrays become their safe text, collections, dictionaries and records become one
        /// line of text (see <see cref="Text"/>), an empty collection a muted dash, a failed getter a warning marker.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="options">Options, or null.</param>
        /// <param name="budget">The pass budget, or null.</param>
        /// <returns>A scalar, a string or a <see cref="TableCell"/>.</returns>
        public static object? Cell(object? value, ShapeOptions? options = null, ShapeBudget? budget = null) {
            ShapeOptions o = options ?? new ShapeOptions();
            object? prepared = Prepare(value, o);
            switch (prepared) {
                case null:
                    return null;
                case FieldError error:
                    return TableCell.ForStatus(StatusKind.Warn, error.Message);
                case BudgetExceeded _:
                    return new TableCell("[muted]…[/]");
                case SecureString _:
                    return SecureText;
                case byte[] _:
                    return ScalarText(prepared, o);
                case TableCell _:
                    return prepared;
            }
            if (prepared is string multiline && multiline.IndexOf('\n') >= 0) {
                // Text with line breaks (descriptions, registry values from a format view) keeps its lines in the cell.
                return new TableCell(ConsoleMonster.Text.Markup.Escape(multiline.Replace("\r\n", "\n").TrimEnd('\n')));
            }
            if (IsScalar(prepared)) {
                return prepared;
            }
            string text = TextAt(prepared, o, budget, 0, null);
            if (text == EmptyText) {
                return new TableCell("[muted]" + EmptyText + "[/]");
            }
            if (text.IndexOf('\n') >= 0) {
                return new TableCell(ConsoleMonster.Text.Markup.Escape(text));
            }
            return text;
        }

        // ---------------------------------------------------------------- flatten

        /// <summary>
        /// The fields of a record with nested records spread into dotted names (<c>Owner.Name</c>, <c>Owner.Id</c>) down to
        /// <paramref name="depth"/> levels. Collections stay one value; a record seen again on the way down becomes
        /// <c>(cycle)</c>.
        /// </summary>
        /// <param name="record">The record.</param>
        /// <param name="depth">How many levels of nesting to spread (1 = one level).</param>
        /// <param name="options">Options, or null.</param>
        /// <param name="budget">The pass budget, or null.</param>
        /// <returns>The dotted names and values.</returns>
        public static IReadOnlyList<KeyValuePair<string, object?>> Flatten(object record, int depth, ShapeOptions? options = null, ShapeBudget? budget = null) {
            ShapeOptions o = options ?? new ShapeOptions();
            List<KeyValuePair<string, object?>> result = new List<KeyValuePair<string, object?>>();
            object? prepared = Prepare(record, o);
            if (prepared == null) {
                return result;
            }
            FlattenInto(prepared, string.Empty, Math.Max(0, depth), o, budget, result, new HashSet<object>(ReferenceIdentity.Instance));
            return result;
        }

        private static void FlattenInto(object record, string prefix, int depth, ShapeOptions options, ShapeBudget? budget, List<KeyValuePair<string, object?>> result, HashSet<object> visiting) {
            if (!record.GetType().IsValueType) {
                visiting.Add(record);
            }
            foreach (InspectMember field in Fields(record, options)) {
                string name = prefix + field.Name;
                object? value = Prepare(Read(field, budget), options);
                if (depth > 0 && value != null && !IsScalar(value) && !(value is FieldError) && !(value is BudgetExceeded) && ShapeOf(value, options) == ValueShape.Record) {
                    if (!value.GetType().IsValueType && visiting.Contains(value)) {
                        result.Add(new KeyValuePair<string, object?>(name, "(cycle)"));
                        continue;
                    }
                    FlattenInto(value, name + ".", depth - 1, options, budget, result, visiting);
                    continue;
                }
                result.Add(new KeyValuePair<string, object?>(name, value));
            }
            visiting.Remove(record);
        }

        // ---------------------------------------------------------------- shaper lookup

        private static Func<object, object?>? FindShaper(object value, ShapeOptions? options) {
            lock (Gate) {
                if (TypedShapers.Count == 0 && NamedShapers.Count == 0) {
                    return null;
                }
            }
            object raw = options?.Unwrap != null ? options.Unwrap(value) ?? value : value;
            lock (Gate) {
                for (Type? type = raw.GetType(); type != null; type = type.BaseType) {
                    foreach (KeyValuePair<Type, Func<object, object?>> entry in TypedShapers) {
                        if (entry.Key == type) {
                            return entry.Value;
                        }
                    }
                }
                foreach (KeyValuePair<Type, Func<object, object?>> entry in TypedShapers) {
                    if (entry.Key.IsInterface && entry.Key.IsInstanceOfType(raw)) {
                        return entry.Value;
                    }
                }
                if (NamedShapers.Count == 0) {
                    return null;
                }
            }
            List<string> names = new List<string>();
            if (options?.TypeNames != null) {
                try {
                    names.AddRange(options.TypeNames(value));
                } catch (Exception ex) when (!ObjectInspector.IsCancellation(ex)) {
                    // Type names are a hint; a failing provider falls back to the .NET type chain.
                }
            }
            for (Type? type = raw.GetType(); type != null; type = type.BaseType) {
                if (type.FullName != null) {
                    names.Add(type.FullName);
                }
            }
            lock (Gate) {
                foreach (string name in names) {
                    foreach (KeyValuePair<string, Func<object, object?>> entry in NamedShapers) {
                        if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase)) {
                            return entry.Value;
                        }
                    }
                }
            }
            return null;
        }

        internal sealed class ReferenceIdentity : IEqualityComparer<object> {
            public static readonly ReferenceIdentity Instance = new ReferenceIdentity();

            public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
