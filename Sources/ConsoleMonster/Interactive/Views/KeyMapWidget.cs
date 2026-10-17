using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>The full key map (opened with ? or F1): every active binding, grouped, in a bordered dialog.</summary>
    internal sealed class KeyMapWidget : IWidget {
        private readonly List<KeyValuePair<string, List<KeyBinding>>> _groups = new List<KeyValuePair<string, List<KeyBinding>>>();
        private readonly PromptLabels _labels;

        public KeyMapWidget(IReadOnlyList<KeyBinding> bindings, PromptLabels labels) {
            _labels = labels ?? throw new ArgumentNullException(nameof(labels));
            foreach (KeyBinding b in bindings) {
                if (string.IsNullOrEmpty(b.Label)) {
                    continue;
                }
                List<KeyBinding>? list = null;
                foreach (KeyValuePair<string, List<KeyBinding>> g in _groups) {
                    if (g.Key == b.Group) {
                        list = g.Value;
                    }
                }
                if (list == null) {
                    list = new List<KeyBinding>();
                    _groups.Add(new KeyValuePair<string, List<KeyBinding>>(b.Group, list));
                }
                list.Add(b);
            }
        }

        private int Lines {
            get {
                int count = 0;
                foreach (KeyValuePair<string, List<KeyBinding>> g in _groups) {
                    count += g.Value.Count + 2;
                }
                return Math.Max(1, count - 1);
            }
        }

        public Size Measure(Constraints constraints, RenderContext context) {
            int keyWidth = KeyWidth();
            int labelWidth = 0;
            foreach (KeyValuePair<string, List<KeyBinding>> g in _groups) {
                labelWidth = Math.Max(labelWidth, DisplayWidth.Measure(g.Key));
                foreach (KeyBinding b in g.Value) {
                    labelWidth = Math.Max(labelWidth, DisplayWidth.Measure(b.Label));
                }
            }
            int width = Math.Min(constraints.MaxWidth, Math.Max(36, keyWidth + labelWidth + 8));
            int height = Lines + 3;
            return new Size(width, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        public void Render(Region region, RenderContext context) {
            Panel panel = new Panel(new Body(this), _labels.Keys) { BorderToken = ThemeToken.Accent, Footer = "[muted]any key closes[/]" };
            panel.Render(region, context);
        }

        private int KeyWidth() {
            int width = 0;
            foreach (KeyValuePair<string, List<KeyBinding>> g in _groups) {
                foreach (KeyBinding b in g.Value) {
                    width = Math.Max(width, DisplayWidth.Measure(Keys(b)));
                }
            }
            return width;
        }

        private static string Keys(KeyBinding b) {
            if (b.KeyText != null) {
                return b.KeyText;
            }
            List<string> keys = new List<string>();
            foreach (Input.KeyChord chord in b.Chords) {
                string text = KeyBinding.Display(chord);
                if (!keys.Contains(text)) {
                    keys.Add(text);
                }
            }
            return string.Join(" / ", keys.ToArray());
        }

        private sealed class Body : IWidget {
            private readonly KeyMapWidget _owner;

            public Body(KeyMapWidget owner) {
                _owner = owner;
            }

            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(_owner.Lines + 1, constraints.MaxHeight));

            public void Render(Region region, RenderContext context) {
                int keyWidth = _owner.KeyWidth();
                int y = 1;
                bool first = true;
                foreach (KeyValuePair<string, List<KeyBinding>> g in _owner._groups) {
                    if (!first) {
                        y++;
                    }
                    first = false;
                    if (y >= region.Height) {
                        return;
                    }
                    region.Write(0, y++, g.Key, context.Style(ThemeToken.Header));
                    foreach (KeyBinding b in g.Value) {
                        if (y >= region.Height) {
                            return;
                        }
                        region.Write(2, y, Keys(b), context.Style(ThemeToken.Accent));
                        region.Write(4 + keyWidth, y, TextWrapper.Truncate(b.Label, Math.Max(1, region.Width - keyWidth - 5), context.Ellipsis), context.Style(ThemeToken.Text));
                        y++;
                    }
                }
            }
        }
    }
}
