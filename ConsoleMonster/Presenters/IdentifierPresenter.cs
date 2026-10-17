using System;
using System.Globalization;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// Numbers that identify rather than measure (process ids, ports, indexes, years): right-aligned digits without thousands
    /// separators, so <c>165628</c> never reads as <c>165 628</c>. Not summed in totals.
    /// </summary>
    public sealed class IdentifierPresenter : ICellPresenter {
        /// <summary>Whether a column name reads as an identifier: Id, PID, Port, Index, Year, Handle, Build, or a name ending in Id, ID or Port.</summary>
        /// <param name="name">The column or member name.</param>
        /// <returns>True for identifier-like names.</returns>
        public static bool LooksLikeIdentifier(string? name) {
            if (string.IsNullOrEmpty(name)) {
                return false;
            }
            switch (name!.ToLowerInvariant()) {
                case "id":
                case "pid":
                case "ppid":
                case "port":
                case "index":
                case "year":
                case "handle":
                case "build":
                case "number":
                    return true;
            }
            return name.EndsWith("Id", StringComparison.Ordinal) || name.EndsWith("ID", StringComparison.Ordinal) || name.EndsWith("Port", StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            if (value == null || !PresenterValues.TryGetNumber(value, out double number) || value is TimeSpan) {
                return new TextPresenter().Present(value, context);
            }
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? number.ToString("0", CultureInfo.InvariantCulture);
            return new PresentedValue(StyledLine.From(text, context.Render.Style(ThemeToken.Text)), HorizontalAlignment.Right);
        }
    }
}
