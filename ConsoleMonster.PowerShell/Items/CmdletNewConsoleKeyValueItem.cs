using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates one key/value pair, with an optional status or presenter, for New-ConsoleKeyValue.</para>
    /// <para type="description">Use it inside the script block of New-ConsoleKeyValue or Write-ConsoleKeyValue when a pair needs more than a hashtable gives: a status marker before the value (-Status), markup in the value (-Markup), or a value formatted by a presenter (-Presenter Bytes, Duration, Date...). Values are shown literally unless -Markup is used. To add pairs to a list made earlier, pipe the list to Add-ConsoleItem.</para>
    /// </summary>
    /// <example>
    ///   <para>Facts with status marks</para>
    ///   <code>
    /// New-ConsoleKeyValue {
    ///     New-ConsoleKeyValueItem 'Module' '[bold]ConsoleMonster[/]' -Markup
    ///     New-ConsoleKeyValueItem 'Signing' 'skipped' -Status Skip
    ///     New-ConsoleKeyValueItem 'Tests' '1 skipped on PowerShell 7' -Status Warn
    /// } | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Values formatted by presenters</para>
    ///   <code>
    /// New-ConsoleKeyValue {
    ///     New-ConsoleKeyValueItem 'Package size' 1482113 -Presenter Bytes
    ///     New-ConsoleKeyValueItem 'Built in' (New-TimeSpan -Seconds 48) -Presenter Duration
    /// } | Write-ConsoleWidget</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleKeyValueItem")]
    [OutputType(typeof(KeyValueItem))]
    public sealed class CmdletNewConsoleKeyValueItem : PSCmdlet {
        /// <summary><para type="description">The key (plain text).</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">The value. Shown literally unless -Markup is used; with -Presenter it keeps its type and is formatted by that presenter.</para></summary>
        [Parameter(Position = 1)]
        [AllowNull]
        public object? Value { get; set; }

        /// <summary><para type="description">A status marker before the value: Pass, Fail, Warn, Skip, Info, Running or Pending (PowerShell's words such as Success, Warning and Error work too).</para></summary>
        [Parameter]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind? Status { get; set; }

        /// <summary><para type="description">Treat the value as markup.</para></summary>
        [Parameter]
        public SwitchParameter Markup { get; set; }

        /// <summary><para type="description">How the value is shown: Bytes, Duration, Date, Number, Boolean, Status, Progress, DataBar, Link, Markup or Text.</para></summary>
        [Parameter]
        [ArgumentCompleter(typeof(Binding.PresenterNameCompleter))]
        public string? Presenter { get; set; }

        /// <summary><para type="description">A unit for the Number presenter, for example ms.</para></summary>
        [Parameter]
        public string? Unit { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            object? value = PSText.Unwrap(Value);
            if (!string.IsNullOrEmpty(Presenter)) {
                WriteObject(KeyValueItem.ForValue(Key, value, Presenters.CellPresenters.FromName(Presenter!, Unit), Status));
                return;
            }
            string text = value is Text.RichText rich ? rich.ToMarkup() : Markup.IsPresent ? PSWidgets.ToText(value) : Text.Markup.Escape(PSWidgets.ToText(value));
            WriteObject(new KeyValueItem(Key, text, Status));
        }
    }
}
