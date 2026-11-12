using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates an accordion for Show-ConsoleApp: collapsible sections that keep one open.</summary>
    /// <remarks><para>-Content emits New-ConsoleCollapsible sections. Opening one folds the others; while a header has focus, + expands every section for reading and - folds them all. Each section keeps its own key and state.</para></remarks>
    /// <example><para>Fold long sections of a help page and keep one open at a time.</para><code>Show-ConsoleApp -Content {
    ///     New-ConsoleAccordion -Content {
    ///         New-ConsoleCollapsible -Key Synopsis -Expanded -Content { 'Gets the processes running on the computer.' }
    ///         New-ConsoleCollapsible -Key Examples -Summary '(2)' -Content {
    ///             'Get-Process -Name pwsh'
    ///             'Get-Process | Sort-Object CPU -Descending | Select-Object -First 5'
    ///         }
    ///     }
    /// }</code></example>
    /// <example>
    /// <para>Compose an inventory accordion</para>
    /// <code>New-ConsoleAccordion -Content {
    ///     New-ConsoleCollapsible -Key network -Label Network -Expanded -Content { New-ConsoleText 'DNS reachable' -Role Pass }
    /// } | Show-Console
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleAccordion", HelpUri = "https://consolemonster.dev/powershell/commands/new-consoleaccordion/")]
    [OutputType(typeof(Accordion))]
    public sealed class CmdletNewConsoleAccordion : NoPipelineInputCmdlet {
        /// <summary><para type="description">The sections of the group: New-ConsoleCollapsible output, or a script block that emits it.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        [AllowNull, AllowEmptyCollection]
        public object? Content { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            List<IWidget> children = PSWidgets.CollectAny(Content, this);
            Collapsible[] sections = children.OfType<Collapsible>().ToArray();
            if (sections.Length == 0 || sections.Length != children.Count) {
                throw new PSArgumentException("New-ConsoleAccordion needs -Content that emits only New-ConsoleCollapsible sections.");
            }
            WriteObject(new Accordion(sections));
        }
    }
}
