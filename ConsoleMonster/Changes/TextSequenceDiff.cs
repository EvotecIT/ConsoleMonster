using System;
using System.Collections.Generic;

namespace ConsoleMonster.Changes {
    /// <summary>
    /// Linear-space Hirschberg alignment with a bounded cell-comparison budget. Equal prefixes/suffixes are always
    /// retained; unresolved ranges become removals followed by additions when the budget is exhausted.
    /// </summary>
    internal static class TextSequenceDiff {
        internal static List<ChangeKind> Compare(string[] before, string[] after) {
            List<ChangeKind> edits = new List<ChangeKind>();
            long budget = 8_000_000;
            Align(before, 0, before.Length, after, 0, after.Length, edits, ref budget);
            return edits;
        }

        private static void Align(string[] a, int startA, int endA, string[] b, int startB, int endB,
            List<ChangeKind> edits, ref long budget) {
            while (startA < endA && startB < endB && a[startA] == b[startB]) {
                edits.Add(ChangeKind.Unchanged);
                startA++;
                startB++;
            }
            int suffix = 0;
            while (startA < endA && startB < endB && a[endA - 1] == b[endB - 1]) {
                endA--;
                endB--;
                suffix++;
            }
            int n = endA - startA;
            int m = endB - startB;
            if (n == 0 || m == 0 || (long)n * m > budget) {
                Repeat(edits, ChangeKind.Removed, n);
                Repeat(edits, ChangeKind.Added, m);
            } else if (n == 1) {
                budget -= m;
                int match = startB;
                while (match < endB && a[startA] != b[match]) {
                    match++;
                }
                if (match == endB) {
                    edits.Add(ChangeKind.Removed);
                    Repeat(edits, ChangeKind.Added, m);
                } else {
                    Repeat(edits, ChangeKind.Added, match - startB);
                    edits.Add(ChangeKind.Unchanged);
                    Repeat(edits, ChangeKind.Added, endB - match - 1);
                }
            } else {
                budget -= (long)n * m;
                int middle = startA + n / 2;
                int split = Split(a, startA, middle, endA, b, startB, endB);
                Align(a, startA, middle, b, startB, split, edits, ref budget);
                Align(a, middle, endA, b, split, endB, edits, ref budget);
            }
            Repeat(edits, ChangeKind.Unchanged, suffix);
        }

        // Keep the two score rows out of recursive frames: peak auxiliary storage is linear, not n*m.
        private static int Split(string[] a, int startA, int middle, int endA, string[] b, int startB, int endB) {
            int[] left = Scores(a, startA, middle, b, startB, endB, false);
            int[] right = Scores(a, middle, endA, b, startB, endB, true);
            int best = -1;
            int split = 0;
            int count = endB - startB;
            for (int i = 0; i <= count; i++) {
                int score = left[i] + right[count - i];
                if (score > best) {
                    best = score;
                    split = i;
                }
            }
            return startB + split;
        }

        private static int[] Scores(string[] a, int startA, int endA, string[] b, int startB, int endB, bool reverse) {
            int count = endB - startB;
            int[] row = new int[count + 1];
            for (int i = 0; i < endA - startA; i++) {
                int diagonal = 0;
                string item = a[reverse ? endA - i - 1 : startA + i];
                for (int j = 1; j <= count; j++) {
                    int previous = row[j];
                    row[j] = item == b[reverse ? endB - j : startB + j - 1]
                        ? diagonal + 1 : Math.Max(row[j], row[j - 1]);
                    diagonal = previous;
                }
            }
            return row;
        }

        private static void Repeat(List<ChangeKind> edits, ChangeKind kind, int count) {
            for (int i = 0; i < count; i++) {
                edits.Add(kind);
            }
        }
    }
}
