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
                case "table": specific = "Property GroupBy ShowTotals MaxRows Border Caption CardsBelow NoHeaderSeparator Expand StatusProperty ShowGroupColumn NoSort Wrap"; break;
                case "list": specific = "Property GroupBy ShowEmpty Depth"; break;
                case "tree": specific = "ChildrenProperty Children LabelProperty StatusProperty DetailProperty Depth MaxItems MaxNodes Include Exclude"; break;
                case "chart": specific = "Property Label Type Sort MaxRows Bins Unit"; break;
                case "code": specific = "Path Language LineNumbers FirstLine MaxLines Highlight Wrap NoTitle"; break;
                case "json": specific = "Path Depth LineNumbers FirstLine MaxLines Highlight Wrap NoTitle"; break;
                case "error": specific = "ShowFrameworkFrames MaxFrames NoStack Compact"; break;
                case "markdown": specific = "Path NoPager"; break;
                case "rule": specific = "Alignment"; break;
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

        private IWidget BuildView(string view, IReadOnlyList<PSObject> items) {
            switch (view.ToLowerInvariant()) {
                case "table": return BuildTableView(items);
                case "list": return PSFormatting.BuildList(items, Property, GroupBy, ShowEmpty.IsPresent, Depth, PSText.Markup(Title), Culture, this);
                case "tree": return BuildTreeView(items);
                case "chart": return BuildChartView(items);
                case "code": case "json": return BuildCodeView(items, view.Equals("Json", StringComparison.OrdinalIgnoreCase));
                case "error": return BuildErrorView(items);
                case "markdown": return MarkdownExportBridge.Parse(string.Join("\n", items.Select(PSText.PlainText)));
                case "rule": return new Rule(PSText.Markup(Title) ?? (items.Count == 0 ? null : PSText.Markup(items[0]))) { Alignment = Alignment };
                default:
                    if (ChildrenProperty != null) { return BuildTreeView(items); }
                    return PSOutputExport.Widget(items.Cast<object>().ToList(), this, Property, PSText.Markup(Title), Depth);
            }
        }

        private IWidget BuildCodeView(IReadOnlyList<PSObject> items, bool json) {
            string text = string.Join("\n", items.Select(PSText.PlainText));
            if (json) {
                if (!items.All(item => PSText.Unwrap(item) is string) || !Values.ValueJson.TryReindent(text, out string indented)) {
                    object value = items.Count == 1 ? (object)items[0] : items;
                    text = Values.ValueJson.Serialize(value, Depth, Binding.PSShaping.Options());
                } else { text = indented; }
            }
            var code = CodeView.For(text, json ? "Json" : Language, Path);
            code.LineNumbers = LineNumbers.IsPresent;
            code.FirstLine = FirstLine;
            code.Wrap = Wrap.IsPresent;
            code.MaxLines = Math.Max(1, MaxLines);
            code.Title = NoTitle.IsPresent ? null : PSText.Markup(Title) ?? (Path == null ? null : System.IO.Path.GetFileName(Path));
            foreach (string range in Highlight ?? Array.Empty<string>()) { code.HighlightLines(range); }
            return code;
        }

        private IWidget BuildErrorView(IReadOnlyList<PSObject> items) {
            var errors = new List<IWidget>();
            foreach (PSObject item in items) {
                errors.Add(new ExceptionView(PSErrors.Describe(PSText.Unwrap(item)!, Compact.IsPresent)) {
                    ShowFrameworkFrames = ShowFrameworkFrames.IsPresent, MaxFrames = Math.Max(1, MaxFrames),
                    ShowStack = !NoStack.IsPresent && (!Compact.IsPresent || ShowFrameworkFrames.IsPresent), Compact = Compact.IsPresent,
                    DetailsHint = Compact.IsPresent && InvokeCommand.GetCommand("Get-Error", CommandTypes.Cmdlet) != null
                        ? Localization.WidgetLabels.For(Terminal.GetCurrentProfile().Language).GetErrorDetails : null,
                    BasePath = SessionState.Path.CurrentFileSystemLocation.ProviderPath
                });
            }
            return errors.Count == 1 ? errors[0] : new Rows(errors.ToArray()) { Gap = 1 };
        }
    }
}
