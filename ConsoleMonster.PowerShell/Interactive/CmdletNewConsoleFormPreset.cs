using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a preset a form comes with: field values under one name, with a description and a preview.</para>
    /// <para type="description">Give the presets to New-ConsoleForm (-Preset, or emit them in its scriptblock next to the fields). They are listed before the presets people save, marked as built in, and cannot be overwritten or deleted; applying one (Read-ConsoleForm -Preset, or the menu's command palette) sets the fields it names the way a saved preset does. -Values maps field names to values: a string, a number, a bool, or a list for a multi-choice field; a value that names a choice (the object, its title or its text) takes that choice, and a value for a field whose choices load is chosen once they arrive.</para>
    /// </summary>
    /// <example>
    ///   <para>A form with two built-in presets</para>
    ///   <code>
    /// $form = New-ConsoleForm 'Report' -Command 'Export-AppReport' {
    ///     New-ConsoleField Period 'Period' -Type Select -Choices 'Day', 'Week', 'Month' -Parameter Period
    ///     New-ConsoleField Path 'Folder' -Type Path -Kind Folder -Parameter Path
    ///     New-ConsoleFormPreset 'Weekly' -Values @{ Period = 'Week'; Path = 'D:\Reports\Weekly' } -Description 'Every Monday.'
    ///     New-ConsoleFormPreset 'Monthly' -Values @{ Period = 'Month'; Path = 'D:\Reports\Monthly' }
    /// }
    /// Read-ConsoleForm $form -Preset Weekly</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleFormPreset")]
    [OutputType(typeof(FormPreset))]
    public sealed class CmdletNewConsoleFormPreset : PSCmdlet {
        /// <summary><para type="description">The name; a preset people save cannot take it.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; } = string.Empty;

        /// <summary><para type="description">Field values by field name (ignoring case): a string, a number, a bool, or a list for a multi-choice field.</para></summary>
        [Parameter(Mandatory = true, Position = 1)]
        public IDictionary Values { get; set; } = new Hashtable();

        /// <summary><para type="description">A sentence about the preset, shown with it.</para></summary>
        [Parameter]
        public string? Description { get; set; }

        /// <summary><para type="description">Content describing what the preset fills in, for lists that preview a preset while it has focus: a scriptblock run once now, widgets, text (markup) or objects (as a list).</para></summary>
        [Parameter]
        public object? Preview { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            WriteObject(new FormPreset(Name, PSForms.ToFieldValues(Values, null)) { Description = Description, Preview = PSForms.Content(Preview, this) });
        }
    }
}
