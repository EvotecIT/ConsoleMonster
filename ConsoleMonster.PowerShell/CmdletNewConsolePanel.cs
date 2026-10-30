using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

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
    public sealed class CmdletNewConsolePanel : ContainerCmdlet {
        /// <summary><para type="description">The panel content: a scriptblock that emits widgets, text or objects, a widget, a string, or objects (one object is shown as labelled fields, several as a table, through the same rules as Show-Console -View List and Show-ConsoleTable).</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [AllowNull, AllowEmptyCollection]
        public override object? Content { get; set; }

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

        /// <summary><para type="description">The semantic border colour.</para></summary>
        [Parameter]
        public ThemeToken BorderRole { get; set; } = ThemeToken.Border;

        /// <summary><para type="description">Cells between the left border and content.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)]
        public int PaddingLeft { get; set; } = 1;

        /// <summary><para type="description">Cells between content and the right border.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)]
        public int PaddingRight { get; set; } = 1;

        /// <summary><para type="description">Rows between the top border and content.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)]
        public int PaddingTop { get; set; } = 0;

        /// <summary><para type="description">Rows between content and the bottom border.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)]
        public int PaddingBottom { get; set; } = 0;

        /// <summary><para type="description">Blank rows between content children when -Content emits several widgets (default 0); 1 spaces a report's sections.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)]
        public int Gap { get; set; }

        /// <summary><para type="description">Fits the panel to content instead of filling its available width.</para></summary>
        [Parameter]
        public SwitchParameter NoExpand { get; set; }

        /// <summary><para type="description">Double-bordered verdict with a centred title.</para></summary>
        [Parameter] public SwitchParameter Verdict { get; set; }
        /// <summary><para type="description">Status colour and title glyph for a verdict.</para></summary>
        [Parameter, Binding.StatusKindTransformation, ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind? Status { get; set; }

        /// <inheritdoc />
        protected override IWidget Build(List<IWidget> children) {
            IWidget content = ContentBuilder.Compose(children.ToArray());
            Panel panel = new Panel(content, PSText.Markup(Title)) { Gap = Gap, HeaderRight = PSText.Markup(HeaderRight), Caption = PSText.Markup(Caption), Border = Border, BorderRole = BorderRole, PaddingLeft = PaddingLeft, PaddingRight = PaddingRight, PaddingTop = PaddingTop, PaddingBottom = PaddingBottom, Expand = !NoExpand.IsPresent, Verdict = Verdict.IsPresent, Status = Status };
            return PSWidgets.WithSize(panel, Weight, Height, Fill.IsPresent);
        }
    }
}
