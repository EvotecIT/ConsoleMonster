using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Updates a summary bar (New-ConsoleSummaryBar) in the running dashboard: progress and counters.</para>
    /// <para type="description">-Done sets the items done (-Total changes the total; without it the bar keeps its total) and -Counter sets counts by label, in one call; a label that is a status word (passed, failed, warnings, skipped, running) takes that status's symbol and colour, and new labels are added after the existing ones. The elapsed time and the time left follow the session clock, so a simulated clock (New-ConsoleClock) drives them too. Safe to call from any runspace or thread.</para>
    /// </summary>
    /// <example>
    ///   <para>Count a finished check</para>
    ///   <code>Update-ConsoleSummaryBar -Done $done -Counter ([ordered] @{ passed = $passed; warnings = $warned; failed = $failed })</code>
    /// </example>
    /// <example>
    ///   <para>A run whose total grows as work is discovered</para>
    ///   <code>Update-ConsoleSummaryBar -Name summary -Done 12 -Total 80</code>
    /// </example>
    [Cmdlet(VerbsData.Update, "ConsoleSummaryBar")]
    public sealed class CmdletUpdateConsoleSummaryBar : LiveCmdlet {
        /// <summary><para type="description">Items done.</para></summary>
        [Parameter(Position = 0)]
        public double? Done { get; set; }

        /// <summary><para type="description">Items in total; without it the bar keeps the total it has (0 = unknown).</para></summary>
        [Parameter(Position = 1)]
        public double? Total { get; set; }

        /// <summary><para type="description">Labels mapped to counts (use [ordered] to keep the order of new labels); status words take their status symbol and colour.</para></summary>
        [Parameter]
        public IDictionary? Counter { get; set; }

        /// <summary><para type="description">The summary bar name (default summary).</para></summary>
        [Parameter]
        public string Name { get; set; } = "summary";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Done.HasValue || Total.HasValue) {
                // The value left out keeps what the bar has (the bar applies updates in order).
                Send(new LiveEvent(Name, "progress", value: Done ?? SummaryBar.Keep, total: Total ?? SummaryBar.Keep));
            }
            if (Counter != null) {
                foreach (DictionaryEntry entry in Counter) {
                    string label = PSWidgets.ToText(entry.Key);
                    string? state = StatusKinds.TryParse(label, out StatusKind status) ? status.ToString() : null;
                    Send(new LiveEvent(Name, "counter", label, state: state, value: LanguagePrimitives.ConvertTo<int>(PSText.Unwrap(entry.Value))));
                }
            }
        }
    }
}
