using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A choice list with its furniture: the filter row, the rows, a preview pane (right of the list when there is room,
    /// below it otherwise), the live selection count and the line for the focused choice's description, a disabled reason
    /// or a limit message. Used by the selection prompts, menu panes and form choice editors.
    /// </summary>
    internal sealed class ChoicePane : IWidget {
        private readonly ChoiceList _list;
        private Choice? _previewFor;
        private IWidget? _preview;
        private bool _previewResolved;

        public ChoicePane(ChoiceList list, CaretSlot caret) {
            _list = list ?? throw new ArgumentNullException(nameof(list));
            Caret = caret ?? throw new ArgumentNullException(nameof(caret));
        }

        public ChoiceList List => _list;

        /// <summary>Where the filter box reports its caret.</summary>
        public CaretSlot Caret { get; set; }

        /// <summary>Builds preview content for the focused choice (cached per choice).</summary>
        public Func<Choice, IWidget?>? Preview { get; set; }

        public string? PreviewTitle { get; set; }

        /// <summary>The preferred number of list rows.</summary>
        public int PageSize { get; set; } = 10;

        /// <summary>Whether the pane sizes to its content (inline) or fills the height it gets.</summary>
        public bool Fill { get; set; }

        public bool ShowFilterRow { get; set; } = true;

        public bool ShowInfoLine { get; set; } = true;

        /// <summary>Whether the info line shows the selection count (off where a title caption already shows it).</summary>
        public bool ShowCount { get; set; } = true;

        /// <summary>A validation error that wins over descriptions.</summary>
        public string? Error { get; set; }

        public PromptLabels Labels => _list.Labels;

        public bool HasPreview => Preview != null || HasChoicePreviews();

        private bool HasChoicePreviews() {
            foreach (Choice c in _list.Choices) {
                if (c.Preview != null) {
                    return true;
                }
            }
            return false;
        }

        private int FixedRows => (FilterVisible ? 1 : 0) + (ShowInfoLine ? 1 : 0);

        private bool FilterVisible => ShowFilterRow && (!_list.HasHotkeys || _list.FilterMode || !_list.Filter.IsEmpty);

        public Size Measure(Constraints constraints, RenderContext context) {
            if (Fill && !constraints.IsHeightUnbounded) {
                return new Size(constraints.MaxWidth, constraints.MaxHeight);
            }
            int rows = _list.DesiredRows(PageSize);
            int height = FixedRows + rows;
            if (HasPreview && !SideBySide(constraints.MaxWidth)) {
                height += Math.Min(7, Math.Max(0, (constraints.IsHeightUnbounded ? 30 : constraints.MaxHeight) - height));
            }
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        private static bool SideBySide(int width) => width >= 84;

        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            int width = region.Width;
            int listWidth = width;
            int previewWidth = 0;
            bool side = HasPreview && SideBySide(width);
            if (side) {
                previewWidth = Math.Max(32, width * 2 / 5);
                listWidth = _list.RowWidth(context, width - previewWidth - 1);
                previewWidth = width - listWidth - 1;
            } else {
                listWidth = _list.RowWidth(context, width);
            }
            int y = 0;
            if (FilterVisible) {
                DrawFilter(region.Sub(0, 0, listWidth, 1), context);
                y++;
            }
            int below = 0;
            int infoRows = ShowInfoLine ? 1 : 0;
            int available = region.Height - y - infoRows;
            if (HasPreview && !side) {
                below = Math.Min(7, Math.Max(0, available - Math.Min(_list.DesiredRows(PageSize), 3)));
                if (below < 4) {
                    below = 0;
                }
            }
            int listRows = Math.Max(1, available - below);
            if (!Fill) {
                listRows = Math.Min(listRows, _list.DesiredRows(PageSize));
            }
            _list.Render(region.Sub(0, y, listWidth, listRows), context);
            int listBottom = y + listRows;
            if (ShowInfoLine && listBottom < region.Height) {
                DrawInfo(region.Sub(0, Fill ? region.Height - 1 - below : listBottom, side ? listWidth : width, 1), context, listWidth);
            }
            if (side) {
                int previewHeight = Fill ? region.Height : Math.Max(listRows + y + infoRows, 6);
                DrawPreview(region.Sub(listWidth + 1, 0, previewWidth, Math.Min(region.Height, previewHeight)), context);
            } else if (below > 0) {
                DrawPreview(region.Sub(0, region.Height - below, width, below), context);
            }
        }

        // The selection count sits at the right end of the filter row (fzf style), so the info line below the list keeps its
        // full width for descriptions and disabled reasons; lists without a filter row show the count on the info line.
        private void DrawFilter(Region row, RenderContext context) {
            row.Write(0, 0, InteractiveGlyphs.For(context).Pointer + " ", context.Style(ThemeToken.Accent));
            StyledLine? count = CountLine(context);
            int inputWidth = row.Width - 2;
            if (count != null && CountOnFilterRow(row.Width)) {
                row.Write(row.Width - count.Width - 1, 0, count);
                inputWidth = row.Width - count.Width - 5;
            }
            TextInput input = new TextInput(_list.Filter, Caret) {
                Placeholder = Labels.TypeToFilter,
                Focused = _list.Focused
            };
            input.Render(row.Sub(2, 0, Math.Max(1, inputWidth), 1), context);
        }

        private bool CountOnFilterRow(int listWidth) => FilterVisible && _list.Multi && ShowCount && listWidth > 16 + PromptLabels.Format(Labels.SelectedCount, _list.Total, _list.Total).Length;

        private StyledLine? CountLine(RenderContext context) {
            if (!_list.Multi || !ShowCount) {
                return null;
            }
            string text = PromptLabels.Format(Labels.SelectedCount, _list.SelectedCount, _list.Total);
            if (_list.IsFiltering || _list.SelectedOnly) {
                text += " " + InteractiveGlyphs.For(context).Separator + " " + PromptLabels.Format(Labels.Shown, _list.VisibleItemCount);
            }
            return StyledLine.From(text, context.Style(_list.SelectedCount > 0 ? ThemeToken.Accent : ThemeToken.Muted));
        }

        private void DrawInfo(Region row, RenderContext context, int countEdge) {
            StyledLine line;
            Choice? focused = _list.FocusedChoice;
            if (!string.IsNullOrEmpty(Error)) {
                line = Problem(Error!, context);
            } else if (!string.IsNullOrEmpty(_list.Message)) {
                line = new StyledLine(new[] {
                    new TextSpan(context.Glyphs.Get(Glyphs.Glyph.Warning) + " ", context.Style(ThemeToken.Warn)),
                    new TextSpan(_list.Message!, context.Style(ThemeToken.Warn))
                });
            } else if (focused != null && !focused.IsEnabled) {
                line = StyledLine.From(PromptLabels.Format(Labels.Disabled, focused.DisabledReason), context.Style(ThemeToken.Muted).With(TextDecoration.Italic));
            } else if (focused != null && !string.IsNullOrEmpty(focused.Description) && !HasPreview) {
                line = StyledLine.From(focused.Description!, context.Style(ThemeToken.Muted));
            } else {
                line = StyledLine.Empty;
            }
            StyledLine? count = CountOnFilterRow(countEdge) ? null : CountLine(context);
            int edge = Math.Min(row.Width, countEdge);
            int right = count == null ? 0 : count.Width + 2;
            row.Write(2, 0, TextWrapper.Truncate(line, Math.Max(1, (count == null ? row.Width : edge) - 3 - right), context.Ellipsis));
            if (count != null && edge - count.Width - 1 > 2) {
                row.Write(edge - count.Width - 1, 0, count);
            }
        }

        internal static StyledLine Problem(string text, RenderContext context) {
            return new StyledLine(new[] {
                new TextSpan(context.Glyphs.Get(Glyphs.Glyph.Cross) + " ", context.Style(ThemeToken.Fail)),
                new TextSpan(text, context.Style(ThemeToken.Fail))
            });
        }

        private void DrawPreview(Region region, RenderContext context) {
            Choice? focused = _list.FocusedChoice;
            if (!ReferenceEquals(focused, _previewFor) || !_previewResolved) {
                _previewFor = focused;
                _previewResolved = true;
                _preview = null;
                if (focused != null) {
                    try {
                        _preview = focused.Preview ?? Preview?.Invoke(focused);
                    } catch (Exception ex) {
                        _preview = TextBlock.Plain(ex.Message, ThemeToken.Fail);
                    }
                }
            }
            string? group = _list.FocusedGroup;
            IWidget body = _preview ?? (focused != null && !string.IsNullOrEmpty(focused.Description) ? (IWidget)new UiParagraph(focused.Description!, ThemeToken.Text, 20) : group != null ? GroupSummary(group) : UiLine.Empty);
            string title = focused != null ? Markup.Escape(focused.Title) : group != null ? Markup.Escape(group) : PreviewTitle ?? Labels.Preview;
            Panel panel = new Panel(body, title) { BorderToken = ThemeToken.Border };
            panel.Render(region, context);
        }

        // A focused group previews its members and how many are selected.
        private IWidget GroupSummary(string group) {
            return new GroupPreview(_list, group);
        }

        private sealed class GroupPreview : IWidget {
            private readonly ChoiceList _list;
            private readonly string _group;

            public GroupPreview(ChoiceList list, string group) {
                _list = list;
                _group = group;
            }

            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? 8 : constraints.MaxHeight);

            public void Render(Region region, RenderContext context) {
                InteractiveGlyphs g = InteractiveGlyphs.For(context);
                int total = 0;
                int selected = 0;
                foreach (Choice c in _list.Choices) {
                    if (string.Equals(c.Group, _group, StringComparison.Ordinal)) {
                        total++;
                        if (_list.IsSelected(c)) {
                            selected++;
                        }
                    }
                }
                region.Write(0, 0, PromptLabels.Format(_list.Labels.SelectedCount, selected, total), context.Style(selected > 0 ? ThemeToken.Accent : ThemeToken.Muted));
                int y = 2;
                foreach (Choice c in _list.Choices) {
                    if (y >= region.Height || !string.Equals(c.Group, _group, StringComparison.Ordinal)) {
                        continue;
                    }
                    bool on = _list.IsSelected(c);
                    region.Write(0, y, (on ? g.Checked : g.Unchecked) + " ", context.Style(on ? ThemeToken.Accent : ThemeToken.Subtle));
                    region.Write(4, y, TextWrapper.Truncate(c.Title, Math.Max(1, region.Width - 5), context.Ellipsis), context.Style(c.IsEnabled ? ThemeToken.Text : ThemeToken.Muted));
                    y++;
                }
            }
        }

        /// <summary>Clears the preview cache (content changed).</summary>
        public void InvalidatePreview() => _previewResolved = false;
    }
}
