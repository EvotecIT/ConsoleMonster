using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates a horizontal rule, optionally with a title.</summary>
    /// <remarks><para>The line uses the border colour and the glyph tier's line character.</para></remarks>
    /// <example>
    ///   <para>A titled separator</para>
    ///   <code>New-ConsoleRule -Title 'Results'</code>
    /// </example>
    /// <example>
    /// <para>Compose an inventory rule</para>
    /// <code>
    /// New-ConsoleRows -Content {
    ///     $ruleOptions = @{
    ///         Title = 'Inventory'
    ///     }
    ///     New-ConsoleRule @ruleOptions
    ///     $textOptions = @{
    ///         Text = @('2 servers checked')
    ///         Role = 'Pass'
    ///     }
    ///     New-ConsoleText @textOptions
    /// } | Show-Console</code>
    /// </example>
    /// <example>
    /// <para>Read any of these gallery scripts to see New-ConsoleRule in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>
    /// Get-ConsoleDemo -Slug 'backup-runner' | Select-Object -ExpandProperty Source
    /// Get-ConsoleDemo -Slug 'everyday-formatting' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleRule", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolerule/")]
    [OutputType(typeof(Rule))]
    public sealed class CmdletNewConsoleRule : NoPipelineInputCmdlet {
        /// <summary><para type="description">The title (markup allowed).</para></summary>
        [Parameter(Position = 0)]
        public object? Title { get; set; }

        /// <summary><para type="description">Title alignment: Left, Center or Right.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "Left")]
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <summary><para type="description">Theme role for the line; default Border.</para></summary>
        [Parameter] [PSDefaultValue(Help = "Border")]
        public Styling.ThemeToken LineRole { get; set; } = Styling.ThemeToken.Border;
        /// <summary><para type="description">Optional line glyph style; omitted follows the active theme.</para></summary>
        [Parameter] public Glyphs.BoxStyle? LineStyle { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            WriteObject(new Rule(PSText.Markup(Title)) { Alignment = Alignment, LineRole = LineRole, LineStyle = LineStyle });
        }
    }
}
