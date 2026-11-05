using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a named, embeddable data grid for Show-ConsoleApp.</para>
    /// <para type="description">The listing retains original objects and stable row identities. Each grid has its own cursor, marks and prefix search. Rows and lazy cells are rendered through the shared DataGrid control. -InputObject lists ordinary objects with the same columns as Show-ConsoleGrid (-Property picks them); -Grid embeds a listing prepared in .NET.</para>
    /// </summary>
    /// <example>
    /// <para>Embed an empty listing in an application</para>
    /// <code>Show-ConsoleApp -Mode Snapshot -Content { New-ConsoleDataGrid -Key Inventory -Grid 'Inventory' -Height 18 }</code>
    /// </example>
    /// <example>
    /// <para>List services with three columns</para>
    /// <code>Show-ConsoleApp -Content { New-ConsoleDataGrid -Key Services -InputObject (Get-Service) -Property Name, Status, StartType -Height 12 }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleDataGrid", DefaultParameterSetName = GridSet)]
    [OutputType(typeof(DataGrid))]
    public sealed class CmdletNewConsoleDataGrid : AppInputCmdlet {
        private const string GridSet = "Grid";
        private const string ObjectSet = "Objects";

        /// <summary><para type="description">The listing's columns, original objects and row identities.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = GridSet)] public Grid Grid { get; set; } = null!;
        /// <summary><para type="description">The objects to list, one row each; the row is the object a selection returns.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = ObjectSet), AllowEmptyCollection] public PSObject[] InputObject { get; set; } = null!;
        /// <summary><para type="description">Columns: names, wildcards and calculated properties with the Show-ConsoleTable column options.</para></summary>
        [Parameter(ParameterSetName = ObjectSet)] public object[]? Property { get; set; }
        /// <summary><para type="description">Natural content height; the terminal viewport can constrain it further.</para></summary>
        [Parameter, ValidateRange(1, 10000)] public int Height { get; set; } = 20;
        /// <summary><para type="description">Leaves printable keys to the app's command registry.</para></summary>
        [Parameter] public SwitchParameter NoQuickSearch { get; set; }
        /// <summary><para type="description">The state key shared with linked controls (New-ConsoleChartView, New-ConsoleSelect, another grid): the selected row's position in the listing.</para></summary>
        [Parameter] public string? SelectionKey { get; set; }
        /// <inheritdoc />
        protected override void EndProcessing() {
            Grid grid = Grid;
            if (ParameterSetName == ObjectSet) {
                grid = new Grid(Label ?? Key);
                PSGridListing.Build(InputObject.Where(item => item != null), Property, null, this, grid, out _);
            }
            Emit(new DataGridInputOptions { Key = Key, Grid = grid, Height = Height, QuickSearch = !NoQuickSearch.IsPresent, SelectionKey = SelectionKey }.Build());
        }
    }
}
