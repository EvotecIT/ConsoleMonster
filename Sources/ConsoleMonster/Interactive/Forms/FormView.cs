using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Input;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The interactive form: pages of fields edited in place, choice editors that open over the page, async option loading
    /// for dependent fields, validation, a progress strip, and the review page with the equivalent command, copy, save
    /// preset and run.
    /// </summary>
    internal sealed partial class FormView : IPane {
        private readonly Form _form;
        private readonly IViewHost _host;
        private readonly ChoiceStore? _store;
        private readonly bool _remember;
        private readonly Dictionary<Field, FieldState> _states = new Dictionary<Field, FieldState>();
        private readonly Func<string, bool> _copy;
        private int _page;
        private int _focus;
        private int _button;
        private int _scroll;
        private ChoiceEditor? _editor;
        private bool _dirty;
        private string? _preset;

        public FormView(Form form, IViewHost host, ChoiceStore? store, bool remember, Func<string, bool>? copy = null) {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _store = store;
            _remember = remember;
            _copy = copy ?? (text => Clipboard.TrySetText(text));
            if (_form.Pages.Count == 0) {
                _form.Pages.Add(new FormPage(_form.Title));
            }
            _form.SeedDefaults();
            foreach (Field field in _form.Fields) {
                if (field.Value == null && field.Default != null) {
                    field.Value = Clone(field.Default);
                }
                if (field is ChoiceField multi && multi.Multiple && field.Value == null) {
                    field.Value = new List<object?>();
                }
                _states[field] = new FieldState(field);
            }
            if (_store != null && _remember && _store.LoadLast(_form.Key) is IDictionary<string, object?> last) {
                Apply(last);
            }
            StartPendingLoads();
            FocusFirstField();
        }

        /// <summary>Raised when the form is submitted (Run on the review page, or Done).</summary>
        public Action<FormResult>? Submitted { get; set; }

        public PromptLabels Labels => _host.Labels;

        public Form Form => _form;

        private bool OnReview => _form.Review && _page >= _form.Pages.Count;

        private int StepCount => _form.Pages.Count + (_form.Review ? 1 : 0);

        public string Title => _form.Title;

        public string? Caption => StepCount > 1 ? PageCaption() : null;

        public bool IsDirty => _dirty;

        /// <summary>Whether typed characters go into a text box now (so ? and : are typed, not shortcuts).</summary>
        public bool WantsText {
            get {
                if (_editor != null) {
                    return !_editor.List.Filter.IsEmpty || _editor.List.FilterMode || !_editor.List.HasHotkeys;
                }
                Field? focused = FocusedField;
                return !OnReview && focused != null && _states[focused].Editor != null;
            }
        }

        public bool IsAnimating {
            get {
                foreach (FieldState state in _states.Values) {
                    if (state.Loading) {
                        return true;
                    }
                }
                return false;
            }
        }

        public IReadOnlyList<string> Crumbs {
            get {
                List<string> crumbs = new List<string>();
                if (StepCount > 1) {
                    crumbs.Add(OnReview ? Labels.Review : _form.Pages[_page].Title);
                }
                if (_editor != null) {
                    crumbs.Add(_editor.Field.Label);
                }
                return crumbs;
            }
        }

        private string PageCaption() {
            return (Math.Min(_page, StepCount - 1) + 1).ToString(System.Globalization.CultureInfo.CurrentCulture) + "/" + StepCount.ToString(System.Globalization.CultureInfo.CurrentCulture);
        }

        /// <summary>Applies a preset by name.</summary>
        public bool ApplyPreset(string name) {
            if (_store == null || !(_store.LoadPreset(_form.Key, name) is IDictionary<string, object?> values)) {
                return false;
            }
            Apply(values);
            _preset = name;
            _dirty = true;
            StartPendingLoads();
            _host.ShowToast(new Toast(PromptLabels.Format(Labels.PresetApplied, name), StatusKind.Info));
            return true;
        }

        /// <summary>Applies stored values (a menu preset) and reloads dependent options.</summary>
        public void ApplyValues(IDictionary<string, object?> values) {
            Apply(values);
            _dirty = true;
            StartPendingLoads();
        }

        public IReadOnlyList<string> PresetNames() => _store == null ? (IReadOnlyList<string>)new string[0] : _store.PresetNames(_form.Key);

        private void Apply(IDictionary<string, object?> stored) {
            IReadOnlyList<string> pending = _form.ApplyStored(stored);
            foreach (string name in pending) {
                Field? field = _form.Find(name);
                if (field != null && stored.TryGetValue(name, out object? value)) {
                    _states[field].PendingStored = value;
                    _states[field].HasPendingStored = true;
                }
            }
            foreach (FieldState state in _states.Values) {
                state.SyncEditor();
            }
        }

        // ---- visibility and focus ---------------------------------------------------------------------------------

        private List<Field> VisibleFields(int page) {
            List<Field> fields = new List<Field>();
            if (page < 0 || page >= _form.Pages.Count) {
                return fields;
            }
            FormValues values = _form.GetValues();
            foreach (Field field in _form.Pages[page].Fields) {
                if (values.IsVisible(field.Name)) {
                    fields.Add(field);
                }
            }
            return fields;
        }

        /// <summary>Focus positions: the visible fields, then the buttons row.</summary>
        private int FocusCount => VisibleFields(_page).Count + 1;

        private Field? FocusedField {
            get {
                List<Field> fields = VisibleFields(_page);
                return _focus >= 0 && _focus < fields.Count ? fields[_focus] : null;
            }
        }

        private bool OnButtons => !OnReview && FocusedField == null;

        private void FocusFirstField() {
            _focus = 0;
            _button = ButtonCaptions().Count - 1;
            _scroll = 0;
        }

        private void MoveFocus(int delta) {
            Leave();
            int count = FocusCount;
            _focus = ((_focus + delta) % count + count) % count;
            if (OnButtons) {
                _button = ButtonCaptions().Count - 1;
            }
        }

        // Validates the field being left, so errors show as soon as the user moves on.
        private void Leave() {
            Field? field = FocusedField;
            if (field == null) {
                return;
            }
            FieldState state = _states[field];
            state.Commit(Labels);
            if (state.ParseError == null) {
                state.Error = field.Check(field.Value, _form.GetValues(), Labels);
            } else {
                state.Error = state.ParseError;
            }
        }

        // ---- values and dependencies ------------------------------------------------------------------------------

        private void Changed(Field field) {
            _dirty = true;
            _states[field].Error = null;
            foreach (Field other in _form.Fields) {
                if (other == field || !Contains(other.DependsOn, field.Name)) {
                    continue;
                }
                FieldState state = _states[other];
                other.Value = other is ChoiceField c && c.Multiple ? new List<object?>() : null;
                state.SyncEditor();
                state.Error = null;
                if (other is ChoiceField choice && choice.Loader != null) {
                    choice.Choices.Clear();
                    state.CancelLoad();
                    if (DependenciesSatisfied(other)) {
                        StartLoad(choice);
                    }
                }
                Changed(other);
            }
        }

        private static bool Contains(IList<string> names, string name) {
            foreach (string n in names) {
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

        private bool DependenciesSatisfied(Field field) {
            foreach (string name in field.DependsOn) {
                Field? dependency = _form.Find(name);
                if (dependency != null && dependency.IsEmpty(dependency.Value)) {
                    return false;
                }
            }
            return true;
        }

        private void StartPendingLoads() {
            foreach (Field field in _form.Fields) {
                if (field is ChoiceField choice && choice.Loader != null && choice.Choices.Count == 0 && !_states[field].Loading && DependenciesSatisfied(field)) {
                    StartLoad(choice);
                }
            }
        }

        private void StartLoad(ChoiceField field) {
            FieldState state = _states[field];
            state.CancelLoad();
            CancellationTokenSource cts = new CancellationTokenSource();
            FormValues values = _form.GetValues();
            Func<FormValues, CancellationToken, Task<IEnumerable<Choice>>> loader = field.Loader!;
            state.Cts = cts;
            state.LoadError = null;
            // Loaders are asynchronous: they return a running task at once (or a finished one, which shows without a frame of spinner).
            try {
                state.Load = loader(values, cts.Token) ?? Task.FromResult<IEnumerable<Choice>>(new Choice[0]);
            } catch (Exception ex) {
                TaskCompletionSource<IEnumerable<Choice>> failed = new TaskCompletionSource<IEnumerable<Choice>>();
                failed.SetException(ex);
                state.Load = failed.Task;
            }
        }

        public bool Poll(TimeSpan now) {
            bool changed = false;
            foreach (KeyValuePair<Field, FieldState> pair in _states) {
                FieldState state = pair.Value;
                Task<IEnumerable<Choice>>? load = state.Load;
                if (load == null || !load.IsCompleted) {
                    continue;
                }
                state.Load = null;
                changed = true;
                ChoiceField field = (ChoiceField)pair.Key;
                if (load.IsCanceled || (state.Cts != null && state.Cts.IsCancellationRequested)) {
                    state.LoadError = Labels.Cancelled;
                    continue;
                }
                if (load.IsFaulted) {
                    Exception error = load.Exception!.GetBaseException();
                    state.LoadError = PromptLabels.Format(Labels.LoadFailed, error.Message);
                    continue;
                }
                field.Choices.Clear();
                foreach (Choice c in load.Result ?? new Choice[0]) {
                    field.Choices.Add(c);
                }
                if (state.HasPendingStored) {
                    state.HasPendingStored = false;
                    if (field.TryRestore(state.PendingStored, out object? value)) {
                        field.Value = value;
                    }
                } else if (field.Multiple && field.Value is IList list && list.Count == 0) {
                    List<object?> preselected = new List<object?>();
                    foreach (Choice c in field.Choices) {
                        if (c.Selected && c.IsEnabled) {
                            preselected.Add(c.Value);
                        }
                    }
                    field.Value = preselected;
                } else if (!field.Multiple && field.Value == null && field.Default != null && field.Find(field.Default) != null) {
                    field.Value = field.Default;
                }
                if (_editor != null && _editor.Field == field) {
                    _editor.Reload();
                }
            }
            return changed;
        }

        // ---- pages and submission ---------------------------------------------------------------------------------

        private bool ValidatePage(int page, bool focus) {
            List<Field> fields = VisibleFields(page);
            FormValues values = _form.GetValues();
            int first = -1;
            for (int i = 0; i < fields.Count; i++) {
                FieldState state = _states[fields[i]];
                state.Commit(Labels);
                state.Error = state.ParseError ?? (state.Loading ? Labels.StillLoading : fields[i].Check(fields[i].Value, values, Labels));
                if (state.Error != null && first < 0) {
                    first = i;
                }
            }
            if (first >= 0 && focus) {
                _page = page;
                _focus = first;
            }
            return first < 0;
        }

        private void NextPage() {
            if (!ValidatePage(_page, true)) {
                return;
            }
            if (_page < _form.Pages.Count - 1 || (_form.Review && _page == _form.Pages.Count - 1)) {
                _page++;
                FocusFirstField();
                if (OnReview) {
                    _button = 0;
                }
                return;
            }
            Submit();
        }

        private void PreviousPage() {
            if (_page == 0) {
                return;
            }
            Leave();
            _page--;
            FocusFirstField();
        }

        private void Submit() {
            for (int p = 0; p < _form.Pages.Count; p++) {
                if (!ValidatePage(p, true)) {
                    return;
                }
            }
            if (_store != null && _remember) {
                TryStore(() => _store.SaveLast(_form.Key, _form.ToStored()));
            }
            _dirty = false;
            Submitted?.Invoke(new FormResult(true, _form.GetValues(), _form.BuildCommand(), _preset));
        }

        private void CopyCommand() {
            string command = _form.BuildCommand();
            bool copied;
            try {
                copied = _copy(command);
            } catch (Exception) {
                copied = false;
            }
            _host.ShowToast(copied ? new Toast(Labels.CommandCopied) : new Toast(Labels.CopyFailed, StatusKind.Warn));
        }

        private void SavePreset() {
            if (_store == null) {
                return;
            }
            _host.ShowModal(new TextDialog(Labels.SavePreset, null, _preset, null, name => {
                if (TryStore(() => _store.SavePreset(_form.Key, name, _form.ToStored()))) {
                    _preset = name;
                    _host.ShowToast(new Toast(PromptLabels.Format(Labels.PresetSaved, name)));
                }
            }, Labels));
        }

        private bool TryStore(Action action) {
            try {
                action();
                return true;
            } catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException) {
                _host.ShowToast(new Toast(ex.Message, StatusKind.Fail, TimeSpan.FromSeconds(4)));
                return false;
            }
        }

        public bool Back() {
            if (_editor != null) {
                _editor = null;
                return true;
            }
            Field? field = FocusedField;
            if (field != null && _states[field].Loading) {
                _states[field].CancelLoad();
                _states[field].LoadError = Labels.Cancelled;
                return true;
            }
            if (_page > 0) {
                PreviousPage();
                return true;
            }
            return false;
        }

        private static object? Clone(object? value) {
            if (value is IEnumerable list && !(value is string)) {
                List<object?> copy = new List<object?>();
                foreach (object? item in list) {
                    copy.Add(item);
                }
                return copy;
            }
            return value;
        }

        // ---- input ----------------------------------------------------------------------------------------------

        public void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            InteractiveGlyphs g = InteractiveGlyphs.For(frame.Context);
            if (_editor != null) {
                _editor.AddBindings(bindings, frame);
                return;
            }
            if (OnReview) {
                AddReviewBindings(bindings, g);
                return;
            }
            bindings.Add(new KeyBinding(Labels.KeyMove, () => { }) { KeyText = g.KeyUpDown, Order = 10, Group = "Form" });
            bindings.Add(new KeyBinding(string.Empty, () => MoveFocus(-1), new KeyChord(ConsoleKey.UpArrow), new KeyChord(ConsoleKey.Tab, KeyModifiers.Shift)) { InFooter = false, Group = "Form" });
            bindings.Add(new KeyBinding(string.Empty, () => MoveFocus(1), new KeyChord(ConsoleKey.DownArrow), new KeyChord(ConsoleKey.Tab)) { InFooter = false, Group = "Form" });
            Field? field = FocusedField;
            if (field == null) {
                List<string> captions = ButtonCaptions();
                bindings.Add(new KeyBinding(Labels.KeyMove, () => _button = (_button + captions.Count - 1) % captions.Count, new KeyChord(ConsoleKey.LeftArrow)) { InFooter = false, Group = "Form" });
                bindings.Add(new KeyBinding(string.Empty, () => _button = (_button + 1) % captions.Count, new KeyChord(ConsoleKey.RightArrow)) { InFooter = false, Group = "Form" });
                bindings.Add(new KeyBinding(captions[_button].ToLowerInvariant(), ActivateButton, new KeyChord(ConsoleKey.Enter)) { Order = 20, Group = "Form" });
            } else {
                AddFieldBindings(bindings, field, g);
            }
            if (_page < _form.Pages.Count - 1 || _form.Review) {
                bindings.Add(new KeyBinding(Labels.KeyNext, NextPage, new KeyChord(ConsoleKey.PageDown)) { KeyText = "pgdn", Order = 45, Group = "Pages" });
            }
            if (_page > 0) {
                bindings.Add(new KeyBinding(Labels.KeyPrevious, PreviousPage, new KeyChord(ConsoleKey.PageUp)) { KeyText = "pgup", Order = 46, Group = "Pages", InFooter = false });
            }
        }

        private void AddFieldBindings(List<KeyBinding> bindings, Field field, InteractiveGlyphs g) {
            FieldState state = _states[field];
            switch (field.Kind) {
                case FieldKind.Toggle:
                    bindings.Add(new KeyBinding(Labels.KeyToggle, () => SetToggle(field, !(field.Value is true)), new KeyChord(ConsoleKey.Spacebar), new KeyChord(' '), new KeyChord(ConsoleKey.Enter)) { KeyText = "space", Order = 20, Group = "Form" });
                    bindings.Add(new KeyBinding(string.Empty, () => SetToggle(field, true), new KeyChord(ConsoleKey.RightArrow)) { InFooter = false });
                    bindings.Add(new KeyBinding(string.Empty, () => SetToggle(field, false), new KeyChord(ConsoleKey.LeftArrow)) { InFooter = false });
                    break;
                case FieldKind.Choice:
                case FieldKind.MultiChoice:
                    ChoiceField choice = (ChoiceField)field;
                    if (state.Loading) {
                        bindings.Add(new KeyBinding(Labels.KeyCancel, () => Back(), new KeyChord(ConsoleKey.Escape)) { Order = 20, Group = "Form" });
                        break;
                    }
                    if (state.LoadError != null && choice.Loader != null) {
                        bindings.Add(new KeyBinding("retry", () => StartLoad(choice), new KeyChord(ConsoleKey.R, KeyModifiers.Control), new KeyChord(ConsoleKey.Enter)) { KeyText = "ctrl+r", Order = 20, Group = "Form" });
                        break;
                    }
                    bindings.Add(new KeyBinding(Labels.KeyOpen, () => Open(choice), new KeyChord(ConsoleKey.Enter), new KeyChord(ConsoleKey.Spacebar), new KeyChord(' ')) { Order = 20, Group = "Form" });
                    if (!choice.Multiple && choice.Choices.Count > 0) {
                        bindings.Add(new KeyBinding(Labels.KeyEdit, () => { }) { KeyText = g.KeyLeftRight, Order = 25, Group = "Form" });
                        bindings.Add(new KeyBinding(string.Empty, () => Cycle(choice, 1), new KeyChord(ConsoleKey.RightArrow)) { InFooter = false });
                        bindings.Add(new KeyBinding(string.Empty, () => Cycle(choice, -1), new KeyChord(ConsoleKey.LeftArrow)) { InFooter = false });
                    }
                    break;
                case FieldKind.Number:
                    bindings.Add(new KeyBinding(Labels.KeyNext, () => MoveFocus(1), new KeyChord(ConsoleKey.Enter)) { Order = 20, Group = "Form" });
                    bindings.Add(new KeyBinding(Labels.KeyStep, () => { }) { KeyText = "ctrl+" + g.KeyUpDown, Order = 26, Group = "Form", InFooter = false });
                    bindings.Add(new KeyBinding(string.Empty, () => StepNumber((NumberField)field, 1), new KeyChord(ConsoleKey.UpArrow, KeyModifiers.Control), new KeyChord(ConsoleKey.UpArrow, KeyModifiers.Alt)) { InFooter = false });
                    bindings.Add(new KeyBinding(string.Empty, () => StepNumber((NumberField)field, -1), new KeyChord(ConsoleKey.DownArrow, KeyModifiers.Control), new KeyChord(ConsoleKey.DownArrow, KeyModifiers.Alt)) { InFooter = false });
                    break;
                case FieldKind.Path:
                    bindings.Add(new KeyBinding(Labels.KeyNext, () => MoveFocus(1), new KeyChord(ConsoleKey.Enter)) { Order = 20, Group = "Form" });
                    if (state.Suggestions.Count > 0) {
                        // Tab completes inside a path field; Down still moves to the next field.
                        bindings.Insert(0, new KeyBinding(Labels.KeyComplete, () => CompletePath((PathField)field), new KeyChord(ConsoleKey.Tab)) { Order = 15, Group = "Form" });
                    }
                    break;
                default:
                    bindings.Add(new KeyBinding(Labels.KeyNext, () => MoveFocus(1), new KeyChord(ConsoleKey.Enter)) { Order = 20, Group = "Form" });
                    if (field.Kind == FieldKind.Secret) {
                        bindings.Add(new KeyBinding(state.Revealed ? Labels.KeyHide : Labels.KeyReveal, () => state.Revealed = !state.Revealed, new KeyChord(ConsoleKey.R, KeyModifiers.Control)) { Order = 27, Group = "Form" });
                    }
                    break;
            }
        }

        private void AddReviewBindings(List<KeyBinding> bindings, InteractiveGlyphs g) {
            List<string> captions = ReviewCaptions();
            bindings.Add(new KeyBinding(Labels.KeyMove, () => _button = (_button + captions.Count - 1) % captions.Count, new KeyChord(ConsoleKey.LeftArrow), new KeyChord(ConsoleKey.Tab, KeyModifiers.Shift), new KeyChord(ConsoleKey.UpArrow)) { KeyText = g.KeyLeftRight, Order = 10, Group = "Review" });
            bindings.Add(new KeyBinding(string.Empty, () => _button = (_button + 1) % captions.Count, new KeyChord(ConsoleKey.RightArrow), new KeyChord(ConsoleKey.Tab), new KeyChord(ConsoleKey.DownArrow)) { InFooter = false, Group = "Review" });
            bindings.Add(new KeyBinding(captions[_button].ToLowerInvariant(), ActivateReview, new KeyChord(ConsoleKey.Enter)) { Order = 20, Group = "Review" });
            for (int i = 0; i < captions.Count; i++) {
                int index = i;
                char key = char.ToLowerInvariant(captions[i][0]);
                bindings.Add(new KeyBinding(captions[i], () => { _button = index; ActivateReview(); }, new KeyChord(key)) { KeyText = key.ToString(), Order = 30 + i, Group = "Review", InFooter = false });
            }
            bindings.Add(new KeyBinding(Labels.KeyPrevious, PreviousPage, new KeyChord(ConsoleKey.PageUp)) { KeyText = "pgup", Order = 46, Group = "Pages", InFooter = false });
        }

        public bool HandleUnbound(InputEvent input) {
            if (_editor != null) {
                return _editor.HandleUnbound(input);
            }
            Field? field = FocusedField;
            if (field == null || OnReview) {
                return false;
            }
            FieldState state = _states[field];
            if (state.Editor == null) {
                return false;
            }
            string before = state.Editor.Text;
            if (!state.Editor.Handle(input)) {
                return false;
            }
            if (!string.Equals(before, state.Editor.Text, StringComparison.Ordinal)) {
                state.Commit(Labels);
                state.Error = state.ParseError;
                if (field is PathField path) {
                    state.Suggestions = path.Prompt().Complete(state.Editor.Text);
                    state.SuggestionIndex = -1;
                }
                Changed(field);
            }
            return true;
        }

        private void SetToggle(Field field, bool value) {
            if (Equals(field.Value, value)) {
                return;
            }
            field.Value = value;
            Changed(field);
        }

        private void Cycle(ChoiceField field, int delta) {
            List<Choice> enabled = new List<Choice>();
            foreach (Choice c in field.Choices) {
                if (c.IsEnabled) {
                    enabled.Add(c);
                }
            }
            if (enabled.Count == 0) {
                return;
            }
            int index = enabled.IndexOf(field.Find(field.Value) ?? enabled[0]);
            index = field.Value == null ? (delta > 0 ? 0 : enabled.Count - 1) : ((index + delta) % enabled.Count + enabled.Count) % enabled.Count;
            field.Value = enabled[index].Value;
            Changed(field);
        }

        private void StepNumber(NumberField field, int direction) {
            decimal current = field.Value is decimal d ? d : field.Minimum ?? 0;
            decimal next = current + direction * field.Step;
            if (field.Minimum.HasValue && next < field.Minimum.Value) {
                next = field.Minimum.Value;
            }
            if (field.Maximum.HasValue && next > field.Maximum.Value) {
                next = field.Maximum.Value;
            }
            field.Value = next;
            _states[field].SyncEditor();
            Changed(field);
        }

        private void CompletePath(PathField field) {
            FieldState state = _states[field];
            if (state.Suggestions.Count == 0 || state.Editor == null) {
                return;
            }
            state.SuggestionIndex = (state.SuggestionIndex + 1) % state.Suggestions.Count;
            state.Editor.Text = state.Suggestions.Count == 1 ? state.Suggestions[0] : state.Suggestions[state.SuggestionIndex];
            state.Commit(Labels);
            if (state.Suggestions.Count == 1) {
                state.Suggestions = field.Prompt().Complete(state.Editor.Text);
                state.SuggestionIndex = -1;
            }
            Changed(field);
        }

        private void Open(ChoiceField field) {
            _editor = new ChoiceEditor(this, field);
        }

        internal void CloseEditor(ChoiceField field, object? value, bool commit) {
            _editor = null;
            if (commit) {
                field.Value = value;
                Changed(field);
                if (!field.Multiple) {
                    MoveFocus(1);
                }
            }
        }

        private List<string> ButtonCaptions() {
            List<string> captions = new List<string>();
            if (_page > 0) {
                captions.Add(Labels.Back);
            }
            bool last = _page >= _form.Pages.Count - 1;
            captions.Add(last ? (_form.Review ? Labels.Review : _form.SubmitText ?? Labels.Done) : Labels.Next);
            return captions;
        }

        private List<string> ReviewCaptions() {
            List<string> captions = new List<string> { _form.SubmitText ?? Labels.Run };
            if (!string.IsNullOrEmpty(_form.Command)) {
                captions.Add(Labels.CopyCommand);
            }
            if (_store != null) {
                captions.Add(Labels.SavePreset);
            }
            captions.Add(Labels.Back);
            return captions;
        }

        private void ActivateButton() {
            List<string> captions = ButtonCaptions();
            if (captions[_button] == Labels.Back && _page > 0) {
                PreviousPage();
                return;
            }
            NextPage();
        }

        private void ActivateReview() {
            List<string> captions = ReviewCaptions();
            string caption = captions[Math.Min(_button, captions.Count - 1)];
            if (caption == (_form.SubmitText ?? Labels.Run)) {
                Submit();
            } else if (caption == Labels.CopyCommand) {
                CopyCommand();
            } else if (caption == Labels.SavePreset) {
                SavePreset();
            } else {
                PreviousPage();
            }
        }

        /// <summary>Per-field editing state.</summary>
        private sealed class FieldState {
            public FieldState(Field field) {
                Field = field;
                if (field.Kind == FieldKind.Text || field.Kind == FieldKind.Secret || field.Kind == FieldKind.Number || field.Kind == FieldKind.Path) {
                    Editor = new LineEditor();
                    if (field is TextField text) {
                        Editor.MaxLength = text.MaxLength;
                    }
                    SyncEditor();
                }
            }

            public Field Field { get; }

            public LineEditor? Editor { get; }

            public string? Error { get; set; }

            public string? ParseError { get; private set; }

            public bool Revealed { get; set; }

            public Task<IEnumerable<Choice>>? Load { get; set; }

            public CancellationTokenSource? Cts { get; set; }

            public string? LoadError { get; set; }

            public bool Loading => Load != null && !Load.IsCompleted;

            public object? PendingStored { get; set; }

            public bool HasPendingStored { get; set; }

            public List<string> Suggestions { get; set; } = new List<string>();

            public int SuggestionIndex { get; set; } = -1;

            public void CancelLoad() {
                try {
                    Cts?.Cancel();
                } catch (ObjectDisposedException) {
                }
                Load = null;
            }

            public void SyncEditor() {
                if (Editor == null) {
                    return;
                }
                if (Field is NumberField number) {
                    Editor.Text = Field.Value is decimal d ? number.Format(d) : string.Empty;
                } else {
                    Editor.Text = Field.Value as string ?? string.Empty;
                }
                ParseError = null;
            }

            public void Commit(PromptLabels labels) {
                if (Editor == null) {
                    return;
                }
                if (Field is NumberField number) {
                    ParseError = number.Parse(Editor.Text, labels, out decimal? value);
                    if (ParseError == null) {
                        Field.Value = value;
                    }
                    return;
                }
                Field.Value = Editor.Text;
                ParseError = null;
            }
        }
    }
}
