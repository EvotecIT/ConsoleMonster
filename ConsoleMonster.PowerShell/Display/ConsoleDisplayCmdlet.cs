using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Shared display construction and output for Show-Console and its Table/List shortcuts.</summary>
    public abstract partial class ConsoleDisplayCmdlet : TableConfigurationCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();
        private readonly List<PSObject?> _chartInputs = new List<PSObject?>();
        private CancellationTokenSource? _paging;
        private volatile bool _stopped;
        /// <summary>The view selected by the concrete command.</summary>
        protected abstract string RequestedView { get; }
        /// <summary><para type="description">The object, widget or text to display.</para></summary>
        public virtual PSObject? InputObject { get; set; }
        /// <summary><para type="description">A file for Code, Json or Markdown.</para></summary>
        public virtual string? Path { get; set; }
        /// <summary><para type="description">A widget, text or script block producing display content.</para></summary>
        public virtual object? Content { get; set; }
        /// <summary><para type="description">Property names, patterns or calculated column records.</para></summary>
        public virtual object[]? Property { get; set; }
        /// <summary><para type="description">Groups rows or records by a property or script block.</para></summary>
        public virtual object? GroupBy { get; set; }
        /// <summary><para type="description">The view title, including markup.</para></summary>
        public virtual object? Title { get; set; }
        /// <summary><para type="description">A table caption.</para></summary>
        public virtual object? Caption { get; set; }
        /// <summary><para type="description">Layout width; zero resolves the terminal with a 100-cell fallback.</para></summary>
        public virtual int Width { get; set; }
        /// <summary><para type="description">The culture used for values.</para></summary>
        public virtual CultureInfo? Culture { get; set; }
        /// <summary><para type="description">Returns plain text instead of displaying it.</para></summary>
        public virtual SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Returns the constructed widget.</para></summary>
        public virtual SwitchParameter AsWidget { get; set; }
        /// <summary><para type="description">Returns the original input after display.</para></summary>
        public virtual SwitchParameter PassThru { get; set; }
        /// <summary><para type="description">Shows a totals row for numeric columns.</para></summary>
        public virtual SwitchParameter ShowTotals { get; set; }
        /// <summary><para type="description">Maximum displayed rows or chart groups; zero shows all.</para></summary>
        public virtual int MaxRows { get; set; }
        /// <summary><para type="description">Table border style; default Rounded.</para></summary>
        public virtual TableBorder Border { get; set; } = TableBorder.Rounded;
        /// <summary><para type="description">Table card breakpoint; default 60 cells.</para></summary>
        public virtual int CardsBelow { get; set; } = 60;
        /// <summary><para type="description">Underlines headers rather than drawing a separator.</para></summary>
        public virtual SwitchParameter NoHeaderSeparator { get; set; }
        /// <summary><para type="description">Expands a table to the available width.</para></summary>
        public virtual SwitchParameter Expand { get; set; }
        /// <summary><para type="description">The property shown through the status presenter.</para></summary>
        public virtual string? StatusProperty { get; set; }
        /// <summary><para type="description">Keeps the grouped property in table rows.</para></summary>
        public virtual SwitchParameter ShowGroupColumn { get; set; }
        /// <summary><para type="description">Preserves the input order of groups.</para></summary>
        public virtual SwitchParameter NoSort { get; set; }
        /// <summary><para type="description">Wraps long table values or code lines.</para></summary>
        public virtual SwitchParameter Wrap { get; set; }
        /// <summary><para type="description">Keeps empty fields in record views.</para></summary>
        public virtual SwitchParameter ShowEmpty { get; set; }
        /// <summary><para type="description">Maximum nesting depth; default three.</para></summary>
        public virtual int Depth { get; set; } = 3;
        /// <summary><para type="description">The property containing a hierarchy node children.</para></summary>
        public virtual string? ChildrenProperty { get; set; }
        /// <summary><para type="description">Produces children for a hierarchy node.</para></summary>
        public virtual ScriptBlock? Children { get; set; }
        /// <summary><para type="description">The hierarchy label property.</para></summary>
        public virtual string? LabelProperty { get; set; }
        /// <summary><para type="description">The hierarchy detail property.</para></summary>
        public virtual string? DetailProperty { get; set; }
        /// <summary><para type="description">Maximum inspected items per collection; default 100.</para></summary>
        public virtual int MaxItems { get; set; } = 100;
        /// <summary><para type="description">Maximum hierarchy nodes; default 1000.</para></summary>
        public virtual int MaxNodes { get; set; } = 1000;
        /// <summary><para type="description">Object members to include.</para></summary>
        public virtual string[]? Include { get; set; }
        /// <summary><para type="description">Object members omitted without reading their getters.</para></summary>
        public virtual string[]? Exclude { get; set; }
        /// <summary><para type="description">Code language; default Auto.</para></summary>
        public virtual string Language { get; set; } = "Auto";
        /// <summary><para type="description">Shows code line numbers.</para></summary>
        public virtual SwitchParameter LineNumbers { get; set; }
        /// <summary><para type="description">Line numbers or ranges to highlight.</para></summary>
        public virtual string[]? Highlight { get; set; }
        /// <summary><para type="description">The first code line number; default one.</para></summary>
        public virtual int FirstLine { get; set; } = 1;
        /// <summary><para type="description">Maximum displayed code lines; default 2000.</para></summary>
        public virtual int MaxLines { get; set; } = 2000;
        /// <summary><para type="description">Hides a code file title.</para></summary>
        public virtual SwitchParameter NoTitle { get; set; }
        /// <summary><para type="description">Numeric chart representation: Bar, Histogram or Sparkline.</para></summary>
        public virtual ChartType Type { get; set; }
        /// <summary><para type="description">The chart label property or expression; default Name.</para></summary>
        public virtual object Label { get; set; } = "Name";
        /// <summary><para type="description">Chart ordering.</para></summary>
        public virtual PipelineChartSort Sort { get; set; }
        /// <summary><para type="description">Histogram bins; default ten.</para></summary>
        public virtual int Bins { get; set; } = 10;
        /// <summary><para type="description">The chart numeric unit.</para></summary>
        public virtual string? Unit { get; set; }
        /// <summary><para type="description">Shows framework stack frames in error views.</para></summary>
        public virtual SwitchParameter ShowFrameworkFrames { get; set; }
        /// <summary><para type="description">Maximum application frames per error; default eight.</para></summary>
        public virtual int MaxFrames { get; set; } = 8;
        /// <summary><para type="description">Hides error stack frames.</para></summary>
        public virtual SwitchParameter NoStack { get; set; }
        /// <summary><para type="description">Uses the compact everyday error layout.</para></summary>
        public virtual SwitchParameter Compact { get; set; }
        /// <summary><para type="description">Maximum inner errors; default four.</para></summary>
        public virtual int MaxInner { get; set; } = 4;
        /// <summary><para type="description">Suppresses default-on source excerpts in error views.</para></summary>
        public virtual SwitchParameter NoSource { get; set; }
        /// <summary><para type="description">Disables default-on Markdown paging.</para></summary>
        public virtual SwitchParameter NoPager { get; set; }
        /// <summary><para type="description">The rule title alignment; default Left.</para></summary>
        public virtual HorizontalAlignment Alignment { get; set; }
        /// <summary><para type="description">Theme role for a rule line; default Border.</para></summary>
        public virtual Styling.ThemeToken LineRole { get; set; } = Styling.ThemeToken.Border;
        /// <summary><para type="description">Optional rule line glyph style.</para></summary>
        public virtual Glyphs.BoxStyle? LineStyle { get; set; }
        /// <inheritdoc />
        protected override void BeginProcessing() { PSOutputExport.Validate(this); ValidateView(RequestedView); }
        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (string.Equals(RequestedView, "Chart", StringComparison.OrdinalIgnoreCase) &&
                (InputObject != null || MyInvocation.BoundParameters.ContainsKey(nameof(InputObject)))) { _chartInputs.Add(InputObject); }
            if (InputObject != null) { _items.Add(InputObject); }
        }
        /// <inheritdoc />
        protected override void EndProcessing() {
            if (Path != null) {
                string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path, out ProviderInfo provider, out PSDriveInfo _);
                if (!string.Equals(provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase)) { throw new PSArgumentException("Path must name a FileSystem path."); }
                _items.Add(PSObject.AsPSObject(File.ReadAllText(path)));
            }
            if (Content != null) {
                foreach (object item in PSItems.Items(Content)) { _items.Add(PSObject.AsPSObject(item)); }
            }
            string view = RequestedView;
            if (_items.Count == 0 && _chartInputs.Count == 0 && !string.Equals(view, "Rule", StringComparison.OrdinalIgnoreCase)) { return; }
            IWidget? widget = BuildView(view, _items);
            if (widget == null) { return; }
            if (AsWidget.IsPresent) { WriteObject(Culture == null ? widget : (IWidget)new CultureScope(widget, Culture)); return; }
            using (var stop = new CancellationTokenSource()) {
                _paging = stop;
                try {
                    if (_stopped) { stop.Cancel(); }
                    if (string.Equals(view, "Markdown", StringComparison.OrdinalIgnoreCase) && !NoPager.IsPresent && !AsString.IsPresent && FormatCommandRouter.IsDirectHostOutput(this)) {
                        if (Interactive.WidgetPager.TryShow(widget, "Markdown", PSInteractive.Options(this, null), stop.Token, contentWidth: Width > 0 ? Width : (int?)null)) { return; }
                    }
                    stop.Token.ThrowIfCancellationRequested();
                    PSFormatting.Emit(this, widget, Width, Culture, AsString.IsPresent, PassThru.IsPresent,
                        appendBlankLine: !string.Equals(view, "Rule", StringComparison.OrdinalIgnoreCase) && !_items.Exists(item => PSText.Unwrap(item) is IWidget));
                    if (PassThru.IsPresent) { foreach (PSObject item in _items) { WriteObject(item); } }
                } catch (OperationCanceledException) when (_stopped) { }
                finally { _paging = null; }
            }
        }
        /// <inheritdoc />
        protected override void StopProcessing() { _stopped = true; try { _paging?.Cancel(); } catch (ObjectDisposedException) { } base.StopProcessing(); }


    }
}
