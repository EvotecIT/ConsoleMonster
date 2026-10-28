using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a horizontal rule, optionally with a title.</para>
    /// <para type="description">The line uses the border colour and the glyph tier's line character.</para>
    /// </summary>
    /// <example>
    ///   <para>A titled separator</para>
    ///   <code>New-ConsoleRule -Title 'Results'</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleRule")]
    [OutputType(typeof(Rule))]
    public sealed class CmdletNewConsoleRule : NoPipelineInputCmdlet {
        /// <summary><para type="description">The title (markup allowed).</para></summary>
        [Parameter(Position = 0)]
        public object? Title { get; set; }

        /// <summary><para type="description">Title alignment: Left, Center or Right.</para></summary>
        [Parameter]
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <summary><para type="description">Theme role for the line; default Border.</para></summary>
        [Parameter] public Styling.ThemeToken LineRole { get; set; } = Styling.ThemeToken.Border;
        /// <summary><para type="description">Optional line glyph style; omitted follows the active theme.</para></summary>
        [Parameter] public Glyphs.BoxStyle? LineStyle { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            WriteObject(new Rule(PSText.Markup(Title)) { Alignment = Alignment, LineRole = LineRole, LineStyle = LineStyle });
        }
    }
}
