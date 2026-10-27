using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a checklist of items with status markers.</para>
    /// <para type="description">Items come from a script block of Key/Label/Status records calls (each with its own status and detail), from the pipeline, from -InputObject (labels mapped to statuses) or from -Item (labels added as pending, for a live checklist). Statuses use the shared vocabulary: Pass, Fail, Warn, Skip, Info, Running, Pending and PowerShell's words such as Success, Warning and Error. Status is always shown with a symbol (or a [PASS]-style word in ASCII mode), never by colour alone. To add items later, pipe the list to Add-ConsoleItem; in a live dashboard, update items with Update-Console.</para>
    /// </summary>
    /// <example>
    ///   <para>A build checklist</para>
    ///   <code>
    /// New-ConsoleStatusList -Content {
    ///     [pscustomobject]@{ Label = 'Restore NuGet packages'; Status = 'Pass'; Detail = '1.6 s' }
    ///     [pscustomobject]@{ Label = 'Compile ConsoleMonster'; Status = 'Pass'; Detail = '4.7 s' }
    ///     [pscustomobject]@{ Label = 'Run Pester on PowerShell 7'; Status = 'Warn'; Detail = '1 skipped' }
    ///     [pscustomobject]@{ Label = 'Pack ConsoleMonster.2.0.0.nupkg'; Status = 'Running' }
    /// } | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Mail security checks from a hashtable</para>
    ///   <code>New-ConsoleStatusList -InputObject ([ordered] @{ 'SPF valid' = 'pass'; 'DMARC enforced' = 'pass'; 'MTA-STS' = 'warn' }) | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Items from objects in the pipeline</para>
    ///   <code>$checks | ForEach-Object { [pscustomobject]@{ Label = $_.Name; Status = $_.Result; Detail = $_.Duration } } | New-ConsoleStatusList | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>A live checklist: items start pending and are updated from the dashboard script</para>
    ///   <code>
    /// $steps = New-ConsoleStatusList -Item 'Scan files', 'Copy changes', 'Verify'
    /// Invoke-ConsoleDashboard (New-ConsoleDashboard -Title 'Backup' -Content { $steps }) { Update-Console status -Key 'Scan files' -Status Pass -Detail '2.4 s' }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleStatusList")]
    [OutputType(typeof(StatusList))]
    public sealed class CmdletNewConsoleStatusList : PSCmdlet {
        private readonly List<object> _piped = new List<object>();
        private readonly List<IDictionary> _dictionaries = new List<IDictionary>();


        /// <summary><para type="description">A script block that emits the items: Key/Label/Status records calls or hashtables with Label, Status and Detail.</para></summary>
        [Parameter(Position = 2)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Items from the pipeline: Key/Label/Status records objects or hashtables with Label, Status and Detail.</para></summary>
        [Parameter(ValueFromPipeline = true, Position = 1)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Item keys to add as pending (for a live checklist).</para></summary>
        [Parameter]
        public object[]? Item { get; set; }

        /// <summary><para type="description">The live name (default status).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "status";

        /// <summary><para type="description">Shared DetailColumn layout settings from New-ConsoleColumnFormat.</para></summary>
        [Parameter] public Layout.ColumnFormat? DetailColumn { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                object? raw = PSText.Unwrap(InputObject);
                if (raw is IDictionary dictionary && !dictionary.Contains("Label")) { _dictionaries.Add(dictionary); }
                else if (raw is ScriptBlock script) {
                    if (Content != null) { throw new PSArgumentException("Give the builder script through Content only once."); }
                    Content = script;
                } else { _piped.AddRange(PSItems.Items(InputObject)); }
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            StatusList list = new StatusList(WidgetId);
            if (Item != null) {
                foreach (object item in Item) {
                    list.Set(PSText.PlainText(item), PSText.LiteralMarkup(item), StatusKind.Pending);
                }
            }
            foreach (IDictionary dictionary in _dictionaries) {
                foreach (DictionaryEntry entry in dictionary) {
                    string label = PSWidgets.ToText(entry.Key);
                    if (!Binding.StatusArguments.TryParse(entry.Value, out StatusKind status)) {
                        throw new PSArgumentException("The value for '" + label + "' is not a status.");
                    }
                    list.Set(label, Text.Markup.Escape(label), status);
                }
            }
            ScriptBlock? content = Content;
            PSItems.AddAll(this, list, PSItems.Items(content));
            PSItems.AddAll(this, list, _piped);
            if (DetailColumn != null) { list.DetailColumn = DetailColumn; }
            WriteObject(list);
        }
    }
}
