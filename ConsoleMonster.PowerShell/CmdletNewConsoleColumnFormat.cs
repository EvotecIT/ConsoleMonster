using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates shared numeric or detail-column layout settings.</para>
    /// <para type="description">Pass the result to a widget's CountColumn, PercentColumn, ValueColumn or DetailColumn parameter. MinWidth is measured in cells; ReserveDigits reserves room for numbers before they grow. Widths remain stable during live updates unless -AllowShrink is specified.</para>
    /// </summary>
    /// <example><para>Reserve four digits for progress counts.</para><code>New-ConsoleProgress -WidgetId build -Label Build -CountColumn (New-ConsoleColumnFormat -ReserveDigits 4)</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleColumnFormat")]
    [OutputType(typeof(ColumnFormat))]
    public sealed class CmdletNewConsoleColumnFormat : PSCmdlet {
        /// <summary><para type="description">Alignment within the column; numbers default to Right.</para></summary>
        [Parameter] public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Right;
        /// <summary><para type="description">Minimum width in terminal cells.</para></summary>
        [Parameter] [ValidateRange(0, 1000)] public int MinWidth { get; set; }
        /// <summary><para type="description">Numeric digits reserved from the first frame.</para></summary>
        [Parameter] [ValidateRange(0, 1000)] public int ReserveDigits { get; set; }
        /// <summary><para type="description">Allows the column to shrink when its current content becomes shorter.</para></summary>
        [Parameter] public SwitchParameter AllowShrink { get; set; }
        /// <inheritdoc />
        protected override void EndProcessing() => WriteObject(new ColumnFormat(Alignment, MinWidth, ReserveDigits) { AllowShrink = AllowShrink.IsPresent });
    }
}
