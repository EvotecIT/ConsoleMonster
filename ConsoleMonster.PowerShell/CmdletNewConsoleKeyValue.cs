using System.Collections;
using System.Management.Automation;
using System.Collections.Generic;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates an aligned key/value list from a hashtable, an object's properties or Key/Value records pairs.</para>
    /// <para type="description">Pairs come from a hashtable (use [ordered] to keep order), from an object's properties, or from a script block of Key/Value records calls when a pair needs a status marker, markup or a presenter. Keys are muted and aligned; values wrap. Values are shown literally unless -Markup is used. To add pairs later, pipe the list to Add-ConsoleItem; in a live dashboard, update pairs with Update-Console.</para>
    /// </summary>
    /// <example>
    ///   <para>A summary block</para>
    ///   <code>New-ConsoleKeyValue -InputObject ([ordered] @{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = 'not signed' }) | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Pairs with status marks</para>
    ///   <code>
    /// New-ConsoleKeyValue -Content {
    ///     [pscustomobject]@{ Key = 'Version'; Value = '0.0.4 [muted]→[/] [bold]2.0.0[/]'; Markup = $true }
    ///     [pscustomobject]@{ Key = 'Signing'; Value = 'skipped'; Status = 'Skip' }
    ///     [pscustomobject]@{ Key = 'Tests'; Value = '1 skipped on PowerShell 7'; Status = 'Warn' }
    /// } | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>A hashtable and pairs together</para>
    ///   <code>
    /// New-ConsoleKeyValue -InputObject ([ordered] @{ Module = 'ConsoleMonster'; Frameworks = 'netstandard2.0, net472, net8.0' }) -Content {
    ///     [pscustomobject]@{ Key = 'Signing'; Value = 'skipped'; Status = 'Skip' }
    /// } | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleKeyValue")]
    [OutputType(typeof(KeyValueList))]
    public sealed class CmdletNewConsoleKeyValue : PSCmdlet {
        private readonly List<object> _input = new List<object>();
        /// <summary><para type="description">A hashtable or an object whose properties become the pairs. A record containing both Key and Value is an item descriptor, even when Value is null; Key by itself is ordinary data. A script block here is taken as -Content.</para></summary>
        [Parameter(Position = 1, ValueFromPipeline = true)]
        public object? InputObject { get; set; }

        /// <summary><para type="description">A script block that emits Key/Value records pairs (or hashtables), added after -InputObject.</para></summary>
        [Parameter(Position = 2)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Treat values as markup.</para></summary>
        [Parameter]
        public SwitchParameter Markup { get; set; }

        /// <summary><para type="description">The live name used by Update-Console (default keyvalue).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "keyvalue";

        /// <summary><para type="description">Shared NumberColumn layout settings from New-ConsoleColumnFormat.</para></summary>
        [Parameter] public Layout.ColumnFormat? NumberColumn { get; set; }

        /// <summary><para type="description">Cells between the key column and values.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] public int Gap { get; set; } = 2;
        /// <summary><para type="description">Preferred key fraction when natural widths do not fit.</para></summary>
        [Parameter, ValidateRange(0.01, 0.99)] public double MaxKeyFraction { get; set; } = 0.4;

        /// <summary><para type="description">Preferred pairs per row; reduces automatically in narrow terminals.</para></summary>
        [Parameter, ValidateRange(1, 12)] public int Columns { get; set; } = 1;
        /// <summary><para type="description">Theme role for keys.</para></summary>
        [Parameter] public ConsoleMonster.Styling.ThemeToken KeyRole { get; set; } = ConsoleMonster.Styling.ThemeToken.Muted;
        /// <summary><para type="description">Shared value-formatting culture, clock and time zone.</para></summary>
        [Parameter] public Presenters.PresentationOptions? Presentation { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            object? raw = PSText.Unwrap(InputObject);
            if (raw is ScriptBlock script) {
                if (Content != null) { throw new PSArgumentException("Give the builder script through Content only once."); }
                Content = script;
            } else { _input.AddRange(PSItems.Items(InputObject)); }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            KeyValueList list = new KeyValueList(WidgetId) { Gap = Gap, MaxKeyFraction = MaxKeyFraction, Columns = Columns, KeyRole = KeyRole };
            foreach (object input in _input) {
                object? raw = PSText.Unwrap(input);
                if (PSItems.IsPairRecord(input) || raw is KeyValueItem) {
                    PSItems.AddAll(this, list, new[] { input });
                } else if (raw is IDictionary dictionary) {
                    foreach (DictionaryEntry entry in dictionary) { list.Add(PSWidgets.ToText(entry.Key), Value(entry.Value)); }
                } else {
                    foreach (PSPropertyInfo property in PSObject.AsPSObject(input).Properties) {
                        if (!property.IsGettable) { continue; }
                        object? value;
                        try { value = property.Value; } catch (GetValueException) { value = null; }
                        list.Add(property.Name, Value(value));
                    }
                }
            }
            PSItems.AddAll(this, list, PSItems.Items(Content));
            if (NumberColumn != null) { list.NumberColumn = NumberColumn; }
            if (Presentation != null) { list.Presentation = Presentation; }
            WriteObject(list);
        }


        private string Value(object? value) {
            string text = PSWidgets.ToText(value);
            return Markup.IsPresent ? text : Text.Markup.Escape(text);
        }
    }
}
