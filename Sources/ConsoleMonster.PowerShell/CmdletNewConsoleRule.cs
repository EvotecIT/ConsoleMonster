using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a horizontal rule, optionally with a title.</para>
    /// <para type="description">The line uses the border colour and the glyph tier's line character.</para>
    /// <example>
    ///   <para>A titled separator</para>
    ///   <code>New-ConsoleRule -Title 'Results'</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleRule")]
    [OutputType(typeof(Rule))]
    public sealed class CmdletNewConsoleRule : PSCmdlet {
        /// <summary><para type="description">The title (markup allowed).</para></summary>
        [Parameter(Position = 0)]
        public string? Title { get; set; }

        /// <summary><para type="description">Title alignment: Left, Center or Right.</para></summary>
        [Parameter]
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            WriteObject(new Rule(Title) { Alignment = Alignment });
        }
    }
}
