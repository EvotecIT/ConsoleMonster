using System.Globalization;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Displays pipeline objects as a tree.</para>
    /// <para type="description">Compatibility command for scripts using Format-ConsoleTree. It uses the same tree display as Show-Console -View Tree.</para>
    /// </summary>
    /// <example><para>Inspect services</para><code>Get-Service | Format-ConsoleTree -Depth 1</code></example>
    [Cmdlet(VerbsCommon.Format, "ConsoleTree")]
    public sealed class CmdletFormatConsoleTree : ConsoleDisplayCmdlet {
        /// <summary><para type="description">Objects to display.</para></summary>
        [Parameter(ValueFromPipeline = true)] public override PSObject? InputObject { get; set; }
        /// <summary><para type="description">Maximum depth below the root.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] public override int Depth { get; set; } = 3;
        /// <summary><para type="description">Maximum collection items.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)] public override int MaxItems { get; set; } = 25;
        /// <summary><para type="description">Maximum inspection nodes.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)] public override int MaxNodes { get; set; } = 5000;
        /// <summary><para type="description">Member names to include.</para></summary>
        [Parameter] public override string[]? Include { get; set; }
        /// <summary><para type="description">Member names or patterns; use * to show all members.</para></summary>
        [Parameter] public override object[]? Property { get; set; }
        /// <summary><para type="description">Member names to exclude.</para></summary>
        [Parameter] public override string[]? Exclude { get; set; }
        /// <summary><para type="description">Root title.</para></summary>
        [Parameter] public override object? Title { get; set; }
        /// <summary><para type="description">Display the inspector instead of a tree.</para></summary>
        [Parameter] public SwitchParameter AsInspector { get; set; }
        /// <summary><para type="description">Output width.</para></summary>
        [Parameter, ValidateRange(0, 1000)] public override int Width { get; set; }
        /// <summary><para type="description">Culture used for values.</para></summary>
        [Parameter] public override CultureInfo? Culture { get; set; }
        /// <summary><para type="description">Return input objects after display.</para></summary>
        [Parameter] public override SwitchParameter PassThru { get; set; }
        /// <summary><para type="description">Return the rendered text.</para></summary>
        [Parameter] public override SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Return the constructed widget.</para></summary>
        [Parameter] public override SwitchParameter AsWidget { get; set; }
        /// <inheritdoc />
        protected override string RequestedView => AsInspector.IsPresent ? "inspector" : "tree";
    }
}
