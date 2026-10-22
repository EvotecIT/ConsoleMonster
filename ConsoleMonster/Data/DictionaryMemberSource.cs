using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ConsoleMonster.Data {
    internal sealed class DictionaryMemberSource : IMemberSource {
        public bool CanRead(object value) => value is IDictionary || IsStringObjectPairs(value);

        public string GetTypeName(object value) =>
            value is IDictionary
                ? ObjectInspector.FriendlyTypeName(value.GetType())
                : "PSCustomObject";

        public IEnumerable<InspectMember> GetMembers(object value) => GetMembers(value, int.MaxValue);

        // Interactive inspection needs a bounded page; static inspection retains its complete, sorted key set.
        internal IEnumerable<InspectMember> GetMembers(object value, int limit) {
            if (value is IDictionary dictionary) {
                // A Hashtable has no order of its own (PowerShell's @{ }): its keys are shown sorted so the output is
                // stable. Ordered and sorted dictionaries, and every other dictionary, keep their own order.
                // IDictionary.GetEnumerator gives DictionaryEntry for every dictionary; IEnumerable (and Cast) would give
                // KeyValuePair for generic ones.
                List<(DictionaryEntry Entry, int Index)> all = new List<(DictionaryEntry, int)>();
                IDictionaryEnumerator enumerator = dictionary.GetEnumerator();
                try {
                    while (all.Count <= limit && enumerator.MoveNext()) {
                        all.Add((enumerator.Entry, all.Count));
                    }
                } finally { (enumerator as IDisposable)?.Dispose(); }
                IEnumerable<(DictionaryEntry Entry, int Index)> entries = all;
                if (dictionary.GetType() == typeof(Hashtable))
                    entries = entries.OrderBy(item => Convert.ToString(item.Entry.Key, CultureInfo.InvariantCulture) ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                foreach ((DictionaryEntry entry, int index) in entries) {
                    object? memberValue = entry.Value;
                    yield return new InspectMember(
                        Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty,
                        memberValue
                    ) { DictionaryKey = entry.Key, DictionaryIndex = index };
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

        private static bool IsStringObjectPairs(object value) =>
            value is IEnumerable<KeyValuePair<string, object>>;
    }
}
