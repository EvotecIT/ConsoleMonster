using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes nested hashtables as a console tree.</para>
    /// <para type="description">Same parameters as New-ConsoleTree; the tree is written immediately.</para>
    /// <example>
    ///   <para>Write a small hierarchy</para>
    ///   <code>Write-ConsoleTree -Title 'Forest' -Data ([ordered] @{ 'ad.evotec.xyz' = @('DC01', 'DC02'); 'eu.ad.evotec.xyz' = @('DC07') })</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "ConsoleTree")]
    public sealed class CmdletWriteConsoleTree : CmdletNewConsoleTree {
        /// <inheritdoc />
        protected override void Emit(Tree tree) => HostOutput.Write(this, tree);
    }
}
