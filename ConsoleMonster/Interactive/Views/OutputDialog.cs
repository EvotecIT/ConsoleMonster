using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>One line an action wrote (console output, Write-Host, a warning or an error) and how it should look.</summary>
    internal readonly struct OutputLine {
        public OutputLine(string text, StatusKind status) {
            Text = text;
            Status = status;
        }

        public string Text { get; }

        public StatusKind Status { get; }
    }

    /// <summary>
    /// Collects what an action writes while the app owns the screen, so it never draws over the app: the lines are shown
    /// afterwards as a toast (one line) or in an output dialog (several). Safe to add to from any thread.
    /// </summary>
    internal sealed class ActionOutput {
        private readonly object _gate = new object();
        private readonly List<OutputLine> _lines = new List<OutputLine>();

        /// <summary>The most lines kept; older lines are dropped and counted.</summary>
        public const int Limit = 500;

        public int Dropped { get; private set; }

        public void Add(string text, StatusKind status) {
            if (text == null) {
                return;
            }
            string clean = Rendering.TextSanitizer.Clean(text);
            lock (_gate) {
                if (_lines.Count >= Limit) {
                    _lines.RemoveAt(0);
                    Dropped++;
                }
                _lines.Add(new OutputLine(clean, status));
            }
        }

        public List<OutputLine> Take() {
            lock (_gate) {
                List<OutputLine> lines = new List<OutputLine>(_lines);
                _lines.Clear();
                return lines;
            }
        }
    }

    /// <summary>The lines an action wrote, newest at the bottom; any key closes it.</summary>
    internal sealed class OutputDialog : Modal {
        private readonly string _title;
        private readonly List<OutputLine> _lines;
        private readonly int _dropped;
        private readonly PromptLabels _labels;

        public OutputDialog(string title, List<OutputLine> lines, int dropped, PromptLabels labels) {
            _title = title;
            _lines = lines;
            _dropped = dropped;
            _labels = labels;
        }

        public override int MaxWidth => 96;

        public override IWidget Build(ViewFrame frame, CaretSlot caret) {
            int visible = Math.Max(3, Math.Min(_lines.Count, frame.Height - 8));
            Panel panel = DialogChrome.Frame(new Body(this, visible), _title);
            panel.Caption = "[muted]" + Markup.Escape(_labels.AnyKeyCloses) + "[/]";
            return panel;
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            bindings.Add(new KeyBinding(_labels.KeyClose, Close, new KeyChord(ConsoleKey.Escape), new KeyChord(ConsoleKey.Enter)) { InFooter = false });
        }

        public override bool HandleUnbound(InputEvent input) {
            if (input.Kind == InputEventKind.Key) {
                Close();
                return true;
            }
            return false;
        }

        private sealed class Body : IWidget {
            private readonly OutputDialog _dialog;
            private readonly int _visible;

            public Body(OutputDialog dialog, int visible) {
                _dialog = dialog;
                _visible = visible;
            }

            public Size Measure(Constraints constraints, RenderContext context) {
                int extra = _dialog._lines.Count > _visible || _dialog._dropped > 0 ? 1 : 0;
                return new Size(constraints.MaxWidth, Math.Min(_visible + extra, constraints.IsHeightUnbounded ? _visible + extra : constraints.MaxHeight));
            }

            public void Render(Region region, RenderContext context) {
                List<OutputLine> lines = _dialog._lines;
                int hidden = Math.Max(0, lines.Count - _visible) + _dialog._dropped;
                int y = 0;
                if (hidden > 0 && region.Height > 1) {
                    region.Write(0, y++, PromptLabels.Format(_dialog._labels.EarlierLines, hidden), context.Style(ThemeToken.Muted));
                }
                int start = Math.Max(0, lines.Count - (region.Height - y));
                for (int i = start; i < lines.Count && y < region.Height; i++, y++) {
                    OutputLine line = lines[i];
                    ThemeToken token = line.Status == StatusKind.Fail ? ThemeToken.Fail : line.Status == StatusKind.Warn ? ThemeToken.Warn : ThemeToken.Text;
                    region.Write(0, y, TextWrapper.Truncate(line.Text, Math.Max(1, region.Width), context.Ellipsis), context.Style(token));
                }
            }
        }
    }
}
