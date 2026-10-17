using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConsoleMonster.Data;
using Xunit;

namespace ConsoleMonster.Tests.Data {
    /// <summary>The inspector stays bounded on wide objects, never forces blocking getters and lets cancellation through.</summary>
    public sealed class InspectorSafetyTests {
        public sealed class Pending {
            public Pending(Task<int> task, Lazy<int> lazy) {
                Task = task;
                Lazy = lazy;
            }

            public Task<int> Task { get; }

            public Lazy<int> Lazy { get; }
#if NET8_0_OR_GREATER

            public ValueTask<int> ValueTask => new ValueTask<int>(Task);
#endif
        }

        public sealed class Cancelling {
            public string Name => "first";

            public string Cancelled => throw new OperationCanceledException("Ctrl+C");
        }

        private static int CountNodes(InspectNode node) {
            switch (node) {
                case InspectObject obj:
                    return 1 + obj.Members.Sum(CountNodes);
                case InspectCollection collection:
                    return 1 + collection.Items.Sum(CountNodes);
                default:
                    return 1;
            }
        }

        private static IEnumerable<InspectNode> All(InspectNode node) {
            yield return node;
            IEnumerable<InspectNode> children = node is InspectObject obj ? obj.Members : node is InspectCollection list ? list.Items : Enumerable.Empty<InspectNode>();
            foreach (InspectNode child in children) {
                foreach (InspectNode descendant in All(child)) {
                    yield return descendant;
                }
            }
        }

        private static Dictionary<string, object> Wide(int keys, int depth) {
            Dictionary<string, object> node = new Dictionary<string, object>();
            for (int i = 0; i < keys; i++) {
                node["k" + i] = depth == 0 ? (object)i : Wide(keys, depth - 1);
            }
            return node;
        }

        [Fact]
        public void Inspect_WideDeepObject_StopsAtTheNodeBudget() {
            // 30 keys at depth 3 would be about 28,000 nodes.
            InspectNode root = ObjectInspector.Inspect(Wide(30, 3), new InspectorOptions { MaxDepth = 6, MaxNodes = 500 });
            int nodes = CountNodes(root);
            Assert.InRange(nodes, 450, 600);
            Assert.Contains(All(root).OfType<InspectLimit>(), limit => limit.Summary.Contains("node limit reached"));
        }

        [Fact]
        public void Inspect_DefaultBudget_IsFiveThousandNodes() {
            Assert.Equal(5000, new InspectorOptions().MaxNodes);
            InspectNode root = ObjectInspector.Inspect(Wide(30, 3), new InspectorOptions { MaxDepth = 6 });
            Assert.InRange(CountNodes(root), 4500, 5600);
        }

        [Fact]
        public async Task Inspect_IncompleteTasksAndUncreatedLazy_AreNotForced() {
            TaskCompletionSource<int> source = new TaskCompletionSource<int>();
            bool forced = false;
            Pending pending = new Pending(source.Task, new Lazy<int>(() => {
                forced = true;
                return 1;
            }));
            // Run off the test thread: a regression would block on Task.Result forever.
            Task<InspectNode> inspection = Task.Run(() => ObjectInspector.Inspect(pending));
            Task finished = await Task.WhenAny(inspection, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.True(ReferenceEquals(finished, inspection), "inspection blocked on a pending getter");
            InspectObject root = Assert.IsType<InspectObject>(await inspection);
            Assert.Contains("not completed", Assert.IsType<InspectScalar>(root.Members.Single(m => m.Name == "Task")).Value as string);
#if NET8_0_OR_GREATER
            Assert.Contains("not completed", Assert.IsType<InspectScalar>(root.Members.Single(m => m.Name == "ValueTask")).Value as string);
#endif
            Assert.Equal("(value not created)", Assert.IsType<InspectScalar>(root.Members.Single(m => m.Name == "Lazy")).Value);
            Assert.False(forced);
            source.SetResult(42);
        }

        [Fact]
        public void Inspect_CompletedTask_ShowsItsResult() {
            InspectObject root = Assert.IsType<InspectObject>(ObjectInspector.Inspect(Task.FromResult(42)));
            Assert.Equal(42, Assert.IsType<InspectScalar>(root.Members.Single(m => m.Name == "Result")).Value);
        }

        [Fact]
        public void Inspect_GetterCancelled_PropagatesInsteadOfBecomingAnErrorNode() {
            Assert.Throws<OperationCanceledException>(() => ObjectInspector.Inspect(new Cancelling()));
        }

        [Fact]
        public void Inspect_ExcludedMember_IsNeverRead() {
            InspectorOptions options = new InspectorOptions();
            options.Exclude.Add("Cancelled");
            InspectObject root = Assert.IsType<InspectObject>(ObjectInspector.Inspect(new Cancelling(), options));
            Assert.Equal(new[] { "Name" }, root.Members.Select(m => m.Name));
        }
    }
}
