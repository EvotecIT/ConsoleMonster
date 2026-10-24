using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a bordered panel with a title around other console widgets.</para>
    /// <para type="description">-Content is a scriptblock, a widget, text or any objects (objects are shown as a record or a table; several pieces are stacked vertically). Borders follow the detected glyph tier: rounded on modern fonts, single lines on Consolas, +-| in ASCII mode. Inside New-ConsoleColumns, panels share the row height.</para>
    /// </summary>
    /// <example>
    ///   <para>A panel holding a lanes widget, taking two shares of a row</para>
    ///   <code>New-ConsolePanel -Title 'Targets' -Weight 2 -Content { New-ConsoleLanes -WidgetId targets }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsolePanel")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsolePanel : PSCmdlet {
        /// <summary><para type="description">The panel content: a scriptblock that emits widgets, text or objects, a widget, a string, or objects (one object is shown as labelled fields, several as a table, through the same rules as Show-ConsoleList and Show-ConsoleTable).</para></summary>
        [Parameter(Position = 0)]
        public object? Content { get; set; }

        /// <summary><para type="description">A widget to use as content instead of a scriptblock.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public IWidget? Widget { get; set; }

        /// <summary><para type="description">The title shown in the top border (Spectre-style markup allowed).</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">Text on the right of the top border, for example a count.</para></summary>
        [Parameter]
        public object? HeaderRight { get; set; }

        /// <summary><para type="description">Text on the right of the bottom border.</para></summary>
        [Parameter]
        public object? Caption { get; set; }

        /// <summary><para type="description">Border style: Rounded, Single, Double, Heavy, Ascii or None.</para></summary>
        [Parameter]
        public BoxStyle Border { get; set; } = BoxStyle.Rounded;

        /// <summary><para type="description">Share of the width (in columns) or height (in a live dashboard) this panel takes.</para></summary>
        [Parameter]
        public double Weight { get; set; }

        /// <summary><para type="description">Fixed height in rows, including the border.</para></summary>
        [Parameter]
        public int Height { get; set; }

        /// <summary><para type="description">Take the remaining height of a live dashboard (for example a log).</para></summary>
        [Parameter]
        public SwitchParameter Fill { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            List<IWidget> children = PSWidgets.CollectAny(Content, this);
            if (Widget != null) {
                children.Add(Widget);
            }
            IWidget content = children.Count == 1 ? children[0] : new Rows(children.ToArray());
            Panel panel = new Panel(content, PSText.Markup(Title)) { HeaderRight = PSText.Markup(HeaderRight), Caption = PSText.Markup(Caption), Border = Border };
            WriteObject(PSWidgets.WithSize(panel, Weight, Height, Fill.IsPresent));
        }
    }
}
