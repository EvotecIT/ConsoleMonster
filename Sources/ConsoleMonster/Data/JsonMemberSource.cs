#if NET8_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace ConsoleMonster.Data {
    internal sealed class JsonMemberSource : IMemberSource {
        public static readonly JsonMemberSource Instance = new JsonMemberSource();

        private JsonMemberSource() { }

        public bool CanRead(object value) =>
            value is JsonElement element && element.ValueKind == JsonValueKind.Object;

        public string GetTypeName(object value) =>
            ((JsonElement)value).ValueKind == JsonValueKind.Array ? "JsonArray" : "JsonObject";

        public IEnumerable<InspectMember> GetMembers(object value) {
            JsonElement element = (JsonElement)value;
            foreach (JsonProperty property in element.EnumerateObject()) {
                JsonElement child = property.Value;
                yield return new InspectMember(property.Name, () => ConvertValue(child));
            }
        }

        public static IEnumerable ArrayValues(JsonElement element) => new JsonArrayValues(element);

        private static object? ConvertValue(JsonElement element) {
            switch (element.ValueKind) {
                case JsonValueKind.Object:
                    return element;
                case JsonValueKind.Array:
                    return element;
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long integer))
                        return integer;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    return Convert.ToString(element, CultureInfo.InvariantCulture);
            }
        }

        private sealed class JsonArrayValues : IReadOnlyCollection<object?> {
            private readonly JsonElement _element;

            public JsonArrayValues(JsonElement element) {
                _element = element;
            }

            public int Count => _element.GetArrayLength();

            public IEnumerator<object?> GetEnumerator() {
                foreach (JsonElement child in _element.EnumerateArray())
                    yield return ConvertValue(child);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
#endif
