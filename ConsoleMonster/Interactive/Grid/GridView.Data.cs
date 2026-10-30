using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    internal sealed class GridActionDetails : IWidget {
        private readonly GridDetailModal _details;
        internal GridActionDetails(GridView view) { _details = new GridDetailModal(view); }
        public Size Measure(Constraints constraints, RenderContext context) => _details.BuildRecord(out _).Measure(constraints, context);
        public void Render(Region region, RenderContext context) => _details.BuildRecord(out _).Render(region, context);
    }

    /// <summary>The focused row as a record (every column, including hidden ones); Up and Down move to the next row.</summary>
    internal sealed class GridDetailModal : Modal {
        private readonly GridView _view;

        public GridDetailModal(GridView view) {
            _view = view;
        }

        public override int MaxWidth => 72;

        public override IWidget Build(ViewFrame frame, CaretSlot caret) {
            RecordView record = BuildRecord(out string title);
            Rows rows = new Rows();
            rows.Add(record);
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            rows.Add(new KeyFooter(bindings));
            return DialogChrome.Frame(rows, title.Length == 0 ? _view.Grid.Title : title);
        }

        internal RecordView BuildRecord(out string title) {
            GridRow? row = _view.FocusedRow;
            RecordView record = new RecordView { Presentation = _view.Grid.Presentation, HideEmpty = false };
            title = string.Empty;
            if (row != null) {
                for (int c = 0; c < _view.Grid.Columns.Count; c++) {
                    TableColumn column = _view.Grid.Columns[c];
                    object? value = c < row.Values.Count ? row.Values[c] : null;
                    if (title.Length == 0 && !PresenterValues.IsEmpty(value)) {
                        title = PresenterValues.ToText(value is DataGridCell lazy ? lazy.Peek() : value, _view.Grid.Presentation.Culture ?? CultureInfo.CurrentCulture);
                    }
                    record.AddField(Markup.Strip(column.Header), TableCell.FromValue(value), column.Presenter, column.RoleRules.Count > 0 ? new List<RoleRule>(column.RoleRules) : null);
                }
            }
            return record;
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            bindings.Add(new KeyBinding(_view.Labels.KeyMove, () => _view.StepFocus(-1), new KeyChord(ConsoleKey.UpArrow)) { KeyText = InteractiveGlyphs.For(frame.Context).KeyUpDown, Order = 10 });
            bindings.Add(new KeyBinding(string.Empty, () => _view.StepFocus(1), new KeyChord(ConsoleKey.DownArrow)) { InFooter = false });
            bindings.Add(new KeyBinding(_view.Labels.KeyClose, Close, new KeyChord(ConsoleKey.Escape), new KeyChord(ConsoleKey.Enter)) { KeyText = "esc", Order = 40 });
        }
    }

    /// <summary>The column chooser: Space shows or hides the focused column (one always stays).</summary>
    internal sealed class GridColumnsModal : Modal {
        private readonly GridView _view;
        private int _focus;

        public GridColumnsModal(GridView view) {
            _view = view;
        }

        public override int MaxWidth => 48;

        public override IWidget Build(ViewFrame frame, CaretSlot caret) {
            Rows rows = new Rows();
            rows.Add(new UiLine(context => StyledLine.Empty));
            for (int c = 0; c < _view.Grid.Columns.Count; c++) {
                int column = c;
                rows.Add(new UiLine(context => {
                    InteractiveGlyphs g = InteractiveGlyphs.For(context);
                    bool shown = !_view.IsHidden(column);
                    bool focused = column == _focus;
                    Style text = focused ? context.Style(ThemeToken.Focus).With(TextDecoration.Bold) : context.Style(ThemeToken.Text);
                    return new StyledLine(new[] {
                        new TextSpan(focused ? "▌" : " ", context.Style(ThemeToken.Accent)),
                        new TextSpan(shown ? "[" + context.Glyphs.Get(Glyph.Check) + "] " : "[ ] ", shown ? context.Style(ThemeToken.Accent) : context.Style(ThemeToken.Subtle)),
                        new TextSpan(Markup.Strip(_view.Grid.Columns[column].Header), text)
                    });
                }));
            }
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            rows.Add(new KeyFooter(bindings));
            return DialogChrome.Frame(rows, _view.Labels.Columns);
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            int count = _view.Grid.Columns.Count;
            bindings.Add(new KeyBinding(_view.Labels.KeyMove, () => _focus = (_focus + count - 1) % count, new KeyChord(ConsoleKey.UpArrow)) { KeyText = InteractiveGlyphs.For(frame.Context).KeyUpDown, Order = 10 });
            bindings.Add(new KeyBinding(string.Empty, () => _focus = (_focus + 1) % count, new KeyChord(ConsoleKey.DownArrow)) { InFooter = false });
            bindings.Add(new KeyBinding(_view.Labels.KeyToggle, () => _view.SetHidden(_focus, !_view.IsHidden(_focus)), new KeyChord(ConsoleKey.Spacebar), new KeyChord(' ')) { KeyText = "space", Order = 20 });
            bindings.Add(new KeyBinding(_view.Labels.KeyClose, Close, new KeyChord(ConsoleKey.Escape), new KeyChord(ConsoleKey.Enter)) { KeyText = "esc", Order = 40 });
        }
    }

    /// <summary>Runs a <see cref="Grid"/> on the screen, or as a numbered list in line mode.</summary>
    internal static class GridRunner {
        public static GridResult Run(Grid grid, GridOptions options, System.Threading.CancellationToken cancellationToken) {
            // Capability discovery can enable VT. Capture the caller's output mode before resolving labels or profiles.
            using ConsoleMonster.Output.TerminalGuard outputMode = new ConsoleMonster.Output.TerminalGuard(() => { }, false);
            InteractiveOptions interactive = options.Interactive ?? InteractiveOptions.Default;
            PromptLabels labels = interactive.ResolveLabels();
            switch (interactive.ResolveMode()) {
                case InteractionMode.Snapshot:
                    throw new PromptUnavailableException("A grid needs an interactive session. Render its source rows as a table for a static view.");
                case InteractionMode.None:
                    throw new PromptUnavailableException("Grid '" + grid.Title + "' needs an interactive session.") { Prompt = grid.Title };
                case InteractionMode.Line:
                case InteractionMode.Accessible:
                    return Lines(grid, new LinePrompter(interactive.ResolveLineConsole(), labels, interactive.ResolveMode() == InteractionMode.Accessible), cancellationToken);
            }
            using (InputLease lease = ConsoleInput.Acquire(interactive.Input)) {
                Capabilities.TerminalProfile profile = interactive.ResolveProfile();
                RenderContext context = Terminal.CreateContext(profile, interactive.ResolveTheme());
                IPromptSurface? surface = interactive.Surface;
                bool owns = false;
                if (surface == null) {
                    surface = PromptHost.Resolve(context) ?? new ScreenSurface(profile);
                    owns = true;
                }
                try {
                    using MouseReporting? mouse = MouseReporting.Open(true, interactive, profile, lease.Source, interactive.Surface != null);
                    using GridView view = new GridView(grid, options, labels, cancellationToken, interactive.ActionClock);
                    InteractionLoop.Run(view, lease.Source, surface, context, cancellationToken);
                    surface.Complete(view.Summary(context));
                    return new GridResult(view.Accepted, view.Accepted ? view.ChosenItems() : new List<object?>());
                } finally {
                    if (owns) {
                        surface.Dispose();
                    }
                }
            }
        }

        private static GridResult Lines(Grid grid, LinePrompter lines, System.Threading.CancellationToken cancellationToken) {
            List<Choice> choices = new List<Choice>();
            CultureInfo culture = grid.Presentation.Culture ?? CultureInfo.CurrentCulture;
            foreach (GridRow row in grid.Rows) {
                List<string> parts = new List<string>();
                foreach (object? value in row.Values) {
                    parts.Add(PresenterValues.ToText(value, culture));
                }
                choices.Add(new Choice(row.Item, string.Join(" | ", parts)));
            }
            List<object?> items = new List<object?>();
            if (grid.OutputMode == GridSelection.Multiple) {
                foreach (int index in lines.MultiSelect(grid.Title, null, choices, null, null, cancellationToken)) {
                    items.Add(grid.Rows[index].Item);
                }
            } else {
                int index = lines.Select(grid.Title, null, choices, 0, cancellationToken);
                if (grid.OutputMode == GridSelection.Single && index >= 0) {
                    items.Add(grid.Rows[index].Item);
                }
            }
            return new GridResult(true, items);
        }
    }
}
