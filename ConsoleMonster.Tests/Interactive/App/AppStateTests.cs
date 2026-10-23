using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Observable dictionary, snapshot and notification contracts of shared app state.</summary>
    public sealed class AppStateTests {
        /// <summary>Keys are case insensitive and typed reads distinguish missing and mismatched values.</summary>
        [Fact]
        public void NamedValues_AreCaseInsensitiveAndTyped() {
            AppState state = new AppState { ["Count"] = 3, ["Optional"] = null };
            Assert.Equal(3, state.Get("COUNT", -1));
            Assert.Equal("fallback", state.Get("count", "fallback"));
            Assert.Equal(-1, state.Get("missing", -1));
            Assert.True(state.ContainsKey("optional"));
            Assert.True(state.TryGetValue("OPTIONAL", out object? optional));
            Assert.Null(optional);
            Assert.False(state.TryGetValue("missing", out _));
            Assert.Throws<KeyNotFoundException>(() => state["missing"]);
        }

        /// <summary>Previously obtained snapshots, keys, values and enumerators survive later edits.</summary>
        [Fact]
        public void SnapshotAndEnumeration_RemainStableAfterWrites() {
            AppState state = new AppState { ["Query"] = "old" };
            IReadOnlyDictionary<string, object?> snapshot = state.Snapshot();
            IEnumerable<string> keys = state.Keys;
            IEnumerable<object?> values = state.Values;
            using (IEnumerator<KeyValuePair<string, object?>> enumerator = state.GetEnumerator()) {
                state["QUERY"] = "new";
                state["count"] = 2;
                Assert.Equal("old", snapshot["query"]);
                Assert.Single(snapshot);
                Assert.Equal(new[] { "Query" }, keys);
                Assert.Equal(new object?[] { "old" }, values);
                Assert.True(enumerator.MoveNext());
                Assert.Equal("old", enumerator.Current.Value);
                Assert.False(enumerator.MoveNext());
            }
            Assert.Equal(2, state.Count);
            Assert.Equal("new", state.Get<string>("query"));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, object?>)snapshot).Add("extra", 1));
        }

        /// <summary>Notifications describe committed transitions, including a first null, and suppress equal assignments.</summary>
        [Fact]
        public void Changed_ReportsCommittedTransitionsOnly() {
            AppState state = new AppState();
            List<AppValueChangedEventArgs> changes = new List<AppValueChangedEventArgs>();
            state.Changed += (sender, change) => {
                Assert.Same(state, sender);
                Assert.Equal(change.Value, state[change.Name]);
                changes.Add(change);
            };
            state["query"] = null;
            state["QUERY"] = null;
            state["query"] = "a";
            state["query"] = "a";
            state["query"] = "ab";
            Assert.Equal(3, changes.Count);
            Assert.Null(changes[0].PreviousValue);
            Assert.Null(changes[0].Value);
            Assert.Null(changes[1].PreviousValue);
            Assert.Equal("a", changes[1].Value);
            Assert.Equal("a", changes[2].PreviousValue);
            Assert.Equal("ab", changes[2].Value);
        }

        /// <summary>A synchronous notification permits another thread to read and update the same dictionary.</summary>
        [Fact]
        public void Changed_AllowsConcurrentReadAndWriteOutsideStateLock() {
            AppState state = new AppState();
            using (ManualResetEventSlim completed = new ManualResetEventSlim()) {
                Exception? workerError = null;
                Thread? worker = null;
                state.Changed += (_, change) => {
                    if (change.Name != "query") { return; }
                    worker = new Thread(() => {
                        try {
                            Assert.Equal("a", state.Snapshot()["query"]);
                            state["count"] = 1;
                            state.RequestExit();
                        } catch (Exception error) { workerError = error; }
                        finally { completed.Set(); }
                    }) { IsBackground = true };
                    worker.Start();
                    // This is a deadlock watchdog, not an elapsed-time performance assertion.
                    Assert.True(completed.Wait(TimeSpan.FromSeconds(5)), "Changed held the dictionary lock while notifying.");
                };
                try { state["query"] = "a"; }
                finally { worker?.Join(TimeSpan.FromSeconds(5)); }
                Assert.Null(workerError);
                Assert.Equal(1, state.Get<int>("count"));
                Assert.True(state.ExitRequested);
            }
        }

        /// <summary>Concurrent producers retain every independent value and deliver one notification per change.</summary>
        [Fact]
        public void ConcurrentWriters_PreserveValuesAndNotificationCount() {
            AppState state = new AppState();
            int notifications = 0;
            state.Changed += (_, __) => Interlocked.Increment(ref notifications);
            Parallel.For(0, 16, index => {
                string key = "item" + index;
                state[key] = index;
                state[key] = index;
                Assert.Contains(key, state.Snapshot().Keys);
            });
            Assert.Equal(16, state.Count);
            Assert.Equal(16, notifications);
            Assert.Equal(Enumerable.Range(0, 16), Enumerable.Range(0, 16).Select(index => state.Get<int>("item" + index)));
        }
    }
}
