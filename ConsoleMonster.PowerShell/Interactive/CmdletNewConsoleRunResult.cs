using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates the result a menu run returns: a title, a status, a summary, figures and next actions.</para>
    /// <para type="description">Return it from the script block of $MenuContext.Run (a live dashboard started from a menu action): when the run ends, the menu shows it as a result screen with the figures as large values and the next actions as items. -Figure maps labels to values (use [ordered] to keep order); -Next maps action titles to script blocks, which run with $MenuContext like any menu action. -Status takes the shared status vocabulary (default Pass).</para>
    /// </summary>
    /// <example>
    ///   <para>A backup run that reports what it did</para>
    ///   <code>
    /// New-ConsoleMenuItem 'Back up now' -Action {
    ///     $dashboard = New-ConsoleDashboard -Title 'Backup' { New-ConsoleStatusList -Item 'Scan files', 'Copy changes' }
    ///     $null = $MenuContext.Run($dashboard, {
    ///         Update-ConsoleStatus 'Scan files' -Status Pass
    ///         Update-ConsoleStatus 'Copy changes' -Status Pass
    ///         New-ConsoleRunResult 'Backup finished' -Summary '1 284 files copied, 312 MB.' -Figure ([ordered] @{ files = '1 284'; copied = '312 MB' }) -Next @{
    ///             'Show where the log is' = { $MenuContext.Toast('Log: D:\Backups\backup.log') }
    ///         }
    ///     })
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleRunResult")]
    [OutputType(typeof(RunResult))]
    public sealed class CmdletNewConsoleRunResult : PSCmdlet {
        /// <summary><para type="description">The result title.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Title { get; set; } = string.Empty;

        /// <summary><para type="description">The outcome (default Pass): Pass, Fail, Warn or Skip, or PowerShell's words such as Success and Error.</para></summary>
        [Parameter]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind Status { get; set; } = StatusKind.Pass;

        /// <summary><para type="description">One or two sentences under the title.</para></summary>
        [Parameter]
        public string? Summary { get; set; }

        /// <summary><para type="description">Figures: labels mapped to values, shown large (use [ordered] to keep order).</para></summary>
        [Parameter]
        public IDictionary? Figure { get; set; }

        /// <summary><para type="description">Next actions: titles mapped to script blocks, run with $MenuContext like menu actions.</para></summary>
        [Parameter]
        public IDictionary? Next { get; set; }

        /// <summary><para type="description">A widget with details under the figures (for example a table of what changed).</para></summary>
        [Parameter]
        public Layout.IWidget? Details { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            RunResult result = new RunResult(Title, Status) { Summary = Summary, Details = Details };
            if (Figure != null) {
                foreach (DictionaryEntry entry in Figure) {
                    result.Figure(PSWidgets.ToText(entry.Key), PSWidgets.ToText(entry.Value));
                }
            }
            if (Next != null) {
                foreach (DictionaryEntry entry in Next) {
                    if (!(PSText.Unwrap(entry.Value) is ScriptBlock action)) {
                        ThrowTerminatingError(new ErrorRecord(new PSArgumentException("Each -Next action needs a script block."), "ConsoleMonsterRunResultNext", ErrorCategory.InvalidArgument, entry.Key));
                        return;
                    }
                    result.Next(PSWidgets.ToText(entry.Key), context => PSMenus.Invoke(action, context));
                }
            }
            WriteObject(result);
        }
    }
}
