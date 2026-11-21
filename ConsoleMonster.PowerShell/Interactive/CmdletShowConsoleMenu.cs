using System;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Shows a menu until the user exits.</summary>
    /// <remarks>
    /// <para>Menu actions run on the pipeline thread. Selections and form values survive navigation. AppName stores presets, and RememberLastChoices also stores the last choices.</para>
    /// <para>Shell menu
    /// ----------</para>
    /// <para>The title bar shows the breadcrumb. The sidebar lists items and folds into the breadcrumb in narrow windows. The content pane shows the open list, form, page or review. The footer lists the current keys. Use Escape to go back and Ctrl+Q to exit.</para>
    /// <para>Classic menu
    /// ------------</para>
    /// <para>Classic shows a centred menu. Escape returns from a submenu and exits from the root. Both full-screen layouts show the key map with ?. When enabled, Ctrl+K opens the command palette. A colon also opens it when focus is outside a text input. Ctrl+Q asks before discarding unsaved form edits.</para>
    /// <para>Inline menu
    /// -----------</para>
    /// <para>Inline displays one selection prompt at a time in the scrollback. In line mode, respond with the numbered choice. Actions write their output in the scrollback and the menu returns after the action. Use Back in a submenu or choose an Exit item to leave.</para>
    /// <para>Static menu
    /// -----------</para>
    /// <para>Redirected output, CI, Mode None and non-interactive sessions print the current menu once. The static view executes no actions.</para>
    /// </remarks>
    /// <example>
    /// <para>Run a menu as an application</para>
    /// <code>
    /// $menuOptions2 = @{
    ///     Title = 'Server toolbox'
    ///     Content = {
    ///         $menuItemOptions = @{
    ///             Label = 'Current time'
    ///             Action = { $MenuContext.Toast((Get-Date).ToString('u')) }
    ///         }
    ///         New-ConsoleMenuItem @menuItemOptions
    ///         New-ConsoleMenuItem -Exit
    ///     }
    /// }
    /// $menu = New-ConsoleMenu @menuOptions2
    /// $menuOptions = @{
    ///     Menu = $menu
    ///     Layout = 'Shell'
    /// }
    /// Show-ConsoleMenu @menuOptions</code>
    /// </example>
    /// <example>
    /// <para>Choose an inventory action from a menu</para>
    /// <code>
    /// $menuOptions2 = @{
    ///     Title = 'Inventory'
    ///     Content = {
    ///         $menuItemOptions = @{
    ///             Label = 'List servers'
    ///             Action = { [pscustomobject]@{ Name = 'web01'; State = 'Online' } | Show-ConsoleTable }
    ///         }
    ///         New-ConsoleMenuItem @menuItemOptions
    ///         New-ConsoleMenuItem -Exit
    ///     }
    /// }
    /// $menu = New-ConsoleMenu @menuOptions2
    /// $menuOptions = @{
    ///     Menu = $menu
    /// }
    /// Show-ConsoleMenu @menuOptions</code>
    /// </example>
    /// <example>
    /// <para>Read any of these gallery scripts to see Show-ConsoleMenu in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>
    /// Get-ConsoleDemo -Slug 'toolbox' | Select-Object -ExpandProperty Source
    /// Get-ConsoleDemo -Slug 'fleet' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleMenu", HelpUri = "https://consolemonster.dev/powershell/commands/show-consolemenu/")]
    public sealed class CmdletShowConsoleMenu : PSCmdlet {
        private CancellationTokenSource? _stop;

        /// <summary><para type="description">The menu from New-ConsoleMenu.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        public PSObject? Menu { get; set; }

        /// <summary><para type="description">The layout: Auto (the menu's own, or Shell when items open content), Classic, Shell or Inline.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "Auto")]
        public MenuLayout Layout { get; set; } = MenuLayout.Auto;

        /// <summary><para type="description">The app name presets and remembered choices are stored under (default: the menu title).</para></summary>
        [Parameter]
        public string? AppName { get; set; }

        /// <summary><para type="description">Remembers selections and form answers between runs.</para></summary>
        [Parameter]

        [PSDefaultValue(Help = "$false")]
        public SwitchParameter RememberLastChoices { get; set; }

        /// <summary><para type="description">The folder that holds the app's storage folder (default: per user).</para></summary>
        [Parameter]
        public string? StorageRoot { get; set; }

        /// <summary><para type="description">Turns the command palette off.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter NoCommandPalette { get; set; }

        /// <summary><para type="description">Forces how to interact: Rich, Line or Accessible.</para></summary>
        [Parameter]
        public InteractionMode? Mode { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (!(PSText.Unwrap(Menu) is Menu current)) {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException("Show-ConsoleMenu needs a menu from New-ConsoleMenu. To pick pipeline objects, use: Get-Process | Read-ConsoleSelect"), "ExpectedConsoleMenu", ErrorCategory.InvalidArgument, Menu));
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
