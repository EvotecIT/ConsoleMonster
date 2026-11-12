using System;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates an embeddable, lazy object tree for Show-ConsoleApp.</summary>
    /// <remarks><para>Uses the same member adapters, paths, cycle guards and expansion budget as Show-ConsoleObject. Arrows expand and collapse; Enter drills into the selected subtree.</para></remarks>
    /// <example>
    /// <para>Browse a nested configuration</para>
    /// <code>$config = [pscustomobject]@{ Name = 'example'; Settings = @{ Port = 443; Enabled = $true } }
    /// Show-ConsoleApp -Content { New-ConsoleTreeSelect -Key Members -InputObject $config }</code>
    /// </example>
    /// <example>
    /// <para>Compose an inventory tree select</para>
    /// <code>$inventory = [pscustomobject]@{ Site = 'Warsaw'; Server = [pscustomobject]@{ Name = 'web01'; State = 'Online' } }
    /// New-ConsoleTreeSelect -Key inventory -InputObject $inventory -Depth 3 | Show-Console
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleTreeSelect", HelpUri = "https://consolemonster.dev/powershell/commands/new-consoletreeselect/")]
    [OutputType(typeof(TreeSelect))]
    public sealed class CmdletNewConsoleTreeSelect : AppInputCmdlet {
        /// <summary><para type="description">The object to browse.</para></summary>
        [Parameter(Mandatory = true), AllowNull] public PSObject? InputObject { get; set; }
        /// <summary><para type="description">Maximum expansion depth.</para></summary>
        [Parameter, ValidateRange(0, 64)] [PSDefaultValue(Help = "8")]
        public int Depth { get; set; } = 8;
        /// <summary><para type="description">Maximum items per collection.</para></summary>
        [Parameter, ValidateRange(1, 10000)] [PSDefaultValue(Help = "100")]
        public int MaxItems { get; set; } = 100;
        /// <summary><para type="description">Maximum nodes in the shared expansion budget.</para></summary>
        [Parameter, ValidateRange(1, 100000)] [PSDefaultValue(Help = "5000")]
        public int MaxNodes { get; set; } = 5000;
        /// <summary><para type="description">Root expression displayed in selected paths.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$x")]
        public string RootPath { get; set; } = "$x";
        /// <summary><para type="description">Natural visible content rows.</para></summary>
        [Parameter, ValidateRange(1, 10000)] [PSDefaultValue(Help = "20")]
        public int Height { get; set; } = 20;
        /// <inheritdoc />
        protected override void EndProcessing() {
            var options = PSFormatting.InspectorOptions(Depth, MaxItems, null, null, MaxNodes, InputObject);
            Emit(new TreeSelect(Key, new ObjectExplorer(InputObject, options, RootPath)) { Height = Height });
        }
    }
}
