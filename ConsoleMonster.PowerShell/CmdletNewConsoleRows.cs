using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>Stacks console widgets vertically.</summary>
    /// <remarks><para>Children emitted by the scriptblock are placed top to bottom. In a live dashboard, children created with -Fill or -Weight share the remaining height.</para></remarks>
    /// <example>
    ///   <para>Two widgets stacked with a blank row between them</para>
    ///   <code>$rowsOptions = @{
    ///     Gap = 1
    ///     Content = { New-ConsoleRule -Title 'Summary'; New-ConsoleKeyValue -InputObject @{ Total = 12 } }
    /// }
    /// New-ConsoleRows @rowsOptions</code>
    /// </example>
    /// <example>
    /// <para>Stack an inventory heading and result</para>
    /// <code>$rowsOptions = @{
    ///     Gap = 1
    ///     Content = {
    ///         $textOptions2 = @{
    ///             Text = @('Inventory')
    ///             Decoration = @('Bold')
    ///         }
    ///         New-ConsoleText @textOptions2
    ///         $textOptions = @{
    ///             Text = @('web01 and web02 are online')
    ///             Role = 'Pass'
    ///         }
    ///         New-ConsoleText @textOptions
    ///     }
    /// }
    /// New-ConsoleRows @rowsOptions | Show-Console</code>
    /// </example>
    /// <example>
    /// <para>Read any of these gallery scripts to see New-ConsoleRows in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>Get-ConsoleDemo -Slug 'live-filter' | Select-Object -ExpandProperty Source
    /// Get-ConsoleDemo -Slug 'task-menu' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleRows", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolerows/")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleRows : ContainerCmdlet {
        /// <summary><para type="description">Scriptblock that emits the child widgets.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [AllowNull, AllowEmptyCollection]
        public override object? Content { get; set; }

        /// <summary><para type="description">Blank rows between children.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "0")]
        public int Gap { get; set; }

        /// <summary><para type="description">Share of the space this group takes inside its parent.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "0")]
        public double Weight { get; set; }

        /// <summary><para type="description">Take the remaining height of a live dashboard.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Fill { get; set; }

        /// <inheritdoc />
        protected override IWidget Build(List<IWidget> children) {
            Rows rows = new Rows(children.ToArray()) { Gap = Gap };
            return PSWidgets.WithSize(rows, Weight, 0, Fill.IsPresent);
        }
    }
}
