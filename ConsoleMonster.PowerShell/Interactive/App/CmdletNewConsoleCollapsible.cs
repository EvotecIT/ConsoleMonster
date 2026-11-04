using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a collapsible section for Show-ConsoleApp, or an accordion of sections that keeps one open.</para>
    /// <para type="description">A section is a focusable header row with its content below. Enter, Space, Left/Right and a click on the header fold and unfold it; while it is collapsed the header shows -Summary and the content's inputs leave the focus order. The final state holds whether each section is expanded. With -Accordion, -Content emits sections and opening one folds the others; + expands every section and - folds them all.</para>
    /// </summary>
    /// <example><para>Fold long sections of a help page and keep one open at a time.</para><code>Show-ConsoleApp -Content {
    ///     New-ConsoleCollapsible -Accordion -Content {
    ///         New-ConsoleCollapsible -Key Synopsis -Expanded -Content { 'Gets the services on a computer.' }
    ///         New-ConsoleCollapsible -Key Examples -Summary '(2)' -Content {
    ///             'Get-Service -Name win*'
    ///             'Get-Service | Where-Object Status -eq Running'
    ///         }
    ///     }
    /// }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleCollapsible", DefaultParameterSetName = SectionSet)]
    [OutputType(typeof(Collapsible), ParameterSetName = new[] { SectionSet })]
    [OutputType(typeof(Accordion), ParameterSetName = new[] { AccordionSet })]
    public sealed class CmdletNewConsoleCollapsible : NoPipelineInputCmdlet {
        private const string SectionSet = "Section";
        private const string AccordionSet = "Accordion";

        /// <summary><para type="description">Unique key in the application's state; the value is whether the section is expanded.</para></summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = SectionSet)]
        [ValidateNotNullOrEmpty]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">Header text; defaults to the key.</para></summary>
        [Parameter(Position = 1, ParameterSetName = SectionSet)]
        public string? Label { get; set; }

        /// <summary><para type="description">The section's widgets, text or a script block that emits them. With -Accordion, the sections of the group.</para></summary>
        [Parameter(ParameterSetName = SectionSet)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = AccordionSet)]
        [AllowNull, AllowEmptyCollection]
        public object? Content { get; set; }

        /// <summary><para type="description">Muted text after the header while collapsed, such as a count.</para></summary>
        [Parameter(ParameterSetName = SectionSet)]
        public string? Summary { get; set; }

        /// <summary><para type="description">Starts the section expanded.</para></summary>
        [Parameter(ParameterSetName = SectionSet)]
        public SwitchParameter Expanded { get; set; }

        /// <summary><para type="description">Groups the sections emitted by -Content so that opening one folds the others.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = AccordionSet)]
        public SwitchParameter Accordion { get; set; }

        /// <summary><para type="description">Disables the header and removes it from keyboard focus traversal.</para></summary>
        [Parameter(ParameterSetName = SectionSet)]
        public SwitchParameter Disabled { get; set; }

        /// <summary><para type="description">Keyboard focus order. Equal values preserve layout declaration order.</para></summary>
        [Parameter(ParameterSetName = SectionSet)]
        public int TabIndex { get; set; }

        /// <summary><para type="description">Runs once each time the section folds or unfolds, on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. Output is discarded.</para></summary>
        [Parameter(ParameterSetName = SectionSet)]
        public ScriptBlock? OnChange { get; set; }

        /// <summary><para type="description">Validation text shown in the header.</para></summary>
        [Parameter(ParameterSetName = SectionSet)]
        public string? ValidationMessage { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            List<IWidget> children = PSWidgets.CollectAny(Content, this);
            if (ParameterSetName == AccordionSet) {
                Collapsible[] sections = children.OfType<Collapsible>().ToArray();
                if (sections.Length == 0 || sections.Length != children.Count) {
                    throw new PSArgumentException("New-ConsoleCollapsible -Accordion needs -Content that emits only New-ConsoleCollapsible sections.");
                }
                WriteObject(new Accordion(sections));
                return;
            }
            Collapsible section = AppInput.Collapsible(new CollapsibleInputOptions {
                Key = Key, Label = Label, Expanded = Expanded.IsPresent, Summary = Summary, Disabled = Disabled.IsPresent, TabIndex = TabIndex, ValidationMessage = ValidationMessage,
                Content = children.Count == 0 ? null : children.Count == 1 ? children[0] : new Rows(children.ToArray())
            });
            PSAppCallbacks.Register(section, OnChange);
            WriteObject(section);
        }
    }
}
