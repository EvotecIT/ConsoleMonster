using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using ConsoleMonster.Data;
using Xunit;
#if NET8_0_OR_GREATER
using System.Text.Json;
#endif

namespace ConsoleMonster.Tests.Data {
    public sealed class ObjectInspectorTests {
        [Fact]
        public void Inspect_PocoWithNestedObjectAndRecords_ProducesTabularCollection() {
            InspectObject root = Assert.IsType<InspectObject>(
                ObjectInspector.Inspect(
                    new Sample {
                        Name = "root",
                        Child = new Child { Label = "nested" },
                        Rows = new List<Row> {
                            new Row { Id = 1, Text = "one" },
                            new Row { Id = 2, Text = "two" },
                        },
                    }
                )
            );
            Assert.Equal("root", Scalar(root.Members[0]));
            InspectObject child = Assert.IsType<InspectObject>(root.Members[1]);
            Assert.Equal("$.Child.Label", child.Members[0].Path);
            InspectCollection rows = Assert.IsType<InspectCollection>(root.Members[2]);
            Assert.Equal(new[] { "Id", "Text" }, rows.Columns);
            Assert.IsType<InspectObject>(rows.Items[0]);
            Assert.Equal(
                "$.Rows[1].Text",
                Assert.IsType<InspectObject>(rows.Items[1]).Members[1].Path
            );
        }

        [Fact]
        public void Inspect_HashtableAndNestedHashtable_UsesKeysAsMembers() {
            Hashtable map = new Hashtable {
                ["First"] = 1,
                ["Nested"] = new Hashtable { ["Second"] = "two" },
            };
            InspectObject result = Assert.IsType<InspectObject>(ObjectInspector.Inspect(map));
            Assert.Equal("Hashtable", result.TypeName);
            InspectObject nested = Assert.IsType<InspectObject>(
                result.Members.Single(member => member.Name == "Nested")
            );
            Assert.Equal("$.Nested.Second", nested.Members[0].Path);
        }

        [Fact]
        public void Inspect_ExpandoObject_UsesDynamicKeys() {
            ExpandoObject value = new ExpandoObject();
            IDictionary<string, object?> fields = value;
            fields["Name"] = "Ada";
            InspectObject result = Assert.IsType<InspectObject>(ObjectInspector.Inspect(value));
            Assert.Equal("PSCustomObject", result.TypeName);
            Assert.Equal("Ada", Scalar(result.Members[0]));
        }

        [Fact]
        public void Inspect_ScalarList_HasNoInferredColumns() {
            InspectCollection result = Assert.IsType<InspectCollection>(
                ObjectInspector.Inspect(new[] { 1, 2, 3 })
            );
            Assert.Null(result.Columns);
            Assert.Equal(3, result.Count);
            Assert.Equal(2, Scalar(result.Items[1]));
        }

        [Fact]
        public void Inspect_DepthLimit_KeepsScalarsAndSummarisesDeeperObjects() {
            // Scalars cost nothing to show, so a value past the depth limit is still its value, never "(depth limit)".
            InspectNode result = ObjectInspector.Inspect(
                new Sample { Child = new Child { Label = "deep" } },
                new InspectorOptions { MaxDepth = 1 }
            );
            InspectObject child = Assert.IsType<InspectObject>(
                Assert.IsType<InspectObject>(result).Members[1]
            );
            InspectScalar label = Assert.IsType<InspectScalar>(child.Members[0]);
            Assert.Equal("deep", label.Value);
            InspectNode deeper = ObjectInspector.Inspect(
                new Sample { Child = new Child { Label = "deep" } },
                new InspectorOptions { MaxDepth = 0 }
            );
            InspectLimit limit = Assert.IsType<InspectLimit>(Assert.IsType<InspectObject>(deeper).Members[1]);
            Assert.Equal("Child", limit.TypeName);
        }

        [Fact]
        public void Inspect_SelfCycle_ReferencesAncestorPath() {
            Link link = new Link();
            link.Next = link;
            InspectCycle cycle = Assert.IsType<InspectCycle>(
                Assert.IsType<InspectObject>(ObjectInspector.Inspect(link)).Members[0]
            );
            Assert.Equal("$", cycle.TargetPath);
        }

        [Fact]
        public void Inspect_IndirectCycle_ReferencesOriginalAncestor() {
            Link first = new Link();
            Link second = new Link();
            first.Next = second;
            second.Next = first;
            InspectObject root = Assert.IsType<InspectObject>(ObjectInspector.Inspect(first));
            InspectCycle cycle = Assert.IsType<InspectCycle>(
                Assert.IsType<InspectObject>(root.Members[0]).Members[0]
            );
            Assert.Equal("$", cycle.TargetPath);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Inspect_SharedReferenceAcrossBranches_IsNotCycle(bool normalizedIdentity) {
            Child shared = new Child { Label = "shared" };
            object identity = new object();
            InspectorOptions options = new InspectorOptions {
                Identity = value => normalizedIdentity && ReferenceEquals(value, shared) ? identity : value
            };
            InspectObject result = Assert.IsType<InspectObject>(
                ObjectInspector.Inspect(new Pair { Left = shared, Right = shared }, options)
            );
            Assert.IsType<InspectObject>(result.Members[0]);
            Assert.IsType<InspectObject>(result.Members[1]);
        }

        [Fact]
        public void Inspect_UnwrapHook_AppliesToRootMembersAndCollectionItems() {
            InspectorOptions options = new InspectorOptions {
                Unwrap = value => value is ValueWrapper wrapper ? wrapper.Value : value,
            };
            ValueWrapper root = new ValueWrapper(
                new UnwrapSample {
                    Child = new ValueWrapper(new Child { Label = "nested" }),
                    Items = new object[] {
                        new ValueWrapper(new Row { Id = 7, Text = "item" }),
                    },
                    Scalar = new ValueWrapper(42),
                }
            );

            InspectObject result = Assert.IsType<InspectObject>(ObjectInspector.Inspect(root, options));
            InspectObject child = Assert.IsType<InspectObject>(result.Members[0]);
            Assert.Equal("nested", Scalar(child.Members[0]));
            InspectCollection items = Assert.IsType<InspectCollection>(result.Members[1]);
            Assert.Equal(new[] { "Id", "Text" }, items.Columns);
            InspectObject item = Assert.IsType<InspectObject>(items.Items[0]);
            Assert.Equal(7, Scalar(item.Members[0]));
            Assert.Equal(42, Scalar(result.Members[2]));
        }

        [Fact]
        public void Inspect_MaxItemsWithCollection_KeepsKnownOmittedCount() {
            InspectCollection result = Assert.IsType<InspectCollection>(
                ObjectInspector.Inspect(
                    new List<int> { 1, 2, 3, 4 },
                    new InspectorOptions { MaxItems = 2 }
                )
            );
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(4, result.Count);
            Assert.Equal(2, result.Omitted);
        }

        [Fact]
        public void Inspect_MaxItemsWithLazySequence_ReportsUnknownOmittedCount() {
            InspectCollection result = Assert.IsType<InspectCollection>(
                ObjectInspector.Inspect(Generate(), new InspectorOptions { MaxItems = 2 })
            );
            Assert.Equal(2, result.Items.Count);
            Assert.Null(result.Count);
            Assert.Equal(-1, result.Omitted);
        }

        [Fact]
        public void Inspect_FiltersAndOrdersMembers_ApplyWildcardRules() {
            InspectorOptions options = new InspectorOptions();
            options.Include.Add("*a*");
            options.Exclude.Add("Beta");
            options.Order.Add("Gamma");
            InspectObject result = Assert.IsType<InspectObject>(
                ObjectInspector.Inspect(new FilterSample(), options)
            );
            Assert.Equal(new[] { "Gamma", "Alpha" }, result.Members.Select(member => member.Name));
        }

        [Fact]
        public void Inspect_ThrowingProperty_ReturnsUnwrappedErrorNode() {
            InspectObject result = Assert.IsType<InspectObject>(
                ObjectInspector.Inspect(new ThrowingSample())
            );
            InspectError error = Assert.IsType<InspectError>(result.Members[0]);
            Assert.Equal("getter failed", error.Message);
        }

        [Fact]
        public void Inspect_MaxColumns_ReportsHiddenColumns() {
            InspectCollection result = Assert.IsType<InspectCollection>(
                ObjectInspector.Inspect(
                    new[] { new WideRow() },
                    new InspectorOptions { MaxColumns = 2 }
                )
            );
            Assert.Equal(new[] { "A", "B" }, result.Columns);
            Assert.Equal(2, result.HiddenColumns);
        }

        [Fact]
        public void ColumnInference_MaxItems_BoundsPublicEnumerableTraversal() {
            int yielded = 0;
            InspectorOptions options = new InspectorOptions { MaxItems = 2 };
            IReadOnlyList<string> columns = ColumnInference.Infer(
                YieldRows(() => yielded++),
                options,
                out int hidden
            );
            Assert.Equal(new[] { "Id", "Text" }, columns);
            Assert.Equal(2, yielded);
            Assert.Equal(0, hidden);
        }

        [Fact]
        public void Inspect_ThrowingCollectionEnumeration_ReturnsErrorNode() {
            InspectError error = Assert.IsType<InspectError>(
                ObjectInspector.Inspect(new ThrowingEnumerable())
            );
            Assert.Equal("enumeration failed", error.Message);
        }

        [Fact]
        public void Inspect_ThrowingCollectionCount_UsesUnknownCountAndContinues() {
            InspectCollection result = Assert.IsType<InspectCollection>(
                ObjectInspector.Inspect(
                    new ThrowingCountCollection(),
                    new InspectorOptions { MaxItems = 1 }
                )
            );
            Assert.Null(result.Count);
            Assert.Equal(-1, result.Omitted);
            Assert.Single(result.Items);
        }

        [Fact]
        public void Inspect_ReflectionDisabled_LeavesUnknownObjectAsScalar() {
            object value = new Child { Label = "not inspected" };
            InspectScalar result = Assert.IsType<InspectScalar>(
                ObjectInspector.Inspect(value, new InspectorOptions { UseReflection = false })
            );
            Assert.Same(value, result.Value);
        }

        [Fact]
        public void Inspect_ScalarsAndLongStrings_RemainScalarValues() {
            Assert.Equal(
                "String",
                Assert
                    .IsType<InspectScalar>(ObjectInspector.Inspect(new string('x', 10001)))
                    .TypeName
            );
            Assert.Equal(
                "Guid",
                Assert.IsType<InspectScalar>(ObjectInspector.Inspect(Guid.NewGuid())).TypeName
            );
            Assert.True(ColumnInference.IsScalar(new byte[] { 1, 2 }));
        }

#if NET8_0_OR_GREATER
        [Fact]
        public void Inspect_JsonElementObjectArrayAndPrimitives_MapsBuiltInShapes() {
            using JsonDocument document = JsonDocument.Parse(
                "{\"Name\":\"Ada\",\"Items\":[1,2.5,true,null]}"
            );
            InspectObject root = Assert.IsType<InspectObject>(
                ObjectInspector.Inspect(document.RootElement)
            );
            Assert.Equal("Ada", Scalar(root.Members[0]));
            InspectCollection items = Assert.IsType<InspectCollection>(root.Members[1]);
            Assert.Equal(4, items.Count);
            Assert.Equal(1L, Scalar(items.Items[0]));
            Assert.Equal(2.5d, Scalar(items.Items[1]));
            Assert.Equal(true, Scalar(items.Items[2]));
            Assert.Null(Scalar(items.Items[3]));
        }
#endif

        private static object? Scalar(InspectNode node) => Assert.IsType<InspectScalar>(node).Value;

        private static IEnumerable<int> Generate() {
            for (int i = 0; i < 100; i++)
                yield return i;
        }

        private static IEnumerable<Row> YieldRows(Action onYield) {
            while (true) {
                onYield();
                yield return new Row();
            }
        }

        private sealed class Sample {
            public string Name { get; set; } = string.Empty;
            public Child Child { get; set; } = new Child();
            public List<Row> Rows { get; set; } = new List<Row>();
        }

        private sealed class Child {
            public string Label { get; set; } = string.Empty;
        }

        private sealed class Row {
            public int Id { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        private sealed class Link {
            public Link? Next { get; set; }
        }

        private sealed class Pair {
            public Child Left { get; set; } = new Child();
            public Child Right { get; set; } = new Child();
        }

        private sealed class UnwrapSample {
            public object Child { get; set; } = new object();
            public object[] Items { get; set; } = Array.Empty<object>();
            public object Scalar { get; set; } = new object();
        }

        private sealed class ValueWrapper {
            public ValueWrapper(object value) {
                Value = value;
            }

            public object Value { get; }
        }

        private sealed class FilterSample {
            public string Alpha { get; set; } = "a";
            public string Beta { get; set; } = "b";
            public string Gamma { get; set; } = "g";
        }

        private sealed class ThrowingSample {
            public string Broken => throw new InvalidOperationException("getter failed");
        }

        private sealed class WideRow {
            public int A { get; set; }
            public int B { get; set; }
            public int C { get; set; }
            public int D { get; set; }
        }

        private sealed class ThrowingEnumerable : IEnumerable {
            public IEnumerator GetEnumerator() =>
                throw new InvalidOperationException("enumeration failed");
        }

        private sealed class ThrowingCountCollection : IReadOnlyCollection<int> {
            public int Count => throw new InvalidOperationException("count failed");

            public IEnumerator<int> GetEnumerator() {
                yield return 1;
                yield return 2;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
