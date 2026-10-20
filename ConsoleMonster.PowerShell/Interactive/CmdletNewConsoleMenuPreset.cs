using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a preset a menu comes with: form values and selections under one name, with a description and a preview.</para>
    /// <para type="description">Give the presets to New-ConsoleMenu (-Preset, or emit them in its scriptblock) and add a New-ConsoleMenuItem -Presets item: it lists them first, marked as built in, before the presets people save; they cannot be overwritten or deleted. Applying one sets the values it names and keeps the rest. -Form with -Values fills in a form opened from the menu (a list for a multi-choice field; a value for a field whose choices load is chosen once they arrive). -Item with -Selection selects values in a selection item. -Preview is shown beside the list while the preset has focus: a scriptblock (run once, now), widgets, text or objects.</para>
    /// </summary>
    /// <example>
    ///   <para>A preset that fills in a form, with what it sets as the preview</para>
    ///   <code>
    /// $form = New-ConsoleForm 'Backup' -Command 'Start-AppBackup' {
    ///     New-ConsoleField Target 'Target' -Type Select -Choices 'Daily', 'Weekly' -Parameter Target
    ///     New-ConsoleField Compress 'Compress' -Type Toggle -Parameter Compress
    /// }
    /// $menu = New-ConsoleMenu 'Backup helper' {
    ///     New-ConsoleMenuPreset 'Weekly · compressed' -Description 'The Sunday backup.' -Form $form -Values @{ Target = 'Weekly'; Compress = $true } -Preview {
    ///         New-ConsoleKeyValue -Data ([ordered] @{ Target = 'Weekly'; Compress = 'yes' })
    ///     }
    ///     New-ConsoleMenuItem 'Presets' -Presets
    ///     New-ConsoleMenuItem 'Back up' -Form $form
    ///     New-ConsoleMenuItem -Exit
    /// }</code>
    /// </example>
    /// <example>
    ///   <para>A preset that selects servers in a selection item</para>
    ///   <code>
    /// $targets = New-ConsoleMenuItem 'Targets' -MultiSelect 'SRV01', 'SRV02', 'SRV03'
    /// $menu = New-ConsoleMenu 'Patch' -Item $targets -Preset (New-ConsoleMenuPreset 'First wave' -Item $targets -Selection 'SRV01', 'SRV02') {
    ///     New-ConsoleMenuItem 'Presets' -Presets
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleMenuPreset")]
    [OutputType(typeof(MenuPreset))]
    public sealed class CmdletNewConsoleMenuPreset : PSCmdlet {
        /// <summary><para type="description">The name; a preset people save cannot take it.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; } = string.Empty;

        /// <summary><para type="description">A sentence shown while the preset has focus in the list.</para></summary>
        [Parameter]
        public string? Description { get; set; }

        /// <summary><para type="description">Content shown beside the list (below it when narrow) while the preset has focus: a scriptblock run once now, widgets, text (markup) or objects (as a list).</para></summary>
        [Parameter]
        public object? Preview { get; set; }

        /// <summary><para type="description">A form opened from the menu whose fields the preset fills in (with -Values).</para></summary>
        [Parameter]
        public Form? Form { get; set; }

        /// <summary><para type="description">The values for -Form by field name: a string, a number, a bool, or a list for a multi-choice field. Values that name a choice (the object, its title or its text) take that choice.</para></summary>
        [Parameter]
        public IDictionary? Values { get; set; }

        /// <summary><para type="description">A selection item of the menu (New-ConsoleMenuItem -Select or -MultiSelect) whose values the preset selects (with -Selection).</para></summary>
        [Parameter]
        public MenuItem? Item { get; set; }

        /// <summary><para type="description">The values to select in -Item: the choice objects or their texts; values the item does not offer are skipped.</para></summary>
        [Parameter]
        [AllowEmptyCollection]
        public object?[]? Selection { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (Values != null && Form == null) {
                ThrowTerminatingError(new ErrorRecord(new PSArgumentException("-Values needs the form they belong to: give -Form."), "ConsoleMonsterPresetNoForm", ErrorCategory.InvalidArgument, Name));
                return;
            }
            if (Selection != null && Item == null) {
                ThrowTerminatingError(new ErrorRecord(new PSArgumentException("-Selection needs the item it selects in: give -Item."), "ConsoleMonsterPresetNoItem", ErrorCategory.InvalidArgument, Name));
                return;
            }
            MenuPreset preset = new MenuPreset(Name) { Description = Description, Preview = PSForms.Content(Preview, this) };
            if (Form != null) {
                preset.Form(Form, PSForms.ToFieldValues(Values, Form));
            }
            if (Item != null) {
                List<object?> selected = new List<object?>();
                foreach (object? value in PSInteractive.Enumerate(Selection)) {
                    Choice? match = PSInteractive.Find(Item.Choices, value);
                    selected.Add(match != null ? match.Value : PSText.Unwrap(value));
                }
                preset.Selection(Item, selected);
            }
            WriteObject(preset);
        }
    }
}
