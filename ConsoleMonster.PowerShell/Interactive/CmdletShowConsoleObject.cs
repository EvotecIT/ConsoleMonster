using System;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Data;
using ConsoleMonster.Interactive;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Explores an object's members with the keyboard.</para>
    /// <para type="description">Arrows move, Right and Left expand and collapse, Enter drills into a member, Backspace returns to its parent, and / searches displayed names and values. Ctrl+Enter or F10 accepts the selected node; Esc cancels. Nested values are read lazily and cycles, depth and collection sizes are bounded. In redirected, CI, line and non-interactive sessions, a static inspector is printed and no selection is returned. Objects parsed by ConvertFrom-Json use the same PowerShell member adapter as other objects.</para>
    /// </summary>
    /// <example><para>Browse a nested configuration</para><code>$config | Show-ConsoleObject</code></example>
    /// <example><para>Return a selected member value</para><code>$config | Show-ConsoleObject -PassThru</code></example>
    /// <example><para>Return its PowerShell path</para><code>$config | Show-ConsoleObject -RootPath '$config' -PassThru Path</code></example>
    [Cmdlet(VerbsCommon.Show, "ConsoleObject")]
    [OutputType(typeof(object), typeof(string))]
    public sealed class CmdletShowConsoleObject : PSCmdlet {
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();

        /// <summary><para type="description">The object to browse, supplied directly or through the pipeline.</para></summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [AllowNull]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Returns the selected value after F10 or Ctrl+Enter. Add the positional word Path to return its expression instead.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">Value (default) or Path. The positional form supports -PassThru Path.</para></summary>
        [Parameter(Position = 0)]
        [ValidateSet("Value", "Path")]
        public string SelectionOutput { get; set; } = "Value";

        /// <summary><para type="description">The variable expression at the start of returned paths; default $x. It is displayed, never evaluated.</para></summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string RootPath { get; set; } = "$x";

        /// <summary><para type="description">The screen title.</para></summary>
        [Parameter]
        public string Title { get; set; } = "Object explorer";

        /// <summary><para type="description">Maximum member depth below the root; default 8.</para></summary>
        [Parameter]
        [ValidateRange(0, 64)]
        public int Depth { get; set; } = 8;

        /// <summary><para type="description">Maximum items read from each collection (default 100).</para></summary>
        [Parameter]
        [ValidateRange(1, 10000)]
        public int MaxItems { get; set; } = 100;

        /// <summary><para type="description">Maximum visible members in an expansion; default 5000.</para></summary>
        [Parameter]
        [ValidateRange(1, 100000)]
        public int MaxNodes { get; set; } = 5000;

        /// <summary><para type="description">Member names to include, supporting wildcards.</para></summary>
        [Parameter]
        public string[]? Include { get; set; }

        /// <summary><para type="description">Member names to skip without invoking their getters, supporting wildcards.</para></summary>
        [Parameter]
        public string[]? Exclude { get; set; }

        /// <summary><para type="description">Rich runs the keyboard explorer. Line, Accessible and None print the static inspector.</para></summary>
        [Parameter]
        public InteractionMode? Mode { get; set; }

        /// <summary><para type="description">Width used by the static fallback; zero detects the host width.</para></summary>
        [Parameter]
        [ValidateRange(0, 10000)]
        public int Width { get; set; }

        /// <summary><para type="description">Prints a static inspector without reading keys.</para></summary>
        [Parameter] public SwitchParameter StaticSnapshot { get; set; }
        /// <summary><para type="description">Returns one plain-text static inspector.</para></summary>
        [Parameter] public SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Returns the static inspector widget.</para></summary>
        [Parameter] public SwitchParameter AsWidget { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            PSOutputExport.Validate(this);
            InspectorOptions options = PSFormatting.InspectorOptions(Depth, MaxItems, Include, Exclude, MaxNodes, InputObject);
            InteractiveOptions interactive = PSInteractive.Options(this, Mode);
            if (StaticSnapshot.IsPresent || AsString.IsPresent || AsWidget.IsPresent || interactive.ResolveMode() != InteractionMode.Rich) {
                InspectNode root = ObjectInspector.Inspect(InputObject, options);
                InspectorView view = new InspectorView(root, Title);
                if (AsWidget.IsPresent) { WriteObject(view); }
                else { PSFormatting.Emit(this, view, Width, null, AsString.IsPresent); }
                return;
            }
            ObjectExplorer explorer = new ObjectExplorer(InputObject, options, RootPath) { Title = Title };
            try {
                ExplorerNode? selected = explorer.Show(interactive, _stop.Token);
                if (PassThru.IsPresent && selected != null && selected.Error == null) {
                    WriteObject(string.Equals(SelectionOutput, "Path", StringComparison.OrdinalIgnoreCase) ? selected.Path : selected.Value, false);
                }
            } catch (Exception ex) when (ex is PromptUnavailableException || ex is PromptCanceledException || ex is Input.InputClosedException) {
                PSInteractive.Fail(this, ex, Title);
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() => _stop.Cancel();

        /// <inheritdoc />
        protected override void EndProcessing() => _stop.Dispose();
    }
}
