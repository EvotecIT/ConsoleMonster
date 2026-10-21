using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows the structure of pipeline objects as a tree (or an inspector view) and writes it to the host.</para>
    /// <para type="description">Works for any object: PSCustomObjects, .NET objects, hashtables and dictionaries, nested to -Depth levels. Collections list up to -MaxItems items followed by "+N more"; reference cycles and the depth limit are marked instead of recursing forever. -Include and -Exclude filter member names (wildcards). With -AsInspector the object is shown as a record view: nested objects as indented sections and uniform collections as nested tables. Redirected output is plain text.</para>
    /// </summary>
    /// <example>
    ///   <para>A nested configuration</para>
    ///   <code>Get-Content settings.json -Raw | ConvertFrom-Json | Format-ConsoleTree -Depth 4</code>
    /// </example>
    /// <example>
    ///   <para>A certificate as an inspector</para>
    ///   <code>Get-ChildItem Cert:\CurrentUser\My | Select-Object -First 1 | Format-ConsoleTree -AsInspector -Include Subject, Issuer, NotAfter, Extensions, DnsNameList</code>
    /// </example>
    [Cmdlet(VerbsCommon.Format, "ConsoleTree")]
    public sealed class CmdletFormatConsoleTree : PSCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();

        /// <summary><para type="description">The objects to show (pipeline input).</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">How deep nested objects are followed. Without it, .NET objects with more than 12 members (a process, a file, a service) open one level, and smaller objects, dictionaries and parsed JSON three, so a tree stays a readable size.</para></summary>
        [Parameter]
        public int Depth { get; set; } = 3;

        /// <summary><para type="description">The most items listed per collection before "+N more" (default 25).</para></summary>
        [Parameter]
        public int MaxItems { get; set; } = 25;

        /// <summary><para type="description">The most nodes one object may expand into (default 5000); beyond it a "node limit reached" marker ends the expansion. Wide objects grow as members to the power of -Depth.</para></summary>
        [Parameter]
        public int MaxNodes { get; set; } = 5000;

        /// <summary><para type="description">Member names to show (wildcards); others are left out.</para></summary>
        [Parameter]
        public string[]? Include { get; set; }

        /// <summary><para type="description">Member names to leave out (wildcards).</para></summary>
        [Parameter]
        public string[]? Exclude { get; set; }

        /// <summary><para type="description">The root label (default: the object's type name).</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">Shows a record view (sections and nested tables) instead of a tree.</para></summary>
        [Parameter]
        public SwitchParameter AsInspector { get; set; }

        /// <summary><para type="description">Lays the output out for this width instead of the terminal's.</para></summary>
        [Parameter]
        public int Width { get; set; }

        /// <summary><para type="description">The culture for numbers and dates, for example en-US or pl-PL (labels stay English). Defaults to CONSOLEMONSTER_CULTURE, then the current culture, as PowerShell's own formatting does.</para></summary>
        [Parameter]
        public System.Globalization.CultureInfo? Culture { get; set; }

        /// <summary><para type="description">Writes the view as one string of plain text instead of drawing it, to keep it in a variable or a file from any script, function or PowerShell version. Without it, PowerShell 7.2+ captures a view typed at the prompt ($x = ..., &gt; file, Out-File); inside scripts and functions, and on Windows PowerShell 5.1, the view is drawn on the screen. Piped into another command (Out-Host, Out-String, Select-String) the view goes on as plain lines, without colour. Cannot be combined with -PassThru.</para></summary>
        [Parameter]
        public SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Also writes the input objects to the pipeline.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">Returns semantic Markdown, fenced Text with the exact console layout, or a self-contained Html page. Cannot be combined with -AsString or -PassThru.</para></summary>
        [Parameter]
        [ValidateSet("Text", "Markdown", "Html")]
        public string? As { get; set; }

        /// <inheritdoc />
        protected override void BeginProcessing() => PSOutputExport.Validate(this);

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _items.Add(InputObject);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_items.Count == 0) {
                return;
            }
            List<IWidget> widgets = new List<IWidget>();
            foreach (PSObject item in _items) {
                // The root object shows all its properties; nested objects with a default display set show that set.
                int depth = MyInvocation.BoundParameters.ContainsKey(nameof(Depth)) ? Depth : InspectorOptions.DefaultDepthFor(PSFormatting.FieldCount(item), Depth);
                InspectorOptions options = PSFormatting.InspectorOptions(depth, MaxItems, Include, Exclude, MaxNodes, item);
#pragma warning disable IL2026
                InspectNode node = ObjectInspector.Inspect(item, options);
#pragma warning restore IL2026
                widgets.Add(AsInspector.IsPresent ? new InspectorView(node, PSText.Markup(Title)) : (IWidget)InspectorTree.Build(node, PSText.Markup(Title)));
            }
            IWidget output = widgets.Count == 1 ? widgets[0] : new Rows(widgets.ToArray()) { Gap = 1 };
            PSFormatting.Emit(this, output, Width, Culture, AsString.IsPresent, PassThru.IsPresent);
            if (PassThru.IsPresent) {
                foreach (PSObject item in _items) {
                    WriteObject(item);
                }
            }
        }
    }
}
