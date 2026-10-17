using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a checklist of items with status markers.</para>
    /// <para type="description">-Data maps item labels to statuses (pass, fail, warn, skip, running, pending, info). Status is always shown with a symbol (or a [PASS]-style word in ASCII mode), never by colour alone. Update items live with Update-ConsoleStatus.</para>
    /// <example>
    ///   <para>Mail security checks</para>
    ///   <code>New-ConsoleStatusList -Data ([ordered] @{ 'SPF valid' = 'pass'; 'DMARC enforced' = 'pass'; 'MTA-STS' = 'warn' }) | Write-ConsoleWidget</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleStatusList")]
    [OutputType(typeof(StatusList))]
    public sealed class CmdletNewConsoleStatusList : PSCmdlet {
        /// <summary><para type="description">Labels mapped to statuses.</para></summary>
        [Parameter(Position = 0)]
        public IDictionary? Data { get; set; }

        /// <summary><para type="description">Item keys to add as pending (for a live checklist).</para></summary>
        [Parameter]
        public object[]? Item { get; set; }

        /// <summary><para type="description">The live name (default status).</para></summary>
        [Parameter]
        public string Name { get; set; } = "status";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            StatusList list = new StatusList(Name);
            if (Item != null) {
                foreach (object item in Item) {
                    list.Set(PSText.PlainText(item), PSText.LiteralMarkup(item), StatusKind.Pending);
                }
            }
            if (Data != null) {
                foreach (DictionaryEntry entry in Data) {
                    string label = PSWidgets.ToText(entry.Key);
                    StatusKinds.TryParse(PSWidgets.ToText(entry.Value), out StatusKind status);
                    list.Set(label, Markup.Escape(label), status);
                }
            }
            WriteObject(list);
        }
    }
}
