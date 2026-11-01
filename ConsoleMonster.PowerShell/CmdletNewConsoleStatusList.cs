using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a checklist of items with status markers.</para>
    /// <para type="description">Content or the pipeline accepts Key/Label/Status records and strings. Records can include Detail; strings begin as pending checklist items. InputObject also accepts a dictionary mapping labels to statuses. Layout selects a vertical List, a horizontal Strip of phases, or a Chain: the phases on one line that folds to fit, for a table cell or a header. In a Strip, records with ParentKey are items of that phase; the running phase fans out to them, prioritizing failed, warning and running items within FanOut rows and counting the remaining items in +N more. The summary uses one row, so urgent items can also be folded when they exceed the limit. Statuses use the shared vocabulary: Pass, Fail, Warn, Skip, Info, Running, Pending and PowerShell's words such as Success, Warning and Error. Status is always shown with a symbol (or a [PASS]-style word in ASCII mode), never by colour alone. In a live dashboard, update items with Update-Console.</para>
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
    ///   <code>$checks = [pscustomobject]@{ Name = 'Build'; Result = 'Pass'; Duration = '2 s' }; $checks | ForEach-Object { [pscustomobject]@{ Label = $_.Name; Status = $_.Result; Detail = $_.Duration } } | New-ConsoleStatusList | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>A live checklist: items start pending and are updated from the dashboard script</para>
    ///   <code>
    /// $steps = 'Scan files', 'Copy changes', 'Verify' | New-ConsoleStatusList -WidgetId status
    /// Invoke-ConsoleDashboard (New-ConsoleDashboard -Title 'Backup' -Content { $steps }) { Update-Console status -Key 'Scan files' -Status Pass -Detail '2.4 s' }</code>
    /// </example>
    /// <example>
    /// <para>Show build phases as a compact strip</para>
    /// <code>New-ConsoleStatusList -Layout Strip -StepCounter -Content {
    ///     @{ Key = 'restore'; Label = 'Restore'; Status = 'Pass' }
    ///     @{ Key = 'build'; Label = 'Build'; Status = 'Running' }
    ///     @{ Key = 'test'; Label = 'Test'; Status = 'Pending' }
    /// } | Show-Console</code>
    /// </example>
    /// <example>
    /// <para>The running phase fans out to the users it is migrating</para>
    /// <code>New-ConsoleStatusList -Layout Strip -FanOut 4 -Content {
    ///     @{ Key = 'snapshot'; Label = 'Snapshot'; Status = 'Pass'; Detail = '12 s' }
    ///     @{ Key = 'migrate'; Label = 'Migrate'; Status = 'Running'; Detail = '6/14' }
    ///     @{ Key = 'verify'; Label = 'Verify' }
    ///     'alice', 'bob', 'carol', 'dave', 'erin' | ForEach-Object { @{ ParentKey = 'migrate'; Key = $_; Status = 'Pass' } }
    ///     @{ ParentKey = 'migrate'; Key = 'frank'; Status = 'Running'; Detail = '40%' }
    ///     @{ ParentKey = 'migrate'; Key = 'mallory'; Status = 'Fail'; Detail = 'access denied' }
    /// } | Show-Console</code>
    /// </example>
    /// <example>
    /// <para>Pipeline stages as one line in a table column</para>
    /// <code>$stages = { param($Build) New-ConsoleStatusList -Layout Chain -Content {
    ///     @{ Key = 'fetch'; Label = 'Fetch'; Status = 'Pass' }
    ///     @{ Key = 'build'; Label = 'Build'; Status = $Build }
    ///     @{ Key = 'test'; Label = 'Test' }
    /// } }
    /// @(
    ///     [pscustomobject]@{ Repository = 'ConsoleMonster'; Stages = &amp; $stages 'Running' }
    ///     [pscustomobject]@{ Repository = 'PSPublishModule'; Stages = &amp; $stages 'Fail' }
    /// ) | Show-ConsoleTable</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleStatusList")]
    [OutputType(typeof(StatusList), typeof(Phases))]
    public sealed class CmdletNewConsoleStatusList : PSCmdlet {
        private readonly List<object> _piped = new List<object>();
        private readonly List<IDictionary> _dictionaries = new List<IDictionary>();


        /// <summary><para type="description">A script block that emits the items: Key/Label/Status records calls or hashtables with Label, Status and Detail.</para></summary>
        [Parameter(Position = 2)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Items from the pipeline: Key/Label/Status records objects or hashtables with Label, Status and Detail.</para></summary>
        [Parameter(ValueFromPipeline = true, Position = 1)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">List stacks the items; Strip shows ordered phases; Chain shows the phases on one line that folds to fit.</para></summary>
        [Parameter, ValidateSet("List", "Strip", "Chain")]
        public string Layout { get; set; } = "List";

        /// <summary><para type="description">Shows Strip phases as a numbered ledger. ParentKey items require Strip without StepCounter.</para></summary>
        [Parameter] public SwitchParameter StepCounter { get; set; }

        /// <summary><para type="description">Rows the running phase of a Strip fans out to, including the +N more row (default 5; 0 hides items).</para></summary>
        [Parameter, ValidateRange(0, 100)] public int FanOut { get; set; } = 5;

        /// <summary><para type="description">The live name (default status).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "status";

        /// <summary><para type="description">Shared DetailColumn layout settings as a hashtable, for example @{ MinWidth = 6; Alignment = 'Right' }.</para></summary>
        [Parameter] public Layout.ColumnFormat? DetailColumn { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                object? raw = PSText.Unwrap(InputObject);
                if (raw is IDictionary dictionary && !PSItems.HasField(dictionary, "Label") && !PSItems.HasField(dictionary, "Key") && !PSItems.HasField(dictionary, "ParentKey")) { _dictionaries.Add(dictionary); }
                else if (raw is ScriptBlock script) {
                    if (Content != null) { throw new PSArgumentException("Give the builder script through Content only once."); }
                    Content = script;
                } else { _piped.AddRange(PSItems.Items(InputObject)); }
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            bool chain = string.Equals(Layout, "Chain", System.StringComparison.OrdinalIgnoreCase);
            bool strip = chain || string.Equals(Layout, "Strip", System.StringComparison.OrdinalIgnoreCase);
            if ((!strip || chain) && StepCounter.IsPresent) { throw new PSArgumentException("StepCounter applies to Layout Strip."); }
            if ((!strip || chain) && MyInvocation.BoundParameters.ContainsKey(nameof(FanOut))) { throw new PSArgumentException("FanOut applies to Layout Strip."); }
            if (strip && DetailColumn != null) { throw new PSArgumentException("DetailColumn applies to Layout List."); }
            ConsoleMonster.Layout.IWidget list = strip ? (ConsoleMonster.Layout.IWidget)new Phases(WidgetId) { StepCounter = StepCounter.IsPresent, Compact = chain, FanOut = FanOut } : new StatusList(WidgetId);
            foreach (IDictionary dictionary in _dictionaries) {
                foreach (DictionaryEntry entry in dictionary) {
                    string label = PSWidgets.ToText(entry.Key);
                    if (!Binding.StatusArguments.TryParse(entry.Value, out StatusKind status)) {
                        throw new PSArgumentException("The value for '" + label + "' is not a status.");
                    }
                    PSItems.AddAll(this, list, new object[] { new StatusItem(label, Text.Markup.Escape(label), status) });
                }
            }
            ScriptBlock? content = Content;
            PSItems.AddAll(this, list, PSItems.Items(content));
            PSItems.AddAll(this, list, _piped);
            if (strip && StepCounter.IsPresent && ((Phases)list).HasItems) { throw new PSArgumentException("ParentKey items require Layout Strip without StepCounter."); }
            if (chain && ((Phases)list).HasItems) { throw new PSArgumentException("Items with ParentKey apply to Layout Strip."); }
            if (DetailColumn != null && list is StatusList checklist) { checklist.DetailColumn = DetailColumn; }
            WriteObject(list);
        }
    }
}
