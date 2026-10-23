using System.Collections.Generic;
using ConsoleMonster.Serialization;
using Xunit;

namespace ConsoleMonster.Tests.Themes {
    /// <summary>Choice-store compatibility when the shared JSON reader also supplies typed schema values.</summary>
    public sealed class ThemeJsonScalarTests {
        /// <summary>Existing choice readers keep the exact numeric lexemes they consumed before theme files.</summary>
        [Fact]
        public void DefaultReader_PreservesNumericLexemesForChoiceStores() {
            Dictionary<string, object?> values = (Dictionary<string, object?>)MiniJson.Read("{\"integer\":12,\"fraction\":-1.25,\"exponent\":1e2}")!;
            Assert.Equal("12", values["integer"]);
            Assert.Equal("-1.25", values["fraction"]);
            Assert.Equal("1e2", values["exponent"]);
        }
    }
}
