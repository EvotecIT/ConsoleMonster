using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Logging {
    internal sealed class LogMessage {
        internal sealed class Part {
            internal Part(string text, ThemeToken? role = null) { Text = text; Role = role; }
            internal string Text { get; }
            internal ThemeToken? Role { get; }
        }

        internal List<Part> Parts { get; } = new List<Part>();
        internal string PlainText => string.Concat(Parts.Select(part => part.Text));

        internal static LogMessage Create(string? template, string fallback, IEnumerable<LogProperty> properties,
            IFormatProvider? provider = null, bool preserveFormattedMessage = false, IReadOnlyList<LogProperty>? templateArguments = null) {
            provider = provider ?? CultureInfo.InvariantCulture;
            Dictionary<string, LogProperty> values = new Dictionary<string, LogProperty>(StringComparer.Ordinal);
            Dictionary<string, Queue<LogProperty>> bindings = new Dictionary<string, Queue<LogProperty>>(StringComparer.Ordinal);
            foreach (LogProperty property in properties) {
                values[property.Name] = property;
                if (!bindings.TryGetValue(property.Name, out Queue<LogProperty>? occurrences)) {
                    occurrences = new Queue<LogProperty>();
                    bindings.Add(property.Name, occurrences);
                }
                occurrences.Enqueue(property);
            }
            HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
            LogMessage message = new LogMessage();
            if (template == null) { message.Parts.Add(new Part(fallback)); }
            else { Parse(message, template, bindings, used, provider, templateArguments); }
            // ILogger allows a custom formatter. Keep its text when it differs from the template's normal rendering.
            if (preserveFormattedMessage && message.PlainText != fallback) {
                message.Parts.Clear();
                message.Parts.Add(new Part(fallback));
            }
            foreach (LogProperty property in values.Values.Where(property => !used.Contains(property.Name)).OrderBy(property => property.Name, StringComparer.Ordinal)) {
                message.Parts.Add(new Part(" " + property.Name + "=" + property.Tail(provider), ThemeToken.Muted));
            }
            return message;
        }

        private static void Parse(LogMessage message, string template, Dictionary<string, Queue<LogProperty>> bindings,
            HashSet<string> used, IFormatProvider provider, IReadOnlyList<LogProperty>? templateArguments) {
            StringBuilder literal = new StringBuilder();
            int argumentIndex = 0;
            for (int i = 0; i < template.Length;) {
                char current = template[i];
                if ((current == '{' || current == '}') && i + 1 < template.Length && template[i + 1] == current) {
                    literal.Append(current); i += 2; continue;
                }
                int end = current == '{' ? template.IndexOf('}', i + 1) : -1;
                if (end < 0) { literal.Append(current); i++; continue; }
                string hole = template.Substring(i + 1, end - i - 1);
                int colon = hole.IndexOf(':');
                int comma = hole.IndexOf(',');
                if (comma > colon && colon >= 0) { comma = -1; }
                int nameEnd = comma >= 0 ? comma : colon >= 0 ? colon : hole.Length;
                string name = hole.Substring(0, nameEnd);
                if (name.StartsWith("@", StringComparison.Ordinal) || name.StartsWith("$", StringComparison.Ordinal)) { name = name.Substring(1); }
                if (!bindings.TryGetValue(name, out Queue<LogProperty>? occurrences)) {
                    literal.Append(template, i, end - i + 1); i = end + 1; continue;
                }
                LogProperty property = occurrences.Count > 1 ? occurrences.Dequeue() : occurrences.Peek();
                // NLog renames duplicate argument keys; their native occurrence order still matches the template.
                if (templateArguments != null && argumentIndex < templateArguments.Count) { property = templateArguments[argumentIndex++]; }
                string? format = colon >= 0 ? hole.Substring(colon + 1) : null;
                int alignment = 0;
                if (comma >= 0 && !int.TryParse(hole.Substring(comma + 1, (colon >= 0 ? colon : hole.Length) - comma - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out alignment)) {
                    literal.Append(template, i, end - i + 1); i = end + 1; continue;
                }
                string rendered = property.Format(format, provider);
                // Keep a malformed event from allocating an unbounded padded string.
                if (alignment > 10000 || alignment < -10000) {
                    literal.Append(template, i, end - i + 1); i = end + 1; continue;
                }
                if (alignment != 0) { rendered = alignment < 0 ? rendered.PadRight(-alignment) : rendered.PadLeft(alignment); }
                if (literal.Length > 0) { message.Parts.Add(new Part(literal.ToString())); literal.Clear(); }
                message.Parts.Add(new Part(rendered, property.Role));
                used.Add(property.Name);
                i = end + 1;
            }
            if (literal.Length > 0) { message.Parts.Add(new Part(literal.ToString())); }
        }
    }
}
