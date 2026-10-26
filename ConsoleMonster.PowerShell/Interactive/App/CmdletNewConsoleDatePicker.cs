using System;
using System.Management.Automation;
using ConsoleMonster.Dates;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a persistent civil-date or inclusive range picker.</para>
    /// <para type="description">Uses Gregorian arithmetic with culture-aware labels. Arrows move the cursor, PageUp/PageDown change month, Ctrl+PageUp/PageDown change year, and Enter commits. In range mode Space anchors a range and Shift+arrows extend it. Bounds and unavailable dates reject a whole invalid range.</para>
    /// </summary>
    /// <example><para>Choose a maintenance range.</para><code>New-ConsoleDatePicker -Key Window -Range -Value ([datetime]'2026-10-08')</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleDatePicker")]
    [OutputType(typeof(DatePicker))]
    public sealed class CmdletNewConsoleDatePicker : AppInputCmdlet {
        /// <summary><para type="description">Initial DateTime or DateRange; a scalar range default means Start=End.</para></summary>
        [Parameter, AllowNull] public object? Value { get; set; }
        /// <summary><para type="description">Commits one inclusive DateRange.</para></summary>
        [Parameter] public SwitchParameter Range { get; set; }
        /// <summary><para type="description">Shared bounds, culture, today and marked-day model.</para></summary>
        [Parameter] public DateSelectionOptions Selection { get; set; } = new DateSelectionOptions();
        /// <inheritdoc />
        protected override void EndProcessing() {
            object? value = Value is PSObject wrapped ? wrapped.BaseObject : Value;
            if (value != null && !(value is DateRange) && !(value is DateTime)) { value = LanguagePrimitives.ConvertTo<DateTime>(value); }
            Emit(Selection.CreatePicker(Key, Range.IsPresent, value));
        }
    }
}
