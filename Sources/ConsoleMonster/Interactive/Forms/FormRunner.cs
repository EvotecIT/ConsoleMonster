using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>Shows a form on its own in every interaction mode.</summary>
    internal static class FormRunner {
        public static FormResult Run(Form form, FormOptions options, CancellationToken cancellationToken) {
            InteractiveOptions interactive = options.Interactive ?? InteractiveOptions.Default;
            PromptLabels labels = interactive.ResolveLabels();
            ChoiceStore? store = options.Store();
            form.SeedDefaults();
            switch (interactive.ResolveMode()) {
                case InteractionMode.None:
                    return NonInteractive(form, labels);
                case InteractionMode.Line:
                case InteractionMode.Accessible:
                    return Lines(form, new LinePrompter(interactive.ResolveLineConsole(), labels, interactive.ResolveMode() == InteractionMode.Accessible), store, options, cancellationToken);
            }
            using (InputLease lease = ConsoleInput.Acquire(interactive.Input)) {
                Capabilities.TerminalProfile profile = interactive.ResolveProfile();
                RenderContext context = Terminal.CreateContext(profile, interactive.ResolveTheme());
                IPromptSurface? surface = interactive.Surface;
                bool owns = false;
                if (surface == null) {
                    surface = PromptHost.Resolve(context) ?? (options.Inline ? (IPromptSurface)new InlineSurface(profile, null, interactive.MaxInlineHeight ?? 30) : new ScreenSurface(profile));
                    owns = true;
                }
                try {
                    FormHostView view = new FormHostView(form, labels, store, options.RememberLastChoices);
                    if (!string.IsNullOrEmpty(options.Preset)) {
                        view.Pane.ApplyPreset(options.Preset!);
                    }
                    InteractionLoop.Run(view, lease.Source, surface, context, cancellationToken);
                    FormResult result = view.Result ?? new FormResult(false, form.GetValues(), form.BuildCommand(), null);
                    surface.Complete(Summary(form, result, context, labels));
                    return result;
                } finally {
                    if (owns) {
                        surface.Dispose();
                    }
                }
            }
        }

        private static IReadOnlyList<StyledLine> Summary(Form form, FormResult result, RenderContext context, PromptLabels labels) {
            if (!result.Submitted) {
                return new[] {
                    new StyledLine(new[] {
                        new TextSpan(context.Glyphs.Get(Glyphs.Glyph.Cross) + " ", context.Style(ThemeToken.Muted)),
                        new TextSpan(form.Title, context.Style(ThemeToken.Muted)),
                        new TextSpan("  " + labels.Cancelled, context.Style(ThemeToken.Muted).With(TextDecoration.Italic))
                    })
                };
            }
            List<StyledLine> lines = new List<StyledLine> {
                new StyledLine(new[] {
                    new TextSpan(context.Glyphs.Get(Glyphs.Glyph.Check) + " ", context.Style(ThemeToken.Pass)),
                    new TextSpan(form.Title, context.Style(ThemeToken.Title))
                })
            };
            if (!string.IsNullOrEmpty(form.Command)) {
                lines.Add(new StyledLine(new[] {
                    new TextSpan("  " + labels.EquivalentCommand + ": ", context.Style(ThemeToken.Muted)),
                    new TextSpan(result.CommandLine, context.Style(ThemeToken.Accent))
                }));
            }
            return lines;
        }

        private static FormResult NonInteractive(Form form, PromptLabels labels) {
            FormValues values = form.GetValues();
            foreach (Field field in form.Fields) {
                if (field.Value == null && field.Default != null) {
                    field.Value = field.Default;
                }
            }
            values = form.GetValues();
            foreach (Field field in form.Fields) {
                if (!values.IsVisible(field.Name)) {
                    continue;
                }
                if (field.Check(field.Value, values, labels) != null) {
                    string message = string.IsNullOrEmpty(field.Parameter)
                        ? PromptLabels.Format(labels.NonInteractiveNeedsDefault, field.Label)
                        : PromptLabels.Format(labels.NonInteractiveNeedsValue, field.Label, CommandLine.Parameter(field.Parameter!, form.CommandStyle));
                    throw new PromptUnavailableException(message) { Prompt = field.Label, Parameter = field.Parameter };
                }
            }
            return new FormResult(true, values, form.BuildCommand(), null);
        }

        private static FormResult Lines(Form form, LinePrompter lines, ChoiceStore? store, FormOptions options, CancellationToken token) {
            if (store != null && options.RememberLastChoices && store.LoadLast(form.Key) is IDictionary<string, object?> last) {
                form.ApplyStored(last);
            }
            lines.Say(form.Title);
            if (!string.IsNullOrEmpty(form.Description)) {
                lines.Say(form.Description!);
            }
            foreach (FormPage page in form.Pages) {
                if (form.Pages.Count > 1) {
                    lines.Say(string.Empty);
                    lines.Say(page.Title);
                }
                foreach (Field field in page.Fields) {
                    if (!form.GetValues().IsVisible(field.Name)) {
                        continue;
                    }
                    if (field is ChoiceField choice && choice.Loader != null) {
                        lines.Say(lines.Labels.Loading);
                        choice.Choices.Clear();
                        foreach (Choice c in choice.Loader(form.GetValues(), token).GetAwaiter().GetResult()) {
                            choice.Choices.Add(c);
                        }
                    }
                    field.Value = Ask(field, lines, form, token);
                }
            }
            lines.Say(string.Empty);
            lines.Say(lines.Labels.Review);
            FormValues values = form.GetValues();
            foreach (Field field in form.Fields) {
                if (values.IsVisible(field.Name)) {
                    lines.Say("  " + field.Label + ": " + field.Display(field.Value, lines.Labels));
                }
            }
            string command = form.BuildCommand();
            if (!string.IsNullOrEmpty(form.Command)) {
                lines.Say(lines.Labels.EquivalentCommand + ": " + command);
            }
            bool run = lines.Confirm(form.SubmitText ?? lines.Labels.Run, null, true, token);
            if (run && store != null && options.RememberLastChoices) {
                store.SaveLast(form.Key, form.ToStored());
            }
            return new FormResult(run, form.GetValues(), command, null);
        }

        private static object? Ask(Field field, LinePrompter lines, Form form, CancellationToken token) {
            PromptLabels labels = lines.Labels;
            FormValues values = form.GetValues();
            switch (field) {
                case ToggleField toggle:
                    return lines.Confirm(toggle.Label, toggle.Help, toggle.Value is true, token);
                case ChoiceField choice when !choice.Multiple: {
                    int defaultIndex = -1;
                    for (int i = 0; i < choice.Choices.Count; i++) {
                        if (Equals(choice.Choices[i].Value, choice.Value)) {
                            defaultIndex = i;
                        }
                    }
                    return choice.Choices[lines.Select(choice.Label, choice.Help, new List<Choice>(choice.Choices), defaultIndex, token)].Value;
                }
                case ChoiceField multi: {
                    HashSet<object?> current = new HashSet<object?>();
                    if (multi.Value is IEnumerable list && !(multi.Value is string)) {
                        foreach (object? v in list) {
                            current.Add(v);
                        }
                    }
                    foreach (Choice c in multi.Choices) {
                        c.Selected = current.Contains(c.Value);
                    }
                    List<object?> result = new List<object?>();
                    foreach (int i in lines.MultiSelect(multi.Label, multi.Help, new List<Choice>(multi.Choices), multi.Minimum, multi.Maximum, token)) {
                        result.Add(multi.Choices[i].Value);
                    }
                    return result;
                }
                case NumberField number: {
                    string text = lines.Text(number.Label, number.Help, number.Value is decimal d ? number.Format(d) : null, t => {
                        string? problem = number.Parse(t, labels, out decimal? parsed);
                        return problem ?? number.Check(parsed, values, labels);
                    }, false, token);
                    number.Parse(text, labels, out decimal? value);
                    return value;
                }
                case SecretField secret:
                    return lines.Text(secret.Label, secret.Help, null, t => secret.Check(t, values, labels), true, token);
                default:
                    return lines.Text(field.Label, field.Help, field.Value as string, t => field.Check(t, values, labels), false, token);
            }
        }
    }

    /// <summary>A form shown on its own: a framed page (or inline block), the footer, dialogs and the key map.</summary>
    internal sealed class FormHostView : InteractiveView, IViewHost {
        private readonly List<Modal> _modals = new List<Modal>();

        public FormHostView(Form form, PromptLabels labels, ChoiceStore? store, bool remember) : base(labels) {
            Pane = new FormView(form, this, store, remember);
            Pane.Submitted = result => {
                Result = result;
                IsDone = true;
            };
        }

        public FormView Pane { get; }

        public FormResult? Result { get; private set; }

        public override bool IsAnimating => Pane.IsAnimating;

        public void ShowModal(Modal modal) => _modals.Add(modal);

        public override bool Poll(TimeSpan now) {
            bool changed = base.Poll(now);
            changed |= Pane.Poll(now);
            changed |= _modals.RemoveAll(m => m.IsClosed) > 0;
            return changed;
        }

        protected override IWidget Build(ViewFrame frame) {
            _modals.RemoveAll(m => m.IsClosed);
            IWidget content;
            if (frame.Inline) {
                Rows rows = new Rows();
                rows.Add(new UiLine(context => new StyledLine(new[] {
                    new TextSpan(InteractiveGlyphs.For(context).Question + " ", context.Style(ThemeToken.Accent).With(TextDecoration.Bold)),
                    new TextSpan(Pane.Title, context.Style(ThemeToken.Title))
                })));
                rows.Add(new Padding(Pane.Build(frame, _modals.Count == 0), 2, 0, 0, 0));
                rows.Add(Footer(frame));
                content = rows;
            } else {
                int width = Math.Min(frame.Width - 2, 100);
                Panel panel = new Panel(new Padding(Pane.Build(frame, _modals.Count == 0), 1, 1, 1, 0), Markup.Escape(Pane.Title)) {
                    Caption = Pane.Caption,
                    BorderToken = ThemeToken.Border,
                    Height = Math.Max(8, frame.Height - 2)
                };
                Rows rows = new Rows();
                rows.Add(new Sized(panel, LayoutSize.Fraction()));
                rows.Add(Footer(frame));
                content = new Centered(rows, width);
            }
            if (_modals.Count > 0) {
                Modal top = _modals[_modals.Count - 1];
                return new Overlay(content, top.Build(frame, frame.Caret), dim: true) { Inline = frame.Inline, Top = top.Top, MaxWidth = top.MaxWidth };
            }
            return content;
        }

        private KeyFooter Footer(ViewFrame frame) {
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            return new KeyFooter(bindings) { Indent = frame.Inline ? 2 : 1, Toast = CurrentToast };
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            if (_modals.Count > 0) {
                _modals[_modals.Count - 1].AddBindings(bindings, frame);
                return;
            }
            Pane.AddBindings(bindings, frame);
            bool hasEscape = false;
            foreach (KeyBinding b in bindings) {
                foreach (KeyChord c in b.Chords) {
                    if (c.Key == ConsoleKey.Escape && c.Modifiers == KeyModifiers.None) {
                        hasEscape = true;
                    }
                }
            }
            if (!hasEscape) {
                bindings.Add(new KeyBinding(Pane.Crumbs.Count > 1 || (Pane.Caption != null && !Pane.Caption.StartsWith("1/", StringComparison.Ordinal)) ? Labels.KeyBack : Labels.KeyCancel, Escape, new KeyChord(ConsoleKey.Escape)) { Order = 70 });
            }
            bindings.Add(HelpBinding(false));
        }

        private void Escape() {
            if (Pane.Back()) {
                return;
            }
            if (Pane.IsDirty) {
                ShowModal(new ConfirmDialog(Labels.Exit, Labels.DiscardChanges, () => { IsCancelled = true; IsDone = true; }, Labels, true));
                return;
            }
            IsCancelled = true;
            IsDone = true;
        }

        protected override bool HandleUnbound(InputEvent input) {
            if (_modals.Count > 0) {
                return _modals[_modals.Count - 1].HandleUnbound(input);
            }
            return Pane.HandleUnbound(input);
        }
    }

    /// <summary>Centres a child horizontally at a width and gives it the full height.</summary>
    internal sealed class Centered : IWidget {
        private readonly IWidget _child;
        private readonly int _width;

        public Centered(IWidget child, int width) {
            _child = child;
            _width = width;
        }

        public Size Measure(Constraints constraints, RenderContext context) {
            Size size = _child.Measure(new Constraints(Math.Min(_width, constraints.MaxWidth), constraints.MaxHeight), context);
            return new Size(constraints.MaxWidth, size.Height);
        }

        public void Render(Region region, RenderContext context) {
            int width = Math.Min(_width, region.Width);
            int x = Math.Max(0, (region.Width - width) / 2);
            _child.Render(region.Sub(x, 0, width, region.Height), context);
        }
    }
}
