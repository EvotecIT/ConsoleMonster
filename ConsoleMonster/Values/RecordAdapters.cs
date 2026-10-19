using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Xml;
using ConsoleMonster.Data;

namespace ConsoleMonster.Values {
    // Data rows: only the table's columns (never RowState, ItemArray, Table or HasErrors). DBNull reads as null.
    internal sealed class DataRowMemberSource : IMemberSource {
        public bool CanRead(object value) => value is DataRow || value is DataRowView;

        public string GetTypeName(object value) => Row(value).Table?.TableName is string name && name.Length > 0 ? name : "DataRow";

        public IEnumerable<InspectMember> GetMembers(object value) {
            DataRow row = Row(value);
            if (row.Table == null) {
                yield break;
            }
            foreach (DataColumn column in row.Table.Columns) {
                DataColumn captured = column;
                yield return new InspectMember(captured.ColumnName, () => {
                    object item = row[captured];
                    return item is DBNull ? null : item;
                });
            }
        }

        private static DataRow Row(object value) => value is DataRowView view ? view.Row : (DataRow)value;
    }

    // XML elements: attributes first, then child elements by name. A child with only text reads as its text; repeated
    // children read as a list. Namespace declarations and DOM plumbing (ParentNode, OwnerDocument...) are never fields.
    internal sealed class XmlMemberSource : IMemberSource {
        public bool CanRead(object value) => value is XmlElement;

        public string GetTypeName(object value) => ((XmlElement)value).LocalName;

        public IEnumerable<InspectMember> GetMembers(object value) {
            XmlElement element = (XmlElement)value;
            foreach (XmlAttribute attribute in element.Attributes) {
                if (attribute.Name == "xmlns" || attribute.Prefix == "xmlns") {
                    continue;
                }
                yield return new InspectMember(attribute.Name, attribute.Value);
            }
            List<string> order = new List<string>();
            Dictionary<string, List<XmlElement>> groups = new Dictionary<string, List<XmlElement>>(StringComparer.Ordinal);
            foreach (XmlNode node in element.ChildNodes) {
                if (node is XmlElement child) {
                    if (!groups.TryGetValue(child.Name, out List<XmlElement>? list)) {
                        list = new List<XmlElement>();
                        groups.Add(child.Name, list);
                        order.Add(child.Name);
                    }
                    list.Add(child);
                }
            }
            foreach (string name in order) {
                List<XmlElement> list = groups[name];
                if (list.Count == 1) {
                    yield return new InspectMember(name, ValueOf(list[0]));
                } else {
                    List<object?> values = new List<object?>(list.Count);
                    foreach (XmlElement child in list) {
                        values.Add(ValueOf(child));
                    }
                    yield return new InspectMember(name, values);
                }
            }
            if (order.Count == 0 && element.Attributes.Count > 0 && element.InnerText.Length > 0) {
                yield return new InspectMember("#text", element.InnerText);
            }
        }

        // An element with neither attributes nor child elements is just its text.
        internal static object? ValueOf(XmlElement element) => IsTextOnly(element) ? element.InnerText : element;

        internal static bool IsTextOnly(XmlElement element) {
            if (element.Attributes.Count > 0) {
                return false;
            }
            foreach (XmlNode node in element.ChildNodes) {
                if (node is XmlElement) {
                    return false;
                }
            }
            return true;
        }
    }

    // DictionaryEntry (Hashtable.GetEnumerator()) without reflection.
    internal sealed class DictionaryEntryMemberSource : IMemberSource {
        public bool CanRead(object value) => value is DictionaryEntry;

        public string GetTypeName(object value) => "DictionaryEntry";

        public IEnumerable<InspectMember> GetMembers(object value) {
            DictionaryEntry entry = (DictionaryEntry)value;
            yield return new InspectMember("Key", entry.Key);
            yield return new InspectMember("Value", entry.Value);
        }
    }

    // Files and folders: what people look at (name, size, time, attributes) and, for a folder, its items, so a tree shows
    // the folder structure instead of the Parent/Root object graph.
    internal sealed class FileSystemMemberSource : IMemberSource {
        public bool CanRead(object value) => value is FileSystemInfo;

        public string GetTypeName(object value) => value is DirectoryInfo ? "Directory" : "File";

        public IEnumerable<InspectMember> GetMembers(object value) {
            FileSystemInfo item = (FileSystemInfo)value;
            yield return new InspectMember("Name", item.Name);
            if (item is FileInfo file) {
                yield return new InspectMember("Length", () => file.Exists ? (object)file.Length : null);
            }
            yield return new InspectMember("LastWriteTime", () => item.Exists ? (object)item.LastWriteTime : null);
            yield return new InspectMember("Attributes", () => item.Exists ? (object)item.Attributes : null);
            if (item is DirectoryInfo directory) {
                yield return new InspectMember("Items", () => directory.Exists ? (object)new LazyItems(directory) : null);
            }
        }

        // Enumerated only as far as the reader asks (inspection limits and budgets apply), never as a whole array.
        private sealed class LazyItems : IEnumerable {
            private readonly DirectoryInfo _directory;

            public LazyItems(DirectoryInfo directory) {
                _directory = directory;
            }

            public IEnumerator GetEnumerator() {
                IEnumerator<FileSystemInfo> inner;
                try {
                    inner = _directory.EnumerateFileSystemInfos().GetEnumerator();
                } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException) {
                    yield break;
                }
                using (inner) {
                    while (true) {
                        FileSystemInfo current;
                        try {
                            if (!inner.MoveNext()) {
                                yield break;
                            }
                            current = inner.Current;
                        } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException) {
                            yield break;
                        }
                        yield return current;
                    }
                }
            }

            public override string ToString() => _directory.FullName;
        }
    }

#if NET8_0_OR_GREATER
    // System.Text.Json.Nodes: objects are records, arrays collections and values scalars.
    internal sealed class JsonNodeMemberSource : IMemberSource {
        public bool CanRead(object value) => value is System.Text.Json.Nodes.JsonObject;

        public string GetTypeName(object value) => "JsonObject";

        public IEnumerable<InspectMember> GetMembers(object value) {
            foreach (KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> property in (System.Text.Json.Nodes.JsonObject)value) {
                System.Text.Json.Nodes.JsonNode? node = property.Value;
                yield return new InspectMember(property.Key, () => ValueOf(node));
            }
        }

        internal static object? ValueOf(System.Text.Json.Nodes.JsonNode? node) {
            switch (node) {
                case null:
                    return null;
                case System.Text.Json.Nodes.JsonObject obj:
                    return obj;
                case System.Text.Json.Nodes.JsonArray array:
                    List<object?> items = new List<object?>(array.Count);
                    foreach (System.Text.Json.Nodes.JsonNode? item in array) {
                        items.Add(ValueOf(item));
                    }
                    return items;
                case System.Text.Json.Nodes.JsonValue jsonValue:
                    if (jsonValue.TryGetValue(out System.Text.Json.JsonElement element)) {
                        switch (element.ValueKind) {
                            case System.Text.Json.JsonValueKind.String:
                                return element.GetString();
                            case System.Text.Json.JsonValueKind.Number:
                                return element.TryGetInt64(out long integer) ? integer : (object)element.GetDouble();
                            case System.Text.Json.JsonValueKind.True:
                                return true;
                            case System.Text.Json.JsonValueKind.False:
                                return false;
                            default:
                                return null;
                        }
                    }
                    if (jsonValue.TryGetValue(out string? text)) {
                        return text;
                    }
                    if (jsonValue.TryGetValue(out bool flag)) {
                        return flag;
                    }
                    if (jsonValue.TryGetValue(out long number)) {
                        return number;
                    }
                    if (jsonValue.TryGetValue(out double real)) {
                        return real;
                    }
                    return jsonValue.ToJsonString();
                default:
                    return Convert.ToString(node, CultureInfo.InvariantCulture);
            }
        }
    }
#endif
}
