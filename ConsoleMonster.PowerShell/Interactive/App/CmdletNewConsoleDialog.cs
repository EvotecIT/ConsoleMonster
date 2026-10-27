using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary><para type="synopsis">Creates a typed modal form for the running app's dialog service.</para><para type="description">Construction does not read input. AppDialogs.Show hosts the form on the owning application thread and returns DialogResult of AppState through its completion callback.</para></summary>
    /// <example><para>Define fields for an app-owned dialog.</para><code>New-ConsoleDialog -Title Options -InitialFocus Destination -Content { New-ConsoleTextInput -Key Destination }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleDialog")]
    [OutputType(typeof(FormDialog<AppState>))]
    public sealed class CmdletNewConsoleDialog : PSCmdlet {
        private readonly List<object> _children = new List<object>();
        /// <summary><para type="description">Visible heading.</para></summary>
        [Parameter(Mandatory = true)] public string Title { get; set; } = string.Empty;
        /// <summary><para type="description">Retained widgets or a script block that emits fields.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)] public object? Content { get; set; }
        /// <summary><para type="description">Affirmative button label.</para></summary>
        [Parameter] public string AcceptText { get; set; } = "OK";
        /// <summary><para type="description">Cancellation button label.</para></summary>
        [Parameter] public string CancelText { get; set; } = "Cancel";
        /// <summary><para type="description">Focuses the affirmative button initially instead of Cancel.</para></summary>
        [Parameter] public SwitchParameter DefaultAccept { get; set; }
        /// <summary><para type="description">Field key to focus instead of the default button.</para></summary>
        [Parameter] public string? InitialFocus { get; set; }
        /// <summary><para type="description">Maximum width in terminal cells.</para></summary>
        [Parameter, ValidateRange(20, 1000)] public int MaxWidth { get; set; } = 64;
        /// <summary><para type="description">App-thread validator returning an error string or null.</para></summary>
        [Parameter] public Func<AppState, string?>? Validate { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() {
            object? value = PSText.Unwrap(Content);
            if (value is ScriptBlock script) { foreach (PSObject item in script.Invoke()) { if (item != null) { _children.Add(item); } } }
            else if (value != null) { _children.Add(Content!); }
        }
        /// <inheritdoc />
        protected override void EndProcessing() {
            List<IWidget> children = PSWidgets.CollectAny(_children, this);
            WriteObject(new FormDialogOptions { Title = Title, Content = children.Count == 1 ? children[0] : new Rows(children.ToArray()), AcceptText = AcceptText, CancelText = CancelText, DefaultAccept = DefaultAccept, InitialFocus = InitialFocus, MaxWidth = MaxWidth, Validate = Validate }.Build());
        }
    }
}
