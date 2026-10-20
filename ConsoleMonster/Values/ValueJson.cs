using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security;
using System.Text;
using ConsoleMonster.Data;

namespace ConsoleMonster.Values {
    /// <summary>
    /// JSON for display, written with the shared value rules (<see cref="ValueShaper"/>): records become objects (dictionary
    /// keys of any type as text), collections arrays, a record seen again on the way down <c>"(cycle)"</c>, values past the
    /// depth limit <c>"…"</c>, secure strings masked and byte arrays as their size. Text that already is JSON is re-indented.
    /// </summary>
    public static class ValueJson {
        /// <summary>
        /// A value as indented JSON. Finite documents are written in full, without the display node/time limits;
        /// collection enumeration must terminate. Record getters retain the shared stall protection.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="depth">How many levels of records and collections to write (default 4).</param>
        /// <param name="options">Shape options, or null.</param>
        /// <returns>The JSON text.</returns>
        public static string Serialize(object? value, int depth = 4, ShapeOptions? options = null) {
            ShapeOptions o = options ?? new ShapeOptions();
            StringBuilder builder = new StringBuilder();
            Write(builder, value, Math.Max(0, depth), 0, o, ShapeBudget.For(o), new HashSet<object>(ValueShaper.ReferenceIdentity.Instance));
            return builder.ToString();
        }

        private static void Write(StringBuilder builder, object? value, int depth, int indent, ShapeOptions options, ShapeBudget budget, HashSet<object> visiting) {
            object? prepared = ValueShaper.Prepare(value, options);
            switch (prepared) {
                case null:
                    builder.Append("null");
                    return;
                case bool flag:
                    builder.Append(flag ? "true" : "false");
                    return;
                case string text:
                    Quote(builder, text);
                    return;
                case FieldError error:
                    Quote(builder, "error: " + error.Message);
                    return;
                case BudgetExceeded _:
                    Quote(builder, "…");
                    return;
                case SecureString _:
                    Quote(builder, ValueShaper.SecureText);
                    return;
                case DateTime date:
                    // Round-trip form: UTC keeps its Z, local times their offset.
                    Quote(builder, date.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture));
                    return;
                case DateTimeOffset offset:
                    Quote(builder, offset.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture));
                    return;
                case Enum _:
                    Quote(builder, Convert.ToString(prepared, CultureInfo.InvariantCulture) ?? string.Empty);
                    return;
                case double d when double.IsNaN(d) || double.IsInfinity(d):
                    Quote(builder, d.ToString(CultureInfo.InvariantCulture));
                    return;
                case float f when float.IsNaN(f) || float.IsInfinity(f):
                    Quote(builder, f.ToString(CultureInfo.InvariantCulture));
                    return;
            }
            if (prepared is IConvertible && Presenters.PresenterValues.TryGetNumber(prepared, out _) && !(prepared is char)) {
                builder.Append(Convert.ToString(prepared, CultureInfo.InvariantCulture));
                return;
            }
            if (ValueShaper.IsScalar(prepared)) {
                Quote(builder, ValueShaper.ScalarText(prepared, new ShapeOptions { Culture = CultureInfo.InvariantCulture }));
                return;
            }
            bool tracked = !prepared.GetType().IsValueType;
            object identity = options.Identity != null && value != null ? options.Identity(value) : prepared;
            if (tracked && visiting.Contains(identity)) {
                Quote(builder, "(cycle)");
                return;
            }
            if (depth <= 0) {
                Quote(builder, ValueShaper.Text(prepared, options));
                return;
            }
            if (tracked) {
                visiting.Add(identity);
            }
            try {
                if (ValueShaper.FindStructuralAdapter(prepared) == null && ValueShaper.TryGetItems(prepared, out IEnumerable? items)) {
                    WriteArray(builder, items!, depth, indent, options, budget, visiting);
                    return;
                }
                IReadOnlyList<InspectMember> fields = ValueShaper.FindAdapter(prepared, options) != null ? ValueShaper.Fields(prepared, options) : Array.Empty<InspectMember>();
                if (fields.Count == 0 && ValueShaper.ShapeOf(prepared, options) != ValueShape.Record) {
                    Quote(builder, ValueShaper.Text(prepared, options));
                    return;
                }
                builder.Append('{');
                for (int i = 0; i < fields.Count; i++) {
                    builder.Append(i == 0 ? "\n" : ",\n");
                    Indent(builder, indent + 1);
                    Quote(builder, fields[i].Name);
                    builder.Append(": ");
                    // Only stalling getters are cut short (a large document is never truncated by a node or time cap).
                    InspectMember field = fields[i];
                    Write(builder, budget.Guard(field.Name, () => ValueShaper.Read(field)), depth - 1, indent + 1, options, budget, visiting);
                }
                if (fields.Count > 0) {
                    builder.Append('\n');
                    Indent(builder, indent);
                }
                builder.Append('}');
            } finally {
                if (tracked) {
                    visiting.Remove(identity);
                }
            }
        }


        private static void WriteArray(StringBuilder builder, IEnumerable items, int depth, int indent, ShapeOptions options, ShapeBudget budget, HashSet<object> visiting) {
            builder.Append('[');
            int count = 0;
            IEnumerator? iterator = null;
            try {
                iterator = items.GetEnumerator();
                while (iterator.MoveNext()) {
                    builder.Append(count == 0 ? "\n" : ",\n");
                    Indent(builder, indent + 1);
                    Write(builder, iterator.Current, depth - 1, indent + 1, options, budget, visiting);
                    count++;
                }
            } catch (Exception ex) when (!ObjectInspector.IsCancellation(ex)) {
                builder.Append(count == 0 ? "\n" : ",\n");
                Indent(builder, indent + 1);
                Quote(builder, "error: " + ex.Message);
                count++;
            } finally {
                (iterator as IDisposable)?.Dispose();
            }
            if (count > 0) {
                builder.Append('\n');
                Indent(builder, indent);
            }
            builder.Append(']');
        }

        private static void Indent(StringBuilder builder, int level) => builder.Append(' ', level * 2);

        private static void Quote(StringBuilder builder, string text) {
            builder.Append('"');
            foreach (char c in text) {
                switch (c) {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < 0x20) {
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        } else {
                            builder.Append(c);
                        }
                        break;
                }
            }
            builder.Append('"');
        }

        /// <summary>
        /// Re-indents a JSON object or array with two spaces per level, or returns false when the text is not valid JSON.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="indented">The indented JSON.</param>
        /// <returns>True when the text is JSON.</returns>
        public static bool TryReindent(string text, out string indented) {
            indented = string.Empty;
            if (text == null) {
                return false;
            }
            string trimmed = text.Trim(' ', '\t', '\r', '\n');
            if (!JsonText.IsContainer(trimmed)) {
                return false;
            }
            StringBuilder builder = new StringBuilder(trimmed.Length * 2);
            Stack<char> open = new Stack<char>();
            bool inString = false;
            bool escaped = false;
            bool done = false;
            for (int i = 0; i < trimmed.Length; i++) {
                char c = trimmed[i];
                if (done) {
                    if (!char.IsWhiteSpace(c)) {
                        return false;
                    }
                    continue;
                }
                if (inString) {
                    builder.Append(c);
                    if (escaped) {
                        escaped = false;
                    } else if (c == '\\') {
                        escaped = true;
                    } else if (c == '"') {
                        inString = false;
                    } else if (c == '\n') {
                        return false;
                    }
                    continue;
                }
                switch (c) {
                    case '"':
                        inString = true;
                        builder.Append(c);
                        break;
                    case '{':
                    case '[':
                        open.Push(c);
                        builder.Append(c);
                        int next = NextSignificant(trimmed, i + 1);
                        if (next >= 0 && (trimmed[next] == '}' || trimmed[next] == ']')) {
                            break;
                        }
                        builder.Append('\n');
                        Indent(builder, open.Count);
                        break;
                    case '}':
                    case ']':
                        if (open.Count == 0 || (c == '}' ? '{' : '[') != open.Peek()) {
                            return false;
                        }
                        bool empty = builder.Length > 0 && (builder[builder.Length - 1] == '{' || builder[builder.Length - 1] == '[');
                        open.Pop();
                        if (!empty) {
                            builder.Append('\n');
                            Indent(builder, open.Count);
                        }
                        builder.Append(c);
                        done = open.Count == 0;
                        break;
                    case ',':
                        if (open.Count == 0) {
                            return false;
                        }
                        builder.Append(",\n");
                        Indent(builder, open.Count);
                        break;
                    case ':':
                        builder.Append(": ");
                        break;
                    default:
                        if (!char.IsWhiteSpace(c)) {
                            builder.Append(c);
                        }
                        break;
                }
            }
            if (inString || open.Count > 0) {
                return false;
            }
            indented = builder.ToString();
            return true;
        }

        private static int NextSignificant(string text, int start) {
            for (int i = start; i < text.Length; i++) {
                if (!char.IsWhiteSpace(text[i])) {
                    return i;
                }
            }
            return -1;
        }
    }
}
