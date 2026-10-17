using System;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// The parameters every Read-Console* prompt shares: the question, help text, the parameter that supplies the value
    /// non-interactively, and an interaction mode override. Esc and Ctrl+C end the prompt with a terminating
    /// PromptCancelled error; without a user (CI, redirected input, -NonInteractive) the default is returned, or a
    /// PromptNeedsValue error names the parameter to pass.
    /// </summary>
    public abstract class PromptCmdlet : PSCmdlet {
        private CancellationTokenSource? _stop;

        /// <summary><para type="description">The question.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("Prompt", "Question")]
        public string Title { get; set; } = string.Empty;

        /// <summary><para type="description">Help text under the question.</para></summary>
        [Parameter]
        public string? Description { get; set; }

        /// <summary><para type="description">The parameter of your script that supplies this value without a prompt (named in the non-interactive error, for example -Domain).</para></summary>
        [Parameter]
        [Alias("Parameter")]
        public string? ParameterName { get; set; }

        /// <summary><para type="description">Forces how to ask: Rich (in-place prompt), Line (question and typed answer), Accessible (numbered, no redraws) or None (use the default or fail).</para></summary>
        [Parameter]
        public InteractionMode? Mode { get; set; }

        /// <summary>Options for the current host.</summary>
        protected InteractiveOptions Options() => PSInteractive.Options(this, Mode);

        /// <summary>Cancelled when the pipeline stops.</summary>
        protected CancellationToken Token => (_stop ??= new CancellationTokenSource()).Token;

        /// <summary>Applies the shared settings to a prompt.</summary>
        protected void Configure<T>(Prompt<T> prompt) {
            prompt.Description = Description;
            prompt.ParameterName = ParameterName;
        }

        /// <summary>Shows a prompt, turning cancellation and missing values into PowerShell errors.</summary>
        protected T Ask<T>(Prompt<T> prompt) {
            try {
                InteractiveOptions options = Options();
                if (options.ResolveMode() == InteractionMode.None && prompt.HasDefault) {
                    WriteVerbose("'" + Title + "': no interactive user, using the default.");
                }
                return prompt.Show(options, Token);
            } catch (Exception ex) when (ex is PromptCancelledException || ex is PromptUnavailableException || ex is Input.InputClosedException) {
                PSInteractive.Fail(this, ex, Title);
                throw;
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            try {
                _stop?.Cancel();
            } catch (ObjectDisposedException) {
            }
            base.StopProcessing();
        }
    }
}
