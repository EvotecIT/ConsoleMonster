using System;
using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a summary bar: one line with progress, done of total, the time left and result counters.</para>
    /// <para type="description">The line Invoke-ConsoleRun ends its view with, as a widget of its own: a thin bar, done of total, the percentage, the time left (estimated from -ElapsedSeconds and the share done) and counters in a fixed order with zeros dimmed rather than hidden. -Counter maps labels to counts; a label that is a status word (passed, failed, warnings, skipped, running) takes that status's symbol and colour. Without -Total only the count done shows.</para>
    /// </summary>
    /// <example>
    ///   <para>A run three quarters through</para>
    ///   <code>New-ConsoleSummaryBar -Done 30 -Total 40 -ElapsedSeconds 42 -Counter ([ordered] @{ passed = 27; failed = 2; skipped = 1 }) | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleSummaryBar")]
    [OutputType(typeof(SummaryBar))]
    public sealed class CmdletNewConsoleSummaryBar : PSCmdlet {
        /// <summary><para type="description">Items done.</para></summary>
        [Parameter(Position = 1)]
        public double Value { get; set; }

        /// <summary><para type="description">Items in total (0 = unknown: no bar, percentage or time left).</para></summary>
        [Parameter(Position = 2)]
        public double Total { get; set; }

        /// <summary><para type="description">Labels mapped to counts, in the order to show them; status words (passed, failed, skipped...) take their status symbol and colour.</para></summary>
        [Parameter]
        public IDictionary? Counter { get; set; }

        /// <summary><para type="description">Elapsed seconds used to estimate remaining time. Fractional seconds are accepted.</para></summary>
        [Parameter]
        public double? ElapsedSeconds { get; set; }
        private System.TimeSpan? Elapsed => ElapsedSeconds.HasValue ? System.TimeSpan.FromSeconds(ElapsedSeconds.Value) : (System.TimeSpan?)null;

        /// <summary><para type="description">The bar width in cells (default 20; narrow space shrinks it).</para></summary>
        [Parameter]
        public int BarWidth { get; set; } = 20;

        /// <summary><para type="description">The live name (default summary).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "summary";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            SummaryBar bar = new SummaryBar(WidgetId) { BarWidth = BarWidth, Elapsed = Elapsed };
            bar.SetProgress(Value, Total);
            if (Counter != null) {
                foreach (DictionaryEntry entry in Counter) {
                    string label = PSWidgets.ToText(entry.Key);
                    StatusKind? status = StatusKinds.TryParse(label, out StatusKind parsed) ? parsed : (StatusKind?)null;
                    bar.SetCounter(label, LanguagePrimitives.ConvertTo<int>(entry.Value), status);
                }
            }
            WriteObject(bar);
        }
    }
}
