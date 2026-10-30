using System;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Runs an embedded ConsoleMonster gallery demo in the current PowerShell runspace.</para>
    /// <para type="description">Without a slug, it lists the demos to pick from, grouped by category. Executes the same self-contained PowerShell source offered by Export-ConsoleDemo -Path. The demo runs in a child scope of the current runspace and uses the current host and loaded ConsoleMonster commands. No child process, download or temporary script file is used. Live demos can be interrupted normally. -StaticSnapshot selects a recorded state using scripted input or queued live updates controlled by the runner. The copied source has no capture parameters. Demo output and errors follow the normal PowerShell streams.</para>
    /// </summary>
    /// <example><para>Pick a demo from the list and run it.</para><code>Show-ConsoleDemo</code></example>
    /// <example><para>Run the fleet demo.</para><code>Show-ConsoleDemo fleet</code></example>
    /// <example><para>Render a deterministic snapshot.</para><code>Show-ConsoleDemo assessment -StaticSnapshot -Step 0</code></example>
    [Cmdlet(VerbsCommon.Show, "ConsoleDemo")]
    [OutputType(typeof(PSObject))]
    public sealed class CmdletShowConsoleDemo : NoPipelineInputCmdlet {
        /// <summary><para type="description">Exact demo slug from Get-ConsoleDemo. Without it, the demos are listed to pick from.</para></summary>
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string? Slug { get; set; }

        /// <summary><para type="description">Renders one snapshot instead of running live interactions.</para></summary>
        [Parameter]
        [ValidateSet("Rich", "Snapshot")]
        public Interactive.InteractionMode Mode { get; set; } = Interactive.InteractionMode.Rich;

        /// <summary><para type="description">Deterministic snapshot state, starting at zero. See the demo's Steps metadata for its snapshot count.</para></summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public int Step { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            if (Slug == null) {
                Slug = Pick();
                if (Slug == null) { return; }
            }
            string? slug = PSDemoResources.Find(Slug);
            if (slug == null) {
                ThrowTerminatingError(new ErrorRecord(
                    new ItemNotFoundException("Unknown ConsoleMonster demo '" + Slug + "'. Use Get-ConsoleDemo to list available slugs."),
                    "ConsoleDemoNotFound", ErrorCategory.ObjectNotFound, Slug));
                return;
            }

            if (MyInvocation.BoundParameters.ContainsKey(nameof(Step)) && Mode != Interactive.InteractionMode.Snapshot) {
                ThrowTerminatingError(new ErrorRecord(new PSArgumentException("Use -Step with -Mode Snapshot."),
                    "ConsoleDemoSnapshotRequired", ErrorCategory.InvalidArgument, Step));
            }
            foreach (PSObject result in ConsoleGallery.Show(slug, InvokeCommand, Mode == Interactive.InteractionMode.Snapshot, Step)) { WriteObject(result); }
        }

        // The demos as a list to pick from; Esc picks nothing. Without a user to ask, it says how to name one.
        private string? Pick() {
            var definition = new SelectQuestionOptions { Title = "Which demo?", Description = "Type to filter. Show-ConsoleDemo <slug> runs one directly." };
            foreach (PSObject demo in ConsoleGallery.List("*", InvokeCommand)) {
                string slug = Text(demo, "slug");
                definition.Choices.Add(new Choice(slug, Text(demo, "title")) {
                    Group = Text(demo, "category"),
                    Description = Text(demo, "pitch"),
                    Badge = RichText.Plain(slug)
                });
            }
            try {
                InteractiveOptions options = PSInteractive.Options(this, null);
                // A redirected command must not write a chooser into its output and wait on the attached stdin.
                // Explicit input/capture devices and hosts that provide their own line console remain usable.
                if (options.Input == null && Input.ConsoleInput.Override == null &&
                    options.Capture == null && options.LineConsole == null && Terminal.GetCurrentProfile().IsOutputRedirected) {
                    throw new PromptUnavailableException();
                }
                return (string?)Question.Select(definition).Show(options);
            } catch (PromptCanceledException) {
                return null;
            } catch (Exception error) when (error is PromptUnavailableException || error is Input.InputClosedException) {
                ThrowTerminatingError(new ErrorRecord(
                    new PSArgumentException("Name the demo to run, for example Show-ConsoleDemo fleet. Get-ConsoleDemo lists every slug.", error),
                    "ConsoleDemoSlugRequired", ErrorCategory.InvalidArgument, null));
                return null;
            }
        }

        private static string Text(PSObject demo, string name) => demo.Properties[name]?.Value?.ToString() ?? string.Empty;
    }
}
