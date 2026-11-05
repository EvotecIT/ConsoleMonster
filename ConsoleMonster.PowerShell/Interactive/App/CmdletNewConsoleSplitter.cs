using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates two panes with a divider that the mouse and keyboard can move.</summary>
    /// <remarks>The panes sit side by side, or top and bottom with -Vertical. Dragging the divider resizes them. Ctrl+Shift+Left/Right (Ctrl+Shift+Up/Down when vertical) also resizes when the focused input and configured keys leave those chords free; text editing and existing bindings take precedence. With -Key, the app state holds the ratio under that key, so a binding that rebuilds the splitter keeps it and a callback can read or set it. A vertical splitter is as tall as its panes; -Fill makes it use the available height.</remarks>
    /// <example><para>Show a list beside its details and let the reader widen either side.</para><code>Show-ConsoleApp -Content {
    ///     New-ConsoleSplitter -Key Panes -Ratio 0.35 -Fill -First {
    ///         New-ConsoleSelect -Key Server -Choices 'web-01', 'web-02', 'db-01'
    ///     } -Second {
    ///         New-ConsoleBinding -Content { param($State) "Details for $($State.Server)" }
    ///     }
    /// }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleSplitter")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleSplitter : NoPipelineInputCmdlet {
        /// <summary><para type="description">The left pane, or the top pane with -Vertical: widgets, text or a script block that emits them.</para></summary>
        [Parameter(Position = 0)]
        [AllowNull, AllowEmptyCollection]
        public object? First { get; set; }

        /// <summary><para type="description">The right pane, or the bottom pane with -Vertical.</para></summary>
        [Parameter(Position = 1)]
        [AllowNull, AllowEmptyCollection]
        public object? Second { get; set; }

        /// <summary><para type="description">Stacks the panes top and bottom with a horizontal divider.</para></summary>
        [Parameter] public SwitchParameter Vertical { get; set; }

        /// <summary><para type="description">The first pane's share of the space, from 0.05 to 0.95.</para></summary>
        [Parameter, ValidateRange(0.05, 0.95)] public double Ratio { get; set; } = 0.5;

        /// <summary><para type="description">Unique key in the application's state; the value is the first pane's share, from 0.05 to 0.95.</para></summary>
        [Parameter] public string? Key { get; set; }

        /// <summary><para type="description">Share of the parent layout.</para></summary>
        [Parameter] public double Weight { get; set; }

        /// <summary><para type="description">Fixed height in cells.</para></summary>
        [Parameter] public int Height { get; set; }

        /// <summary><para type="description">Use the available height.</para></summary>
        [Parameter] public SwitchParameter Fill { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Splitter splitter = new Splitter(Pane(First), Pane(Second)) { Vertical = Vertical.IsPresent, Ratio = Ratio, Key = Key };
            WriteObject(PSWidgets.WithSize(splitter, Weight, Height, Fill.IsPresent));
        }

        private IWidget Pane(object? content) {
            List<IWidget> children = PSWidgets.CollectAny(content, this);
            return children.Count == 1 ? children[0] : new Rows(children.ToArray());
        }
    }
}
