using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Sets field values of a form, whether or not it is shown.</para>
    /// <para type="description">Each named field takes its value and shows it at once, text and number boxes included; fields not named keep theirs. -Values maps field names (ignoring case) to values: a string, a number, a bool, or a list for a multi-choice field; a value that names a choice (the object, its title or its text) takes that choice. Choice fields whose options load from a field that changed load again, and a value given for such a field is chosen once its options arrive. Unknown names and values a field cannot take are ignored. -Values also takes the result of an earlier run of the form (Read-ConsoleForm -PassThru, or $FormResult in -OnSubmit) to fill in every field again. Call it from a menu action or handler while the menu runs, or before the form is shown.</para>
    /// </summary>
    /// <example>
    ///   <para>A menu action that fills in the form opened from another item</para>
    ///   <code>New-ConsoleMenuItem 'Use the lab' -Action { Set-ConsoleFormValue -Form $form -Values @{ Domain = 'lab.example.test'; Parallel = 2 }; $MenuContext.Toast('Lab values applied') }</code>
    /// </example>
    /// <example>
    ///   <para>Start a form from the answers of its last run</para>
    ///   <code>
    /// $last = Read-ConsoleForm $form -PassThru
    /// Set-ConsoleFormValue $form $last
    /// Read-ConsoleForm $form</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "ConsoleFormValue")]
    public sealed class CmdletSetConsoleFormValue : PSCmdlet {
        /// <summary><para type="description">The form from New-ConsoleForm.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public Form Form { get; set; } = null!;

        /// <summary><para type="description">Values by field name (a hashtable), or the result of an earlier run of the form (a form result or its Values).</para></summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true)]
        public object Values { get; set; } = null!;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            switch (PSText.Unwrap(Values)) {
                case FormResult result:
                    Form.Apply(result.Values);
                    break;
                case FormValues values:
                    Form.Apply(values);
                    break;
                case IDictionary map:
                    Form.Apply(PSForms.ToFieldValues(map, Form));
                    break;
                default:
                    ThrowTerminatingError(new ErrorRecord(new PSArgumentException("-Values takes a hashtable of field values or the result of a form run."), "ConsoleMonsterFormValue", ErrorCategory.InvalidArgument, Values));
                    break;
            }
        }
    }
}
