using System;
using System.Collections.Generic;
using ConsoleMonster.Serialization;
using Xunit;

namespace ConsoleMonster.Tests.Serialization {
    public sealed class MiniJsonTests {
        [Theory]
        [InlineData("duplicate-key")]
        [InlineData("leading-zero")]
        [InlineData("missing-fraction")]
        [InlineData("missing-exponent")]
        [InlineData("leading-plus")]
        [InlineData("unknown-escape")]
        [InlineData("control-character")]
        [InlineData("unicode-escape")]
        [InlineData("object-comma")]
        [InlineData("array-comma")]
        [InlineData("trailing-text")]
        public void Read_RejectsMalformedJsonInsteadOfSilentlyChangingData(string scenario) {
            string json = scenario switch {
                "duplicate-key" => "{\"a\":1,\"a\":2}",
                "leading-zero" => "{\"a\":01}",
                "missing-fraction" => "{\"a\":1.}",
                "missing-exponent" => "{\"a\":1e}",
                "leading-plus" => "{\"a\":+1}",
                "unknown-escape" => "{\"a\":\"\\q\"}",
                "control-character" => "{\"a\":\"raw\nline\"}",
                "unicode-escape" => "{\"a\":\"\\uZZZZ\"}",
                "object-comma" => "{\"a\":true,}",
                "array-comma" => "[1,]",
                _ => "{} trailing"
            };
            Assert.Throws<FormatException>(() => MiniJson.Read(json));
        }

        [Fact]
        public void Read_RejectsDeepInputWithoutOverflowingTheProcessStack() {
            string text = new string('[', 70) + "null" + new string(']', 70);
            Assert.Contains("nesting", Assert.Throws<FormatException>(() => MiniJson.Read(text)).Message);
        }

        [Fact]
        public void Read_KeepsChoiceStoreNumbersAsStringsAndDecodesJsonEscapes() {
            Dictionary<string, object?> data = Assert.IsType<Dictionary<string, object?>>(MiniJson.Read("{\"n\":-1.25e+2,\"s\":\"a\\/b\\\\c\\t\\u017c\"}"));
            Assert.Equal("-1.25e+2", data["n"]);
            Assert.Equal("a/b\\c\tż", data["s"]);
        }
    }
}
