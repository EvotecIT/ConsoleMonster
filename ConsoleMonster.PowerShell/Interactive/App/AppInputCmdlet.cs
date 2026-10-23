using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Shared native parameters for stable outer application inputs, supported on PowerShell 5.1 and 7.</summary>
    public abstract class AppInputCmdlet : PSCmdlet {
        /// <summary><para type="description">Unique key in the application's state and final hashtable.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; } = string.Empty;

        /// <summary><para type="description">Visible label; defaults to the name using the core control's labels.</para></summary>
        [Parameter(Position = 1)]
        public string? Label { get; set; }

        /// <summary><para type="description">Disables editing and removes the input from keyboard focus traversal.</para></summary>
        [Parameter]
        public SwitchParameter Disabled { get; set; }

        /// <summary><para type="description">Keyboard focus order. Equal values preserve layout declaration order.</para></summary>
        [Parameter]
        public int TabIndex { get; set; }

        /// <summary><para type="description">Runs once per committed edit on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. State is a hashtable; add or assign keys to write back. Output is discarded. Retains closure variables.</para></summary>
        [Parameter]
        public ScriptBlock? OnChange { get; set; }

        /// <summary>Applies common configuration and attaches callback metadata without subscribing outside an app run.</summary>
        /// <param name="control">The new core input.</param>
        /// <param name="onClick">Optional button activation script.</param>
        protected void Emit(InputControl control, ScriptBlock? onClick = null) {
            control.Label = Label;
            control.Enabled = !Disabled.IsPresent;
            control.TabIndex = TabIndex;
            PSAppCallbacks.Register(control, OnChange, onClick);
            WriteObject(control);
        }
    }
}
