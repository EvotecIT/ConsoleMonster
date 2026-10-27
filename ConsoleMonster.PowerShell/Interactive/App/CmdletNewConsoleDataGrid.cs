using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a named, embeddable data grid for Show-ConsoleApp.</para>
    /// <para type="description">The listing retains original objects and stable row identities. Each grid has its own cursor, marks and prefix search. Rows and lazy cells are rendered through the shared DataGrid control.</para>
    /// </summary>
    /// <example><para>Embed an existing listing.</para><code>New-ConsoleDataGrid -Key Left -Grid $listing -Height 18</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleDataGrid")]
    [OutputType(typeof(DataGrid))]
    public sealed class CmdletNewConsoleDataGrid : AppInputCmdlet {
        /// <summary><para type="description">The listing's columns, original objects and row identities.</para></summary>
        [Parameter(Mandatory = true)] public Grid Grid { get; set; } = null!;
        /// <summary><para type="description">Natural content height; the terminal viewport can constrain it further.</para></summary>
        [Parameter, ValidateRange(1, 10000)] public int Height { get; set; } = 20;
        /// <summary><para type="description">Leaves printable keys to the app's command registry.</para></summary>
        [Parameter] public SwitchParameter NoQuickSearch { get; set; }
        /// <inheritdoc />
        protected override void EndProcessing() => Emit(new DataGridInputOptions { Key = Key, Grid = Grid, Height = Height, QuickSearch = !NoQuickSearch.IsPresent }.Build());
    }
}
