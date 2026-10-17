using System.Collections.Generic;

namespace ConsoleMonster.Text {
    /// <summary>The score and text positions selected by a fuzzy match.</summary>
    public sealed class FuzzyMatch {
        /// <summary>Creates a match result.</summary>
        /// <param name="score">The additive score.</param>
        /// <param name="positions">Ascending UTF-16 positions matched in the text.</param>
        internal FuzzyMatch(int score, IReadOnlyList<int> positions) {
            Score = score;
            Positions = positions;
        }

        /// <summary>The match score; higher scores rank first.</summary>
        public int Score { get; }

        /// <summary>Ascending UTF-16 indexes of matched characters.</summary>
        public IReadOnlyList<int> Positions { get; }
    }
}
