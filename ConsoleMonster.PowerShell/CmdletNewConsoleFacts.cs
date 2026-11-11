using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a facts line with bold counts, semantic colours and shared separators.</para>
    /// <para type="description">Each fact is a value or a record with Value, Label, Caption, Presenter, Role and NewLine. Counts are bold, status words use the same colour, and neutral facts are muted. Facts wrap between items. ASCII uses | instead of the middle dot.</para>
    /// </summary>
    /// <example>
    /// <para>A report summary</para>
    /// <code>New-ConsoleFacts -Content { @{ Value = 3; Label = 'passed'; Role = 'Pass' }; @{ Value = 1; Label = 'warning'; Role = 'Warn' }; @{ Value = 5; Label = 'checks' } } | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleFacts")]
    [OutputType(typeof(ViewFacts))]
    public sealed class CmdletNewConsoleFacts : PSCmdlet {
        private readonly List<object> _items = new List<object>();
        /// <summary><para type="description">Values or fact records, in display order.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }
        /// <summary><para type="description">A script block emitting values or records with Value, Label and an optional Role, Caption, Presenter and NewLine.</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) _items.AddRange(PSItems.Items(InputObject));
        }
        /// <inheritdoc />
        protected override void EndProcessing() {
            ViewFacts facts = new ViewFacts();
            _items.AddRange(PSItems.Items(Content));
            foreach (object item in _items) {
                ViewFact? fact = ViewSummary.Fact(item);
                if (fact != null) facts.Facts.Add(fact);
            }
            WriteObject(facts);
        }
    }
}
