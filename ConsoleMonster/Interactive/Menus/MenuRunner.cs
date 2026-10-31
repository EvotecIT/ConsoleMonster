using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>Runs a menu in the layout and interaction mode that fit the session.</summary>
    internal static class MenuRunner {
        public static void Run(Menu menu, MenuOptions options, CancellationToken cancellationToken) {
            InteractiveOptions interactive = options.Interactive ?? InteractiveOptions.Default;
            PromptLabels labels = interactive.ResolveLabels();
            InteractionMode mode = interactive.ResolveMode();
            ChoiceStore store = new ChoiceStore(options.AppName ?? menu.Title, options.StorageRoot);
            if (mode == InteractionMode.None || mode == InteractionMode.Snapshot) {
                throw new PromptUnavailableException("Menu '" + menu.Title + "' needs an interactive session.") { Prompt = menu.Title };
            }
            MenuLayout layout = options.Layout != MenuLayout.Auto ? options.Layout : menu.Layout != MenuLayout.Auto ? menu.Layout : menu.HasContent ? MenuLayout.Shell : MenuLayout.Classic;
            if (mode != InteractionMode.Rich || layout == MenuLayout.Inline) {
                new InlineMenu(menu, options, labels, store, cancellationToken).Run();
                return;
            }
            using (InputLease lease = ConsoleInput.Acquire(interactive.Input)) {
                Capabilities.TerminalProfile profile = interactive.ResolveProfile();
                RenderContext context = Terminal.CreateContext(profile, interactive.ResolveTheme());
                IPromptSurface surface = interactive.Surface ?? new ScreenSurface(profile, interactive.ResolveSurfaceWriter(), interactive.ResolveSurfaceSize());
                try {
                    MenuApp app = new MenuApp(menu, options, labels, store, lease.Source, surface, context, layout, cancellationToken);
                    try {
                        InteractionLoop.Run(app, lease.Source, surface, context, cancellationToken);
                    } finally {
                        app.Shutdown();
                    }
                    surface.Complete(null);
                } finally {
                    if (interactive.Surface == null) {
                        surface.Dispose();
                    }
                }
            }
        }
    }

    /// <summary>
    /// A menu in the scrollback (and the line-based and accessible modes): each step is a selection prompt; actions run
    /// and print as usual; forms and selections are asked inline; the menu comes back until Exit.
    /// </summary>
    internal sealed class InlineMenu : IMenuHost {
        private readonly Menu _root;
        private readonly MenuOptions _options;
        private readonly PromptLabels _labels;
        private readonly ChoiceStore _store;
        private readonly CancellationToken _token;
        private readonly List<Menu> _stack = new List<Menu>();
        private bool _exit;

        public InlineMenu(Menu root, MenuOptions options, PromptLabels labels, ChoiceStore store, CancellationToken token) {
            _root = root;
            _options = options;
            _labels = labels;
            _store = store;
            _token = token;
        }

        public MenuLayout Layout => MenuLayout.Inline;

        // Inline menus live in the scrollback, so action output is simply written there.
        public void Log(string text, StatusKind status) {
            Terminal.WriteRich(status == StatusKind.Info || status == StatusKind.Pass ? RichText.Plain(text) : RichText.Plain(text, new RichStyle(status == StatusKind.Fail ? ThemeToken.Fail : ThemeToken.Warn)));
        }

        private InteractiveOptions Interactive => _options.Interactive ?? InteractiveOptions.Default;

        public void Run() {
            _stack.Add(_root);
            while (!_exit && _stack.Count > 0) {
                _token.ThrowIfCancellationRequested();
                Menu menu = _stack[_stack.Count - 1];
                List<Choice> choices = new List<Choice>();
                string? group = null;
                foreach (MenuItem item in menu.Items) {
                    if (item.Kind == MenuItemKind.Separator) {
                        group = string.IsNullOrEmpty(item.Label) ? null : item.Label;
                        continue;
                    }
                    RichText? badge = item.CurrentBadge;
                    string? state = item.CurrentDetail;
                    choices.Add(new Choice(item, item.Label) {
                        Group = group,
                        Hotkey = item.Hotkey,
                        Description = item.Description,
                        DisabledReason = item.CurrentDisabledReason,
                        Badge = badge ?? (string.IsNullOrEmpty(state) ? null : RichText.Plain(state!, new RichStyle(ThemeToken.Muted)))
                    });
                }
                if (_stack.Count > 1) {
                    choices.Add(new Choice(null, _labels.Back) { Hotkey = HasHotkeys(choices) ? 'b' : (char?)null });
                }
                string title = _stack[0].Title;
                for (int i = 1; i < _stack.Count; i++) {
                    title += " › " + _stack[i].Title;
                }
                SelectPrompt<object?> prompt = new SelectPrompt<object?>(title, choices) { Description = menu.Description, PageSize = 14 };
                object? chosen;
                try {
                    chosen = prompt.Show(Interactive, _token);
                } catch (PromptCanceledException) {
                    if (_stack.Count > 1) {
                        _stack.RemoveAt(_stack.Count - 1);
                        continue;
                    }
                    return;
                }
                if (!(chosen is MenuItem picked)) {
                    _stack.RemoveAt(_stack.Count - 1);
                    continue;
                }
                Open(picked);
            }
        }

        private static bool HasHotkeys(List<Choice> choices) {
            foreach (Choice c in choices) {
                if (c.Hotkey.HasValue) {
                    return true;
                }
            }
            return false;
        }

        public void Open(MenuItem item) {
            MenuContext context = new MenuContext(this, _root, item, null, _token);
            try {
                if ((item.Kind == MenuItemKind.Select || item.Kind == MenuItemKind.MultiSelect) && item.LoadChoices != null && item.Choices.Count == 0) {
                    item.ReplaceChoices(item.LoadChoices(context, _token).GetAwaiter().GetResult() ?? Array.Empty<Choice>());
                }
                switch (item.Kind) {
                    case MenuItemKind.Exit:
                        _exit = true;
                        return;
                    case MenuItemKind.Submenu:
                        _stack.Add(item.Submenu!);
                        return;
                    case MenuItemKind.Action:
                        if (item.Action != null) {
                            item.Action(context);
                        } else if (item.ActionAsync != null) {
                            item.ActionAsync(context, _token).GetAwaiter().GetResult();
                        }
                        return;
                    case MenuItemKind.Form:
                        FormResult result = item.Form!.Show(new FormOptions {
                            Interactive = Interactive,
                            Inline = true,
                            AppName = _store.AppName,
                            StorageRoot = _options.StorageRoot,
                            RememberLastChoices = _options.RememberLastChoices
                        }, _token);
                        if (result.Submitted && item.OnSubmit != null) {
                            MenuContext submitted = new MenuContext(this, _root, item, result.Values, _token) { FormResult = result };
                            item.OnSubmit(submitted, result);
                        }
                        return;
                    case MenuItemKind.Select: {
                        SelectPrompt<object?> select = new SelectPrompt<object?>(item.Label, item.Choices) { Description = item.Description };
                        if (item.SelectedValue != null) {
                            select.Default = item.SelectedValue;
                        }
                        item.SetSelection(new[] { select.Show(Interactive, _token) });
                        item.OnChange?.Invoke(context);
                        return;
                    }
                    case MenuItemKind.MultiSelect: {
                        foreach (Choice c in item.Choices) {
                            c.Selected = false;
                            foreach (object? v in item.Selection) {
                                if (Equals(c.Value, v)) {
                                    c.Selected = true;
                                }
                            }
                        }
                        MultiSelectPrompt<object?> multi = new MultiSelectPrompt<object?>(item.Label, item.Choices) { Description = item.Description, Minimum = item.Minimum, Maximum = item.Maximum };
                        item.SetSelection(multi.Show(Interactive, _token));
                        item.OnChange?.Invoke(context);
                        return;
                    }
                    case MenuItemKind.Page:
                        if (item.Page != null) {
                            Terminal.Write(item.Page(context));
                        }
                        return;
                    case MenuItemKind.Presets:
                        ChoosePreset(item);
                        return;
                }
            } catch (PromptCanceledException) {
                // Backing out of a prompt returns to the menu.
            }
        }

        // The presets as a selection, as the Shell and Classic layouts list them: the built-in ones first, with their
        // preview, then the saved ones. Picking one applies it (selections, then the forms' values); forms opened after
        // show its values.
        private void ChoosePreset(MenuItem item) {
            IReadOnlyList<PresetListing> presets = MenuPresets.List(_root, _store);
            if (presets.Count == 0) {
                Log(_labels.NoPresets, StatusKind.Info);
                return;
            }
            List<Choice> choices = new List<Choice>();
            foreach (PresetListing preset in presets) {
                choices.Add(new Choice(preset.Name, preset.Name) {
                    Description = preset.Description,
                    Preview = preset.Preview,
                    Badge = preset.BuiltIn ? RichText.Plain(_labels.BuiltIn, new RichStyle(ThemeToken.Muted)) : null
                });
            }
            choices.Add(new Choice(null, _labels.Back));
            SelectPrompt<object?> prompt = new SelectPrompt<object?>(item.Label, choices) { Description = item.Description, PageSize = 14 };
            if (prompt.Show(Interactive, _token) is string name && MenuPresets.Apply(_root, _store, name)) {
                ShowToast(new Toast(PromptLabels.Format(_labels.PresetApplied, name), StatusKind.Info));
            }
        }

        public void ShowToast(Toast toast) {
            Terminal.WriteMarkupLine("[" + StatusName(toast.Status) + "]" + Markup.Escape(toast.Text) + "[/]");
        }

        private static string StatusName(StatusKind status) {
            switch (status) {
                case StatusKind.Fail:
                    return "fail";
                case StatusKind.Warn:
                    return "warn";
                case StatusKind.Info:
                    return "info";
                default:
                    return "pass";
            }
        }

        public T Ask<T>(Prompt<T> prompt, CancellationToken cancellationToken) => prompt.Show(Interactive, cancellationToken);

        public MenuRunResult Run(MenuContext context, IWidget dashboard, Func<RunContext, MenuRunResult?> work, LiveSessionOptions? options) {
            LiveSession session = new LiveSession(dashboard, options ?? new LiveSessionOptions());
            MenuRunResult result;
            session.Start();
            try {
                result = work(new RunContext(session, context)) ?? new MenuRunResult(context.Item?.Label ?? _root.Title);
            } catch (OperationCanceledException) {
                result = new MenuRunResult(context.Item?.Label ?? _root.Title, StatusKind.Warn) { Summary = _labels.Canceled };
            } catch (Exception ex) {
                result = new MenuRunResult(context.Item?.Label ?? _root.Title, StatusKind.Fail) { Error = ex };
            } finally {
                session.Stop();
            }
            // The run's time on the session clock: real time, or the simulated clock the options give.
            if (result.Elapsed == TimeSpan.Zero) {
                result.Elapsed = session.Elapsed;
            }
            ShowResult(result);
            return result;
        }

        public void ShowResult(MenuRunResult result) {
            Terminal.Write(new ResultSummary(result, _labels));
            if (result.NextActions.Count == 0) {
                return;
            }
            List<Choice> choices = new List<Choice>();
            foreach (MenuItem item in result.NextActions) {
                choices.Add(new Choice(item, item.Label) { Description = item.Description });
            }
            choices.Add(new Choice(null, _labels.Back));
            try {
                if (new SelectPrompt<object?>(_labels.NextActions, choices).Show(Interactive, _token) is MenuItem next) {
                    Open(next);
                }
            } catch (PromptCanceledException) {
                // Back to the menu.
            }
        }

        public void Exit() => _exit = true;
    }

    /// <summary>A run result as print-once output: the status line, summary, figures and details.</summary>
    internal sealed class ResultSummary : IWidget {
        private readonly MenuRunResult _result;
        private readonly PromptLabels _labels;

        public ResultSummary(MenuRunResult result, PromptLabels labels) {
            _result = result;
            _labels = labels;
        }

        private Rows Content() {
            Rows rows = new Rows();
            rows.Add(new UiLine(context => new StyledLine(new[] {
                new TextSpan(context.Glyphs.StatusMarker(_result.Status) + " ", context.Style(_result.Status).With(TextDecoration.Bold)),
                new TextSpan(_result.Title, context.Style(ThemeToken.Title)),
                new TextSpan(_result.Elapsed > TimeSpan.Zero ? "  " + Widgets.NumberFormat.Clock(_result.Elapsed) : string.Empty, context.Style(ThemeToken.Muted))
            })));
            if (!string.IsNullOrEmpty(_result.Summary)) {
                rows.Add(new Padding(new UiParagraph(_result.Summary!, ThemeToken.Text, 6), 2, 0, 0, 0));
            }
            if (_result.Error != null) {
                rows.Add(new Padding(new ConsoleMonster.Widgets.ExceptionView(_result.Error) { MaxFrames = 3, MaxInner = 1 }, 2, 0, 0, 0));
            }
            if (_result.Figures.Count > 0) {
                rows.Add(new UiLine(context => {
                    List<TextSpan> spans = new List<TextSpan>();
                    foreach (KeyValuePair<string, string> figure in _result.Figures) {
                        if (spans.Count > 0) {
                            spans.Add(new TextSpan("     ", Style.Plain));
                        }
                        spans.Add(new TextSpan(figure.Value + " ", _result.FigureStatus.TryGetValue(figure.Key, out StatusKind status) ? context.Style(status).With(TextDecoration.Bold) : context.Style(ThemeToken.Highlight)));
                        spans.Add(new TextSpan(figure.Key, context.Style(ThemeToken.Muted)));
                    }
                    return new StyledLine(spans);
                }) { Indent = 2 });
            }
            if (_result.Details != null) {
                rows.Add(new Padding(_result.Details, 2, 0, 0, 0));
            }
            return rows;
        }

        public Size Measure(Constraints constraints, RenderContext context) => Content().Measure(constraints, context);

        public void Render(Region region, RenderContext context) => Content().Render(region, context);
    }
}
