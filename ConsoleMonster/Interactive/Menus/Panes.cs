using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>The items of a menu as a list (sidebar, classic menu, or a submenu in the content area).</summary>
    internal sealed class MenuListPane : IPane {
        private readonly Menu _menu;
        private readonly List<MenuItem> _items = new List<MenuItem>();
        private readonly Dictionary<MenuItem, Choice> _choices = new Dictionary<MenuItem, Choice>();
        private readonly Action<MenuItem> _activate;
        private readonly PromptLabels _labels;
        private readonly bool _compact;
        private readonly IGlyphSet _glyphs;

        public MenuListPane(Menu menu, Action<MenuItem> activate, PromptLabels labels, bool compact, IGlyphSet glyphs) {
            _glyphs = glyphs;
            _menu = menu;
            _activate = activate;
            _labels = labels;
            _compact = compact;
            string? group = null;
            List<Choice> choices = new List<Choice>();
            foreach (MenuItem item in menu.Items) {
                if (item.Kind == MenuItemKind.Separator) {
                    group = string.IsNullOrEmpty(item.Title) ? " " : item.Title;
                    continue;
                }
                Choice choice = new Choice(item, Label(item)) { Group = group, Hotkey = item.HotkeyChar };
                _choices[item] = choice;
                _items.Add(item);
                choices.Add(choice);
            }
            List = new ChoiceList(choices, false) { Labels = labels };
            Pane = new ChoicePane(List, new CaretSlot()) { Fill = true, ShowFilterRow = !compact, ShowInfoLine = !compact };
            Refresh();
        }

        public Menu Menu => _menu;

        public ChoiceList List { get; }

        public ChoicePane Pane { get; }

        public string Title => _menu.Title;

        public string? Caption => null;

        public bool IsAnimating => false;

        public bool IsDirty => false;

        public IReadOnlyList<string> Crumbs => new string[0];

        public bool WantsText => !List.Filter.IsEmpty || List.FilterMode;

        public MenuItem? FocusedItem => List.FocusedChoice?.Value as MenuItem;

        public void Focus(MenuItem item) {
            if (_choices.TryGetValue(item, out Choice? choice)) {
                List.FocusChoice(choice);
            }
        }

        private string Label(MenuItem item) {
            string? icon = item.IconText(_glyphs);
            return icon == null ? item.Title : icon + " " + item.Title;
        }

        /// <summary>Re-reads badges, states and disabled reasons (they are computed by the app).</summary>
        public void Refresh() {
            foreach (MenuItem item in _items) {
                Choice choice = _choices[item];
                choice.Title = Label(item);
                choice.DisabledReason = item.CurrentDisabledReason;
                choice.Description = item.Description;
                RichText? badge = item.CurrentBadge;
                string? state = item.CurrentState;
                bool opens = item.Kind == MenuItemKind.Submenu || item.Kind == MenuItemKind.Form || item.Kind == MenuItemKind.Select || item.Kind == MenuItemKind.MultiSelect || item.Kind == MenuItemKind.Page || item.Kind == MenuItemKind.Review || item.Kind == MenuItemKind.Presets;
                RichTextBuilder builder = new RichTextBuilder();
                bool any = false;
                if (!string.IsNullOrEmpty(state)) {
                    builder.Append(state!, ThemeToken.Muted);
                    any = true;
                }
                if (badge != null) {
                    if (any) {
                        builder.Append("  ");
                    }
                    builder.Append(badge);
                    any = true;
                }
                if (opens && !_compact) {
                    builder.Append(any ? "  ›" : "›", ThemeToken.Subtle);
                    any = true;
                }
                choice.Badge = any ? builder.Build() : null;
            }
            List.Invalidate();
        }

        public IWidget Build(ViewFrame frame, bool focused) {
            Refresh();
            List.Focused = focused;
            Rows rows = new Rows();
            if (!_compact && !string.IsNullOrEmpty(_menu.Description)) {
                rows.Add(new UiParagraph(_menu.Description!, ThemeToken.Muted, 2));
                rows.Add(UiLine.Empty);
            }
            rows.Add(new CaretBridge(Pane, frame.Caret, focused), LayoutSize.Fraction());
            return rows;
        }

        public void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            List.AddBindings(bindings, frame.Context);
            bindings.Add(new KeyBinding(_labels.KeyOpen, () => {
                MenuItem? item = FocusedItem;
                if (item != null) {
                    Activate(item);
                }
            }, new KeyChord(ConsoleKey.Enter)) { Order = 20, Group = "Menu" });
        }

        private void Activate(MenuItem item) {
            string? reason = item.CurrentDisabledReason;
            if (reason != null) {
                List.Message = PromptLabels.Format(_labels.Disabled, reason);
                return;
            }
            _activate(item);
        }

        public bool HandleUnbound(InputEvent input) {
            if (List.HandleText(input, out Choice? picked)) {
                if (picked?.Value is MenuItem item) {
                    Activate(item);
                }
                return true;
            }
            return false;
        }

        public bool Poll(TimeSpan now) => false;

        public bool Back() => List.ClearFilter();
    }

    /// <summary>Passes a pane's caret into the frame's slot, or hides it while the pane is not focused.</summary>
    internal sealed class CaretBridge : IWidget {
        private readonly ChoicePane _pane;
        private readonly CaretSlot _outer;
        private readonly bool _focused;
        private readonly CaretSlot _inner = new CaretSlot();

        public CaretBridge(ChoicePane pane, CaretSlot outer, bool focused) {
            _pane = pane;
            _outer = outer;
            _focused = focused;
        }

        public Size Measure(Constraints constraints, RenderContext context) => _pane.Measure(constraints, context);

        public void Render(Region region, RenderContext context) {
            _pane.Caret = _inner;
            _inner.Clear();
            _pane.Render(region, context);
            if (_focused && _inner.Caret.HasValue && (!_pane.List.Filter.IsEmpty || _pane.List.FilterMode || !_pane.List.HasHotkeys)) {
                _outer.Set(_inner.Caret.Value.X, _inner.Caret.Value.Y);
            }
        }
    }

    /// <summary>A selection item in the content area: the list, its preview and count; changes apply to the item at once.</summary>
    internal sealed class ChoiceItemPane : IPane {
        private readonly MenuItem _item;
        private readonly PromptLabels _labels;
        private readonly Action _changed;
        private readonly Action _back;
        private readonly Func<MenuContext>? _context;
        private System.Threading.Tasks.Task<IEnumerable<Choice>>? _load;
        private System.Threading.CancellationTokenSource? _cancel;
        private string? _loadError;

        public ChoiceItemPane(MenuItem item, PromptLabels labels, Action changed, Action back, Func<MenuContext>? context = null) {
            _item = item;
            _labels = labels;
            _changed = changed;
            _back = back;
            _context = context;
            bool multi = item.Kind == MenuItemKind.MultiSelect;
            foreach (Choice c in item.Choices) {
                c.Selected = false;
            }
            List = new ChoiceList(item.Choices, multi) { Labels = labels, Minimum = item.Minimum, Maximum = item.Maximum };
            foreach (object? value in item.Selection) {
                foreach (Choice c in item.Choices) {
                    if (Equals(c.Value, value)) {
                        if (multi) {
                            List.SetSelected(c, true);
                        } else {
                            List.FocusChoice(c);
                        }
                    }
                }
            }
            Pane = new ChoicePane(List, new CaretSlot()) { Fill = true, Preview = item.Preview, ShowCount = false };
            if (item.ChoicesLoader != null && item.Choices.Count == 0) {
                StartLoad();
            }
        }

        public bool Loading => _load != null && !_load.IsCompleted;

        private void StartLoad() {
            if (_item.ChoicesLoader == null || _context == null) {
                return;
            }
            try {
                _cancel?.Cancel();
            } catch (ObjectDisposedException) {
            }
            _cancel = new System.Threading.CancellationTokenSource();
            System.Threading.CancellationToken token = _cancel.Token;
            MenuContext context = _context();
            Func<MenuContext, System.Threading.CancellationToken, System.Threading.Tasks.Task<IEnumerable<Choice>>> loader = _item.ChoicesLoader;
            _loadError = null;
            try {
                _load = loader(context, token) ?? System.Threading.Tasks.Task.FromResult<IEnumerable<Choice>>(new Choice[0]);
            } catch (Exception ex) {
                System.Threading.Tasks.TaskCompletionSource<IEnumerable<Choice>> failed = new System.Threading.Tasks.TaskCompletionSource<IEnumerable<Choice>>();
                failed.SetException(ex);
                _load = failed.Task;
            }
        }

        public ChoiceList List { get; }

        public ChoicePane Pane { get; }

        public string Title => _item.Title;

        public string? Caption => Loading ? null : List.Multi ? PromptLabels.Format(_labels.SelectedCount, List.SelectedCount, List.Total) : null;

        public bool IsAnimating => Loading;

        public bool IsDirty => false;

        public IReadOnlyList<string> Crumbs => new string[0];

        public bool WantsText => !List.Filter.IsEmpty || List.FilterMode;

        public IWidget Build(ViewFrame frame, bool focused) {
            List.Focused = focused;
            Rows rows = new Rows();
            if (!string.IsNullOrEmpty(_item.Description)) {
                rows.Add(new UiParagraph(_item.Description!, ThemeToken.Muted, 2));
                rows.Add(UiLine.Empty);
            }
            if (Loading) {
                rows.Add(new UiLine(context => {
                    IReadOnlyList<string> frames = context.Glyphs.SpinnerFrames;
                    return new StyledLine(new[] {
                        new TextSpan(frames[(int)(context.Tick % frames.Count)] + " ", context.Style(ThemeToken.Running)),
                        new TextSpan(_labels.Loading, context.Style(ThemeToken.Muted).With(TextDecoration.Italic))
                    });
                }));
                return rows;
            }
            if (_loadError != null) {
                rows.Add(new UiLine(context => StyledLine.From(_loadError, context.Style(ThemeToken.Warn))));
                return rows;
            }
            rows.Add(new CaretBridge(Pane, frame.Caret, focused), LayoutSize.Fraction());
            return rows;
        }

        public void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            if (_item.ChoicesLoader != null) {
                bindings.Add(new KeyBinding("reload", StartLoad, new KeyChord(ConsoleKey.R, KeyModifiers.Control)) { KeyText = "ctrl+r", Order = 65, Group = "Selection", InFooter = Loading || _loadError != null });
            }
            if (Loading) {
                bindings.Add(new KeyBinding(_labels.KeyCancel, () => {
                    _cancel?.Cancel();
                    _load = null;
                    _loadError = _labels.Cancelled;
                }, new KeyChord(ConsoleKey.Escape)) { Order = 20 });
                return;
            }
            List.AddBindings(bindings, frame.Context);
            if (List.Multi) {
                bindings.Add(new KeyBinding(_labels.KeyConfirm, () => {
                    string? problem = List.ValidateSelection();
                    if (problem != null) {
                        Pane.Error = problem;
                        return;
                    }
                    _back();
                }, new KeyChord(ConsoleKey.Enter)) { Order = 25, Group = "Selection" });
            } else {
                bindings.Add(new KeyBinding(_labels.KeySelect, Choose, new KeyChord(ConsoleKey.Enter)) { Order = 25, Group = "Selection" });
            }
        }

        private void Choose() {
            Choice? focused = List.FocusedChoice;
            if (focused == null) {
                return;
            }
            if (!focused.IsEnabled) {
                List.Message = PromptLabels.Format(_labels.Disabled, focused.DisabledReason);
                return;
            }
            _item.SetSelection(new[] { focused.Value });
            _changed();
            _back();
        }

        /// <summary>Copies the list's selection to the item.</summary>
        public void Sync() {
            if (!List.Multi) {
                return;
            }
            List<object?> values = new List<object?>();
            foreach (Choice c in List.SelectedChoices) {
                values.Add(c.Value);
            }
            bool changed = values.Count != _item.Selection.Count;
            for (int i = 0; !changed && i < values.Count; i++) {
                changed = !Equals(values[i], _item.Selection[i]);
            }
            if (changed) {
                _item.SetSelection(values);
                Pane.Error = null;
                _changed();
            }
        }

        public bool HandleUnbound(InputEvent input) {
            if (List.HandleText(input, out Choice? picked)) {
                Pane.InvalidatePreview();
                if (picked != null) {
                    if (List.Multi) {
                        List.SetSelected(picked, !List.IsSelected(picked));
                    } else {
                        Choose();
                    }
                }
                return true;
            }
            return false;
        }

        public bool Poll(TimeSpan now) {
            System.Threading.Tasks.Task<IEnumerable<Choice>>? load = _load;
            if (load == null || !load.IsCompleted) {
                return false;
            }
            _load = null;
            if (load.IsCanceled) {
                _loadError = _labels.Cancelled;
            } else if (load.IsFaulted) {
                _loadError = PromptLabels.Format(_labels.LoadFailed, load.Exception!.GetBaseException().Message);
            } else {
                _item.ReplaceChoices(load.Result ?? new Choice[0]);
                List<object?> keep = new List<object?>();
                foreach (object? value in _item.Selection) {
                    foreach (Choice c in _item.Choices) {
                        if (Equals(c.Value, value) || (c.Value != null && value != null && CommandLine.Format(c.Value) == CommandLine.Format(value))) {
                            keep.Add(c.Value);
                        }
                    }
                }
                if (keep.Count == 0) {
                    foreach (Choice c in _item.Choices) {
                        if (c.Selected && c.IsEnabled) {
                            keep.Add(c.Value);
                        }
                    }
                }
                foreach (Choice c in _item.Choices) {
                    c.Selected = keep.Contains(c.Value);
                }
                List.Replace(_item.Choices);
                _item.SetSelection(keep);
                Pane.InvalidatePreview();
                _changed();
            }
            return true;
        }

        public bool Back() => List.ClearFilter();
    }

    /// <summary>A page item: content built by the app each time it is drawn.</summary>
    internal sealed class PagePane : IPane {
        private readonly MenuItem _item;
        private readonly Func<MenuContext> _context;

        public PagePane(MenuItem item, Func<MenuContext> context) {
            _item = item;
            _context = context;
        }

        public string Title => _item.Title;

        public string? Caption => _item.CurrentState;

        public bool IsAnimating => true;

        public bool IsDirty => false;

        public IReadOnlyList<string> Crumbs => new string[0];

        public IWidget Build(ViewFrame frame, bool focused) {
            try {
                return _item.PageFactory?.Invoke(_context()) ?? (IWidget)UiLine.Empty;
            } catch (Exception ex) {
                return TextBlock.Plain(ex.Message, ThemeToken.Fail);
            }
        }

        public void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
        }

        public bool HandleUnbound(InputEvent input) => false;

        public bool Poll(TimeSpan now) => false;

        public bool Back() => false;
    }

    /// <summary>An action item: what it does, and Enter to run it (with a spinner while an asynchronous action runs).</summary>
    internal sealed class ActionPane : IPane {
        private readonly MenuItem _item;
        private readonly PromptLabels _labels;
        private readonly Action _run;

        public ActionPane(MenuItem item, PromptLabels labels, Action run) {
            _item = item;
            _labels = labels;
            _run = run;
        }

        public bool Running { get; set; }

        public Action? Cancel { get; set; }

        public string Title => _item.Title;

        public string? Caption => _item.CurrentState;

        public bool IsAnimating => Running;

        public bool IsDirty => false;

        public IReadOnlyList<string> Crumbs => new string[0];

        public IWidget Build(ViewFrame frame, bool focused) {
            Rows rows = new Rows();
            if (!string.IsNullOrEmpty(_item.Description)) {
                rows.Add(new UiParagraph(_item.Description!, ThemeToken.Text, 8));
                rows.Add(UiLine.Empty);
            }
            string? reason = _item.CurrentDisabledReason;
            if (reason != null) {
                rows.Add(new UiLine(context => StyledLine.From(PromptLabels.Format(_labels.Disabled, reason), context.Style(ThemeToken.Warn))));
            } else if (Running) {
                rows.Add(new UiLine(context => {
                    IReadOnlyList<string> frames = context.Glyphs.SpinnerFrames;
                    return new StyledLine(new[] {
                        new TextSpan(frames[(int)(context.Tick % frames.Count)] + " ", context.Style(ThemeToken.Running)),
                        new TextSpan(_item.Title + " " + context.Ellipsis, context.Style(ThemeToken.Text))
                    });
                }));
            } else {
                rows.Add(new UiLine(context => new StyledLine(new[] {
                    new TextSpan(InteractiveGlyphs.For(context).Enter, context.Style(ThemeToken.Accent).With(TextDecoration.Bold)),
                    new TextSpan("  " + _labels.PressEnterToRun, context.Style(ThemeToken.Muted))
                })));
            }
            return rows;
        }

        public void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            if (Running) {
                if (Cancel != null) {
                    bindings.Add(new KeyBinding(_labels.KeyCancel, Cancel, new KeyChord(ConsoleKey.Escape)) { Order = 20 });
                }
                return;
            }
            if (_item.CurrentDisabledReason == null) {
                bindings.Add(new KeyBinding(_labels.KeyRun, _run, new KeyChord(ConsoleKey.Enter)) { Order = 20, Group = "Menu" });
            }
        }

        public bool HandleUnbound(InputEvent input) => false;

        public bool Poll(TimeSpan now) => false;

        public bool Back() => Running;
    }

    /// <summary>The result of a run: status, summary, key figures, details and the next actions.</summary>
    internal sealed class ResultPane : IPane {
        private readonly RunResult _result;
        private readonly PromptLabels _labels;
        private readonly ChoiceList _next;
        private readonly Action<MenuItem> _activate;
        private readonly Action _back;

        public ResultPane(RunResult result, PromptLabels labels, Action<MenuItem> activate, Action back) {
            _result = result;
            _labels = labels;
            _activate = activate;
            _back = back;
            List<Choice> choices = new List<Choice>();
            foreach (MenuItem item in result.NextActions) {
                choices.Add(new Choice(item, item.Title) { Description = item.Description, Hotkey = item.HotkeyChar });
            }
            choices.Add(new Choice(null, labels.Back));
            _next = new ChoiceList(choices, false) { Labels = labels };
        }

        public string Title => _labels.Results;

        // The same clock the run showed in this place a moment before, so the caption does not change format.
        public string? Caption => _result.Elapsed > TimeSpan.Zero ? Widgets.NumberFormat.Clock(_result.Elapsed) : null;

        public bool IsAnimating => false;

        public bool IsDirty => false;

        public IReadOnlyList<string> Crumbs => new string[0];

        public IWidget Build(ViewFrame frame, bool focused) {
            _next.Focused = focused;
            Rows rows = new Rows();
            rows.Add(new UiLine(context => new StyledLine(new[] {
                new TextSpan(context.Glyphs.StatusMarker(_result.Status) + " ", context.Style(_result.Status).With(TextDecoration.Bold)),
                new TextSpan(_result.Title, context.Style(ThemeToken.Title))
            })));
            if (!string.IsNullOrEmpty(_result.Summary)) {
                rows.Add(new Padding(new UiParagraph(_result.Summary!, ThemeToken.Text, 4), 2, 0, 0, 0));
            }
            if (_result.Error != null) {
                rows.Add(new Padding(new ConsoleMonster.Widgets.ExceptionView(_result.Error) { MaxFrames = 3, MaxInner = 1 }, 2, 0, 0, 0));
            }
            if (_result.Figures.Count > 0) {
                rows.Add(UiLine.Empty);
                rows.Add(new UiLine(context => Figures(context)) { Indent = 2 });
            }
            if (_result.Details != null) {
                rows.Add(UiLine.Empty);
                rows.Add(new Padding(_result.Details, 2, 0, 0, 0));
            }
            rows.Add(UiLine.Empty);
            rows.Add(new UiLine(context => StyledLine.From(_labels.NextActions, context.Style(ThemeToken.Header))));
            rows.Add(new Sized(_next, LayoutSize.Fixed(Math.Max(1, _result.NextActions.Count + 1))));
            return rows;
        }

        private StyledLine Figures(RenderContext context) {
            List<TextSpan> spans = new List<TextSpan>();
            foreach (KeyValuePair<string, string> figure in _result.Figures) {
                if (spans.Count > 0) {
                    spans.Add(new TextSpan("     ", Style.Plain));
                }
                spans.Add(new TextSpan(figure.Value + " ", _result.FigureStatus.TryGetValue(figure.Key, out StatusKind status) ? context.Style(status).With(TextDecoration.Bold) : context.Style(ThemeToken.Highlight)));
                spans.Add(new TextSpan(figure.Key, context.Style(ThemeToken.Muted)));
            }
            return new StyledLine(spans);
        }

        public void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            _next.AddBindings(bindings, frame.Context);
            bindings.Add(new KeyBinding(_labels.KeySelect, () => {
                if (_next.FocusedChoice?.Value is MenuItem item) {
                    _activate(item);
                } else {
                    _back();
                }
            }, new KeyChord(ConsoleKey.Enter)) { Order = 25 });
        }

        public bool HandleUnbound(InputEvent input) {
            if (_next.HandleText(input, out Choice? picked)) {
                if (picked?.Value is MenuItem item) {
                    _activate(item);
                }
                return true;
            }
            return false;
        }

        public bool Poll(TimeSpan now) => false;

        public bool Back() => false;
    }
}
