using System;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class RecordViewLayoutTests {
        private static int ValueColumn(string[] rows, string value) {
            string row = Assert.Single(rows, line => line.Contains(value));
            return row.IndexOf(value, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(80)]
        [InlineData(120)]
        public void Render_KeysAfterANestedBlockShareTheLevelsValueColumn(int width) {
            // The shape Show-Console -View List builds for a release asset: fields, a nested object, then more fields.
            RecordView nested = new RecordView().AddFieldAt(0, "login", new TableCell("daxian-dbw"));
            RecordView view = new RecordView()
                .AddFieldAt(0, "content_type", new TableCell("application/zip"))
                .AddFieldAt(0, "id", new TableCell("127450"));
            view.AddHeading("uploader", 0);
            view.AddWidgetAt(1, nested);
            view.AddFieldAt(0, "url", new TableCell("https://api.example/asset"));
            string[] rows = TestTerminal.RenderText(view, width).Split('\n');
            Assert.Equal(ValueColumn(rows, "application/zip"), ValueColumn(rows, "127450"));
            Assert.Equal(ValueColumn(rows, "application/zip"), ValueColumn(rows, "https://api.example/asset"));
        }

        [Theory]
        [InlineData(80)]
        [InlineData(120)]
        public void Render_KeyPastTheLabelCapKeepsItsNameAndPutsItsValueBelowInTheValueColumn(int width) {
            string longKey = new string('k', width / 2);
            RecordView view = new RecordView()
                .AddFieldAt(0, "name", new TableCell("asset.zip"))
                .AddFieldAt(0, longKey, new TableCell("long-value"))
                .AddFieldAt(0, "size", new TableCell("1024"));
            string[] rows = TestTerminal.RenderText(view, width).Split('\n');
            int keyRow = Array.FindIndex(rows, row => row.Contains(longKey));
            Assert.Equal(longKey, rows[keyRow].Trim());
            Assert.Contains("long-value", rows[keyRow + 1]);
            Assert.Equal(ValueColumn(rows, "asset.zip"), ValueColumn(rows, "long-value"));
            Assert.Equal(ValueColumn(rows, "asset.zip"), ValueColumn(rows, "1024"));
        }
    }
}
