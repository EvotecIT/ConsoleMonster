using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a clipped vertical viewport for a small widget tree inside an app.</para>
    /// <para type="description">Mouse wheel scrolls the viewport. Tab navigation reveals focused inputs. Lists, grids and logs keep their own row windows for large data.</para>
    /// </summary>
    /// <example><para>A scrollable detail pane.</para><code>New-ConsoleScrollView -Content { $details } -Height 12</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleScrollView")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleScrollView : PSCmdlet {
        /// <summary><para type="description">Widgets, text, objects, or a script block that emits them.</para></summary>
        [Parameter(Position = 0)] public object? Content { get; set; }
        /// <summary><para type="description">A widget supplied through the pipeline.</para></summary>
        [Parameter(ValueFromPipeline = true)] public IWidget? Widget { get; set; }
        /// <summary><para type="description">First visible content row.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] public int ScrollOffset { get; set; }
        /// <summary><para type="description">Maximum composed rows for ordinary content; use virtualized lists or grids for larger data.</para></summary>
        [Parameter, ValidateRange(1, 16384)] public int MaxContentHeight { get; set; } = 16384;
        /// <summary><para type="description">Share of the parent layout.</para></summary>
        [Parameter] public double Weight { get; set; }
        /// <summary><para type="description">Fixed viewport height in cells.</para></summary>
        [Parameter] public int Height { get; set; }
        /// <summary><para type="description">Use the available height.</para></summary>
        [Parameter] public SwitchParameter Fill { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() {
            List<IWidget> children = PSWidgets.CollectAny(Content, this);
            if (Widget != null) { children.Add(Widget); }
            ScrollView scroll = new ScrollView(children.Count == 1 ? children[0] : new Rows(children.ToArray())) { MaxContentHeight = MaxContentHeight };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(ScrollOffset))) { scroll.ScrollOffset = ScrollOffset; }
            WriteObject(PSWidgets.WithSize(scroll, Weight, Height, Fill.IsPresent));
        }
    }
}
