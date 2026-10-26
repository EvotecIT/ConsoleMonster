using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Dates;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Asks for one civil date or inclusive date range.</para>
    /// <para type="description">Returns one DateTime at midnight with unspecified kind, or one DateRange with -Range. Escape follows the normal prompt cancellation policy. Line and accessible modes accept yyyy-MM-dd and culture-aware dates; non-interactive mode requires a valid explicit default.</para>
    /// </summary>
    /// <example><para>Read an inclusive maintenance window.</para><code>Read-ConsoleDate -Title Window -Range -Default ([datetime]'2026-10-08')</code></example>
    [Cmdlet(VerbsCommunications.Read, "ConsoleDate")]
    [OutputType(typeof(DateTime), typeof(DateRange))]
    public sealed class CmdletReadConsoleDate : PromptCmdlet {
        private readonly List<object?> _marks = new List<object?>();
        /// <summary><para type="description">Default DateTime or DateRange, validated even in non-interactive mode.</para></summary>
        [Parameter, AllowNull] public object? Default { get; set; }
        /// <summary><para type="description">Returns one inclusive DateRange.</para></summary>
        [Parameter] public SwitchParameter Range { get; set; }
        /// <summary><para type="description">Shared date bounds, culture, today and immutable marked-day model.</para></summary>
        [Parameter] public DateSelectionOptions Selection { get; set; } = new DateSelectionOptions();
        /// <summary><para type="description">Optional dated status records, using Date, Status and Label members.</para></summary>
        [Parameter(ValueFromPipeline = true), AllowNull] public PSObject? InputObject { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() { if (InputObject != null) { _marks.Add(InputObject); } }
        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_marks.Count > 0) { Selection.Data = ConsoleHostViews.Calendar(_marks, new ConsoleCalendarOptions { Year = Selection.Today.Year, Month = Selection.Today.Month }).Data; }
            var prompt = new DatePrompt(Title) { Range = Range.IsPresent, Selection = Selection };
            Configure(prompt);
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Default)) && Default != null) {
                object value = Default is PSObject wrapped ? wrapped.BaseObject : Default;
                prompt.Default = value is DateRange || value is DateTime ? value : LanguagePrimitives.ConvertTo<DateTime>(value);
            }
            if (TrySnapshot(prompt)) { return; }
            WriteObject(Ask(prompt), false);
        }
    }
}
