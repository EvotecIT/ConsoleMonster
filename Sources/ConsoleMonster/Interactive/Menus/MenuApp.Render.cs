using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    internal sealed partial class MenuApp {
        private bool WantsText {
            get {
                if (UsesSidebar && !_contentFocused) {
                    return false;
                }
                switch (CurrentPane) {
                    case MenuListPane list:
                        return list.WantsText;
                    case ChoiceItemPane choice:
                        return choice.WantsText;
                    case FormView form:
                        return form.WantsText;
                }
                return false;
            }
        }

        private List<string> Crumbs() {
            List<string> crumbs = new List<string>();
            int start = Layout == MenuLayout.Shell ? 0 : 1;
            for (int i = start; i < _stack.Count; i++) {
                if (ReferenceEquals(_stack[i].Pane, _lists[_root])) {
                    continue;
                }
                crumbs.Add(_stack[i].Title);
            }
            if (CurrentPane != null) {
                crumbs.AddRange(CurrentPane.Crumbs);
            }
            return crumbs;
        }

        protected override IWidget Build(ViewFrame frame) {
            _modals.RemoveAll(m => m.IsClosed);
            if (UsesSidebar && !_contentFocused) {
                SyncSidebar();
            }
            if (Layout == MenuLayout.Shell && Narrow && _stack.Count == 0) {
                SyncSidebar();
            }
            if (CurrentPane is ChoiceItemPane choice) {
                choice.Sync();
            }
            IWidget content = Layout == MenuLayout.Classic ? BuildClassic(frame) : BuildShell(frame);
            if (_modals.Count > 0) {
                Modal top = _modals[_modals.Count - 1];
                return new Overlay(content, top.Build(frame, frame.Caret), dim: true) { Top = top.Top, MaxWidth = top.MaxWidth };
            }
            return content;
        }

        private IWidget BuildShell(ViewFrame frame) {
            Rows rows = new Rows();
            rows.Add(new UiLine(context => HeaderLine(context)) { Fill = ThemeToken.Surface }, LayoutSize.Fixed(1));
            IWidget body;
            bool narrow = frame.Width < _options.SidebarMinimumWidth;
            if (!narrow) {
                bool sidebarFocused = !_contentFocused && _modals.Count == 0;
                Panel sidebar = new Panel(_sidebar.Build(frame, sidebarFocused), (string?)null) {
                    BorderToken = sidebarFocused ? ThemeToken.Accent : ThemeToken.Border,
                    PaddingLeft = 0,
                    PaddingRight = 1,
                    PaddingTop = 1
                };
                Columns columns = new Columns { Gap = 1 };
                columns.Add(sidebar, LayoutSize.Fixed(SidebarWidth(frame)));
                columns.Add(ContentPanel(frame, _contentFocused && _modals.Count == 0), LayoutSize.Fraction());
                body = columns;
            } else {
                if (_stack.Count == 0 || (Layout == MenuLayout.Shell && _stack.Count == 1 && !_contentFocused)) {
                    // Too narrow for the sidebar: the root items become the content and the breadcrumb shows the way back.
                    _stack.Clear();
                    _stack.Add(new Location(_root.Title, _lists[_root], null));
                    _sidebarItem = null;
                    _contentFocused = true;
                }
                body = ContentPanel(frame, _modals.Count == 0);
            }
            rows.Add(new Padding(body, 0, 0, 0, 0), LayoutSize.Fraction());
            rows.Add(Footer(frame, true), LayoutSize.Fixed(1));
            return rows;
        }

        private int SidebarWidth(ViewFrame frame) {
            int widest = 0;
            foreach (MenuItem item in _root.Items) {
                int width = DisplayWidth.Measure(item.Title) + (item.Icon == null ? 0 : DisplayWidth.Measure(item.Icon) + 1);
                string? state = item.CurrentState;
                if (!string.IsNullOrEmpty(state)) {
                    width += DisplayWidth.Measure(state) + 2;
                }
                if (item.HotkeyChar.HasValue) {
                    width += 3;
                }
                widest = Math.Max(widest, width);
            }
            return Math.Max(22, Math.Min(Math.Min(36, frame.Width / 3), widest + 7));
        }

        private Panel ContentPanel(ViewFrame frame, bool focused) {
            IPane? pane = CurrentPane;
            string title = Current?.Title ?? _root.Title;
            IWidget inner = pane == null ? (IWidget)UiLine.Empty : pane.Build(frame, focused);
            return new Panel(new Padding(inner, 1, 1, 1, 0), Markup.Escape(title)) {
                Caption = pane?.Caption == null ? null : Markup.Escape(pane.Caption),
                BorderToken = focused && UsesSidebar ? ThemeToken.Accent : ThemeToken.Border
            };
        }

        private IWidget BuildClassic(ViewFrame frame) {
            if (_stack.Count == 0) {
                _stack.Add(new Location(_root.Title, _lists[_root], null));
            }
            IPane? pane = CurrentPane;
            InteractiveGlyphs g = InteractiveGlyphs.For(frame.Context);
            List<string> crumbs = Crumbs();
            string title = _root.Title;
            foreach (string crumb in crumbs) {
                title += " " + g.Breadcrumb + " " + crumb;
            }
            int width = Math.Min(frame.Width - 2, 80);
            if (pane is MenuListPane compactList) {
                // A short menu gets a card that fits it rather than a wide, mostly empty one.
                int titleWidth = DisplayWidth.Measure(title) + 8;
                width = Math.Min(frame.Width - 2, Math.Max(Math.Max(52, titleWidth), compactList.List.RowWidth(frame.Context, 76) + 8));
            }
            IWidget inner = pane == null ? (IWidget)UiLine.Empty : pane.Build(frame, _modals.Count == 0);
            int height = frame.Height - 2;
            if (pane is MenuListPane list) {
                // Header padding and borders (3), the list, the hidden-or-shown filter row, the info line, the description.
                int rows = list.List.DesiredRows(100) + (list.List.HasHotkeys ? 0 : 1) + 1 + (string.IsNullOrEmpty(list.Menu.Description) ? 0 : 3);
                height = Math.Min(frame.Height - 2, rows + 3);
            } else if (pane is ResultPane || pane is ActionPane || pane is PagePane) {
                Size measured = inner.Measure(new Constraints(width - 4), frame.Context);
                height = Math.Min(frame.Height - 2, Math.Max(8, measured.Height + 3));
            }
            Panel card = new Panel(new Padding(inner, 1, 1, 1, 0), Markup.Escape(title)) {
                Caption = pane?.Caption == null ? null : Markup.Escape(pane.Caption),
                BorderToken = ThemeToken.Accent,
                Height = Math.Max(6, height)
            };
            Rows column = new Rows();
            int top = Math.Max(0, (frame.Height - card.Height!.Value - 1) / 3);
            if (top > 0) {
                column.Add(UiLine.Empty, LayoutSize.Fixed(top));
            }
            column.Add(card, LayoutSize.Fixed(card.Height!.Value));
            column.Add(Footer(frame, false), LayoutSize.Fixed(1));
            return new Centered(column, width);
        }

        private StyledLine HeaderLine(RenderContext context) {
            InteractiveGlyphs g = InteractiveGlyphs.For(context);
            Style bar = context.Style(ThemeToken.Surface);
            List<TextSpan> spans = new List<TextSpan> {
                new TextSpan(" " + _root.Title, context.Style(ThemeToken.SurfaceAccent))
            };
            foreach (string crumb in Crumbs()) {
                spans.Add(new TextSpan(" " + g.Breadcrumb + " ", bar));
                spans.Add(new TextSpan(crumb, bar));
            }
            return new StyledLine(spans);
        }

        private KeyFooter Footer(ViewFrame frame, bool surface) {
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            KeyFooter footer = new KeyFooter(bindings) { OnSurface = surface, Indent = 1, Toast = CurrentToast };
            if (!string.IsNullOrEmpty(_root.Subtitle) && surface) {
                footer.Right = StyledLine.From(_root.Subtitle!, frame.Context.Style(ThemeToken.Surface));
            }
            return footer;
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            if (_modals.Count > 0) {
                _modals[_modals.Count - 1].AddBindings(bindings, frame);
                return;
            }
            bool sidebar = UsesSidebar && !_contentFocused;
            if (sidebar) {
                _sidebar.AddBindings(bindings, frame);
                bindings.Add(new KeyBinding(string.Empty, () => {
                    MenuItem? item = _sidebar.FocusedItem;
                    if (item != null && item.Kind != MenuItemKind.Action && item.Kind != MenuItemKind.Exit) {
                        ActivateFromSidebar(item);
                    }
                }, new KeyChord(ConsoleKey.RightArrow), new KeyChord(ConsoleKey.Tab)) { InFooter = false, Group = "Menu" });
            } else if (CurrentPane != null) {
                CurrentPane.AddBindings(bindings, frame);
                if (UsesSidebar) {
                    bindings.Add(new KeyBinding(Labels.KeyPane, () => _contentFocused = false, new KeyChord(ConsoleKey.Tab, KeyModifiers.Shift)) { KeyText = "shift+tab", Order = 55, Group = "App", InFooter = false });
                }
                if (!HasEscape(bindings)) {
                    bool canGoBack = _stack.Count > 1 || UsesSidebar || CurrentPane.Crumbs.Count > 0 || Layout != MenuLayout.Classic;
                    bindings.Add(new KeyBinding(canGoBack ? Labels.KeyBack : Labels.KeyQuit, () => {
                        if (CurrentPane.Back()) {
                            return;
                        }
                        if (_stack.Count > 1 || UsesSidebar) {
                            Back();
                        } else if (Layout == MenuLayout.Classic) {
                            RequestExit();
                        }
                    }, new KeyChord(ConsoleKey.Escape)) { Order = 70, Group = "App" });
                }
            }
            bool typing = WantsText;
            if (_options.CommandPalette) {
                List<KeyChord> palette = new List<KeyChord> { new KeyChord(ConsoleKey.K, KeyModifiers.Control) };
                if (!typing) {
                    palette.Add(new KeyChord(':'));
                }
                bindings.Add(new KeyBinding(Labels.KeyPalette, OpenPalette, palette.ToArray()) { KeyText = "ctrl+k", Order = 80, Group = "App" });
            }
            bindings.Add(new KeyBinding(Labels.KeyHome, Home, new KeyChord(ConsoleKey.Home, KeyModifiers.Control)) { Order = 85, Group = "App", InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyQuit, RequestExit, new KeyChord(ConsoleKey.Q, KeyModifiers.Control)) { Order = sidebar ? 75 : 90, Group = "App", InFooter = sidebar });
            foreach (KeyBinding custom in _root.KeyBindings) {
                bindings.Add(custom);
            }
            bindings.Add(HelpBinding(!typing));
        }

        private static bool HasEscape(List<KeyBinding> bindings) {
            foreach (KeyBinding b in bindings) {
                foreach (KeyChord c in b.Chords) {
                    if (c.Key == ConsoleKey.Escape && c.Modifiers == KeyModifiers.None) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
