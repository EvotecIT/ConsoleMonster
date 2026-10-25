using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes a horizontal rule, optionally with a title.</para>
    /// <para type="description">A quick section separator for scripts.</para>
    /// </summary>
    /// <example>
    ///   <para>A section header</para>
    ///   <code>Show-ConsoleRule 'Step 2: collect'</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleRule")]
    public sealed class CmdletShowConsoleRule : PSCmdlet {
        /// <summary><para type="description">The title (markup allowed).</para></summary>
        [Parameter(Position = 0)]
        public object? Title { get; set; }

        /// <summary><para type="description">The width in cells (default the window), to match other output laid out with -Width.</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Width { get; set; }

        /// <summary><para type="description">Alignment of the section title (default left, as on the rule builder).</para></summary>
        [Parameter] public Layout.HorizontalAlignment Alignment { get; set; } = Layout.HorizontalAlignment.Left;
        /// <summary><para type="description">Returns plain text without drawing.</para></summary>
        [Parameter] public SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Returns the rule widget without drawing.</para></summary>
        [Parameter] public SwitchParameter AsWidget { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() {
            PSOutputExport.Validate(this);
            Rule rule = new Rule(PSText.Markup(Title)) { Alignment = Alignment };
            if (AsWidget.IsPresent) { WriteObject(rule); return; }
            PSFormatting.Emit(this, rule, Width, null, AsString.IsPresent, appendBlankLine: false);
        }
    }
}
