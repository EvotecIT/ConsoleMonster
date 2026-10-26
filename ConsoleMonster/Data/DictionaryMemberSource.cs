using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Diagnostics;

namespace ConsoleMonster.Data {
    internal sealed class DictionaryMemberSource : IMemberSource {
        public bool CanRead(object value) => IsDictionary(value);

        internal static bool IsDictionary(object value) => value is IDictionary || IsStringObjectPairs(value);

        public string GetTypeName(object value) =>
            value is IDictionary
                ? ObjectInspector.FriendlyTypeName(value.GetType())
                : "PSCustomObject";

        public IEnumerable<InspectMember> GetMembers(object value) => GetMembers(value, int.MaxValue);

        // Interactive inspection needs a bounded page; static inspection retains its complete, sorted key set.
        internal IEnumerable<InspectMember> GetMembers(object value, int limit, InspectorOptions? options = null) {
            Stopwatch elapsed = Stopwatch.StartNew();
            bool Expired() => options != null && options.TimeBudget > TimeSpan.Zero && elapsed.Elapsed >= options.TimeBudget;
            if (value is IDictionary dictionary) {
                // A Hashtable has no order of its own (PowerShell's @{ }): its keys are shown sorted so the output is
                // stable. Ordered and sorted dictionaries, and every other dictionary, keep their own order.
                // IDictionary.GetEnumerator gives DictionaryEntry for every dictionary; IEnumerable (and Cast) would give
                // KeyValuePair for generic ones.
                List<(DictionaryEntry Entry, int Index)> all = new List<(DictionaryEntry, int)>();
                int count = int.MaxValue;
                if (options != null) {
                    try { count = Math.Max(0, dictionary.Count); }
                    catch (Exception ex) when (!ObjectInspector.IsCancellation(ex)) { }
                }
                IDictionaryEnumerator enumerator = dictionary.GetEnumerator();
                int index = 0;
                try {
                    while (index < count && all.Count <= limit && !Expired() && enumerator.MoveNext()) {
                        DictionaryEntry entry = enumerator.Entry;
                        int position = index++;
                        string name = Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty;
                        if (options == null || ObjectInspector.MemberIncluded(name, options)) { all.Add((entry, position)); }
                    }
                } finally { (enumerator as IDisposable)?.Dispose(); }
                IEnumerable<(DictionaryEntry Entry, int Index)> entries = all;
                if (dictionary.GetType() == typeof(Hashtable))
                    entries = entries.OrderBy(item => Convert.ToString(item.Entry.Key, CultureInfo.InvariantCulture) ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                foreach ((DictionaryEntry entry, int position) in entries) {
                    object? memberValue = entry.Value;
                    yield return new InspectMember(
                        Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty,
                        memberValue
                    ) { DictionaryKey = entry.Key, DictionaryIndex = position };
                }
                yield break;
            }
            int matched = 0;
            foreach (
                KeyValuePair<string, object> entry in (IEnumerable<KeyValuePair<string, object>>)
                    value
            ) {
                if (Expired()) { yield break; }
                if (options != null && !ObjectInspector.MemberIncluded(entry.Key, options)) { continue; }
                if (matched++ > limit) { yield break; }
                yield return new InspectMember(
                    Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty,
                    entry.Value
                );
            }
        }

        private static bool IsStringObjectPairs(object value) =>
            value is IEnumerable<KeyValuePair<string, object>>;
    }
}
