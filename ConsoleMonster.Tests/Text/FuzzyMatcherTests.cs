using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Text {
    public sealed class FuzzyMatcherTests {
        [Theory]
        [InlineData("rpl", "Replication")]
        [InlineData("dns", "DNS scavenging")]
        public void Rank_TypicalSearch_PrefersBetterAlignment(string pattern, string expected) {
            string[] values = pattern == "rpl"
                ? new[] { "Group policy links", "Replication" }
                : new[] { "Kerberos and DNS", "DNS scavenging" };
            IReadOnlyList<FuzzyResult<string>> results = FuzzyMatcher.Rank(values, value => value, pattern);
            Assert.Equal(expected, results[0].Item);
        }

        [Fact]
        public void Rank_EqualScores_PreservesOriginalOrderAndIndex() {
            string[] values = { "cat", "cat", "cat" };
            IReadOnlyList<FuzzyResult<string>> results = FuzzyMatcher.Rank(values, value => value, "cat");
            Assert.Equal(new[] { 0, 1, 2 }, results.Select(result => result.Index));
        }

        [Fact]
        public void Rank_EqualAlignment_PrefersShorterText() {
            string[] values = { "cat extra", "cat" };
            IReadOnlyList<FuzzyResult<string>> results = FuzzyMatcher.Rank(values, value => value, "cat");
            Assert.Equal("cat", results[0].Item);
            Assert.Equal(1, results[0].Index);
        }

        [Fact]
        public void Rank_EmptyPattern_PreservesOriginalOrder() {
            string[] values = { "a much longer name", "x", "medium" };
            IReadOnlyList<FuzzyResult<string>> results = FuzzyMatcher.Rank(values, value => value, " ");
            Assert.Equal(new[] { 0, 1, 2 }, results.Select(result => result.Index));
        }

        [Fact]
        public void TryMatch_EmptyOrWhitespacePattern_MatchesEverythingWithoutHighlights() {
            foreach (string pattern in new[] { string.Empty, " ", "\t\r\n" }) {
                FuzzyMatch? match;
                Assert.True(FuzzyMatcher.TryMatch(pattern, "anything", out match));
                Assert.NotNull(match);
                Assert.Equal(0, match!.Score);
                Assert.Empty(match.Positions);
            }
        }

        [Fact]
        public void TryMatch_FuzzySubsequence_ReturnsAscendingHighlightIndexes() {
            FuzzyMatch? match;
            Assert.True(FuzzyMatcher.TryMatch("fzf", "foo bar fzf", out match));
            Assert.Equal(new[] { 8, 9, 10 }, match!.Positions);
        }

        [Fact]
        public void TryMatch_MultipleTerms_RequiresAllAndUnionsPositions() {
            FuzzyMatch? match;
            Assert.True(FuzzyMatcher.TryMatch("dns scav", "DNS scavenging", out match));
            Assert.Equal(new[] { 0, 1, 2, 4, 5, 6, 7 }, match!.Positions);
            Assert.False(FuzzyMatcher.TryMatch("dns absent", "DNS scavenging", out match));
        }

        [Theory]
        [InlineData("'scav", "DNS scavenging", true)]
        [InlineData("'scav", "DNS scavenging today", true)]
        [InlineData("'scav", "scavenging DNS", true)]
        [InlineData("'xyz", "DNS scavenging", false)]
        [InlineData("^dns", "DNS scavenging", true)]
        [InlineData("^dns", "Kerberos DNS", false)]
        [InlineData("scav$", "DNS scav", true)]
        [InlineData("scav$", "DNS scavenging", false)]
        public void TryMatch_ExactPrefixAndSuffix_UsesRequestedMatchKind(string pattern, string text, bool expected) {
            FuzzyMatch? match;
            Assert.Equal(expected, FuzzyMatcher.TryMatch(pattern, text, out match));
        }

        [Fact]
        public void TryMatch_ExclusionTerm_RejectsContainsAndAddsNoHighlights() {
            FuzzyMatch? match;
            Assert.False(FuzzyMatcher.TryMatch("dns !kerberos", "DNS and Kerberos", out match));
            Assert.True(FuzzyMatcher.TryMatch("dns !kerberos", "DNS scavenging", out match));
            Assert.Equal(new[] { 0, 1, 2 }, match!.Positions);
            Assert.True(FuzzyMatcher.TryMatch("!kerberos", "DNS scavenging", out match));
            Assert.Empty(match!.Positions);
        }

        [Fact]
        public void TryMatch_SmartCase_UsesCaseInsensitiveUnlessTermHasUppercase() {
            FuzzyMatch? match;
            Assert.True(FuzzyMatcher.TryMatch("dns", "DNS scavenging", out match));
            Assert.False(FuzzyMatcher.TryMatch("DNS", "dns scavenging", out match));
            Assert.True(FuzzyMatcher.TryMatch("DNS", "DNS scavenging", out match));
        }

        [Theory]
        [InlineData("klys", "Kłys", 4)]
        [InlineData("ore", "Øre", 3)]
        [InlineData("dz", "Đź", 2)]
        [InlineData("cafe", "café", 4)]
        public void TryMatch_DiacriticsFoldWithoutChangingHighlightIndexes(string pattern, string text, int count) {
            FuzzyMatch? match;
            Assert.True(FuzzyMatcher.TryMatch(pattern, text, out match));
            Assert.Equal(count, match!.Positions.Count);
            Assert.Equal(Enumerable.Range(0, count), match.Positions);
        }

        [Fact]
        public void TryMatch_SharpS_RemainsSharpS() {
            FuzzyMatch? match;
            Assert.True(FuzzyMatcher.TryMatch("ß", "straße", out match));
            Assert.False(FuzzyMatcher.TryMatch("ss", "straße", out match));
        }

        [Fact]
        public void TryMatch_LongText_UsesGreedyFallbackAndKeepsSourceIndexes() {
            string text = new string('x', 600) + "Kłys";
            FuzzyMatch? match;
            Assert.True(FuzzyMatcher.TryMatch("klys", text, out match));
            Assert.Equal(new[] { 600, 601, 602, 603 }, match!.Positions);
        }

        [Fact]
        public void Rank_TenThousandItems_CompletesWithinCiBudget() {
            string[] values = Enumerable.Range(0, 10000).Select(index => "Replication policy " + index.ToString()).ToArray();
            FuzzyMatcher.Rank(values.Take(100), value => value, "rpl");
            Stopwatch stopwatch = Stopwatch.StartNew();
            IReadOnlyList<FuzzyResult<string>> results = FuzzyMatcher.Rank(values, value => value, "rpl");
            stopwatch.Stop();
            Assert.Equal(10000, results.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 200, "Ranking took " + stopwatch.ElapsedMilliseconds + " ms.");
        }
    }
}
