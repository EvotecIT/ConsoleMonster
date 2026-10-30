using System;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows a menu until the user exits.</para>
    /// <para type="description">Shell is an app: a title bar with the breadcrumb, a sidebar (it folds into the breadcrumb in narrow windows), the content pane with the open item's list, form, page or review, and a footer generated from the keys that work right now. Classic is one centred menu; Inline is a list in the scrollback for scripts. Everywhere: Esc goes back, Ctrl+K (or :) opens the command palette, ? shows every key, Ctrl+Q leaves (asking first when a form has unsaved changes). Item actions run on the pipeline thread. Selections and form values survive navigation; -AppName stores presets, and with -RememberLastChoices the last choices.</para>
    /// </summary>
    /// <example>
    /// <para>Run a menu as an application</para>
    /// <code>$menu = New-ConsoleMenu 'Server toolbox' -Content {
    ///     New-ConsoleMenuItem 'Current time' -Action { $MenuContext.Toast((Get-Date).ToString('u')) }
    ///     New-ConsoleMenuItem -Exit
    /// }
    /// Show-ConsoleMenu $menu -Layout Shell</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleMenu")]
    public sealed class CmdletShowConsoleMenu : PSCmdlet {
        private CancellationTokenSource? _stop;

        /// <summary><para type="description">The menu from New-ConsoleMenu.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        public PSObject? Menu { get; set; }

        /// <summary><para type="description">The layout: Auto (the menu's own, or Shell when items open content), Classic, Shell or Inline.</para></summary>
        [Parameter]
        public MenuLayout Layout { get; set; } = MenuLayout.Auto;

        /// <summary><para type="description">The app name presets and remembered choices are stored under (default: the menu title).</para></summary>
        [Parameter]
        public string? AppName { get; set; }

        /// <summary><para type="description">Remembers selections and form answers between runs.</para></summary>
        [Parameter]

        public SwitchParameter RememberLastChoices { get; set; }

        /// <summary><para type="description">The folder that holds the app's storage folder (default: per user).</para></summary>
        [Parameter]
        public string? StorageRoot { get; set; }

        /// <summary><para type="description">Turns the command palette off.</para></summary>
        [Parameter]
        public SwitchParameter NoCommandPalette { get; set; }

        /// <summary><para type="description">Forces how to interact: Rich, Line or Accessible.</para></summary>
        [Parameter]
        public InteractionMode? Mode { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (!(PSText.Unwrap(Menu) is Menu current)) {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException("Show-ConsoleMenu needs a menu from New-ConsoleMenu. To pick pipeline objects, use: Get-Service | Read-ConsoleSelect"), "ExpectedConsoleMenu", ErrorCategory.InvalidArgument, Menu));
                return;
            }
            MenuOptions options = new MenuOptions {
                    Layout = Layout,
                    Interactive = PSInteractive.Options(this, Mode),
                    AppName = AppName,
                    RememberLastChoices = RememberLastChoices.IsPresent,
                    StorageRoot = StorageRoot,
                    CommandPalette = !NoCommandPalette.IsPresent
                };
            options.Interactive ??= PSInteractive.Options(this, null);
            _stop = new CancellationTokenSource();
            try {
                current.Run(options, _stop.Token);
            } catch (Exception ex) when (ex is PromptCanceledException || ex is PromptUnavailableException || ex is Input.InputClosedException) {
                PSInteractive.Fail(this, ex, current.Title);
            } catch (OperationCanceledException) {
                // The pipeline was stopped (Ctrl+C); the screen has been restored.
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            try {
                _stop?.Cancel();
            } catch (ObjectDisposedException) {
            }
        }
    }
}
