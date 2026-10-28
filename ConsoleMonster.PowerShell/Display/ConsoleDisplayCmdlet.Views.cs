using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    public abstract partial class ConsoleDisplayCmdlet {
        private void ValidateView(string view) {
            const string common = "InputObject Content Title Width Culture AsString AsWidget PassThru View";
            string specific;
            switch (view.ToLowerInvariant()) {
                case "auto": specific = "Property Depth ChildrenProperty"; break;
                case "table": specific = "Property GroupBy ShowTotals MaxRows Border Caption CardsBelow NoHeaderSeparator Expand StatusProperty ShowGroupColumn NoSort Wrap TitleIndent WrapHeaders KeepAllColumns GroupHeadersAsRows RecordViewRows ShowKeyField NoHiddenColumns NoStableWidths NoHeader AlwaysCards RowTitles ShowSubtotals TotalLabel WrapLines TotalRowCount Presentation"; break;
                case "list": specific = "Property GroupBy ShowEmpty Depth"; break;
                case "tree": specific = "ChildrenProperty Children LabelProperty StatusProperty DetailProperty Depth MaxItems MaxNodes Include Exclude"; break;
                case "inspector": specific = "Depth MaxItems MaxNodes Include Exclude"; break;
                case "chart": specific = "Property Label Type Sort MaxRows Bins Unit"; break;
                case "code": specific = "Path Language LineNumbers FirstLine MaxLines Highlight Wrap NoTitle"; break;
                case "json": specific = "Path Depth LineNumbers FirstLine MaxLines Highlight Wrap NoTitle"; break;
                case "error": specific = "ShowFrameworkFrames MaxFrames NoStack Compact MaxInner NoSource"; break;
                case "markdown": specific = "Path NoPager"; break;
                case "rule": specific = "Alignment LineRole LineStyle"; break;
                default: throw new PSArgumentException("Unknown View '" + view + "'.");
            }
            var allowed = new HashSet<string>((common + " " + specific).Split(' '), StringComparer.OrdinalIgnoreCase);
            foreach (string name in MyInvocation.BoundParameters.Keys) {
                // Common PowerShell parameters are owned by the engine, rather than view configuration.
                if (typeof(ConsoleDisplayCmdlet).GetProperty(name) != null && !allowed.Contains(name)) {
                    throw new PSArgumentException(view + " does not support " + name + ". Supported view options: " + specific + ".");
                }
            }
        }

        private IWidget? BuildView(string view, IReadOnlyList<PSObject> items) {
            switch (view.ToLowerInvariant()) {
                case "table": return BuildTableView(items);
                case "list": return ConsoleHostViews.List(items, Property, GroupBy, ShowEmpty.IsPresent, Depth, PSText.Markup(Title), Culture, this);
                case "tree": return BuildTreeView(items);
                case "inspector": return BuildInspectorView(items);
                case "chart": return BuildChartView(items);
                case "code": case "json": return BuildCodeView(items, view.Equals("Json", StringComparison.OrdinalIgnoreCase));
                case "error": return BuildErrorView(items);
                case "markdown": return MarkdownExportBridge.Parse(string.Join("\n", items.Select(PSText.PlainText)));
                case "rule": return new Rule(PSText.Markup(Title) ?? (items.Count == 0 ? null : PSText.Markup(items[0]))) { Alignment = Alignment, LineRole = LineRole, LineStyle = LineStyle };
                default:
                    if (ChildrenProperty != null) { return BuildTreeView(items); }
                    // `catch { $_ | Show-Console }` means the error, not the ErrorRecord's object graph.
                    if (items.Count > 0 && items.All(item => PSText.Unwrap(item) is ErrorRecord || PSText.Unwrap(item) is Exception)) { return BuildErrorView(items); }
                    return PSOutputExport.Widget(items.Cast<object>().ToList(), this, Property, PSText.Markup(Title), Depth);
            }
        }

        private IWidget BuildCodeView(IReadOnlyList<PSObject> items, bool json) => Views.ConsoleView.BuildCode(
            items.Select(item => PSText.Unwrap(item) is string text ? (object?)text : item).ToArray(),
            new Views.ConsoleViewOptions {
                View = json ? Views.ConsoleViewKind.Json : Views.ConsoleViewKind.Code,
                Language = Language, Path = Path, LineNumbers = LineNumbers.IsPresent, FirstLine = FirstLine,
                Wrap = Wrap.IsPresent, MaxLines = MaxLines, Title = PSText.Markup(Title), NoTitle = NoTitle.IsPresent,
                Highlight = Highlight, Depth = Depth, Shape = Binding.PSShaping.Options()
            });

        private IWidget BuildErrorView(IReadOnlyList<PSObject> items) => Views.ConsoleView.BuildErrors(
            items.Select(item => (object?)PSErrors.Describe(PSText.Unwrap(item)!, Compact.IsPresent)).ToArray(),
            new Views.ConsoleViewOptions {
                ShowFrameworkFrames = ShowFrameworkFrames.IsPresent, MaxFrames = MaxFrames,
                ShowStack = !NoStack.IsPresent, MaxInner = MaxInner, ShowSource = !NoSource.IsPresent,
                Compact = Compact.IsPresent,
                DetailsHint = Compact.IsPresent && InvokeCommand.GetCommand("Get-Error", CommandTypes.Cmdlet) != null
                    ? Localization.WidgetLabels.For(Terminal.GetCurrentProfile().Language).GetErrorDetails : null,
                BasePath = SessionState.Path.CurrentFileSystemLocation.ProviderPath
            });
    }
}
