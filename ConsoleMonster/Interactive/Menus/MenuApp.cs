using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A menu running as an app on the alternate screen (Shell or Classic layout): navigation with a breadcrumb, panes kept
    /// per item (selections and form values survive navigation), dialogs, the palette, toasts, runs into a live dashboard
    /// and back, and prompts from actions shown in front of the app.
    /// </summary>
    internal sealed partial class MenuApp : InteractiveView, IViewHost, IMenuHost {
        private readonly Menu _root;
        private readonly MenuOptions _options;
        private readonly ChoiceStore? _store;
        private readonly IInputSource _input;
        private readonly ScreenSurface? _screen;
        private readonly IPromptSurface _surface;
        private readonly RenderContext _context;
        private readonly CancellationToken _token;
        private readonly Func<string, bool> _copy;
        private readonly List<Modal> _modals = new List<Modal>();
        private readonly Dictionary<MenuItem, IPane> _panes = new Dictionary<MenuItem, IPane>();
        private readonly Dictionary<Menu, MenuListPane> _lists = new Dictionary<Menu, MenuListPane>();
        private readonly List<Location> _stack = new List<Location>();
        private readonly MenuListPane _sidebar;
        private bool _contentFocused;
        private MenuItem? _sidebarItem;
        private Task? _asyncAction;
        private CancellationTokenSource? _asyncCancel;
        private ActionPane? _asyncPane;
        private MenuItem? _asyncItem;

        public MenuApp(Menu root, MenuOptions options, PromptLabels labels, ChoiceStore? store, IInputSource input, IPromptSurface surface, RenderContext context, MenuLayout layout, CancellationToken token) : base(labels) {
            _root = root;
            _options = options;
            _store = store;
            _input = input;
            _surface = surface;
            _screen = surface as ScreenSurface;
            _context = context;
            _token = token;
            _copy = InteractiveClipboard.ForTarget((options.Interactive ?? InteractiveOptions.Default).ResolveTarget());
            Layout = layout;
            _sidebar = new MenuListPane(root, ActivateFromSidebar, labels, compact: true, context.Glyphs);
            _sidebar.List.ShowInactiveFocus = true;
            _lists[root] = new MenuListPane(root, Activate, labels, compact: false, context.Glyphs);
            RestoreSelections();
            SyncSidebar();
        }

        public MenuLayout Layout { get; }

        /// <summary>Whether the window is too narrow for the sidebar (it collapses into the breadcrumb).</summary>
        private bool Narrow => LastFrame != null && LastFrame.Width < _options.SidebarMinimumWidth;

        private bool UsesSidebar => Layout == MenuLayout.Shell && !Narrow;

        private Location? Current => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        private IPane? CurrentPane => Current?.Pane;

        public override bool IsAnimating => _modals.Count > 0 && _modals[_modals.Count - 1].IsAnimating || (CurrentPane?.IsAnimating ?? false) || _asyncAction != null;

        // ---- services --------------------------------------------------------------------------------------------

        public void ShowModal(Modal modal) => _modals.Add(modal);

        public T Ask<T>(Prompt<T> prompt, CancellationToken cancellationToken) {
            Func<RenderContext, IPromptSurface?>? previous = PromptHost.Current;
            PromptHost.Current = ResolvePromptSurface;
            try {
                return prompt.Show(PromptOptions(), cancellationToken);
            } finally {
                PromptHost.Current = previous;
                _screen?.Invalidate();
            }
        }

        private InteractiveOptions PromptOptions() {
            InteractiveOptions o = (_options.Interactive ?? InteractiveOptions.Default).Clone();
            o.Mode = InteractionMode.Rich;
            o.Input = _input;
            o.Labels = Labels;
            return o;
        }

        private IPromptSurface? ResolvePromptSurface(RenderContext context) {
            return _screen == null ? null : new BoxedSurface(_screen, LastRendered, context);
        }

        // A run stays inside the app: the live session draws the app frame with a run pane as the content (same header,
        // sidebar and footer), on the app's own screen or capture, so nothing clears or jumps between choosing and running.
        public MenuRunResult Run(MenuContext context, IWidget dashboard, Func<RunContext, MenuRunResult?> work, LiveSessionOptions? options) {
            Location? location = null;
            MenuRunResult result = RunHost.Run(this, _surface, _screen, _input, _context, context, dashboard, work, options, pane => {
                // The breadcrumb says the app is running; the dashboard's title names the panel, so it is not said twice.
                location = new Location(Labels.Running, pane, context.Item);
                Push(location);
            }, () => {
                if (location != null) {
                    _stack.Remove(location);
                }
            }, context.Item?.Label ?? _root.Title);
            ShowResult(result);
            return result;
        }
        public void ShowResult(MenuRunResult result) {
            ResultPane pane = new ResultPane(result, Labels, Activate, Back);
            Push(new Location(Labels.Results, pane, null));
        }

        public void Open(MenuItem item) => Activate(item);

        public void Exit() {
            IsDone = true;
        }

        private MenuContext Context(MenuItem? item, FormValues? values = null) => new MenuContext(this, _root, item, values, _token);

        // ---- navigation ------------------------------------------------------------------------------------------

        private sealed class Location {
            public Location(string title, IPane pane, MenuItem? item) {
                Title = title;
                Pane = pane;
                Item = item;
            }

            public string Title { get; }

            /// <summary>The content panel's title when it differs from the breadcrumb (a run: "Running" there, the dashboard here).</summary>
            public string? PanelTitle => (Pane as RunPane)?.Title;

            public IPane Pane { get; }

            public MenuItem? Item { get; }
        }

        private void Push(Location location) {
            _stack.Add(location);
            _contentFocused = true;
        }

        private void Back() {
            if (_stack.Count > 1) {
                _stack.RemoveAt(_stack.Count - 1);
                return;
            }
            if (UsesSidebar) {
                _contentFocused = false;
            }
        }

        private void Home() {
            if (UsesSidebar) {
                SyncSidebar();
                _contentFocused = false;
                return;
            }
            while (_stack.Count > 1) {
                _stack.RemoveAt(_stack.Count - 1);
            }
        }

        // The content area follows the sidebar's focus: moving through the sidebar previews each item's pane.
        private void SyncSidebar() {
            if (Layout != MenuLayout.Shell) {
                if (_stack.Count == 0) {
                    _stack.Add(new Location(_root.Title, _lists[_root], null));
                }
                return;
            }
            MenuItem? item = _sidebar.FocusedItem;
            if (item == null) {
                return;
            }
            if (ReferenceEquals(item, _sidebarItem) && _stack.Count > 0) {
                return;
            }
            _sidebarItem = item;
            _stack.Clear();
            _stack.Add(new Location(item.Label, PaneFor(item), item));
        }

        private IPane PaneFor(MenuItem item) {
            if (_panes.TryGetValue(item, out IPane? existing)) {
                return existing;
            }
            IPane pane;
            switch (item.Kind) {
                case MenuItemKind.Submenu:
                    pane = ListFor(item.Submenu!);
                    break;
                case MenuItemKind.Form:
                    FormView form = new FormView(item.Form!, this, _store, _options.RememberLastChoices, _copy);
                    form.Submitted = result => OnFormSubmitted(item, result);
                    pane = form;
                    break;
                case MenuItemKind.Select:
                case MenuItemKind.MultiSelect:
                    pane = new ChoiceItemPane(item, Labels, () => OnChange(item), Back, () => Context(item));
                    break;
                case MenuItemKind.Page:
                    pane = new PagePane(item, () => Context(item));
                    break;
                case MenuItemKind.Review:
                    pane = new ReviewPane(item, () => Context(item), Labels, action => Invoke(item, action, null), _copy, _store == null ? null : (Action)PromptPresetName, ShowToast, Back);
                    break;
                case MenuItemKind.Presets:
                    pane = new PresetsPane(item, Labels, ListMenuPresets, ApplyMenuPreset, DeleteMenuPreset);
                    break;
                case MenuItemKind.Exit:
                    pane = new ActionPane(item, Labels, RequestExit);
                    break;
                default:
                    pane = new ActionPane(item, Labels, () => RunAction(item));
                    break;
            }
            _panes[item] = pane;
            return pane;
        }

        private MenuListPane ListFor(Menu menu) {
            if (!_lists.TryGetValue(menu, out MenuListPane? list)) {
                list = new MenuListPane(menu, Activate, Labels, compact: false, _context.Glyphs);
                _lists[menu] = list;
            }
            return list;
        }

        private void ActivateFromSidebar(MenuItem item) {
            switch (item.Kind) {
                case MenuItemKind.Action:
                    RunAction(item);
                    return;
                case MenuItemKind.Exit:
                    RequestExit();
                    return;
            }
            SyncSidebar();
            _contentFocused = true;
        }

        private void Activate(MenuItem item) {
            string? reason = item.CurrentDisabledReason;
            if (reason != null) {
                ShowToast(new Toast(PromptLabels.Format(Labels.Disabled, reason), StatusKind.Warn));
                return;
            }
            switch (item.Kind) {
                case MenuItemKind.Action:
                    RunAction(item);
                    return;
                case MenuItemKind.Exit:
                    RequestExit();
                    return;
                case MenuItemKind.Separator:
                    return;
            }
            if (UsesSidebar && _stack.Count > 0 && ReferenceEquals(_stack[0].Item, item)) {
                _contentFocused = true;
                return;
            }
            Push(new Location(item.Label, PaneFor(item), item));
        }

        /// <summary>
        /// Leaves the current pane, asking first when it is a form with changes that were not saved: Discard puts back the
        /// values it had when it was opened or last saved, so nothing unsaved leaks into reviews and runs; No stays.
        /// </summary>
        private void LeaveForm(Action leave) {
            if (CurrentPane is FormView form && form.IsDirty) {
                ShowModal(new ConfirmDialog(Labels.Discard, PromptLabels.Format(Labels.DiscardFormChanges, form.Title), () => {
                    form.Revert();
                    leave();
                }, Labels, true));
                return;
            }
            leave();
        }

        private void RequestExit() {
            bool dirty = false;
            foreach (IPane pane in _panes.Values) {
                dirty |= pane.IsDirty;
            }
            if (dirty && _options.ConfirmExit) {
                ShowModal(new ConfirmDialog(Labels.Exit, Labels.DiscardChanges, () => IsDone = true, Labels, true));
                return;
            }
            IsDone = true;
        }

        // ---- actions ---------------------------------------------------------------------------------------------

        private void RunAction(MenuItem item) {
            if (item.ActionAsync != null) {
                StartAsync(item);
                return;
            }
            if (item.Action == null) {
                return;
            }
            Invoke(item, ctx => item.Action(ctx), null);
        }

        private void Invoke(MenuItem? item, Action<MenuContext> action, FormResult? form) {
            Func<RenderContext, IPromptSurface?>? previous = PromptHost.Current;
            PromptHost.Current = ResolvePromptSurface;
            bool collecting = BeginOutput(item?.Label ?? _root.Title);
            try {
                MenuContext context = Context(item, form?.Values);
                context.FormResult = form;
                action(context);
            } catch (PromptCanceledException) {
                // The user backed out of a prompt the action asked; nothing else to do.
            } catch (Exception ex) {
                ShowResult(new MenuRunResult(item?.Label ?? _root.Title, StatusKind.Fail) { Error = ex });
            } finally {
                PromptHost.Current = previous;
                if (collecting) {
                    EndOutput();
                }
                _screen?.Invalidate();
            }
        }

        private void StartAsync(MenuItem item) {
            if (_asyncAction != null) {
                return;
            }
            _asyncCancel = CancellationTokenSource.CreateLinkedTokenSource(_token);
            _asyncItem = item;
            _asyncPane = PaneFor(item) as ActionPane;
            if (_asyncPane != null) {
                _asyncPane.Running = true;
                _asyncPane.Cancel = () => _asyncCancel?.Cancel();
            }
            MenuContext context = Context(item);
            CancellationToken token = _asyncCancel.Token;
            BeginOutput(item.Label);
            _asyncAction = Task.Run(() => item.ActionAsync!(context, token), token);
        }

        private bool PollAsync() {
            Task? task = _asyncAction;
            if (task == null || !task.IsCompleted) {
                return false;
            }
            _asyncAction = null;
            if (_asyncPane != null) {
                _asyncPane.Running = false;
                _asyncPane.Cancel = null;
            }
            EndOutput();
            if (task.IsCanceled || (task.IsFaulted && task.Exception!.GetBaseException() is PromptCanceledException)) {
                ShowToast(new Toast(_asyncItem?.Label + ": " + Labels.Canceled, StatusKind.Warn));
            } else if (task.IsFaulted) {
                ShowResult(new MenuRunResult(_asyncItem?.Label ?? _root.Title, StatusKind.Fail) { Error = task.Exception!.GetBaseException() });
            }
            _asyncCancel?.Dispose();
            _asyncCancel = null;
            return true;
        }

        private void OnFormSubmitted(MenuItem item, FormResult result) {
            if (item.OnSubmit == null) {
                ShowToast(new Toast(item.Label));
                return;
            }
            Invoke(item, ctx => item.OnSubmit(ctx, result), result);
        }

        private void OnChange(MenuItem item) {
            if (item.OnChange != null) {
                Invoke(item, item.OnChange, null);
            }
            SaveSelections();
        }

        private const string SelectionKey = "menu";

        private void SaveSelections() {
            if (_store == null || !_options.RememberLastChoices) {
                return;
            }
            Dictionary<string, object?> values = new Dictionary<string, object?>(StringComparer.Ordinal);
            MenuPresets.Collect(_root, string.Empty, values);
            try {
                _store.SaveLast(SelectionKey, values);
            } catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException) {
                ShowToast(new Toast(ex.Message, StatusKind.Warn));
            }
        }

        private void RestoreSelections() {
            if (_store == null || !_options.RememberLastChoices || !(_store.LoadLast(SelectionKey) is IDictionary<string, object?> values)) {
                return;
            }
            MenuPresets.Restore(_root, string.Empty, values);
        }

        // ---- palette ---------------------------------------------------------------------------------------------

        private void OpenPalette() {
            List<PaletteCommand> commands = new List<PaletteCommand>();
            AddNavigation(_root, new List<string>(), commands);
            foreach (KeyValuePair<MenuItem, IPane> pair in _panes) {
                if (pair.Value is FormView form) {
                    foreach (string preset in form.PresetNames()) {
                        string name = preset;
                        MenuItem item = pair.Key;
                        commands.Add(new PaletteCommand(name, _ => {
                            Activate(item);
                            form.ApplyPreset(name);
                        }) {
                            Category = Labels.Presets + " " + InteractiveGlyphs.For(_context).Breadcrumb + " " + item.Label,
                            Description = form.IsBuiltInPreset(name) ? Labels.BuiltIn : null
                        });
                    }
                }
            }
            AddUnopenedFormPresets(_root, commands);
            foreach (ICommandProvider provider in _root.CommandProviders) {
                try {
                    foreach (PaletteCommand command in provider.GetCommands()) {
                        if (command.Category == null) {
                            command.Category = provider.Name;
                        }
                        commands.Add(command);
                    }
                } catch (Exception ex) {
                    ShowToast(new Toast(provider.Name + ": " + ex.Message, StatusKind.Warn));
                }
            }
            commands.Add(new PaletteCommand(Labels.Home, _ => Home()) { Category = Labels.AppCommands, Keys = "ctrl+home" });
            commands.Add(new PaletteCommand(Labels.Keys, _ => KeyMapOpen = true) { Category = Labels.AppCommands, Keys = "?" });
            bool hasExit = false;
            foreach (MenuItem item in _root.Items) {
                hasExit |= item.Kind == MenuItemKind.Exit;
            }
            if (!hasExit) {
                commands.Add(new PaletteCommand(Labels.Exit, _ => RequestExit()) { Category = Labels.AppCommands, Keys = "ctrl+q" });
            }
            ShowModal(new PaletteModal(commands, command => LeaveForm(() => Invoke(null, command.Invoke, null)), Labels));
        }

        private void AddNavigation(Menu menu, List<string> path, List<PaletteCommand> commands) {
            string separator = " " + InteractiveGlyphs.For(_context).Breadcrumb + " ";
            foreach (MenuItem item in menu.Items) {
                if (item.Kind == MenuItemKind.Separator) {
                    continue;
                }
                MenuItem target = item;
                List<MenuItem> chain = new List<MenuItem>();
                commands.Add(new PaletteCommand(item.Label, _ => NavigateTo(target)) {
                    Category = path.Count == 0 ? Labels.GoTo : Labels.GoTo + separator + string.Join(separator, path.ToArray()),
                    Description = item.Description,
                    Keys = item.Hotkey.HasValue ? item.Hotkey.Value.ToString() : null
                });
                if (item.Submenu != null) {
                    path.Add(item.Label);
                    AddNavigation(item.Submenu, path, commands);
                    path.RemoveAt(path.Count - 1);
                }
            }
        }

        // Opens an item wherever it lives: rebuilds the location stack along its path from the root.
        private void NavigateTo(MenuItem target) {
            List<MenuItem> path = new List<MenuItem>();
            if (!FindPath(_root, target, path)) {
                Activate(target);
                return;
            }
            if (UsesSidebar) {
                _sidebar.Focus(path[0]);
                _sidebarItem = null;
                SyncSidebar();
            } else {
                Home();
                if (path.Count > 0) {
                    _lists[_root].Focus(path[0]);
                }
            }
            for (int i = UsesSidebar ? 1 : 0; i < path.Count; i++) {
                MenuItem step = path[i];
                if (i > 0 && _stack.Count > 0 && _stack[_stack.Count - 1].Pane is MenuListPane list) {
                    list.Focus(step);
                }
                if (step.Kind == MenuItemKind.Action || step.Kind == MenuItemKind.Exit) {
                    if (i == path.Count - 1) {
                        Activate(step);
                    }
                    continue;
                }
                Push(new Location(step.Label, PaneFor(step), step));
            }
            _contentFocused = true;
        }

        private static bool FindPath(Menu menu, MenuItem target, List<MenuItem> path) {
            foreach (MenuItem item in menu.Items) {
                path.Add(item);
                if (ReferenceEquals(item, target) || (item.Submenu != null && FindPath(item.Submenu, target, path))) {
                    return true;
                }
                path.RemoveAt(path.Count - 1);
            }
            return false;
        }

        // ---- loop hooks ------------------------------------------------------------------------------------------

        public override bool Poll(TimeSpan now) {
            bool changed = base.Poll(now);
            changed |= _modals.RemoveAll(m => m.IsClosed) > 0;
            foreach (IPane pane in _panes.Values) {
                changed |= pane.Poll(now);
            }
            if (_modals.Count > 0) {
                changed |= _modals[_modals.Count - 1].Poll(now);
            }
            changed |= DrainCalls();
            changed |= PollAsync();
            return changed;
        }

        protected override bool HandleUnbound(InputEvent input) {
            if (_modals.Count > 0) {
                return _modals[_modals.Count - 1].HandleUnbound(input);
            }
            bool handled = UsesSidebar && !_contentFocused ? _sidebar.HandleUnbound(input) : CurrentPane?.HandleUnbound(input) ?? false;
            if (UsesSidebar && !_contentFocused) {
                SyncSidebar();
            }
            return handled;
        }
    }
}
