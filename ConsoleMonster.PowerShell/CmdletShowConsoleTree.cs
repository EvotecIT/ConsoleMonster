using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows a hierarchy from objects, dictionaries or explicit tree nodes.</para>
    /// <para type="description">Use ChildrenProperty or Children for a hierarchy, Content for New-ConsoleTreeNode definitions, or InputObject for a nested dictionary or object graph. Object inspection is bounded by Depth, MaxItems and MaxNodes. Show-ConsoleObject provides the keyboard explorer.</para>
    /// </summary>
    /// <example><para>Display an object graph.</para><code>$configuration | Show-ConsoleTree -Depth 4</code></example>
    /// <example><para>Show explicit nodes.</para><code>Show-ConsoleTree 'Checks' -Content { New-ConsoleTreeNode 'DNS' -Status Pass }</code></example>
    [Cmdlet(VerbsCommon.Show, "ConsoleTree", DefaultParameterSetName = "Nodes")]
    public sealed class CmdletShowConsoleTree : CmdletNewConsoleTree {
        private readonly List<PSObject> _items = new List<PSObject>();
        /// <summary><para type="description">Member names to include when inspecting an object graph.</para></summary>
        [Parameter] public string[]? Include { get; set; }
        /// <summary><para type="description">Member names to omit without reading their getters.</para></summary>
        [Parameter] public string[]? Exclude { get; set; }
        /// <summary><para type="description">Layout width in cells; zero resolves the current target, falling back to 100.</para></summary>
        [Parameter] [ValidateRange(0, 1000)] public int Width { get; set; }
        /// <summary><para type="description">Culture used for values.</para></summary>
        [Parameter] public System.Globalization.CultureInfo? Culture { get; set; }
        /// <summary><para type="description">Returns one plain-text string without drawing.</para></summary>
        [Parameter] public SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Returns the tree widget without drawing.</para></summary>
        [Parameter] public SwitchParameter AsWidget { get; set; }
        /// <summary><para type="description">Returns the original pipeline objects after drawing.</para></summary>
        [Parameter] public SwitchParameter PassThru { get; set; }
        /// <inheritdoc />
        protected override void BeginProcessing() => PSOutputExport.Validate(this);
        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) { _items.Add(InputObject); }
            base.ProcessRecord();
        }
        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_items.Count == 0 || ChildrenProperty != null || Children != null || LabelProperty != null || StatusProperty != null || DetailProperty != null
                || (_items.Count == 1 && PSText.Unwrap(_items[0]) is IDictionary) || _items.TrueForAll(item => PSText.Unwrap(item) is TreeNode)) {
                base.EndProcessing();
            } else {
                List<IWidget> widgets = new List<IWidget>();
                foreach (PSObject item in _items) {
                    InspectorOptions options = PSFormatting.InspectorOptions(Depth, MaxItems, Include, Exclude, MaxNodes, item);
#pragma warning disable IL2026
                    widgets.Add(InspectorTree.Build(ObjectInspector.Inspect(item, options), PSText.Markup(Title)));
#pragma warning restore IL2026
                }
                EmitView(PSFormatting.Combine(widgets));
            }
            if (PassThru.IsPresent) { foreach (PSObject item in _items) { WriteObject(item); } }
        }
        /// <inheritdoc />
        protected override void Emit(Tree tree) => EmitView(tree);
        private void EmitView(IWidget view) {
            if (AsWidget.IsPresent) { WriteObject(Culture == null ? view : (IWidget)new CultureScope(view, Culture)); return; }
            PSFormatting.Emit(this, view, Width, Culture, AsString.IsPresent, PassThru.IsPresent);
        }
    }
}
