using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The document viewer <see cref="WidgetPager"/> opens for Markdown with headings and for a command's help: an
    /// outline beside the text (a <see cref="Splitter"/>), the current section's path pinned above it, headings that fold,
    /// tabs for the top-level parts, find that opens folded sections, links with back and forward history, and the palette
    /// listing sections and parameters. Every pointer action has a key, and the footer lists the keys.
    /// </summary>
    internal sealed class DocumentView : InteractiveView {
        private const int CardMinimumHeight = 18;
        private const int OutlineMinimumWidth = 72;
        private readonly int? _contentWidth;
        private readonly Func<string, IWidget?>? _open;
        private readonly FindBar _find = new FindBar();
        private readonly List<Modal> _modals = new List<Modal>();
        // The viewer is built from the same parts apps use: an Outline beside the text, a Breadcrumb following it as the
        // pinned row, a NavigationHistory for links, a Splitter between them and the shared tab strip.
        private readonly NavigationHistory<Location> _history = new NavigationHistory<Location>();
        private readonly Outline _outline = new Outline("outline");
        private readonly Breadcrumb _crumb = new Breadcrumb("path");
        private List<int> _outlineSections = new List<int>();
        private DocumentPart? _outlinePart;
        private Page? _outlinePage;
        private int _outlinePartIndex = -1;
        private readonly Splitter _splitter;
        private Page _page;
        private Pane _focus = Pane.Document;
        private bool? _outlineChoice;
        // What the last frame drew, which keys and the pointer act on.
        private bool _outlineShown;
        private int _docWidth = 1;
        private int _pageRows = 1;
        private bool _pinned;
        private Rect _docArea;
        private Rect _outlineArea;
        private List<TabStrip.Header> _tabHeaders = new List<TabStrip.Header>();

        private int _followed = -1;
        // The section a jump went to, which stays current while its heading is on screen even when the end of the
        // document keeps it from reaching the top; any scrolling hands back to the reading position.
        private int _jumped = -1;

        private enum Pane { Strip, Outline, Document }

        internal DocumentView(IWidget content, string title, DocumentModel model, PromptLabels labels, int? contentWidth = null, Func<string, IWidget?>? open = null) : base(labels) {
            _contentWidth = contentWidth;
            _open = open;
            _page = new Page(content, title, model);
            _splitter = new Splitter(new OutlinePane(this), new DocumentPane(this)) { Ratio = 0.28 };
            _crumb.Outline = _outline;
            // The outline asks for focus when clicked; in the viewer that makes it the pane the keys go to.
            _outline.FocusRequested += control => { _focus = Pane.Outline; control.Focused = true; };
            _outline.Opened += (sender, item) => {
                int index = _outline.CurrentIndex;
                if (index >= 0 && index < _outlineSections.Count) { JumpTo(_page.Part, _outlineSections[index], false, false); }
            };
        }

        private DocumentPart Part => _page.Model.Parts[_page.Part];

        private PartState State => _page.States[_page.Part];

        protected override IWidget Build(ViewFrame frame) {
            _modals.RemoveAll(m => m.IsClosed);
            _page.Refresh(frame.Context, _open != null);
            Rows rows = new Rows();
            int fixedRows = 2;
            IWidget? card = _page.Model.Header != null && frame.Height >= CardMinimumHeight ? _page.Model.Header : null;
            int cardRows = card == null ? 0 : Math.Min(6, card.Measure(new Constraints(frame.Width, frame.Height), frame.Context).Height);
            bool tabs = _page.Model.Parts.Count > 1;
            fixedRows += cardRows + (tabs ? TabStrip.Height : 0);
            int bodyRows = Math.Max(1, frame.Height - fixedRows);
            _outlineShown = (_outlineChoice ?? frame.Width >= OutlineMinimumWidth) && Outline().Count > 1 && frame.Width >= 20;
            if (!_outlineShown && _focus == Pane.Outline) { _focus = Pane.Document; }
            if (!tabs && _focus == Pane.Strip) { _focus = Pane.Document; }
            int paneWidth = _outlineShown ? Math.Max(1, frame.Width - _splitter.FirstExtent(frame.Width) - 1) : frame.Width;
            _docWidth = Math.Max(1, Math.Min(paneWidth, _contentWidth ?? paneWidth));
            _pinned = HasHeadings(Part);
            _pageRows = Math.Max(1, bodyRows - (_pinned ? 1 : 0));
            DocumentLayout layout = Layout();
            if (_find.Active) { _find.Search(layout.Lines, State.Top); }
            Move(0);
            TrackOutline();
            rows.Add(new HeaderBar(Markup.Escape(_page.Title)) {
                HeaderRight = (State.Top + 1).ToString(CultureInfo.InvariantCulture) + "–"
                    + Math.Min(layout.Height, State.Top + _pageRows).ToString(CultureInfo.InvariantCulture)
                    + " / " + layout.Height.ToString(CultureInfo.InvariantCulture)
            }, LayoutSize.Fixed(1));
            if (card != null) { rows.Add(card, LayoutSize.Fixed(cardRows)); }
            if (tabs) { rows.Add(new StripWidget(this), LayoutSize.Fixed(TabStrip.Height)); }
            rows.Add(_outlineShown ? (IWidget)_splitter : new DocumentPane(this), LayoutSize.Fraction());
            if (_find.Open) { rows.Add(_find.Widget(Labels), LayoutSize.Fixed(1)); }
            else {
                List<KeyBinding> bindings = new List<KeyBinding>();
                AddBindings(bindings, frame);
                rows.Add(new KeyFooter(bindings) { Toast = CurrentToast }, LayoutSize.Fixed(1));
            }
            if (_modals.Count > 0) {
                Modal top = _modals[_modals.Count - 1];
                return new Overlay(rows, top.Build(frame, frame.Caret), dim: true) { Top = top.Top, MaxWidth = top.MaxWidth };
            }
            return rows;
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            if (_modals.Count > 0) { _modals[_modals.Count - 1].AddBindings(bindings, frame); return; }
            if (_find.Open) { FindBar.AddKeys(bindings, Labels); return; }
            InteractiveGlyphs glyphs = InteractiveGlyphs.For(frame.Context);
            int parts = _page.Model.Parts.Count;
            switch (_focus) {
                case Pane.Strip:
                    bindings.Add(new KeyBinding(Labels.KeyTabs, () => SelectPart(_page.Part + 1), new KeyChord(ConsoleKey.RightArrow)) { KeyText = glyphs.KeyLeftRight, Order = 10 });
                    bindings.Add(new KeyBinding(string.Empty, () => SelectPart(_page.Part - 1), new KeyChord(ConsoleKey.LeftArrow)) { InFooter = false });
                    bindings.Add(new KeyBinding(string.Empty, () => SelectPart(0), new KeyChord(ConsoleKey.Home)) { InFooter = false });
                    bindings.Add(new KeyBinding(string.Empty, () => SelectPart(parts - 1), new KeyChord(ConsoleKey.End)) { InFooter = false });
                    bindings.Add(new KeyBinding(string.Empty, () => _focus = Pane.Document, new KeyChord(ConsoleKey.DownArrow), new KeyChord(ConsoleKey.Enter)) { InFooter = false });
                    break;
                case Pane.Outline:
                    // The outline takes its own keys; these list them and hand them over.
                    bindings.Add(new KeyBinding(Labels.KeyMove, () => OutlineKey(ConsoleKey.UpArrow), new KeyChord(ConsoleKey.UpArrow)) { KeyText = glyphs.KeyUpDown, Order = 10 });
                    foreach (ConsoleKey key in new[] { ConsoleKey.DownArrow, ConsoleKey.Home, ConsoleKey.End, ConsoleKey.PageUp, ConsoleKey.PageDown }) {
                        ConsoleKey pressed = key;
                        bindings.Add(new KeyBinding(string.Empty, () => OutlineKey(pressed), new KeyChord(pressed)) { InFooter = false });
                    }
                    bindings.Add(new KeyBinding(Labels.KeyOpen, () => OutlineKey(ConsoleKey.Enter), new KeyChord(ConsoleKey.Enter)) { Order = 15 });
                    break;
                default:
                    bindings.Add(new KeyBinding(Labels.KeyMove, () => Move(-1), new KeyChord(ConsoleKey.UpArrow)) { KeyText = glyphs.KeyUpDown, Order = 10 });
                    bindings.Add(new KeyBinding(string.Empty, () => Move(1), new KeyChord(ConsoleKey.DownArrow)) { InFooter = false });
                    bindings.Add(new KeyBinding(Labels.KeyPage, () => Move(_pageRows), new KeyChord(ConsoleKey.PageDown), new KeyChord(ConsoleKey.Spacebar), new KeyChord(' ')) { KeyText = "pgup/pgdn", Order = 50 });
                    bindings.Add(new KeyBinding(string.Empty, () => Move(-_pageRows), new KeyChord(ConsoleKey.PageUp)) { InFooter = false });
                    bindings.Add(new KeyBinding(string.Empty, () => Move(-State.Top), new KeyChord(ConsoleKey.Home)) { InFooter = false });
                    bindings.Add(new KeyBinding(string.Empty, () => Move(int.MaxValue), new KeyChord(ConsoleKey.End)) { InFooter = false });
                    if (HasHeadings(Part) || SelectedLink() != null) {
                        bindings.Add(new KeyBinding(Labels.KeyFollow, Activate, new KeyChord(ConsoleKey.Enter)) { KeyText = glyphs.Enter, Order = 15 });
                    }
                    if (HasHeadings(Part)) {
                        bindings.Add(new KeyBinding(Labels.KeyExpand, () => Fold(CurrentSection(), true), new KeyChord(ConsoleKey.LeftArrow)) { KeyText = glyphs.KeyLeftRight, InFooter = false });
                        bindings.Add(new KeyBinding(string.Empty, () => Fold(CurrentSection(), false), new KeyChord(ConsoleKey.RightArrow)) { InFooter = false });
                    }
                    if (Layout().Links.Count > 0) {
                        bindings.Add(new KeyBinding(Labels.KeyLinks, () => StepLink(1), new KeyChord(']')) { KeyText = "[/]", Order = 27 });
                        bindings.Add(new KeyBinding(string.Empty, () => StepLink(-1), new KeyChord('[')) { InFooter = false });
                    }
                    break;
            }
            if (PaneCount() > 1) {
                bindings.Add(new KeyBinding(Labels.KeyPane, () => CyclePane(1), new KeyChord(ConsoleKey.Tab)) { KeyText = "tab", Order = 25 });
                bindings.Add(new KeyBinding(string.Empty, () => CyclePane(-1), new KeyChord(ConsoleKey.Tab, KeyModifiers.Shift)) { InFooter = false });
            }
            if (parts > 1) {
                for (int i = 0; i < Math.Min(9, parts); i++) {
                    int part = i;
                    bindings.Add(new KeyBinding(i == 0 ? Labels.KeyOpenTab : string.Empty, () => SelectPart(part), new KeyChord((char)('1' + i))) { KeyText = Tabs.DigitKeys(parts), Order = 26, InFooter = i == 0 });
                }
                bindings.Add(new KeyBinding(string.Empty, () => SelectPart(_page.Part + 1), new KeyChord(ConsoleKey.PageDown, KeyModifiers.Control)) { InFooter = false });
                bindings.Add(new KeyBinding(string.Empty, () => SelectPart(_page.Part - 1), new KeyChord(ConsoleKey.PageUp, KeyModifiers.Control)) { InFooter = false });
            }
            bindings.Add(new KeyBinding(Labels.KeySearch, OpenFind, new KeyChord('/'), new KeyChord(ConsoleKey.F, KeyModifiers.Control)) { KeyText = "/", Order = 30 });
            if (_find.Active) {
                bindings.Add(new KeyBinding(Labels.KeyNext, () => StepMatch(1), new KeyChord('n')) { Order = 31, InFooter = false });
                bindings.Add(new KeyBinding(Labels.KeyPrevious, () => StepMatch(-1), new KeyChord('N')) { Order = 32, InFooter = false });
            }
            bindings.Add(new KeyBinding(Labels.KeyGoTo, OpenPalette, new KeyChord(ConsoleKey.K, KeyModifiers.Control)) { KeyText = "ctrl+k", Order = 33 });
            if (_history.CanGoBack || _history.CanGoForward) {
                bindings.Add(new KeyBinding(Labels.KeyBackForward, Back, new KeyChord(ConsoleKey.LeftArrow, KeyModifiers.Alt), new KeyChord(ConsoleKey.Backspace)) { KeyText = "alt+←/→", Order = 34 });
                bindings.Add(new KeyBinding(string.Empty, Forward, new KeyChord(ConsoleKey.RightArrow, KeyModifiers.Alt)) { InFooter = false });
            }
            if (Outline().Count > 1) {
                bindings.Add(new KeyBinding(Labels.KeyOutline, () => { _outlineChoice = !_outlineShown; }, new KeyChord('o')) { KeyText = "o", Order = 36, InFooter = false });
            }
            if (_outlineShown) {
                bindings.Add(new KeyBinding(Labels.KeyResize, () => Resize(-0.03), new KeyChord(ConsoleKey.LeftArrow, KeyModifiers.Control | KeyModifiers.Shift)) { KeyText = "ctrl+shift+←/→", Order = 60, InFooter = false });
                bindings.Add(new KeyBinding(string.Empty, () => Resize(0.03), new KeyChord(ConsoleKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift)) { InFooter = false });
            }
            if (_find.Active) { bindings.Add(new KeyBinding(string.Empty, () => _find.Close(false), new KeyChord(ConsoleKey.Escape)) { InFooter = false }); }
            bindings.Add(new KeyBinding(Labels.KeyClose, () => IsDone = true, new KeyChord(ConsoleKey.Escape), new KeyChord('q')) { KeyText = "esc/q", Order = 22 });
            bindings.Add(HelpBinding());
        }

        protected override bool HandlePriorityInput(InputEvent input) {
            if (_modals.Count > 0) { return false; }
            if (input.Kind == InputEventKind.Mouse) { return Pointer(input); }
            if (!_find.Open) { return false; }
            switch (_find.Handle(input)) {
                case FindKey.Edited: Search(); return true;
                case FindKey.Next: StepMatch(1); return true;
                case FindKey.Previous: StepMatch(-1); return true;
                case FindKey.Closed: _find.Close(true); return true;
                case FindKey.Kept: return true;
                default: return input.Kind == InputEventKind.Key && !input.Is(ConsoleKey.UpArrow) && !input.Is(ConsoleKey.DownArrow) && !input.Is(ConsoleKey.PageUp) && !input.Is(ConsoleKey.PageDown);
            }
        }

        protected override bool HandleUnbound(InputEvent input) => _modals.Count > 0 && _modals[_modals.Count - 1].HandleUnbound(input);

        public override bool Poll(TimeSpan now) {
            bool changed = base.Poll(now);
            foreach (Modal modal in _modals) { changed |= modal.Poll(now); }
            return changed | _modals.RemoveAll(m => m.IsClosed) > 0;
        }

        // --- Pointer -------------------------------------------------------------------------------------------------

        private bool Pointer(InputEvent input) {
            if (_outlineShown && ((IAppInputContainer)_splitter).HandleContainerInput(input, null)) { return true; }
            bool press = input.MouseAction == MouseAction.Down && input.MouseButton == 1;
            if (input.MouseAction == MouseAction.Wheel && input.WheelDelta != 0) {
                int rows = -Math.Sign(input.WheelDelta) * 3;
                if (_outlineShown && Inside(_outlineArea, input)) { _outline.HandleInput(input, LastFrame!.Context); }
                else { Move(rows); }
                return true;
            }
            if (!press) { return false; }
            foreach (TabStrip.Header header in _tabHeaders) {
                if (header.Contains(input.Column, input.Row)) { _focus = Pane.Strip; SelectPart(header.Index); return true; }
            }
            if (_outlineShown && Inside(_outlineArea, input)) {
                _focus = Pane.Outline;
                _outline.HandleInput(input, LastFrame!.Context);
                return true;
            }
            if (Inside(_docArea, input)) {
                _focus = Pane.Document;
                int row = input.Row - _docArea.Y;
                if (_pinned && row == 0) { _crumb.HandleInput(input, LastFrame!.Context); return true; }
                int line = State.Top + row - (_pinned ? 1 : 0);
                int column = input.Column - _docArea.X;
                DocumentLayout layout = Layout();
                if (line < 0 || line >= layout.Height) { return true; }
                for (int i = 0; i < layout.Links.Count; i++) {
                    DocumentLink link = layout.Links[i];
                    if (link.Row == line && column >= link.Column && column < link.Column + link.Width) { State.Link = i; Follow(link); return true; }
                }
                int section = layout.RowSections[line];
                if (layout.HeadingRows[section] == line) { Fold(section, !State.Folded[section]); }
                return true;
            }
            return false;
        }

        private static bool Inside(Rect area, InputEvent input) => input.Column >= area.X && input.Column < area.Right && input.Row >= area.Y && input.Row < area.Bottom;

        // --- Navigation ----------------------------------------------------------------------------------------------

        private DocumentLayout Layout() => _page.Layout(_docWidth);

        private void Move(int delta) {
            if (delta != 0) { _jumped = -1; }
            int max = Math.Max(0, Layout().Height - _pageRows);
            State.Top = (int)Math.Max(0L, Math.Min(max, (long)State.Top + delta));
        }

        private int CurrentSection() {
            DocumentLayout layout = Layout();
            if (layout.RowSections.Length == 0) { return 0; }
            if (_jumped >= 0 && _jumped < layout.HeadingRows.Length) {
                int heading = layout.HeadingRows[_jumped];
                if (heading >= State.Top && heading < State.Top + _pageRows) { return _jumped; }
            }
            return layout.RowSections[Math.Max(0, Math.Min(layout.RowSections.Length - 1, State.Top))];
        }

        // The outline lists the current part's headings and follows the reading position; the breadcrumb follows the outline.
        private void TrackOutline() {
            if (!ReferenceEquals(_outlinePart, Part)) {
                // The same part rebuilt for a new theme keeps the reader's place in the outline.
                bool samePart = ReferenceEquals(_outlinePage, _page) && _outlinePartIndex == _page.Part;
                _outlinePart = Part;
                _outlinePage = _page;
                _outlinePartIndex = _page.Part;
                _outlineSections = Outline();
                _outline.SetItems(_outlineSections.ConvertAll(section => new OutlineItem(Part.Sections[section].Title, Math.Max(1, Part.Sections[section].Level))), samePart);
                _crumb.Ancestors.Clear();
                if (_page.Model.Parts.Count > 1) { _crumb.Ancestors.Add(Part.Title); }
                _followed = -1;
            }
            _outline.Focused = _focus == Pane.Outline;
            int current = CurrentSection();
            if (current == _followed) { return; }
            _followed = current;
            List<int> path = DocumentLayout.Path(Part, current);
            _outline.CurrentIndex = path.Count == 0 ? -1 : _outlineSections.IndexOf(path[path.Count - 1]);
        }

        // The headings of the current part, which the outline and the palette list.
        private List<int> Outline() {
            List<int> headings = new List<int>();
            for (int i = 0; i < Part.Sections.Count; i++) { if (Part.Sections[i].Level > 0) { headings.Add(i); } }
            return headings;
        }

        private static bool HasHeadings(DocumentPart part) => part.Sections.Exists(section => section.Level > 0);

        private void OutlineKey(ConsoleKey key) { if (LastFrame != null) { _outline.HandleInput(InputEvent.Press(key), LastFrame.Context); } }
        /// <summary>Shows a section's heading at the top, unfolding the headings around it (and itself when asked).</summary>
        private void JumpTo(int part, int section, bool expand, bool remember) {
            if (remember) { Remember(); }
            SelectPart(part);
            List<int> path = DocumentLayout.Path(Part, section);
            for (int i = 0; i < path.Count; i++) {
                if (path[i] != section || expand) { State.Folded[path[i]] = false; }
            }
            _page.Invalidate();
            DocumentLayout layout = Layout();
            int row = layout.HeadingRows[section];
            if (row < 0) { row = Array.IndexOf(layout.RowSections, section); }
            State.Top = 0;
            Move(Math.Max(0, row));
            _jumped = section;
            _followed = -1;
        }

        private void SelectPart(int part) {
            int count = _page.Model.Parts.Count;
            int target = ((part % count) + count) % count;
            if (target == _page.Part) { return; }
            _page.Part = target;
            _followed = -1;
            _jumped = -1;
            if (_find.Active) { _find.Close(false); }
        }

        private void CyclePane(int delta) {
            List<Pane> panes = new List<Pane>();
            if (_page.Model.Parts.Count > 1) { panes.Add(Pane.Strip); }
            if (_outlineShown) { panes.Add(Pane.Outline); }
            panes.Add(Pane.Document);
            int at = Math.Max(0, panes.IndexOf(_focus));
            _focus = panes[(at + delta + panes.Count) % panes.Count];
        }

        private int PaneCount() => (_page.Model.Parts.Count > 1 ? 1 : 0) + (_outlineShown ? 1 : 0) + 1;

        private void Resize(double delta) => _splitter.Ratio = Math.Max(0.1, Math.Min(0.6, _splitter.Ratio + delta));

        private void Fold(int section, bool fold) {
            if (section < 0 || section >= Part.Sections.Count || Part.Sections[section].Level == 0) { return; }
            if (State.Folded[section] == fold) { return; }
            State.Folded[section] = fold;
            State.FindOpened.Remove(section);
            _page.Invalidate();
            // Folding keeps the heading in view.
            int row = Layout().HeadingRows[section];
            if (row >= 0 && row < State.Top) { State.Top = 0; Move(row); }
            Move(0);
        }

        // Enter on the text: the selected link when it is on screen, otherwise the current section's fold.
        private void Activate() {
            DocumentLink? link = SelectedLink();
            if (link != null) { Follow(link); return; }
            int section = CurrentSection();
            Fold(section, !State.Folded[section]);
        }

        private DocumentLink? SelectedLink() {
            DocumentLayout layout = Layout();
            int index = State.Link;
            if (index < 0 || index >= layout.Links.Count) { return null; }
            DocumentLink link = layout.Links[index];
            return link.Row >= State.Top && link.Row < State.Top + _pageRows ? link : null;
        }

        private void StepLink(int delta) {
            DocumentLayout layout = Layout();
            if (layout.Links.Count == 0) { return; }
            int current = SelectedLink() == null ? -1 : State.Link;
            int next = -1;
            if (current < 0) {
                // From nothing selected: the first link on screen going down, the last one going up.
                for (int i = 0; i < layout.Links.Count; i++) {
                    int row = layout.Links[i].Row;
                    if (row >= State.Top && row < State.Top + _pageRows) { next = i; if (delta > 0) { break; } }
                }
                if (next < 0) { next = delta > 0 ? FirstLinkAfter(layout, State.Top) : FirstLinkAfter(layout, State.Top) - 1; }
            } else {
                next = current + delta;
            }
            if (next < 0 || next >= layout.Links.Count) { return; }
            State.Link = next;
            int line = layout.Links[next].Row;
            if (line < State.Top || line >= State.Top + _pageRows) { State.Top = 0; Move(line - _pageRows / 3); }
        }

        private static int FirstLinkAfter(DocumentLayout layout, int row) {
            for (int i = 0; i < layout.Links.Count; i++) { if (layout.Links[i].Row >= row) { return i; } }
            return layout.Links.Count;
        }

        private void Follow(DocumentLink link) {
            string target = link.Target;
            if (target.StartsWith("#", StringComparison.Ordinal)) {
                string anchor = target.Substring(1);
                for (int p = 0; p < _page.Model.Parts.Count; p++) {
                    int found = _page.Model.Parts[p].Sections.FindIndex(section => section.Level > 0 && string.Equals(section.Anchor, anchor, StringComparison.OrdinalIgnoreCase));
                    if (found >= 0) { JumpTo(p, found, true, true); return; }
                }
            } else if (target.StartsWith(DocumentModel.HelpScheme, StringComparison.Ordinal) && _open != null) {
                string name = target.Substring(DocumentModel.HelpScheme.Length);
                IWidget? content = _open(name);
                if (content != null && LastFrame != null) {
                    Remember();
                    DocumentModel model = DocumentModel.From(content, name, LastFrame.Context, true) ?? DocumentModel.Plain(content, name);
                    _page = new Page(content, name, model);
                    _followed = -1;
                            _focus = Pane.Document;
                    if (_find.Active) { _find.Close(false); }
                    return;
                }
                target = name;
            }
            ShowToast(new Toast(PromptLabels.Format(Labels.LinkUnavailable, target), StatusKind.Warn));
        }

        private void Remember() {
            _history.Visit(new Location(_page, _page.Part, State.Top));
        }

        private void Back() { if (_history.TryBack(Here(), out Location place)) { Go(place); } }

        private void Forward() { if (_history.TryForward(Here(), out Location place)) { Go(place); } }

        private Location Here() => new Location(_page, _page.Part, State.Top);

        private void Go(Location place) {
            _page = place.Page;
            _page.Part = place.Part;
            State.Top = place.Top;
            _followed = -1;
            _jumped = -1;
            _outlinePart = null;
            if (_find.Active) { _find.Close(false); }
        }
        // --- Find and palette ----------------------------------------------------------------------------------------

        private void OpenFind() { _find.Start(); Search(); }

        // Folded sections holding the query open first, so every match is on a drawn row. Sections find opened fold again
        // once the query no longer matches inside them; the ones it leaves open stay open when the bar closes.
        private void Search() {
            string query = _find.Query;
            if (LastFrame != null) {
                RenderContext context = LastFrame.Context;
                bool changed = false;
                bool Matches(int index) {
                    if (query.Length == 0) { return false; }
                    int level = Part.Sections[index].Level;
                    for (int i = index; i < Part.Sections.Count && (i == index || Part.Sections[i].Level > level); i++) {
                        if (DocumentLayout.BodyText(Part.Sections[i], _docWidth, context).Exists(line => FindBar.Contains(line, query))) { return true; }
                    }
                    return false;
                }
                foreach (int index in new List<int>(State.FindOpened)) {
                    if (Matches(index)) { continue; }
                    State.FindOpened.Remove(index);
                    State.Folded[index] = true;
                    changed = true;
                }
                for (int i = 0; i < Part.Sections.Count && query.Length > 0; i++) {
                    if (Part.Sections[i].Level == 0 || !DocumentLayout.Path(Part, i).Exists(around => State.Folded[around])) { continue; }
                    if (!DocumentLayout.BodyText(Part.Sections[i], _docWidth, context).Exists(line => FindBar.Contains(line, query))) { continue; }
                    foreach (int around in DocumentLayout.Path(Part, i)) {
                        if (!State.Folded[around]) { continue; }
                        State.Folded[around] = false;
                        State.FindOpened.Add(around);
                        changed = true;
                    }
                }
                if (changed) { _page.Invalidate(); }
            }
            _find.Search(Layout().Lines, State.Top);
            Reveal();
        }

        private void StepMatch(int delta) { if (_find.Step(delta)) { Reveal(); } }

        private void Reveal() {
            if (_find.Current < 0) { return; }
            int line = _find.Matches[_find.Current].Line;
            if (line >= State.Top && line < State.Top + _pageRows) { return; }
            State.Top = 0;
            Move(line - _pageRows / 3);
        }

        private void OpenPalette() {
            List<PaletteCommand> commands = new List<PaletteCommand>();
            for (int p = 0; p < _page.Model.Parts.Count; p++) {
                DocumentPart part = _page.Model.Parts[p];
                for (int s = 0; s < part.Sections.Count; s++) {
                    DocumentSection section = part.Sections[s];
                    if (section.Level == 0) { continue; }
                    int partIndex = p, sectionIndex = s;
                    commands.Add(new PaletteCommand(section.Title, _ => JumpTo(partIndex, sectionIndex, true, true)) {
                        Category = section.Category ?? part.Title, Description = section.Summary
                    });
                }
            }
            if (commands.Count == 0) { return; }
            _modals.Add(new PaletteModal(commands, command => command.Invoke(null!), Labels));
        }

        // --- Drawing -------------------------------------------------------------------------------------------------

        private void DrawStrip(Region region, RenderContext context) {
            List<string> labels = _page.Model.Parts.ConvertAll(part => part.Title);
            _tabHeaders = TabStrip.Draw(region, labels, _page.Part, _focus == Pane.Strip, context);
        }

        private void DrawOutline(Region region, RenderContext context) {
            _outlineArea = region.Bounds;
            ((IWidget)_outline).Render(region, context);
        }
        private void DrawDocument(Region region, RenderContext context) {
            _docArea = region.Bounds;
            if (region.Width <= 0 || region.Height <= 0) { return; }
            DocumentLayout layout = Layout();
            int offset = 0;
            if (_pinned) { ((IWidget)_crumb).Render(region.Sub(0, 0, region.Width, 1), context); offset = 1; }
            CellBuffer document = layout.Buffer;
            int top = State.Top;
            DocumentLink? selected = SelectedLink();
            InteractiveGlyphs glyphs = InteractiveGlyphs.For(context);
            int current = CurrentSection();
            for (int y = 0; y + offset < region.Height && y + top < document.Height; y++) {
                int line = y + top;
                for (int x = 0; x < region.Width && x < document.Width; x++) {
                    Cell cell = document[x, line];
                    if (cell.Width == 0) { continue; }
                    // Links inside the document are the viewer's to follow, not the terminal's.
                    Style style = cell.Style.Link != null && DocumentLink.IsInternal(cell.Style.Link) ? cell.Style.WithLink(null) : cell.Style;
                    region.Put(x, y + offset, cell.Glyph, style, cell.Width);
                }
                if (selected != null && selected.Row == line) {
                    for (int x = selected.Column; x < selected.Column + selected.Width && x < region.Width; x++) {
                        Cell cell = region.Get(x, y + offset);
                        if (!cell.IsContinuation) { region.Put(x, y + offset, cell.Glyph, cell.Style.With(TextDecoration.Reverse), Math.Max(1, (int)cell.Width)); }
                    }
                }
                if (_focus == Pane.Document && layout.HeadingRows[layout.RowSections[line]] == line && layout.RowSections[line] == current) {
                    int indent = DocumentLayout.HeadingIndent(Part, Part.Sections[current]);
                    if (indent < region.Width) { region.Write(indent, y + offset, glyphs.Pointer, context.Style(ThemeToken.Focus)); }
                }
                _find.Highlight(region, y + offset, 0, _find.Active ? _find.CurrentOrdinal(line) : -1, context);
            }
        }

        // --- State ---------------------------------------------------------------------------------------------------

        /// <summary>One document in the history: its model, the open part and each part's folds and position.</summary>
        private sealed class Page {
            private readonly IWidget _content;
            private RenderContext? _context;
            private DocumentLayout? _layout;
            private int _layoutWidth = -1;
            private int _layoutPart = -1;
            private List<Collapsible>[] _headings;

            internal Page(IWidget content, string title, DocumentModel model) {
                _content = content;
                Title = title;
                Model = model;
                States = new PartState[model.Parts.Count];
                _headings = new List<Collapsible>[model.Parts.Count];
                Reset(model);
            }

            internal string Title { get; }

            internal DocumentModel Model { get; private set; }

            internal PartState[] States { get; private set; }

            internal int Part { get; set; }

            /// <summary>
            /// Rebuilds the model when the terminal profile, theme or glyphs change, keeping folds when the shape matches.
            /// Each frame brings a context with a new clock; that alone keeps the model, the layout and the outline as they are.
            /// </summary>
            internal void Refresh(RenderContext context, bool openCommands) {
                RenderContext? previous = _context;
                _context = context;
                if (previous != null && ReferenceEquals(previous.Profile, context.Profile) && ReferenceEquals(previous.Theme, context.Theme)
                    && ReferenceEquals(previous.Glyphs, context.Glyphs)) { return; }
                DocumentModel? model = DocumentModel.From(_content, Title, context, openCommands);
                if (model != null && Same(model, Model)) { Model = model; Headings(model); }
                Invalidate();
            }

            internal DocumentLayout Layout(int width) {
                if (_layout == null || _layoutWidth != width || _layoutPart != Part || _context == null) {
                    RenderContext context = _context ?? throw new InvalidOperationException("The document has not been drawn yet.");
                    _layout = DocumentLayout.Build(Model.Parts[Part], States[Part].Folded, _headings[Part], width, context);
                    _layoutWidth = width;
                    _layoutPart = Part;
                    if (States[Part].Link >= _layout.Links.Count) { States[Part].Link = -1; }
                }
                return _layout;
            }

            internal void Invalidate() { _layout = null; }

            private void Reset(DocumentModel model) {
                for (int p = 0; p < model.Parts.Count; p++) {
                    DocumentPart part = model.Parts[p];
                    bool[] folded = new bool[part.Sections.Count];
                    for (int s = 0; s < part.Sections.Count; s++) { folded[s] = part.Sections[s].StartFolded; }
                    States[p] = new PartState(folded);
                }
                Headings(model);
            }

            private void Headings(DocumentModel model) {
                for (int p = 0; p < model.Parts.Count; p++) {
                    List<Collapsible> headings = new List<Collapsible>();
                    foreach (DocumentSection section in model.Parts[p].Sections) {
                        headings.Add(new Collapsible("section") { Label = section.Title, Summary = section.Summary });
                    }
                    _headings[p] = headings;
                }
            }

            private static bool Same(DocumentModel a, DocumentModel b) {
                if (a.Parts.Count != b.Parts.Count) { return false; }
                for (int p = 0; p < a.Parts.Count; p++) { if (a.Parts[p].Sections.Count != b.Parts[p].Sections.Count) { return false; } }
                return true;
            }
        }

        private sealed class PartState {
            internal PartState(bool[] folded) { Folded = folded; }
            internal bool[] Folded { get; }
            internal int Top { get; set; }
            internal int Link { get; set; } = -1;
            // Sections find unfolded, which it folds again when the query stops matching inside them.
            internal HashSet<int> FindOpened { get; } = new HashSet<int>();
        }

        private readonly struct Location {
            internal Location(Page page, int part, int top) { Page = page; Part = part; Top = top; }
            internal Page Page { get; }
            internal int Part { get; }
            internal int Top { get; }
        }

        // --- Panes ---------------------------------------------------------------------------------------------------

        private sealed class StripWidget : IWidget {
            private readonly DocumentView _view;
            internal StripWidget(DocumentView view) { _view = view; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(TabStrip.Height, constraints.MaxHeight));
            public void Render(Region region, RenderContext context) => _view.DrawStrip(region, context);
        }

        private sealed class OutlinePane : IWidget {
            private readonly DocumentView _view;
            internal OutlinePane(DocumentView view) { _view = view; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? 1 : constraints.MaxHeight);
            public void Render(Region region, RenderContext context) => _view.DrawOutline(region, context);
        }

        private sealed class DocumentPane : IWidget {
            private readonly DocumentView _view;
            internal DocumentPane(DocumentView view) { _view = view; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? 1 : constraints.MaxHeight);
            public void Render(Region region, RenderContext context) => _view.DrawDocument(region, context);
        }
    }
}
