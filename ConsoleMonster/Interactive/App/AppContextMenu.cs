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
    /// The context menu: the bindings the keys reach right now (the focused input's, the screen's, the app's), each with
    /// its key, on the app overlay layer. A right click, the Menu key or Shift+F10 opens it; choosing an entry presses
    /// its key, so the menu never does anything the keyboard cannot.
    /// </summary>
    internal sealed class AppContextMenu : IWidget {
        private readonly IReadOnlyList<AppKeyMap.Entry> _entries;
        private readonly object _gate = new object();
        private int _cursor;
        private int _pressed = -1;
        private Rect _area;

        internal AppContextMenu(IReadOnlyList<AppKeyMap.Entry> entries, int column, int row, KeyScope? scope = null) {
            _entries = entries;
            Column = column;
            Row = row;
            Scope = scope;
            _cursor = Next(-1, 1);
        }

        /// <summary>The scope whose bindings it lists; the menu closes when another scope takes the keys.</summary>
        internal KeyScope? Scope { get; }

        /// <summary>Where it opens: the pointer, or the focused input's corner.</summary>
        internal int Column { get; }
        internal int Row { get; }

        internal bool IsEmpty => _entries.Count == 0;

        internal void Place(Rect area) { lock (_gate) { _area = area; } }

        /// <summary>Keys while open. Returns the chosen entry, or null; <paramref name="close"/> says the menu is done.</summary>
        internal AppKeyMap.Entry? HandleKey(InputEvent input, out bool close, out bool handled) {
            close = false; handled = true;
            if (input.Is(ConsoleKey.UpArrow)) { _cursor = Next(_cursor, -1); return null; }
            if (input.Is(ConsoleKey.DownArrow)) { _cursor = Next(_cursor, 1); return null; }
            if (input.Is(ConsoleKey.Home)) { _cursor = Next(-1, 1); return null; }
            if (input.Is(ConsoleKey.End)) { _cursor = Next(_entries.Count, -1); return null; }
            close = true;
            if (input.Is(ConsoleKey.Enter)) { return Chosen(_cursor); }
            // Escape and the opening keys close it; any other key closes it and goes on as usual.
            handled = input.Is(ConsoleKey.Escape) || IsOpenKey(input);
            return null;
        }

        /// <summary>Pointer while open: a press and release on an entry chooses it; a press elsewhere closes the menu.</summary>
        internal AppKeyMap.Entry? HandlePointer(InputEvent input, out bool close, out bool handled) {
            close = false; handled = true;
            int index = IndexAt(input);
            if (input.MouseAction == MouseAction.Wheel) {
                if (index >= 0 || Inside(input)) { _cursor = Next(_cursor, input.WheelDelta > 0 ? -1 : 1); }
                return null;
            }
            if (input.MouseAction == MouseAction.Move) {
                if (index >= 0 && Available(index)) { _cursor = index; }
                return null;
            }
            if (input.MouseAction == MouseAction.Down) {
                if (!Inside(input)) { close = true; handled = false; return null; }
                _pressed = index;
                if (index >= 0 && Available(index)) { _cursor = index; }
                return null;
            }
            if (input.MouseAction == MouseAction.Up) {
                int pressed = _pressed;
                _pressed = -1;
                if (index >= 0 && index == pressed && Available(index)) { close = true; return Chosen(index); }
            }
            return null;
        }

        internal static bool IsOpenKey(InputEvent input) => input.Is(ConsoleKey.Applications) || input.Is(ConsoleKey.F10, KeyModifiers.Shift);

        private AppKeyMap.Entry? Chosen(int index) => index >= 0 && index < _entries.Count && Available(index) ? _entries[index] : null;

        private bool Available(int index) => _entries[index].Binding.IsAvailable;

        private int Next(int from, int step) {
            for (int i = from + step; i >= 0 && i < _entries.Count; i += step) {
                if (Available(i)) { return i; }
            }
            return from < 0 || from >= _entries.Count ? -1 : from;
        }

        private bool Inside(InputEvent input) {
            Rect area;
            lock (_gate) { area = _area; }
            return input.Column >= area.X && input.Column < area.Right && input.Row >= area.Y && input.Row < area.Bottom;
        }

        // Entries start one row and two columns inside the border.
        private int IndexAt(InputEvent input) {
            Rect area;
            lock (_gate) { area = _area; }
            int index = input.Row - area.Y - 1;
            return input.Column > area.X && input.Column < area.Right - 1 && index >= 0 && index < _entries.Count && index < area.Height - 2 ? index : -1;
        }

        public Size Measure(Constraints constraints, RenderContext context) {
            int label = 0, keys = 0;
            foreach (AppKeyMap.Entry entry in _entries) {
                label = Math.Max(label, DisplayWidth.Measure(entry.Binding.Label));
                keys = Math.Max(keys, DisplayWidth.Measure(entry.Keys));
            }
            return new Size(Math.Min(constraints.MaxWidth, label + keys + 9), Math.Min(constraints.MaxHeight, _entries.Count + 2));
        }

        public void Render(Region region, RenderContext context) {
            new Panel(new Lines(this)) { BorderRole = ThemeToken.Accent }.Render(region, context);
        }

        private sealed class Lines : IWidget {
            private readonly AppContextMenu _menu;
            internal Lines(AppContextMenu menu) { _menu = menu; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, _menu._entries.Count));
            public void Render(Region region, RenderContext context) {
                InteractiveGlyphs glyphs = InteractiveGlyphs.For(context);
                for (int y = 0; y < region.Height && y < _menu._entries.Count; y++) {
                    AppKeyMap.Entry entry = _menu._entries[y];
                    bool focused = y == _menu._cursor;
                    bool available = entry.Binding.IsAvailable;
                    Style text = context.Style(available ? ThemeToken.Text : ThemeToken.Muted);
                    Style keys = context.Style(available ? ThemeToken.Accent : ThemeToken.Muted);
                    if (!available) { text = text.With(TextDecoration.Dim); keys = keys.With(TextDecoration.Dim); }
                    if (focused) {
                        Style focus = context.HasColor ? context.Style(ThemeToken.Focus) : Style.Plain.With(TextDecoration.Reverse);
                        region.Fill(0, y, region.Width, 1, " ", focus);
                        text = focus.With(TextDecoration.Bold);
                        keys = focus;
                    }
                    region.Write(0, y, focused ? glyphs.Pointer : " ", text);
                    string shown = TextWrapper.Truncate(entry.Keys, Math.Max(0, region.Width - 4), string.Empty);
                    int keysX = Math.Max(2, region.Width - DisplayWidth.Measure(shown));
                    region.Write(2, y, TextWrapper.Truncate(entry.Binding.Label, Math.Max(0, keysX - 3), context.Ellipsis), text);
                    region.Write(keysX, y, shown, keys);
                }
            }
        }
    }
}
