using ConsoleMonster.Layout;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Widgets {
    public partial class Table {
        // The same presentation and aggregate pass as console layout, before width fitting, truncation or hidden columns.
        internal TableLayout SemanticLayout(RenderContext context) {
            Prepare();
            var presenter = Presentation.CreateContext(context);
            TableLayout layout = new TableLayout(TableMode.Grid);
            layout.Rows.AddRange(BuildRows(presenter));
            layout.Total = BuildTotalRow(presenter);
            layout.Alignments = new HorizontalAlignment[_columns.Count];
            for (int c = 0; c < _columns.Count; c++) { layout.Alignments[c] = _columns[c].Alignment ?? AutomaticAlignment(c, layout.Rows); }
            return layout;
        }
    }
}
