using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes pipeline objects as a console table.</para>
    /// <para type="description">Same parameters as New-ConsoleTable; the table is written immediately in the detected output mode (live terminal, redirected stream or plain ASCII).</para>
    /// </summary>
    /// <example>
    ///   <para>Top processes by memory</para>
    ///   <code>Get-Process | Sort-Object WS -Descending | Select-Object -First 10 Name, Id, @{ n = 'MB'; e = { [math]::Round($_.WS / 1MB) } } | Write-ConsoleTable -Title 'Top processes'</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Write, "ConsoleTable")]
    public sealed class CmdletWriteConsoleTable : CmdletNewConsoleTable {
        /// <inheritdoc />
        protected override void Emit(Table table) => HostOutput.Write(this, table);
    }
}
