using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates an aligned key/value list from a hashtable, an object's properties or New-ConsoleKeyValueItem pairs.</para>
    /// <para type="description">Pairs come from a hashtable (use [ordered] to keep order), from an object's properties, or from a script block of New-ConsoleKeyValueItem calls when a pair needs a status marker, markup or a presenter. Keys are muted and aligned; values wrap. Values are shown literally unless -Markup is used. To add pairs later, pipe the list to Add-ConsoleItem; in a live dashboard, update pairs with Set-ConsoleValue.</para>
    /// </summary>
    /// <example>
    ///   <para>A summary block</para>
    ///   <code>New-ConsoleKeyValue -InputObject ([ordered] @{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = 'not signed' }) | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Pairs with status marks</para>
    ///   <code>
    /// New-ConsoleKeyValue -Content {
    ///     New-ConsoleKeyValueItem 'Version' '0.0.4 [muted]→[/] [bold]2.0.0[/]' -Markup
    ///     New-ConsoleKeyValueItem 'Signing' 'skipped' -Status Skip
    ///     New-ConsoleKeyValueItem 'Tests' '1 skipped on PowerShell 7' -Status Warn
    /// } | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>A hashtable and pairs together</para>
    ///   <code>
    /// New-ConsoleKeyValue -InputObject ([ordered] @{ Module = 'ConsoleMonster'; Frameworks = 'netstandard2.0, net472, net8.0' }) -Content {
    ///     New-ConsoleKeyValueItem 'Signing' 'skipped' -Status Skip
    /// } | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleKeyValue")]
    [OutputType(typeof(KeyValueList))]
    public class CmdletNewConsoleKeyValue : PSCmdlet {
        /// <summary><para type="description">A hashtable or an object whose properties become the pairs (a script block here is taken as -Content).</para></summary>
        [Parameter(Position = 1, ValueFromPipeline = true)]
        public object? InputObject { get; set; }

        /// <summary><para type="description">A script block that emits New-ConsoleKeyValueItem pairs (or hashtables), added after -InputObject.</para></summary>
        [Parameter(Position = 2)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Treat values as markup.</para></summary>
        [Parameter]
        public SwitchParameter Markup { get; set; }

        /// <summary><para type="description">The live name used by Set-ConsoleValue (default keyvalue).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "keyvalue";

        /// <summary><para type="description">Shared NumberColumn layout settings from New-ConsoleColumnFormat.</para></summary>
        [Parameter] public Layout.ColumnFormat? NumberColumn { get; set; }

        /// <summary><para type="description">Preferred pairs per row; reduces automatically in narrow terminals.</para></summary>
        [Parameter, ValidateRange(1, 12)] public int Columns { get; set; } = 1;
        /// <summary><para type="description">Theme role for keys.</para></summary>
        [Parameter] public ConsoleMonster.Styling.ThemeToken KeyRole { get; set; } = ConsoleMonster.Styling.ThemeToken.Muted;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            KeyValueList list = new KeyValueList(WidgetId) { Columns = Columns, KeyRole = KeyRole };
            object? raw = InputObject is PSObject ps ? ps.BaseObject : InputObject;
            ScriptBlock? content = Content;
            if (raw is ScriptBlock script) {
                content = script;
                raw = null;
            }
            if (raw is IDictionary dictionary) {
                foreach (DictionaryEntry entry in dictionary) {
                    list.Add(PSWidgets.ToText(entry.Key), Value(entry.Value));
                }
            } else if (raw != null) {
                foreach (PSPropertyInfo property in PSObject.AsPSObject(raw).Properties) {
                    if (property.IsGettable) {
                        object? value;
                        try {
                            value = property.Value;
                        } catch (GetValueException) {
                            value = null;
                        }
                        list.Add(property.Name, Value(value));
                    }
                }
            }
            PSItems.AddAll(this, list, PSItems.Items(content));
            if (NumberColumn != null) { list.NumberColumn = NumberColumn; }
            Emit(list);
        }

        /// <summary>Emits the list (Show-ConsoleList overrides this to write it).</summary>
        /// <param name="list">The list.</param>
        protected virtual void Emit(KeyValueList list) => WriteObject(list);

        private string Value(object? value) {
            string text = PSWidgets.ToText(value);
            return Markup.IsPresent ? text : Text.Markup.Escape(text);
        }
    }
}
