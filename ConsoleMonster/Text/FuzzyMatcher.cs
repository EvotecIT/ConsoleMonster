using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ConsoleMonster.Text {
    /// <summary>Matches and ranks short search patterns against display text.</summary>
    public static class FuzzyMatcher {
        // Alignment constants: 10 per matched character, 12 at the start, 8 at a word boundary,
        // up to 6*5 for consecutive characters, 4 for the first gap character, 1 per further gap
        // character, and 2 for exact case. These favor compact, readable matches.
        private const int CharacterScore = 10;
        private const int StartBonus = 12;
        private const int BoundaryBonus = 8;
        private const int ConsecutiveBonus = 5;
        private const int MaximumConsecutiveBonus = 6;
        private const int GapOpenPenalty = 4;
        private const int GapCharacterPenalty = 1;
        private const int CaseBonus = 2;
        private const int DynamicLimit = 512;

        /// <summary>Attempts to match a pattern against text.</summary>
        /// <param name="pattern">Terms separated by whitespace.</param>
        /// <param name="text">The text to search.</param>
        /// <param name="match">The score and matched positions when successful.</param>
        /// <returns>True when all positive terms match and no exclusion term matches.</returns>
        public static bool TryMatch(string pattern, string text, out FuzzyMatch? match) {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            if (text == null) throw new ArgumentNullException(nameof(text));
            string trimmed = pattern.Trim();
            if (trimmed.Length == 0) {
                match = new FuzzyMatch(0, new int[0]);
                return true;
            }
            List<string> terms = new List<string>();
            int termStart = -1;
            for (int i = 0; i < trimmed.Length; i++) {
                if (char.IsWhiteSpace(trimmed[i])) {
                    if (termStart >= 0) { terms.Add(trimmed.Substring(termStart, i - termStart)); termStart = -1; }
                } else if (termStart < 0) {
                    termStart = i;
                }
            }
            if (termStart >= 0) terms.Add(trimmed.Substring(termStart));
            List<int> positions = new List<int>();
            int score = 0;
            bool hasPositive = false;
            for (int i = 0; i < terms.Count; i++) {
                string term = terms[i];
                bool exclude = term[0] == '!';
                bool exact = term[0] == '\'';
                bool prefix = term[0] == '^';
                bool suffix = term[term.Length - 1] == '$';
                if (exclude || exact || prefix) term = term.Substring(1);
                if (suffix) term = term.Substring(0, term.Length - 1);
                if (term.Length == 0) {
                    match = null;
                    return false;
                }
                bool ignoreCase = !HasUppercase(term);
                List<int> found = new List<int>();
                int termScore = 0;
                bool matched = exclude
                    ? Contains(text, term, ignoreCase)
                    : exact
                        ? FindSubstring(text, term, ignoreCase, out found, out termScore)
                        : prefix
                            ? FindPrefix(text, term, ignoreCase, out found, out termScore)
                            : suffix
                                ? FindSuffix(text, term, ignoreCase, out found, out termScore)
                                : FindFuzzy(text, term, ignoreCase, out found, out termScore);
                if (exclude) {
                    if (matched) { match = null; return false; }
                } else {
                    if (!matched) { match = null; return false; }
                    hasPositive = true;
                    score += termScore;
                    positions.AddRange(found);
                }
            }
            if (!hasPositive) {
                match = new FuzzyMatch(0, new int[0]);
            } else {
                positions.Sort();
                for (int i = positions.Count - 1; i > 0; i--) {
                    if (positions[i] == positions[i - 1]) positions.RemoveAt(i);
                }
                match = new FuzzyMatch(score, positions.AsReadOnly());
            }
            return true;
        }

        /// <summary>Ranks matching items by score, retaining source order for ties.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="items">The source items.</param>
        /// <param name="text">Gets searchable text for an item.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <returns>Matching results in rank order.</returns>
        public static IReadOnlyList<FuzzyResult<T>> Rank<T>(IEnumerable<T> items, Func<T, string> text, string pattern) {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            List<FuzzyResult<T>> results = new List<FuzzyResult<T>>();
            bool simpleFuzzy = pattern.Length <= DynamicLimit && IsSimpleFuzzyPattern(pattern);
            bool ignoreCase = simpleFuzzy && !HasUppercase(pattern);
            int[]? foldedPattern = simpleFuzzy ? Folded(pattern, ignoreCase) : null;
            int[]? scoreBuffer = simpleFuzzy ? new int[pattern.Length * DynamicLimit] : null;
            int[]? previousBuffer = simpleFuzzy ? new int[pattern.Length * DynamicLimit] : null;
            int[]? streakBuffer = simpleFuzzy ? new int[pattern.Length * DynamicLimit] : null;
            int index = 0;
            foreach (T item in items) {
                string itemText = text(item) ?? string.Empty;
                FuzzyMatch? match;
                if (simpleFuzzy) {
                    List<int> positions;
                    int score;
                    if (FindFuzzy(itemText, pattern, ignoreCase, foldedPattern!, scoreBuffer!, previousBuffer!, streakBuffer!, out positions, out score)) {
                        match = new FuzzyMatch(score, positions.AsReadOnly());
                    } else {
                        match = null;
                    }
                } else if (TryMatch(pattern, itemText, out match)) {
                    results.Add(new FuzzyResult<T>(item, index, match!, itemText.Length));
                } else {
                    match = null;
                }
                if (simpleFuzzy && match != null) {
                    results.Add(new FuzzyResult<T>(item, index, match!, itemText.Length));
                }
                index++;
            }
            results.Sort((left, right) => {
                int byScore = right.Match.Score.CompareTo(left.Match.Score);
                if (byScore != 0) return byScore;
                if (left.Match.Positions.Count > 0 && SamePositions(left.Match.Positions, right.Match.Positions)) {
                    int byLength = left.TextLength.CompareTo(right.TextLength);
                    if (byLength != 0) return byLength;
                }
                return left.Index.CompareTo(right.Index);
            });
            return results.AsReadOnly();
        }

        private static bool FindSubstring(string text, string term, bool ignoreCase, out List<int> positions, out int score) {
            positions = new List<int>(); score = 0;
            int start = IndexOf(text, term, ignoreCase);
            if (start < 0) return false;
            for (int i = 0; i < term.Length; i++) positions.Add(start + i);
            score = ScoreFixed(text, term, positions, ignoreCase);
            return true;
        }

        private static bool FindPrefix(string text, string term, bool ignoreCase, out List<int> positions, out int score) {
            positions = new List<int>(); score = 0;
            if (!MatchesAt(text, term, 0, ignoreCase)) return false;
            for (int i = 0; i < term.Length; i++) positions.Add(i);
            score = ScoreFixed(text, term, positions, ignoreCase);
            return true;
        }

        private static bool FindSuffix(string text, string term, bool ignoreCase, out List<int> positions, out int score) {
            positions = new List<int>(); score = 0;
            int start = text.Length - term.Length;
            if (start < 0 || !MatchesAt(text, term, start, ignoreCase)) return false;
            for (int i = 0; i < term.Length; i++) positions.Add(start + i);
            score = ScoreFixed(text, term, positions, ignoreCase);
            return true;
        }

        private static bool FindFuzzy(string text, string term, bool ignoreCase, out List<int> positions, out int score) {
            positions = new List<int>(); score = 0;
            if (term.Length > text.Length) return false;
            if (text.Length > DynamicLimit) return FindGreedy(text, term, ignoreCase, out positions, out score);
            int n = text.Length;
            int m = term.Length;
            return FindFuzzy(text, term, ignoreCase, Folded(term, ignoreCase), new int[m * n], new int[m * n], new int[m * n], out positions, out score);
        }

        private static bool FindFuzzy(string text, string term, bool ignoreCase, int[] foldedTerm, int[] scores, int[] previous, int[] streaks, out List<int> positions, out int score) {
            positions = new List<int>(); score = 0;
            if (term.Length > text.Length) return false;
            if (text.Length > DynamicLimit) return FindGreedy(text, term, ignoreCase, out positions, out score);
            int n = text.Length;
            int m = term.Length;
            int size = m * n;
            for (int i = 0; i < size; i++) scores[i] = int.MinValue;
            for (int row = 0; row < m; row++) {
                int rowOffset = row * n;
                int previousOffset = rowOffset - n;
                int bestGap = int.MinValue;
                int bestGapIndex = -1;
                for (int col = 0; col < n; col++) {
                    if (row > 0 && col >= 2 && scores[previousOffset + col - 2] != int.MinValue) {
                        int candidate = scores[previousOffset + col - 2] + (col - 2) * GapCharacterPenalty;
                        if (candidate > bestGap) { bestGap = candidate; bestGapIndex = col - 2; }
                    }
                    if (Fold(text[col], ignoreCase) != foldedTerm[row]) continue;
                    int local = CharacterScore + (col == 0 ? StartBonus : IsBoundary(text, col) ? BoundaryBonus : 0);
                    if (!ignoreCase && text[col] == term[row]) local += CaseBonus;
                    if (row == 0) {
                        scores[rowOffset + col] = local;
                        streaks[rowOffset + col] = 1;
                    } else {
                        int selected = -1;
                        int selectedScore = int.MinValue;
                        bool consecutive = false;
                        if (col > 0 && scores[previousOffset + col - 1] != int.MinValue) {
                            selected = col - 1;
                            selectedScore = scores[previousOffset + selected] + Math.Min(MaximumConsecutiveBonus, streaks[previousOffset + selected] + 1) * ConsecutiveBonus;
                            consecutive = true;
                        }
                        if (bestGapIndex >= 0) {
                            int gapScore = bestGap + (2 - col) * GapCharacterPenalty - GapOpenPenalty;
                            if (gapScore > selectedScore) { selected = bestGapIndex; selectedScore = gapScore; consecutive = false; }
                        }
                        if (selected >= 0) {
                            scores[rowOffset + col] = selectedScore + local;
                            previous[rowOffset + col] = selected;
                            streaks[rowOffset + col] = consecutive ? Math.Min(MaximumConsecutiveBonus, streaks[previousOffset + selected] + 1) : 1;
                        }
                    }
                }
            }
            int end = -1;
            int best = int.MinValue;
            int lastRowOffset = (m - 1) * n;
            for (int col = 0; col < n; col++) if (scores[lastRowOffset + col] > best) { best = scores[lastRowOffset + col]; end = col; }
            if (end < 0) return false;
            score = best;
            for (int row = m - 1; row >= 0; row--) {
                positions.Add(end);
                if (row > 0) end = previous[row * n + end];
            }
            positions.Reverse();
            return true;
        }

        private static bool IsSimpleFuzzyPattern(string pattern) {
            if (pattern.Length == 0 || pattern[0] == '!' || pattern[0] == '\'' || pattern[0] == '^' || pattern[pattern.Length - 1] == '$') return false;
            for (int i = 0; i < pattern.Length; i++) if (char.IsWhiteSpace(pattern[i])) return false;
            return true;
        }

        private static bool FindGreedy(string text, string term, bool ignoreCase, out List<int> positions, out int score) {
            positions = new List<int>(); score = 0;
            int[] foldedText = Folded(text, ignoreCase);
            int[] foldedTerm = Folded(term, ignoreCase);
            int cursor = 0;
            for (int i = 0; i < foldedTerm.Length; i++) {
                while (cursor < foldedText.Length && foldedText[cursor] != foldedTerm[i]) cursor++;
                if (cursor == foldedText.Length) { positions.Clear(); return false; }
                positions.Add(cursor++);
            }
            score = ScoreFixed(text, term, positions, ignoreCase);
            return true;
        }

        private static int ScoreFixed(string text, string term, IList<int> positions, bool ignoreCase) {
            int score = 0;
            int streak = 0;
            for (int i = 0; i < positions.Count; i++) {
                int position = positions[i];
                score += CharacterScore;
                if (position == 0) score += StartBonus;
                else if (IsBoundary(text, position)) score += BoundaryBonus;
                if (!ignoreCase && position < text.Length && i < term.Length && text[position] == term[i]) score += CaseBonus;
                if (i > 0) {
                    int gap = position - positions[i - 1] - 1;
                    if (gap == 0) score += Math.Min(MaximumConsecutiveBonus, ++streak) * ConsecutiveBonus;
                    else { score -= GapOpenPenalty + Math.Max(0, gap - 1) * GapCharacterPenalty; streak = 0; }
                }
            }
            return score;
        }

        private static bool Contains(string text, string term, bool ignoreCase) => IndexOf(text, term, ignoreCase) >= 0;
        private static int IndexOf(string text, string term, bool ignoreCase) {
            if (term.Length > text.Length) return -1;
            int[] source = Folded(text, ignoreCase);
            int[] query = Folded(term, ignoreCase);
            for (int i = 0; i <= source.Length - query.Length; i++) {
                int j = 0;
                while (j < query.Length && source[i + j] == query[j]) j++;
                if (j == query.Length) return i;
            }
            return -1;
        }

        private static bool MatchesAt(string text, string term, int start, bool ignoreCase) {
            if (start < 0 || start + term.Length > text.Length) return false;
            int[] source = Folded(text.Substring(start, term.Length), ignoreCase);
            int[] query = Folded(term, ignoreCase);
            for (int i = 0; i < query.Length; i++) if (source[i] != query[i]) return false;
            return true;
        }

        private static int[] Folded(string value, bool ignoreCase) {
            int[] folded = new int[value.Length];
            for (int i = 0; i < value.Length; i++) {
                char c = value[i];
                if (ignoreCase) c = char.ToLowerInvariant(c);
                folded[i] = Fold(c, ignoreCase);
            }
            return folded;
        }

        private static int Fold(char c, bool ignoreCase) {
            if (c < 0x80) return ignoreCase ? char.ToLowerInvariant(c) : c;
            switch (c) {
                case 'ł': return 'l';
                case 'Ł': return ignoreCase ? 'l' : 'L';
                case 'ø': return 'o';
                case 'Ø': return ignoreCase ? 'o' : 'O';
                case 'đ': return 'd';
                case 'Đ': return ignoreCase ? 'd' : 'D';
                case 'ß': return 'ß';
            }
            string normalized = c.ToString().Normalize(NormalizationForm.FormD);
            for (int i = 0; i < normalized.Length; i++) {
                if (CharUnicodeInfo.GetUnicodeCategory(normalized[i]) != UnicodeCategory.NonSpacingMark) {
                    return ignoreCase ? char.ToLowerInvariant(normalized[i]) : normalized[i];
                }
            }
            return c;
        }

        private static bool IsBoundary(string text, int index) {
            char current = text[index];
            char previous = text[index - 1];
            if (previous == ' ' || previous == '-' || previous == '_' || previous == '.' || previous == '/' || previous == '\\' || previous == ':' || previous == ',' || previous == '(' || previous == '[') return true;
            if (char.IsLower(previous) && char.IsUpper(current)) return true;
            return char.IsLetter(previous) && char.IsDigit(current);
        }

        private static bool HasUppercase(string value) {
            for (int i = 0; i < value.Length; i++) if (char.IsUpper(value[i])) return true;
            return false;
        }

        private static bool SamePositions(IReadOnlyList<int> left, IReadOnlyList<int> right) {
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++) if (left[i] != right[i]) return false;
            return true;
        }
    }

    /// <summary>An item that matched a ranked fuzzy query.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    public sealed class FuzzyResult<T> {
        /// <summary>Creates a ranked result.</summary>
        /// <param name="item">The source item.</param>
        /// <param name="index">Its original zero-based position.</param>
        /// <param name="match">The match details.</param>
        /// <param name="textLength">The source text length used to prefer shorter equal alignments.</param>
        internal FuzzyResult(T item, int index, FuzzyMatch match, int textLength) {
            Item = item;
            Index = index;
            Match = match;
            TextLength = textLength;
        }

        /// <summary>The matched item.</summary>
        public T Item { get; }

        /// <summary>The item's original zero-based position.</summary>
        public int Index { get; }

        /// <summary>The match score and highlights.</summary>
        public FuzzyMatch Match { get; }

        internal int TextLength { get; }
    }
}
