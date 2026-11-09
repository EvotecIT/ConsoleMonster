using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleMonster.Frames;

/// <summary>
/// Counts attention findings as rows go by, keeping a total and the first few identities per state and reason, so the
/// summary of a streamed or very large result can name what is wrong without holding every row. Memory grows with the
/// number of distinct reasons, not with the number of rows, so keep reasons generic and put what is specific to one row
/// (an error code, a path) in its identity. A tally is not thread-safe; use one per stream.
/// </summary>
public sealed class AttentionTally {
    private readonly Dictionary<(HealthState State, string Reason), Bucket> _buckets = new Dictionary<(HealthState, string), Bucket>();
    private int _sampleSize = 5;

    /// <summary>How many identities each reason keeps (default 5).</summary>
    public int SampleSize {
        get => _sampleSize;
        set => _sampleSize = value < 1 ? throw new ArgumentOutOfRangeException(nameof(value), "Keep at least one identity per reason.") : value;
    }

    /// <summary>Counts one finding.</summary>
    /// <param name="state">The finding's state; healthy findings are not counted.</param>
    /// <param name="reason">The literal reason; findings with the same state and reason are counted together.</param>
    /// <param name="identity">What the finding is about, kept while the reason has room for samples.</param>
    public void Add(HealthState state, string reason, string identity) {
        if (state == HealthState.Healthy) return;
        var key = (state, reason ?? string.Empty);
        if (!_buckets.TryGetValue(key, out Bucket? bucket)) { bucket = new Bucket(_buckets.Count); _buckets.Add(key, bucket); }
        bucket.Count++;
        if (bucket.Names.Count < _sampleSize) bucket.Names.Add(identity ?? string.Empty);
    }

    /// <summary>How many findings were counted, over every reason.</summary>
    public long Count => _buckets.Values.Sum(bucket => bucket.Count);

    /// <summary>
    /// Adds the findings to an attention list: a reason whose every identity was kept adds them as items, which the list
    /// groups as usual; a reason with more findings than samples becomes a counted group. Each call adds them again, so call
    /// it once per list.
    /// </summary>
    /// <param name="list">The attention list.</param>
    public void AddTo(AttentionList list) {
        if (list == null) throw new ArgumentNullException(nameof(list));
        foreach (var pair in _buckets.OrderBy(pair => pair.Value.First)) {
            if (pair.Value.Count == pair.Value.Names.Count) {
                foreach (string name in pair.Value.Names) list.Items.Add(new AttentionItem(pair.Key.State, name, pair.Key.Reason, pair.Value.First));
            } else {
                list.Groups.Add(new AttentionGroup(pair.Key.State, pair.Key.Reason, pair.Value.Count, pair.Value.Names, pair.Value.First));
            }
        }
    }

    private sealed class Bucket {
        internal Bucket(int first) { First = first; }
        internal int First { get; }
        internal long Count { get; set; }
        internal List<string> Names { get; } = new List<string>();
    }
}