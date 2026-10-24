using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    internal sealed partial class FormView {
        public IWidget Build(ViewFrame frame, bool focused) => new FormWidget(this, frame, focused);

        private sealed class FormWidget : IWidget {
            private readonly FormView _view;
            private readonly ViewFrame _frame;
            private readonly bool _focused;

            public FormWidget(FormView view, ViewFrame frame, bool focused) {
                _view = view;
                _frame = frame;
                _focused = focused;
            }

            public Size Measure(Constraints constraints, RenderContext context) {
                if (!constraints.IsHeightUnbounded && !_frame.Inline) {
                    return new Size(constraints.MaxWidth, constraints.MaxHeight);
                }
                int height = Header(context, constraints.MaxWidth).Count + 2;
                if (_view.OnReview) {
                    height += _view.VisibleSummaryCount() + 6;
                } else if (_view._editor != null) {
                    height += 14;
                } else {
                    foreach (Field field in _view.VisibleFields(_view._page)) {
                        height += _view.RowHeight(field);
                    }
                    height += 4;
                }
                return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
            }

            private List<IWidget> Header(RenderContext context, int width) {
                List<IWidget> rows = new List<IWidget>();
                if (_view.StepCount > 1) {
                    rows.Add(new UiLine(c => _view.Strip(c, width)));
                    rows.Add(UiLine.Empty);
                }
                if (_view._editor == null) {
                    string title = _view.OnReview ? _view.Labels.Review : _view._form.Pages[_view._page].Title;
                    if (_view.StepCount > 1 || !string.Equals(title, _view._form.Title, StringComparison.Ordinal)) {
                        rows.Add(new UiLine(c => StyledLine.From(title, c.Style(ThemeToken.Title))));
                    }
                    string? description = _view.OnReview ? null : _view._form.Pages[_view._page].Description ?? (_view._page == 0 ? _view._form.Description : null);
                    if (!string.IsNullOrEmpty(description)) {
                        rows.Add(new UiParagraph(description!, ThemeToken.Muted, 2));
                    }
                    if (rows.Count > 0) {
                        rows.Add(UiLine.Empty);
                    }
                }
                return rows;
            }

            public void Render(Region region, RenderContext context) {
                if (region.Width <= 4 || region.Height <= 0) {
                    return;
                }
                int y = 0;
                foreach (IWidget header in Header(context, region.Width)) {
                    Size size = header.Measure(new Constraints(region.Width, region.Height - y), context);
                    header.Render(region.Sub(0, y, region.Width, Math.Min(size.Height, region.Height - y)), context);
                    y += size.Height;
                }
                Region body = region.Sub(0, y, region.Width, Math.Max(0, region.Height - y));
                if (_view._editor != null) {
                    _view._editor.Build(_frame, _frame.Caret).Render(body, context);
                    return;
                }
                if (_view.OnReview) {
                    _view.RenderReview(body, context, _focused);
                } else {
                    _view.RenderPage(body, context, _frame, _focused);
                }
            }
        }

        private int VisibleSummaryCount() {
            int count = 0;
            for (int p = 0; p < _form.Pages.Count; p++) {
                count += VisibleFields(p).Count;
            }
            return count;
        }

        private int RowHeight(Field field) {
            FieldState state = _states[field];
            int height = 1;
            if (state.Error != null) {
                height++;
            }
            if (field == FocusedField && state.Suggestions.Count > 0 && state.Editor != null && !state.Editor.IsEmpty) {
                height += Math.Min(4, state.Suggestions.Count);
            }
            return height;
        }

        internal StyledLine Strip(RenderContext context, int width) {
            InteractiveGlyphs g = InteractiveGlyphs.For(context);
            List<string> titles = new List<string>();
            foreach (FormPage page in _form.Pages) {
                titles.Add(page.Title);
            }
            if (_form.Review) {
                titles.Add(Labels.Review);
            }
            int current = Math.Min(_page, titles.Count - 1);
            List<TextSpan> spans = new List<TextSpan>();
            string line = " " + g.StepLine + g.StepLine + " ";
            for (int i = 0; i < titles.Count; i++) {
                if (i > 0) {
                    spans.Add(new TextSpan(line, context.Style(i <= current ? ThemeToken.Accent : ThemeToken.Subtle)));
                }
                if (i < current) {
                    spans.Add(new TextSpan(g.StepDone + " ", context.Style(ThemeToken.Accent)));
                    spans.Add(new TextSpan(titles[i], context.Style(ThemeToken.Text)));
                } else if (i == current) {
                    spans.Add(new TextSpan(g.StepCurrent + " ", context.Style(ThemeToken.Accent).With(TextDecoration.Bold)));
                    spans.Add(new TextSpan(g.BracketFocus ? "[" + titles[i] + "]" : titles[i], context.Style(ThemeToken.Accent).With(TextDecoration.Bold)));
                } else {
                    spans.Add(new TextSpan(g.StepTodo + " ", context.Style(ThemeToken.Subtle)));
                    spans.Add(new TextSpan(titles[i], context.Style(ThemeToken.Muted)));
                }
            }
            StyledLine full = new StyledLine(spans);
            if (full.Width <= width - 1) {
                return full;
            }
            // Too narrow for every step: the current step and the count.
            return new StyledLine(new[] {
                new TextSpan(g.StepCurrent + " ", context.Style(ThemeToken.Accent).With(TextDecoration.Bold)),
                new TextSpan(titles[current], context.Style(ThemeToken.Accent).With(TextDecoration.Bold)),
                new TextSpan("  " + (current + 1).ToString(context.Culture) + "/" + titles.Count.ToString(context.Culture), context.Style(ThemeToken.Muted))
            });
        }

        private int LabelWidth(IEnumerable<Field> fields, int width) {
            int widest = 0;
            foreach (Field f in fields) {
                widest = Math.Max(widest, DisplayWidth.Measure(f.Label) + (f.Required ? 2 : 0));
            }
            return Math.Max(10, Math.Min(Math.Min(28, width / 3), widest + 3));
        }

        private void RenderPage(Region region, RenderContext context, ViewFrame frame, bool active) {
            List<Field> fields = VisibleFields(_page);
            int labelWidth = LabelWidth(fields, region.Width);
            int valueX = 2 + labelWidth;
            int valueWidth = Math.Max(4, region.Width - valueX - 1);
            Field? focusedField = FocusedField;
            string? help = focusedField?.Help;
            List<StyledLine> helpLines = new List<StyledLine>();
            if (!string.IsNullOrEmpty(help)) {
                foreach (StyledLine l in TextWrapper.Wrap(StyledLine.From(help!, context.Style(ThemeToken.Muted)), Math.Max(10, region.Width - valueX - 1))) {
                    if (helpLines.Count < 2) {
                        helpLines.Add(l);
                    }
                }
            }
            int footer = 2 + (helpLines.Count > 0 ? helpLines.Count + 1 : 0);
            int area = Math.Max(1, region.Height - footer);
            // Rows of each field and the scroll position that keeps the focused one visible.
            List<int> tops = new List<int>();
            int total = 0;
            foreach (Field f in fields) {
                tops.Add(total);
                total += RowHeight(f);
            }
            if (_focus < fields.Count) {
                int top = tops[_focus];
                int bottom = top + RowHeight(fields[_focus]);
                if (top < _scroll) {
                    _scroll = top;
                }
                if (bottom > _scroll + area) {
                    _scroll = bottom - area;
                }
            }
            _scroll = Math.Max(0, Math.Min(_scroll, Math.Max(0, total - area)));
            for (int i = 0; i < fields.Count; i++) {
                int y = tops[i] - _scroll;
                int height = RowHeight(fields[i]);
                if (y + height <= 0 || y >= area) {
                    continue;
                }
                DrawField(region, context, frame, fields[i], i == _focus && active, y, labelWidth, valueX, valueWidth, area);
            }
            if (total > area) {
                InteractiveGlyphs g = InteractiveGlyphs.For(context);
                if (_scroll > 0) {
                    region.Write(region.Width - 2, 0, g.Up, context.Style(ThemeToken.Muted));
                }
                if (_scroll + area < total) {
                    region.Write(region.Width - 2, area - 1, g.Down, context.Style(ThemeToken.Muted));
                }
            }
            int helpY = region.Height - footer + 1;
            for (int i = 0; i < helpLines.Count; i++) {
                region.Write(valueX, helpY + i, helpLines[i]);
            }
            List<string> captions = ButtonCaptions();
            List<TextSpan> spans = Buttons.Spans(captions, _button, context, active && OnButtons);
            StyledLine buttons = new StyledLine(spans);
            region.Write(Math.Max(0, region.Width - buttons.Width - 1), region.Height - 1, buttons);
        }

        private void DrawField(Region region, RenderContext context, ViewFrame frame, Field field, bool focused, int y, int labelWidth, int valueX, int valueWidth, int area) {
            FieldState state = _states[field];
            InteractiveGlyphs g = InteractiveGlyphs.For(context);
            bool brackets = g.BracketFocus || !context.HasColor;
            Style focus = context.HasColor ? context.Style(ThemeToken.Focus) : Style.Plain;
            Style row = focused ? focus : Style.Plain;
            if (y >= 0 && y < area) {
                if (focused && context.HasColor) {
                    region.Fill(0, y, region.Width, 1, " ", focus);
                }
                if (focused) {
                    region.Put(0, y, g.FocusBar, context.Style(ThemeToken.Accent).WithBackgroundColor(row.BackgroundColor));
                }
                Style labelStyle = row.Combine(context.Style(focused ? ThemeToken.Text : ThemeToken.Muted));
                if (focused) {
                    labelStyle = labelStyle.With(TextDecoration.Bold);
                }
                string label = field.Label;
                int end = region.Write(2, y, TextWrapper.Truncate(label, labelWidth - 2, context.Ellipsis), labelStyle);
                if (field.Required) {
                    region.Write(end, y, "*", row.Combine(context.Style(ThemeToken.Accent)));
                }
                DrawValue(region.Sub(valueX, y, valueWidth, 1), context, frame, field, state, focused, row, brackets);
            }
            int next = y + 1;
            if (focused && state.Suggestions.Count > 0 && state.Editor != null && !state.Editor.IsEmpty) {
                for (int i = 0; i < Math.Min(4, state.Suggestions.Count); i++, next++) {
                    if (next < 0 || next >= area) {
                        continue;
                    }
                    bool selected = i == state.SuggestionIndex;
                    string suggestion = state.Suggestions[i];
                    region.Write(valueX, next, (selected ? g.Pointer : " ") + " " + TextWrapper.Truncate(suggestion, valueWidth - 3, context.Ellipsis), context.Style(selected ? ThemeToken.Accent : ThemeToken.Muted));
                }
            }
            if (state.Error != null && next >= 0 && next < area) {
                region.Write(valueX, next, TextWrapper.Truncate(ChoicePane.Problem(state.Error, context), valueWidth, context.Ellipsis));
            }
        }

        private void DrawValue(Region cell, RenderContext context, ViewFrame frame, Field field, FieldState state, bool focused, Style row, bool brackets) {
            InteractiveGlyphs g = InteractiveGlyphs.For(context);
            Style text = row.Combine(context.Style(ThemeToken.Text));
            Style muted = row.Combine(context.Style(ThemeToken.Muted));
            switch (field.Kind) {
                case FieldKind.Toggle:
                    bool on = field.Value is true;
                    int x = cell.Write(0, 0, on ? g.Checked : g.Unchecked, row.Combine(context.Style(on ? ThemeToken.Accent : ThemeToken.Muted)));
                    cell.Write(x + 1, 0, field.Display(field.Value, Labels), on ? text : muted);
                    return;
                case FieldKind.Choice:
                case FieldKind.MultiChoice:
                    if (state.Loading) {
                        IReadOnlyList<string> frames = context.Glyphs.SpinnerFrames;
                        string spinner = frames[(int)(context.Tick % frames.Count)];
                        int sx = cell.Write(0, 0, spinner, row.Combine(context.Style(ThemeToken.Running)));
                        cell.Write(sx + 1, 0, Labels.Loading, muted.With(TextDecoration.Italic));
                        return;
                    }
                    if (state.LoadError != null) {
                        cell.Write(0, 0, TextWrapper.Truncate(state.LoadError, Math.Max(1, cell.Width - 1), context.Ellipsis), row.Combine(context.Style(ThemeToken.Warn)));
                        return;
                    }
                    string display = field.Display(field.Value, Labels);
                    if (field.Kind == FieldKind.MultiChoice && field.Value is System.Collections.ICollection collection && collection.Count > 0) {
                        display = PromptLabels.Format(Labels.SelectedCount, collection.Count, ((ChoiceField)field).EnabledCount) + "  " + g.Separator + "  " + display;
                    }
                    bool empty = display.Length == 0;
                    string shown = empty ? (field.Kind == FieldKind.MultiChoice ? Labels.KeyNone : "—") : display;
                    string dropdown = " " + g.Dropdown;
                    int width = Math.Max(1, cell.Width - DisplayWidth.Measure(dropdown) - 1);
                    string value = TextWrapper.Truncate(shown, width, context.Ellipsis);
                    if (focused && brackets) {
                        value = "[" + TextWrapper.Truncate(shown, Math.Max(1, width - 2), context.Ellipsis) + "]";
                    }
                    int vx = cell.Write(0, 0, value, empty ? muted : text);
                    cell.Write(vx, 0, dropdown, row.Combine(context.Style(focused ? ThemeToken.Accent : ThemeToken.Subtle)));
                    return;
            }
            LineEditor? editor = state.Editor;
            if (editor == null) {
                cell.Write(0, 0, field.Display(field.Value, Labels), text);
                return;
            }
            string? placeholder = field is TextField tf ? tf.Placeholder : null;
            string? mask = field.Kind == FieldKind.Secret && !state.Revealed ? (g.BracketFocus ? "*" : "•") : null;
            if (focused) {
                if (brackets) {
                    cell.Write(0, 0, "[", text);
                    cell.Write(cell.Width - 1, 0, "]", text);
                    cell = cell.Sub(1, 0, Math.Max(1, cell.Width - 2), 1);
                }
                TextInput input = new TextInput(editor, frame.Caret) { Placeholder = placeholder, Mask = mask, Focused = true };
                input.Render(cell, context);
                if (context.HasColor) {
                    // The editor draws on the plain background; keep the focus band behind it.
                    for (int i = 0; i < cell.Width; i++) {
                        Cell c = cell.Get(i, 0);
                        cell.SetStyle(i, 0, c.Style.WithBackgroundColor(row.BackgroundColor));
                    }
                }
                return;
            }
            if (editor.IsEmpty) {
                cell.Write(0, 0, placeholder ?? "—", muted.With(TextDecoration.Italic));
                return;
            }
            string shownText = mask != null ? new string(mask[0], Math.Min(editor.Length, 12)) : editor.Text;
            cell.Write(0, 0, TextWrapper.Truncate(shownText, Math.Max(1, cell.Width - 1), context.Ellipsis), text);
        }

        private void RenderReview(Region region, RenderContext context, bool active) {
            List<KeyValuePair<string, string>> rows = new List<KeyValuePair<string, string>>();
            for (int p = 0; p < _form.Pages.Count; p++) {
                foreach (Field field in VisibleFields(p)) {
                    string value = field.Display(field.Value, Labels);
                    if (field is ChoiceField choice && choice.Multiple && field.Value is System.Collections.ICollection items) {
                        value = items.Count == 0 ? string.Empty : PromptLabels.Format(Labels.SelectedCount, items.Count, choice.EnabledCount) + ": " + value;
                    }
                    rows.Add(new KeyValuePair<string, string>(field.Label, value));
                }
            }
            string? command = string.IsNullOrEmpty(_form.Command) ? null : _form.BuildCommand();
            ReviewDrawing.Draw(region, context, rows, new string[0], command, Labels.EquivalentCommand, ReviewCaptions(), _button, active);
        }
    }
}
