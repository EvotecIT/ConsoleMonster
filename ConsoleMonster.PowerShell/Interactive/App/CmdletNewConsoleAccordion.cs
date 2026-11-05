using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates an accordion for Show-ConsoleApp: collapsible sections that keep one open.</summary>
    /// <remarks>-Content emits New-ConsoleCollapsible sections. Opening one folds the others; while a header has focus, + expands every section for reading and - folds them all. Each section keeps its own key and state.</remarks>
    /// <example><para>Fold long sections of a help page and keep one open at a time.</para><code>Show-ConsoleApp -Content {
    ///     New-ConsoleAccordion -Content {
    ///         New-ConsoleCollapsible -Key Synopsis -Expanded -Content { 'Gets the services on a computer.' }
    ///         New-ConsoleCollapsible -Key Examples -Summary '(2)' -Content {
    ///             'Get-Service -Name win*'
    ///             'Get-Service | Where-Object Status -eq Running'
    ///         }
    ///     }
    /// }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleAccordion")]
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
