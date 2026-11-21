using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Sets field values of a form, whether or not it is shown.</summary>
    /// <remarks><para>Each named field takes its value and shows it at once, text and number boxes included; fields not named keep theirs. -Values maps field names (ignoring case) to values: a string, a number, a bool, or a list for a multi-choice field; a value that names a choice (the object, its title or its text) takes that choice. Choice fields whose options load from a field that changed load again, and a value given for such a field is chosen once its options arrive. Unknown names and values a field cannot take are ignored. -Values also takes the result of an earlier run of the form (Read-ConsoleForm -PassThru, or $FormResult in -OnSubmit) to fill in every field again. Call it from a menu action or handler while the menu runs, or before the form is shown.</para></remarks>
    /// <example>
    /// <para>Apply a preset before showing a form</para>
    /// <code>$formOptions2 = @{
    ///     Title = 'Connection'
    ///     Content = {
    ///         $fieldOptions2 = @{
    ///             Key = 'Domain'
    ///             Label = 'Domain'
    ///         }
    ///         New-ConsoleField @fieldOptions2
    ///         $fieldOptions = @{
    ///             Integer = $true
    ///             Key = 'Parallel'
    ///             Label = 'Parallel workers'
    ///             Kind = 'Number'
    ///             Minimum = 1
    ///         }
    ///         New-ConsoleField @fieldOptions
    ///     }
    /// }
    /// $form = New-ConsoleForm @formOptions2
    /// $formValueOptions = @{
    ///     Form = $form
    ///     Values = @{ Domain = 'lab.example.test'; Parallel = 2 }
    /// }
    /// Set-ConsoleFormValue @formValueOptions
    /// $formOptions = @{
    ///     Form = $form
    /// }
    /// Read-ConsoleForm @formOptions</code>
    /// </example>
    /// <example>
    /// <para>Start a form from the answers of its last run</para>
    /// <code>$formOptions3 = @{
    ///     Title = 'Connection'
    ///     Content = {
    ///         $fieldOptions = @{
    ///             Key = 'Domain'
    ///             Label = 'Domain'
    ///             Default = 'lab.example.test'
    ///         }
    ///         New-ConsoleField @fieldOptions
    ///     }
    /// }
    /// $form = New-ConsoleForm @formOptions3
    /// $formOptions2 = @{
    ///     PassThru = $true
    ///     Form = $form
    /// }
    /// $last = Read-ConsoleForm @formOptions2
    /// if ($last) {
    ///     $formValueOptions = @{
    ///         Form = $form
    ///         Values = $last
    ///     }
    ///     Set-ConsoleFormValue @formValueOptions
    ///     $formOptions = @{
    ///         Form = $form
    ///     }
    ///     Read-ConsoleForm @formOptions
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "ConsoleFormValue", HelpUri = "https://consolemonster.dev/powershell/commands/set-consoleformvalue/")]
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
