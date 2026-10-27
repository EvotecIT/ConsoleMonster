using System;

namespace ConsoleMonster.Widgets {
    public partial class Table {
        /// <summary>Adds a row with a whole-row tone and emphasis, retaining raw values for presenters and totals.</summary>
        /// <param name="role">The row role. For a dedicated marker column use <see cref="RoleTable"/>.</param>
        /// <param name="cells">The original cells, which are not modified.</param>
        /// <returns>This table.</returns>
        public Table AddRow(TableRowRole role, params TableCell[] cells) {
            if (role == null) throw new ArgumentNullException(nameof(role));
            TableCell[] styled = Normalize(cells);
            for (int i = 0; i < styled.Length; i++) styled[i] = styled[i].WithRowRole(role);
            return AddRow(styled);
        }
    }
}
