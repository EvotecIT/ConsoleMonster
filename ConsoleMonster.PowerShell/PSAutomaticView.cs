using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.Values;
using ConsoleMonster.Views;
using ConsoleMonster.Widgets;
using ConsoleMonster.PowerShell.Binding;

namespace ConsoleMonster.PowerShell {
    /// <summary>PowerShell member adaptation for the core automatic view, shared by display and export.</summary>
    internal static class PSAutomaticView {
        internal static IWidget Build(IReadOnlyList<PSObject> input, PSCmdlet cmdlet, object[]? property = null, string? title = null, int depth = 3) {
            ShapeOptions shape = PSShaping.Options();
            IReadOnlyList<PSObject> items = PSFormatting.Expand(input, shape);
            ConsoleViewOptions options = new ConsoleViewOptions { Title = title, Depth = depth };
            options.Sources.Add(new DirectoryEntryMemberSource());
            options.Sources.Add(new PSObjectMemberSource());
            List<object?> values = new List<object?>(items.Count);
            foreach (PSObject item in items) {
                values.Add(PSShaping.IsValueLike(item, shape) ? ValueShaper.Prepare(item, shape) : item);
            }
            ConsoleViewKind kind = ConsoleView.Decide(values, options);
            if (kind == ConsoleViewKind.Inspector && items.Count == 1) {
                return PSFormatting.BuildList(items, property, null, false, 1, title, null, cmdlet);
            }
            if (kind == ConsoleViewKind.Table || kind == ConsoleViewKind.Cards || kind == ConsoleViewKind.Inspector) {
                bool first = true;
                return PSFormatting.Combine(PSFormatting.BuildTables(items, property, null,
                    new TableBuildOptions { AutoPresenters = true, Cmdlet = cmdlet }, table => {
                        if (title != null) { table.Title = first ? title : null; }
                        table.AlwaysCards = kind == ConsoleViewKind.Cards;
                        first = false;
                    }));
            }
            options.View = kind;
            return ConsoleView.Auto(values, options);
        }
    }
}
