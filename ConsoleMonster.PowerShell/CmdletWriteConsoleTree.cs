using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes a console tree from New-ConsoleTreeNode calls, hierarchical objects or nested hashtables.</para>
    /// <para type="description">Same parameters as New-ConsoleTree (nodes in a script block, hierarchical objects from the pipeline with -ChildrenProperty or -Children, or -Data hashtables); the tree is written immediately instead of returned.</para>
    /// </summary>
    /// <example>
    ///   <para>Write a small hierarchy</para>
    ///   <code>Write-ConsoleTree -Title 'Forest' -Data ([ordered] @{ 'ad.evotec.xyz' = @('DC01', 'DC02'); 'eu.ad.evotec.xyz' = @('DC07') })</code>
    /// </example>
    /// <example>
    ///   <para>Nodes with a status and a detail</para>
    ///   <code>
    /// Write-ConsoleTree 'Replication' {
    ///     New-ConsoleTreeNode 'DC01' -Status Pass -Detail 'last sync 42s ago'
    ///     New-ConsoleTreeNode 'DC02' -Status Warn -Detail 'last sync 42 min ago'
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Write, "ConsoleTree")]
    public sealed class CmdletWriteConsoleTree : CmdletNewConsoleTree {
        /// <inheritdoc />
        protected override void Emit(Tree tree) => HostOutput.Write(this, tree);
    }
}
