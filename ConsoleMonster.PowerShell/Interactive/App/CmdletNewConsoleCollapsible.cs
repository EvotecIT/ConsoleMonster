using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates a collapsible section for Show-ConsoleApp.</summary>
    /// <remarks>A section is a focusable header row with its content below. Enter, Space, Left/Right and a click on the header fold and unfold it; while it is collapsed the header shows -Summary and the content's inputs leave the focus order. The final state holds whether each section is expanded. New-ConsoleAccordion groups sections so that opening one folds the others.</remarks>
    /// <example><para>Fold long sections of a help page and keep one open at a time.</para><code>Show-ConsoleApp -Content {
    ///     New-ConsoleAccordion -Content {
    ///         New-ConsoleCollapsible -Key Synopsis -Expanded -Content { 'Gets the services on a computer.' }
    ///         New-ConsoleCollapsible -Key Examples -Summary '(2)' -Content {
    ///             'Get-Service -Name win*'
    ///             'Get-Service | Where-Object Status -eq Running'
    ///         }
    ///     }
    /// }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleCollapsible")]
    [OutputType(typeof(Collapsible))]
    public sealed class CmdletNewConsoleCollapsible : NoPipelineInputCmdlet {
        /// <summary><para type="description">Unique key in the application's state; the value is whether the section is expanded.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">Header text; defaults to the key.</para></summary>
        [Parameter(Position = 1)]
        public string? Label { get; set; }

        /// <summary><para type="description">The section's widgets, text or a script block that emits them.</para></summary>
        [Parameter]
        [AllowNull, AllowEmptyCollection]
        public object? Content { get; set; }

        /// <summary><para type="description">Muted text after the header while collapsed, such as a count.</para></summary>
        [Parameter]
        public string? Summary { get; set; }

        /// <summary><para type="description">Starts the section expanded.</para></summary>
        [Parameter]
        public SwitchParameter Expanded { get; set; }

        /// <summary><para type="description">Disables the header and removes it from keyboard focus traversal.</para></summary>
        [Parameter]
        public SwitchParameter Disabled { get; set; }

        /// <summary><para type="description">Keyboard focus order. Equal values preserve layout declaration order.</para></summary>
        [Parameter]
        public int TabIndex { get; set; }

        /// <summary><para type="description">Runs once each time the section folds or unfolds, on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. Output is discarded.</para></summary>
        [Parameter]
        public ScriptBlock? OnChange { get; set; }

        /// <summary><para type="description">Validation text shown in the header.</para></summary>
        [Parameter]
        public string? ValidationMessage { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            List<IWidget> children = PSWidgets.CollectAny(Content, this);
            Collapsible section = AppInput.Collapsible(new CollapsibleInputOptions {
                Key = Key, Label = Label, Expanded = Expanded.IsPresent, Summary = Summary, Disabled = Disabled.IsPresent, TabIndex = TabIndex, ValidationMessage = ValidationMessage,
                Content = children.Count == 0 ? null : children.Count == 1 ? children[0] : new Rows(children.ToArray())
            });
            PSAppCallbacks.Register(section, OnChange);
            WriteObject(section);
        }
    }
}
