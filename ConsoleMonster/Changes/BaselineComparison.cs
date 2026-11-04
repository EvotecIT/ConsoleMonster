using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleMonster.Changes {
    /// <summary>One original property value and its relationship to the reference column.</summary>
    public sealed class BaselineCell {
        internal BaselineCell(object? value, bool isPresent, ChangeKind kind, bool caseOnly) {
            Value = value;
            IsPresent = isPresent;
            Kind = kind;
            IsCaseOnly = caseOnly;
        }

        /// <summary>The original value, before normalization.</summary>
        public object? Value { get; }
        /// <summary>Whether this property exists; null remains a present value.</summary>
        public bool IsPresent { get; }
        /// <summary>The comparison with the baseline; Added means absent on the baseline.</summary>
        public ChangeKind Kind { get; }
        /// <summary>Whether only letter case differs and was ignored for equality.</summary>
        public bool IsCaseOnly { get; }
    }

    /// <summary>A property path with one cell per object, in input order.</summary>
    public sealed class BaselineRow {
        internal BaselineRow(string path, IReadOnlyList<BaselineCell> cells) {
            Path = path;
            Cells = cells;
        }

        /// <summary>The property path, using the object comparison path rules.</summary>
        public string Path { get; }
        /// <summary>The original values, in the same order as the comparison names.</summary>
        public IReadOnlyList<BaselineCell> Cells { get; }
        /// <summary>The number of objects without this property.</summary>
        public int MissingCount => Cells.Count(c => !c.IsPresent);
        /// <summary>Whether any cell differs from the baseline, excluding case-only differences.</summary>
        public bool HasDifferences => Cells.Any(c => c.Kind != ChangeKind.Unchanged);
        /// <summary>Whether a case-only difference is visible in this row.</summary>
        public bool HasCaseDifferences => Cells.Any(c => c.IsCaseOnly);
    }

    /// <summary>A baseline comparison whose columns preserve the supplied object order.</summary>
    public sealed class BaselineComparison {
        internal BaselineComparison(IReadOnlyList<string> names, int baselineIndex, IReadOnlyList<BaselineRow> rows, IReadOnlyList<string> notCompared, int hiddenMatchingCount) {
            Names = names;
            BaselineIndex = baselineIndex;
            Rows = rows;
            NotCompared = notCompared;
            HiddenMatchingCount = hiddenMatchingCount;
        }

        /// <summary>The object names used as column headings.</summary>
        public IReadOnlyList<string> Names { get; }
        /// <summary>The zero-based reference column index.</summary>
        public int BaselineIndex { get; }
        /// <summary>Different rows and visible case-only differences; equal rows appear when requested.</summary>
        public IReadOnlyList<BaselineRow> Rows { get; }
        /// <summary>The number of matching property rows omitted because unchanged rows were not requested.</summary>
        public int HiddenMatchingCount { get; }
        /// <summary>Paths skipped because member reading exceeded the time budget.</summary>
        public IReadOnlyList<string> NotCompared { get; }
    }
}
