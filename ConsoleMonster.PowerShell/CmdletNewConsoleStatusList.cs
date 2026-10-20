using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a checklist of items with status markers.</para>
    /// <para type="description">Items come from a script block of New-ConsoleStatusItem calls (each with its own status and detail), from the pipeline, from -Data (labels mapped to statuses) or from -Item (labels added as pending, for a live checklist). Statuses use the shared vocabulary: Pass, Fail, Warn, Skip, Info, Running, Pending and PowerShell's words such as Success, Warning and Error. Status is always shown with a symbol (or a [PASS]-style word in ASCII mode), never by colour alone. To add items later, pipe the list to Add-ConsoleItem; in a live dashboard, update items with Update-ConsoleStatus.</para>
    /// </summary>
    /// <example>
    ///   <para>A build checklist</para>
    ///   <code>
    /// New-ConsoleStatusList {
    ///     New-ConsoleStatusItem 'Restore NuGet packages' -Status Pass -Detail '1.6 s'
    ///     New-ConsoleStatusItem 'Compile ConsoleMonster' -Status Pass -Detail '4.7 s'
    ///     New-ConsoleStatusItem 'Run Pester on PowerShell 7' -Status Warn -Detail '1 skipped'
    ///     New-ConsoleStatusItem 'Pack ConsoleMonster.2.0.0.nupkg' -Status Running
    /// } | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Mail security checks from a hashtable</para>
    ///   <code>New-ConsoleStatusList ([ordered] @{ 'SPF valid' = 'pass'; 'DMARC enforced' = 'pass'; 'MTA-STS' = 'warn' }) | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Items from objects in the pipeline</para>
    ///   <code>$checks | ForEach-Object { New-ConsoleStatusItem $_.Name -Status $_.Result -Detail $_.Duration } | New-ConsoleStatusList | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>A live checklist: items start pending and are updated from the dashboard script</para>
    ///   <code>
    /// $steps = New-ConsoleStatusList -Item 'Scan files', 'Copy changes', 'Verify'
    /// Start-ConsoleDashboard (New-ConsoleDashboard -Title 'Backup' { $steps }) { Update-ConsoleStatus 'Scan files' -Status Pass -Detail '2.4 s' }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleStatusList")]
    [OutputType(typeof(StatusList))]
    public sealed class CmdletNewConsoleStatusList : PSCmdlet {
        private readonly List<object> _piped = new List<object>();

        /// <summary><para type="description">Labels mapped to statuses (a script block here is taken as -Content).</para></summary>
        [Parameter(Position = 0)]
        public object? Data { get; set; }

        /// <summary><para type="description">A script block that emits the items: New-ConsoleStatusItem calls or hashtables with Label, Status and Detail.</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Items from the pipeline: New-ConsoleStatusItem objects or hashtables with Label, Status and Detail.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Item keys to add as pending (for a live checklist).</para></summary>
        [Parameter]
        public object[]? Item { get; set; }

        /// <summary><para type="description">The live name (default status).</para></summary>
        [Parameter]
        public string Name { get; set; } = "status";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _piped.AddRange(PSItems.Items(InputObject));
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            StatusList list = new StatusList(Name);
            if (Item != null) {
                foreach (object item in Item) {
                    list.Set(PSText.PlainText(item), PSText.LiteralMarkup(item), StatusKind.Pending);
                }
            }
            object? data = PSText.Unwrap(Data);
            ScriptBlock? content = Content;
            if (data is ScriptBlock script) {
                content = script;
            } else if (data is IDictionary dictionary) {
                foreach (DictionaryEntry entry in dictionary) {
                    string label = PSWidgets.ToText(entry.Key);
                    object? state = PSText.Unwrap(entry.Value);
                    StatusKind status;
                    if (state is StatusKind kind) {
                        status = kind;
                    } else if (!StatusKinds.TryParse(PSWidgets.ToText(state), out status)) {
                        ThrowTerminatingError(new ErrorRecord(new PSArgumentException("'" + PSWidgets.ToText(state) + "' (for '" + label + "') is not a status. Use one of: " + string.Join(", ", StatusKinds.Words) + "."), "ConsoleMonsterStatusWord", ErrorCategory.InvalidArgument, state));
                        return;
                    }
                    list.Set(label, Markup.Escape(label), status);
                }
            } else if (data != null) {
                ThrowTerminatingError(new ErrorRecord(new PSArgumentException("-Data takes a hashtable of labels and statuses; give items with a script block of New-ConsoleStatusItem calls."), "ConsoleMonsterStatusData", ErrorCategory.InvalidArgument, data));
            }
            PSItems.AddAll(this, list, PSItems.Items(content));
            PSItems.AddAll(this, list, _piped);
            WriteObject(list);
        }
    }
}
