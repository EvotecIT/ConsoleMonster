using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Aligns console widgets left, centre or right, and top, middle or bottom, inside their space.</para>
    /// <para type="description">The content keeps its measured size and is placed within the space the parent gives it. Horizontal alignment uses the full width; vertical alignment applies when the parent gives more rows than the content needs (-Height or -Fill). Widgets that already use the full width, such as progress bars, only move vertically.</para>
    /// <example>
    ///   <para>A centred logo above a dashboard</para>
    ///   <code>New-ConsoleAlign -Horizontal Center { New-ConsoleLogo -Text 'TESTIMOX' }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleAlign")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleAlign : PSCmdlet {
        /// <summary><para type="description">Scriptblock that emits the content (several widgets are stacked vertically).</para></summary>
        [Parameter(Position = 0)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">A widget to align instead of a scriptblock.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public IWidget? Widget { get; set; }

        /// <summary><para type="description">Horizontal placement: Left, Center (default) or Right.</para></summary>
        [Parameter]
        public HorizontalAlignment Horizontal { get; set; } = HorizontalAlignment.Center;

        /// <summary><para type="description">Vertical placement: Top (default), Middle or Bottom.</para></summary>
        [Parameter]
        public VerticalAlignment Vertical { get; set; } = VerticalAlignment.Top;

        /// <summary><para type="description">Share of the width (in columns) or height (in a live dashboard) this widget takes.</para></summary>
        [Parameter]
        public double Weight { get; set; }

        /// <summary><para type="description">Fixed height in rows.</para></summary>
        [Parameter]
        public int Height { get; set; }

        /// <summary><para type="description">Take the remaining height of a live dashboard.</para></summary>
        [Parameter]
        public SwitchParameter Fill { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            List<IWidget> children = PSWidgets.Collect(Content);
            if (Widget != null) {
                children.Add(Widget);
            }
            IWidget content = children.Count == 1 ? children[0] : new Rows(children.ToArray());
            WriteObject(PSWidgets.WithSize(new Align(content, Horizontal, Vertical), Weight, Height, Fill.IsPresent));
        }
    }
}
