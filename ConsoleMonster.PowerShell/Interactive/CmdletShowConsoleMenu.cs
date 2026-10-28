using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows a menu until the user exits.</para>
    /// <para type="description">Shell is an app: a title bar with the breadcrumb, a sidebar (it folds into the breadcrumb in narrow windows), the content pane with the open item's list, form, page or review, and a footer generated from the keys that work right now. Classic is one centred menu; Inline is a list in the scrollback for scripts. Everywhere: Esc goes back, Ctrl+K (or :) opens the command palette, ? shows every key, Ctrl+Q leaves (asking first when a form has unsaved changes). Item actions run on the pipeline thread. Selections and form values survive navigation; -AppName stores presets, and with -RememberLastChoices the last choices.</para>
    /// </summary>
    /// <example>
    ///   <para>Run a menu as an app</para>
    ///   <code>Show-ConsoleMenu $menu -Layout Shell -AppName 'ServerToolbox' -RememberLastChoices</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleMenu")]
    public sealed class CmdletShowConsoleMenu : PSCmdlet {
        private CancellationTokenSource? _stop;
        private readonly List<object?> _pipelineItems = new List<object?>();

        /// <summary><para type="description">The menu from New-ConsoleMenu.</para></summary>
        [Parameter(Position = 0)]
        public Menu? Menu { get; set; }

        /// <summary><para type="description">Objects to select from when using the pipeline.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Allows selecting multiple pipeline objects.</para></summary>
        [Parameter]
        public SwitchParameter Multiple { get; set; }

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

        /// <summary><para type="description">Typed C# MenuOptions configuration. Use this instead of individual interaction settings; omitted host devices use the owning PowerShell runspace and the supplied object is not modified.</para></summary>
        [Parameter, ValidateNotNull] public MenuOptions? Options { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _pipelineItems.Add(InputObject);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_pipelineItems.Count > 0) {
                if (Menu != null) {
                    ThrowTerminatingError(new ErrorRecord(new ArgumentException("Provide a menu or pipeline objects, not both."), "MenuAndInput", ErrorCategory.InvalidArgument, null));
                    return;
                }
                if (_pipelineItems.Count == 1 && PSText.Unwrap(_pipelineItems[0]) is Menu pipedMenu) {
                    Menu = pipedMenu;
                } else {
                    ShowChoices();
                    return;
                }
            }
            if (Menu == null) {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException("Provide a menu or pipe objects to select."), "MissingMenu", ErrorCategory.InvalidArgument, null));
                return;
            }
            MenuOptions options = PSOptions.Resolve(this, Options, () => new MenuOptions {
                    Layout = Layout,
                    Interactive = PSInteractive.Options(this, Mode),
                    AppName = AppName,
                    RememberLastChoices = RememberLastChoices.IsPresent,
                    StorageRoot = StorageRoot,
                    CommandPalette = !NoCommandPalette.IsPresent
                }, nameof(Layout), nameof(AppName), nameof(RememberLastChoices), nameof(StorageRoot), nameof(NoCommandPalette), nameof(Mode)).Clone();
            options.Interactive ??= PSInteractive.Options(this, null);
            _stop = new CancellationTokenSource();
            try {
                Menu.Run(options, _stop.Token);
            } catch (Exception ex) when (ex is PromptCanceledException || ex is PromptUnavailableException || ex is Input.InputClosedException) {
                PSInteractive.Fail(this, ex, Menu.Title);
            } catch (OperationCanceledException) {
                // The pipeline was stopped (Ctrl+C); the screen has been restored.
            }
        }

        private void ShowChoices() {
            var definition = new SelectQuestionOptions { Title = "Select an item", Multiple = Multiple.IsPresent };
            var shape = Binding.PSShaping.Options();
            foreach (object? item in _pipelineItems) {
                string label = PSInteractive.ChoiceLabel(item, shape);
                definition.Choices.Add(new Choice(item, label));
            }
            _stop = new CancellationTokenSource();
            try {
                object? answer = Question.Select(definition).Show(PSInteractive.Options(this, Mode), _stop.Token);
                if (Multiple.IsPresent) {
                    foreach (object? value in (IReadOnlyList<object?>)answer!) { WriteObject(value); }
                } else {
                    WriteObject(answer);
                }
            } catch (Exception ex) when (ex is PromptCanceledException || ex is PromptUnavailableException || ex is Input.InputClosedException) {
                PSInteractive.Fail(this, ex, "Select an item");
            } catch (OperationCanceledException) {
                // The pipeline was stopped.
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
