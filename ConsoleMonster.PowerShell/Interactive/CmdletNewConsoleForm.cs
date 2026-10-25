using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a form or wizard from fields (New-ConsoleField) for Read-ConsoleForm or a menu item.</para>
    /// <para type="description">The scriptblock's New-ConsoleField output becomes the fields. Fields with -Page are grouped into pages in the order the page names first appear, which turns the form into a wizard with a progress strip. Use -PageDescriptions to give each page its own sentence under the title. Unless -NoReview is given, a review page follows: every answer, the equivalent command line (from -Command and each field's -Parameter), Run, Copy command, Save preset and Back. Presets the form comes with (New-ConsoleForm -Preset, in the scriptblock or -Preset) are listed before the saved ones.</para>
    /// </summary>
    /// <example>
    ///   <para>A two-page wizard</para>
    ///   <code>
    /// $form = New-ConsoleForm 'New share' -Command 'New-AppShare' -PageDescriptions @{ Basics = 'Name and location of the share.'; Access = 'How people can use the share.' } -Content {
    ///     New-ConsoleField Name 'Share name' -Page Basics -Required -ParameterName Name
    ///     New-ConsoleField Path 'Folder' -Type Path -Kind Folder -MustExist -Page Basics -ParameterName Path
    ///     New-ConsoleField ReadOnly 'Read only' -Type Toggle -Page Access -ParameterName ReadOnly
    /// }</code>
    /// </example>
    /// <example>
    ///   <para>A form that comes with a preset</para>
    ///   <code>
    /// $form = New-ConsoleForm 'New share' -Command 'New-AppShare' -Content {
    ///     New-ConsoleField Name 'Share name' -Required -ParameterName Name
    ///     New-ConsoleField ReadOnly 'Read only' -Type Toggle -ParameterName ReadOnly
    ///     [pscustomobject]@{ Name = 'Archive'; Values = @{ Name = 'Archive'; ReadOnly = $true }; Description = 'A read-only archive share.' }
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleForm")]
    [OutputType(typeof(Form))]
    public sealed class CmdletNewConsoleForm : PSCmdlet {
        /// <summary><para type="description">The title.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Title { get; set; } = string.Empty;

        /// <summary><para type="description">A scriptblock whose New-ConsoleField output becomes the fields.</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Fields created beforehand.</para></summary>
        [Parameter]
        public Field[]? Field { get; set; }

        /// <summary><para type="description">A sentence under the title.</para></summary>
        [Parameter]
        public string? Description { get; set; }

        /// <summary><para type="description">Descriptions by page name, for pages declared by the fields. Pages without a matching entry keep their default description.</para></summary>
        [Parameter]
        public IDictionary? PageDescriptions { get; set; }

        /// <summary><para type="description">The command the equivalent command line starts with.</para></summary>
        [Parameter]
        public string? Command { get; set; }

        /// <summary><para type="description">How the equivalent command line is written: PowerShell (-Name value), Posix (--name value) or Windows (/name:value).</para></summary>
        [Parameter]
        public CommandStyle CommandStyle { get; set; } = CommandStyle.PowerShell;

        /// <summary><para type="description">Leaves out the review page.</para></summary>
        [Parameter]
        public SwitchParameter NoReview { get; set; }

        /// <summary><para type="description">The caption of the submit button (default Run).</para></summary>
        [Parameter]
        public string? SubmitText { get; set; }

        /// <summary><para type="description">The key presets and remembered choices are stored under (default: the title).</para></summary>
        [Parameter]
        public string? StorageKey { get; set; }

        /// <summary><para type="description">Presets the form comes with (New-ConsoleForm -Preset), listed before the saved ones; presets emitted in the scriptblock are added too.</para></summary>
        [Parameter]
        public object[]? Preset { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Form form = new Form(Title) {
                Description = Description,
                Command = Command,
                CommandStyle = CommandStyle,
                Review = !NoReview.IsPresent,
                SubmitText = SubmitText,
                StorageKey = StorageKey
            };
            List<Field> fields = new List<Field>();
            List<FormPreset> presets = new List<FormPreset>();
            if (Field != null) {
                fields.AddRange(Field);
            }
            if (Preset != null) {
                foreach (object preset in Preset) { presets.Add(PSPresets.Form(preset, this)); }
            }
            if (Content != null) {
                foreach (PSObject output in Content.Invoke()) {
                    if (output?.BaseObject is Field field) {
                        fields.Add(field);
                    } else if (output != null && (output.BaseObject is FormPreset || PSPresets.IsRecord(output))) {
                        presets.Add(PSPresets.Form(output, this));
                    }
                }
            }
            Dictionary<string, FormPage> pages = new Dictionary<string, FormPage>(System.StringComparer.OrdinalIgnoreCase);
            foreach (Field field in fields) {
                string name = PSForms.PageOf(field) ?? Title;
                if (!pages.TryGetValue(name, out FormPage? page)) {
                    string? description = PageDescriptions != null && PageDescriptions.Contains(name)
                        ? LanguagePrimitives.ConvertTo<string>(PageDescriptions[name])
                        : null;
                    page = form.Page(name, description);
                    pages[name] = page;
                }
                page.Add(field);
            }
            foreach (FormPreset preset in presets) {
                PSForms.MatchChoices(preset.Values, form);
                form.Presets.Add(preset);
            }
            WriteObject(form);
        }
    }
}
