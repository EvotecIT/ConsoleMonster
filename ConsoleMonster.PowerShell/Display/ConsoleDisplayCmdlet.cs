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
    public abstract partial class ConsoleDisplayCmdlet : PSCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();
        private CancellationTokenSource? _paging;
        private volatile bool _stopped;
        /// <summary>The view selected by the concrete command.</summary>
        protected abstract string RequestedView { get; }
        /// <summary><para type="description">The object, widget or text to display.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = "Objects")]
        public PSObject? InputObject { get; set; }
        /// <summary><para type="description">A file for Code, Json or Markdown.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Path")]
        public string? Path { get; set; }
        /// <summary><para type="description">A widget, text or script block producing display content.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Content")]
        public object? Content { get; set; }
        /// <summary><para type="description">Property names, patterns or calculated column records.</para></summary>
        [Parameter]
        public object[]? Property { get; set; }
        /// <summary><para type="description">Groups rows or records by a property or script block.</para></summary>
        [Parameter]
        public object? GroupBy { get; set; }
        /// <summary><para type="description">The view title, including markup.</para></summary>
        [Parameter]
        public object? Title { get; set; }
        /// <summary><para type="description">A table caption.</para></summary>
        [Parameter]
        public object? Caption { get; set; }
        /// <summary><para type="description">Layout width; zero resolves the terminal with a 100-cell fallback.</para></summary>
        [Parameter, ValidateRange(0, 1000)]
        public int Width { get; set; }
        /// <summary><para type="description">The culture used for values.</para></summary>
        [Parameter]
        public CultureInfo? Culture { get; set; }
        /// <summary><para type="description">Returns plain text instead of displaying it.</para></summary>
        [Parameter]
        public SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Returns the constructed widget.</para></summary>
        [Parameter]
        public SwitchParameter AsWidget { get; set; }
        /// <summary><para type="description">Returns the original input after display.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }
        /// <summary><para type="description">Shows a totals row for numeric columns.</para></summary>
        [Parameter]
        public SwitchParameter ShowTotals { get; set; }
        /// <summary><para type="description">Maximum displayed rows or chart groups; zero shows all.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)]
        public int MaxRows { get; set; }
        /// <summary><para type="description">Table border style; default Rounded.</para></summary>
        [Parameter]
        public TableBorder Border { get; set; } = TableBorder.Rounded;
        /// <summary><para type="description">Table card breakpoint; default 60 cells.</para></summary>
        [Parameter]
        public int CardsBelow { get; set; } = 60;
        /// <summary><para type="description">Underlines headers rather than drawing a separator.</para></summary>
        [Parameter]
        public SwitchParameter NoHeaderSeparator { get; set; }
        /// <summary><para type="description">Expands a table to the available width.</para></summary>
        [Parameter]
        public SwitchParameter Expand { get; set; }
        /// <summary><para type="description">The property shown through the status presenter.</para></summary>
        [Parameter]
        public string? StatusProperty { get; set; }
        /// <summary><para type="description">Keeps the grouped property in table rows.</para></summary>
        [Parameter]
        public SwitchParameter ShowGroupColumn { get; set; }
        /// <summary><para type="description">Preserves the input order of groups.</para></summary>
        [Parameter]
        public SwitchParameter NoSort { get; set; }
        /// <summary><para type="description">Wraps long table values or code lines.</para></summary>
        [Parameter]
        public SwitchParameter Wrap { get; set; }
        /// <summary><para type="description">Keeps empty fields in record views.</para></summary>
        [Parameter]
        public SwitchParameter ShowEmpty { get; set; }
        /// <summary><para type="description">Maximum nesting depth; default three.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)]
        public int Depth { get; set; } = 3;
        /// <summary><para type="description">The property containing a hierarchy node children.</para></summary>
        [Parameter]
        public string? ChildrenProperty { get; set; }
        /// <summary><para type="description">Produces children for a hierarchy node.</para></summary>
        [Parameter]
        public ScriptBlock? Children { get; set; }
        /// <summary><para type="description">The hierarchy label property.</para></summary>
        [Parameter]
        public string? LabelProperty { get; set; }
        /// <summary><para type="description">The hierarchy detail property.</para></summary>
        [Parameter]
        public string? DetailProperty { get; set; }
        /// <summary><para type="description">Maximum inspected items per collection; default 100.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)]
        public int MaxItems { get; set; } = 100;
        /// <summary><para type="description">Maximum hierarchy nodes; default 1000.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)]
        public int MaxNodes { get; set; } = 1000;
        /// <summary><para type="description">Object members to include.</para></summary>
        [Parameter]
        public string[]? Include { get; set; }
        /// <summary><para type="description">Object members omitted without reading their getters.</para></summary>
        [Parameter]
        public string[]? Exclude { get; set; }
        /// <summary><para type="description">Code language; default Auto.</para></summary>
        [Parameter]
        public string Language { get; set; } = "Auto";
        /// <summary><para type="description">Shows code line numbers.</para></summary>
        [Parameter]
        public SwitchParameter LineNumbers { get; set; }
        /// <summary><para type="description">Line numbers or ranges to highlight.</para></summary>
        [Parameter]
        public string[]? Highlight { get; set; }
        /// <summary><para type="description">The first code line number; default one.</para></summary>
        [Parameter]
        public int FirstLine { get; set; } = 1;
        /// <summary><para type="description">Maximum displayed code lines; default 2000.</para></summary>
        [Parameter]
        public int MaxLines { get; set; } = 2000;
        /// <summary><para type="description">Hides a code file title.</para></summary>
        [Parameter]
        public SwitchParameter NoTitle { get; set; }
        /// <summary><para type="description">Numeric chart representation: Bar, Histogram or Sparkline.</para></summary>
        [Parameter]
        public ChartType Type { get; set; }
        /// <summary><para type="description">The chart label property or expression; default Name.</para></summary>
        [Parameter]
        public object Label { get; set; } = "Name";
        /// <summary><para type="description">Chart ordering.</para></summary>
        [Parameter]
        public PipelineChartSort Sort { get; set; }
        /// <summary><para type="description">Histogram bins; default ten.</para></summary>
        [Parameter, ValidateRange(1, 128)]
        public int Bins { get; set; } = 10;
        /// <summary><para type="description">The chart numeric unit.</para></summary>
        [Parameter]
        public string? Unit { get; set; }
        /// <summary><para type="description">Shows framework stack frames in error views.</para></summary>
        [Parameter]
        public SwitchParameter ShowFrameworkFrames { get; set; }
        /// <summary><para type="description">Maximum application frames per error; default eight.</para></summary>
        [Parameter]
        public int MaxFrames { get; set; } = 8;
        /// <summary><para type="description">Hides error stack frames.</para></summary>
        [Parameter]
        public SwitchParameter NoStack { get; set; }
        /// <summary><para type="description">Uses the compact everyday error layout.</para></summary>
        [Parameter]
        public SwitchParameter Compact { get; set; }
        /// <summary><para type="description">Disables default-on Markdown paging.</para></summary>
        [Parameter]
        public SwitchParameter NoPager { get; set; }
        /// <summary><para type="description">The rule title alignment; default Left.</para></summary>
        [Parameter]
        public HorizontalAlignment Alignment { get; set; }
        /// <inheritdoc />
        protected override void BeginProcessing() => PSOutputExport.Validate(this);
        /// <inheritdoc />
        protected override void ProcessRecord() { if (InputObject != null) { _items.Add(InputObject); } }
        /// <inheritdoc />
        protected override void EndProcessing() {
            if (Path != null) {
                string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path, out ProviderInfo provider, out PSDriveInfo _);
                if (!string.Equals(provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase)) { throw new PSArgumentException("Path must name a file system path."); }
                _items.Add(PSObject.AsPSObject(File.ReadAllText(path)));
            }
            if (Content != null) {
                foreach (object item in PSItems.Items(Content)) { _items.Add(PSObject.AsPSObject(item)); }
            }
            string view = RequestedView;
            ValidateView(view);
            if (_items.Count == 0 && !string.Equals(view, "Rule", StringComparison.OrdinalIgnoreCase)) { return; }
            IWidget widget = BuildView(view, _items);
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
