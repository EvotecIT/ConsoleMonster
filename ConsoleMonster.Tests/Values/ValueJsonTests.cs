using System;
using System.Collections.Generic;
using ConsoleMonster.Values;
using ConsoleMonster.Data;
using Xunit;

namespace ConsoleMonster.Tests.Values {
    public sealed class ValueJsonTests {
        [Fact]
        public void Serialize_DoesNotTruncateFiniteDocumentsAtTheDisplayNodeLimit() {
            Assert.Equal("[\n  0,\n  1,\n  2\n]", ValueJson.Serialize(new[] { 0, 1, 2 }, options: new ShapeOptions { MaxNodes = 1 }));
        }

        [Fact]
        public void Serialize_PreservesCaseDistinctDataKeys() {
            Dictionary<string, object> value = new Dictionary<string, object>(StringComparer.Ordinal) { ["Name"] = "first", ["name"] = "second" };
            Assert.Equal(2, ValueShaper.Fields(value).Count);
            Assert.True(ValueShaper.TryGetField(value, "name", out object? lower));
            Assert.Equal("second", lower);
            string json = ValueJson.Serialize(value);
            Assert.Contains("\"Name\": \"first\"", json);
            Assert.Contains("\"name\": \"second\"", json);
            InspectObject inspected = Assert.IsType<InspectObject>(ObjectInspector.Inspect(value));
            Assert.Equal(2, inspected.Members.Count);
            Assert.Equal("first", Assert.IsType<InspectScalar>(inspected.Members[0]).Value);
            Assert.Equal("second", Assert.IsType<InspectScalar>(inspected.Members[1]).Value);
            Assert.Equal(new[] { "Name", "name" }, ColumnInference.Infer(new[] { value }, new InspectorOptions(), out int hidden));
            Assert.Equal(0, hidden);
        }

#if NET8_0_OR_GREATER
        [Fact]
        public void Serialize_PreservesConstructedJsonNodePrimitiveTypes() {
            System.Text.Json.Nodes.JsonObject value = new System.Text.Json.Nodes.JsonObject {
                ["count"] = 42, ["price"] = 12.50m, ["ratio"] = 0.25f, ["enabled"] = true, ["text"] = "42"
            };
            using (System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(ValueJson.Serialize(value))) {
                System.Text.Json.JsonElement root = document.RootElement;
                Assert.Equal(42, root.GetProperty("count").GetInt32());
                Assert.Equal(12.50m, root.GetProperty("price").GetDecimal());
                Assert.Equal(0.25f, root.GetProperty("ratio").GetSingle());
                Assert.True(root.GetProperty("enabled").GetBoolean());
                Assert.Equal("42", root.GetProperty("text").GetString());
            }
        }
#endif

        [Theory]
        [InlineData("[1 2]")]
        [InlineData("{broken}")]
        [InlineData("[true false]")]
        [InlineData("[\"\\q\"]")]
        [InlineData("[\"\\u12xy\"]")]
        [InlineData("[01]")]
        [InlineData("[1.]")]
        [InlineData("[1e+]")]
        [InlineData("[1,]")]
        [InlineData("{\"a\":1,}")]
        [InlineData("[1] false")]
        public void TryReindent_RejectsMalformedJsonWithoutChangingItsValue(string text) {
            Assert.False(ValueJson.TryReindent(text, out string formatted));
            Assert.Empty(formatted);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("[]")]
        [InlineData("{\"a\":[true,false,null,-12.5e+2,{\"b\":\"\\u0041 \\\" x\"}]}")]
        public void TryReindent_PreservesValidTokensAndIsStable(string text) {
            Assert.True(ValueJson.TryReindent(text, out string formatted));
            Assert.True(ValueJson.TryReindent(formatted, out string repeated));
            Assert.Equal(formatted, repeated);
#if NET8_0_OR_GREATER
            using (System.Text.Json.JsonDocument original = System.Text.Json.JsonDocument.Parse(text))
            using (System.Text.Json.JsonDocument result = System.Text.Json.JsonDocument.Parse(formatted)) {
                Assert.Equal(System.Text.Json.JsonSerializer.Serialize(original.RootElement), System.Text.Json.JsonSerializer.Serialize(result.RootElement));
            }
#endif
        }
    }
}
