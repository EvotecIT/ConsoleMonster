using System;
using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates the result a menu run returns: a title, a status, a summary, figures and next actions.</para>
    /// <para type="description">Return it from the script block of $MenuContext.Run (a live dashboard started from a menu action): when the run ends, the menu shows it as a result screen with the figures as large values and the next actions as items. -Figure maps labels to values (use [ordered] to keep order); -Next maps action titles to script blocks, which run with $MenuContext like any menu action. -FigureStatus colours the figures that need attention (Pass, Warn, Fail by label). -Status takes the shared status vocabulary (default Pass). The time shown with the result is the run's time on its session clock (real time, or a simulated clock given to the run); -ElapsedSeconds overrides it.</para>
    /// </summary>
    /// <example>
    ///   <para>A backup run that reports what it did</para>
    ///   <code>
    /// New-ConsoleMenuItem 'Back up now' -Action {
    ///     $dashboard = New-ConsoleDashboard -Title 'Backup' -Content { New-ConsoleStatusList -Item 'Scan files', 'Copy changes' }
    ///     $null = $MenuContext.Run($dashboard, {
    ///         Update-Console -Key 'Scan files' -Status Pass -WidgetId 'status'
    ///         Update-Console -Key 'Copy changes' -Status Pass -WidgetId 'status'
    ///         New-ConsoleMenuRunResult 'Backup finished' -Summary '1 284 files copied, 312 MB.' -Figure ([ordered] @{ files = '1 284'; copied = '312 MB' }) -Next @{
    ///             'Show where the log is' = { $MenuContext.Toast('Log: D:\Backups\backup.log') }
    ///         }
    ///     })
    /// }</code>
    /// </example>
    /// <example>
    ///   <para>Coloured figures</para>
    ///   <code>New-ConsoleMenuRunResult 'Checks finished' -Status Warn -Figure ([ordered] @{ passed = 41; warnings = 3; failed = 0 }) -FigureStatus @{ passed = 'Pass'; warnings = 'Warn' }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleMenuRunResult")]
    [OutputType(typeof(MenuRunResult))]
    public sealed class CmdletNewConsoleMenuRunResult : PSCmdlet {
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

        /// <summary><para type="description">The status of figures that need attention, by label: their numbers take the status colour (Pass, Warn, Fail...). Figures not named stay neutral.</para></summary>
        [Parameter]
        public IDictionary? FigureStatus { get; set; }

        /// <summary><para type="description">Elapsed seconds shown with the result and kept in history; defaults to the run session clock. Fractional seconds are accepted.</para></summary>
        [Parameter]
        public double? ElapsedSeconds { get; set; }
        private System.TimeSpan? Elapsed => ElapsedSeconds.HasValue ? System.TimeSpan.FromSeconds(ElapsedSeconds.Value) : (System.TimeSpan?)null;

        /// <summary><para type="description">A widget with details under the figures (for example a table of what changed).</para></summary>
        [Parameter]
        public Layout.IWidget? Details { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            MenuRunResult result = new MenuRunResult(Title, Status) { Summary = Summary, Details = Details };
            if (Elapsed.HasValue) {
                result.Elapsed = Elapsed.Value;
            }
            if (Figure != null) {
                foreach (DictionaryEntry entry in Figure) {
                    string label = PSWidgets.ToText(entry.Key);
                    object? status = FigureStatus == null ? null : FindStatus(FigureStatus, label);
                    if (status == null) {
                        result.Figure(label, PSWidgets.ToText(entry.Value));
                    } else if (Binding.StatusArguments.TryParse(status, out StatusKind kind)) {
                        result.Figure(label, PSWidgets.ToText(entry.Value), kind);
                    } else {
                        ThrowTerminatingError(new ErrorRecord(new PSArgumentException("'" + PSWidgets.ToText(status) + "' is not a status. Use one of: " + string.Join(", ", StatusKinds.Words) + "."), "ConsoleMonsterRunResultFigureStatus", ErrorCategory.InvalidArgument, label));
                        return;
                    }
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

        private static object? FindStatus(IDictionary statuses, string label) {
            foreach (DictionaryEntry entry in statuses) {
                if (string.Equals(PSWidgets.ToText(entry.Key), label, StringComparison.OrdinalIgnoreCase)) {
                    return PSText.Unwrap(entry.Value);
                }
            }
            return null;
        }
    }
}
