using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>A prompt that does not use pipeline objects as choices or data.</summary>
    public abstract class NonSelectionPromptCmdlet : PromptCmdlet {
        /// <summary>Unsupported pipeline input, reported once with a suggested command.</summary>
        [Parameter(ValueFromPipeline = true, DontShow = true)]
        public PSObject? PipelineInput { get; set; }

        /// <inheritdoc />
        protected sealed override void ProcessRecord() {
            if (PipelineInput != null) { NoPipelineInputCmdlet.Reject(this, PipelineInput); }
        }
    }
}
