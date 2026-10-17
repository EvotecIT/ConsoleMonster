using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace ConsoleMonster.Data {
    internal sealed class DictionaryMemberSource : IMemberSource {
        public bool CanRead(object value) => value is IDictionary || IsStringObjectPairs(value);

        public string GetTypeName(object value) =>
            value is IDictionary
                ? ObjectInspector.FriendlyTypeName(value.GetType())
                : "PSCustomObject";

        public IEnumerable<InspectMember> GetMembers(object value) {
            if (value is IDictionary dictionary) {
                foreach (DictionaryEntry entry in dictionary) {
                    object? memberValue = entry.Value;
                    yield return new InspectMember(
                        Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty,
                        memberValue
                    );
                }
                yield break;
            }
            foreach (
                KeyValuePair<string, object> entry in (IEnumerable<KeyValuePair<string, object>>)
                    value
            )
                yield return new InspectMember(
                    Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty,
                    entry.Value
                );
        }

        private static bool IsStringObjectPairs(object value) {
            if (!(value is IEnumerable) || value is string)
                return false;
            Type? pair = null;
            foreach (Type candidate in value.GetType().GetInterfaces()) {
                if (
                    candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                ) {
                    pair = candidate.GetGenericArguments()[0];
                    break;
                }
            }
            return pair != null
                && pair.IsGenericType
                && pair.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                && pair.GetGenericArguments()[0] == typeof(string)
                && pair.GetGenericArguments()[1] == typeof(object);
        }
    }
}
