using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Presenters;

namespace ConsoleMonster.Interactive {
    public sealed partial class DataGrid {
        private string Text(GridRow row, int column) => PresenterValues.ToText(Value(row, column), Culture);
        private static object? Value(GridRow row, int column) {
            object? value = column >= 0 && column < row.Values.Count ? row.Values[column] : null;
            return value is DataGridCell lazy ? lazy.Peek() : value;
        }

        private void Rebuild(object? focused = null) {
            focused ??= CurrentRowCore?.Id;
            string[] words = _filter.Trim().Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<int> shown = ShownColumns();
            _visible = Sorted(AllRows().Where(i => words.All(word => shown.Any(c => Text(_grid.Rows[i], c).IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0))));
            int keep = focused == null ? -1 : _visible.FindIndex(i => Equals(_grid.Rows[i].Id, focused));
            _cursor = keep < 0 ? Clamp(_cursor) : keep;
            _dragAnchor = -1;
        }

        private List<int> Sorted(IEnumerable<int> rows) {
            var result = new List<int>(rows);
            if (_sortColumn < 0) { return result; }
            int column = _sortColumn;
            result.Sort((a, b) => {
                object? x = Value(_grid.Rows[a], column), y = Value(_grid.Rows[b], column);
                int compare;
                bool emptyX = PresenterValues.IsEmpty(x), emptyY = PresenterValues.IsEmpty(y);
                if (emptyX || emptyY) { compare = emptyX == emptyY ? 0 : emptyX ? 1 : -1; }
                else if (!(x is string) && !(y is string) && PresenterValues.TryGetNumber(x, out double dx) && PresenterValues.TryGetNumber(y, out double dy)) { compare = dx.CompareTo(dy); }
                else if (!(x is string) && x is IComparable comparable && y != null && x.GetType() == y.GetType()) { compare = comparable.CompareTo(y); }
                else { compare = string.Compare(Text(_grid.Rows[a], column), Text(_grid.Rows[b], column), Culture, CompareOptions.IgnoreCase); }
                return compare == 0 ? a.CompareTo(b) : _descending ? -compare : compare;
            });
            return result;
        }

        private void ApplyPattern(string pattern, int column, bool mark) {
            if (pattern == null) { throw new ArgumentNullException(nameof(pattern)); }
            if (column < 0 || column >= _grid.Columns.Count) { throw new ArgumentOutOfRangeException(nameof(column)); }
            Change(() => {
                foreach (int index in _visible) {
                    GridRow row = _grid.Rows[index];
                    if (!PatternMatches(Text(row, column), pattern)) { continue; }
                    if (mark) { _marked.Add(row.Id); } else { _marked.Remove(row.Id); }
                }
            });
        }

        // Linear-space wildcard matching without a regex timeout or an allocation per candidate.
        private static bool PatternMatches(string text, string pattern) {
            int t = 0, p = 0, star = -1, retry = 0;
            while (t < text.Length) {
                if (p < pattern.Length && (pattern[p] == '?' || char.ToUpperInvariant(pattern[p]) == char.ToUpperInvariant(text[t]))) { p++; t++; }
                else if (p < pattern.Length && pattern[p] == '*') { star = p++; retry = t; }
                else if (star >= 0) { p = star + 1; t = ++retry; }
                else { return false; }
            }
            while (p < pattern.Length && pattern[p] == '*') { p++; }
            return p == pattern.Length;
        }
    }
}
