using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Replay {
    /// <summary>
    /// The replay player: header (run name, state, position, speed), the recorded screen, a timeline with markers, the
    /// current marker or the log search, and the key footer.
    /// </summary>
    internal sealed class ReplayView : InteractiveView {
        /// <summary>Rows around the recorded screen: header, timeline, info line and footer.</summary>
        public const int Chrome = 4;

        private static readonly double[] Speeds = { 0.25, 0.5, 1, 2, 4, 8, 16 };

        private readonly string _title;
        private readonly bool _explicitTitle;
        private readonly ReplayFrames _frames;
        private readonly IReadOnlyList<ReplayMarker> _markers;
        private readonly LineEditor _search = new LineEditor();
        private readonly bool _hasNamedMarkers;
        private double _position;
        private bool _playing;
        private double _speed;
        private TimeSpan? _lastNow;
        private int _anchor = -1;
        private bool _searching;

        public ReplayView(string title, bool explicitTitle, ReplayFrames frames, IReadOnlyList<ReplayMarker> markers, ReplayOptions options, PromptLabels labels) : base(labels) {
            _title = title;
            _explicitTitle = explicitTitle;
            _frames = frames;
            _markers = markers;
            _speed = options.Speed;
            _playing = options.AutoPlay && frames.End > 0;
            foreach (ReplayMarker marker in markers) {
                if (marker.Kind == ReplayMarkerKind.Marker) {
                    _hasNamedMarkers = true;
                    break;
                }
            }
        }

        /// <summary>The playback position in milliseconds.</summary>
        public double Position => _position;

        public bool IsPlaying => _playing;

        public double Speed => _speed;

        public bool IsSearching => _searching;

        public ReplayFrames Frames => _frames;

        public IReadOnlyList<ReplayMarker> Markers => _markers;

        public override bool IsAnimating => _playing;

        public override bool Poll(TimeSpan now) {
            bool changed = base.Poll(now);
            if (!_playing) {
                _lastNow = null;
                return changed;
            }
            if (_lastNow.HasValue) {
                _position += (now - _lastNow.Value).TotalMilliseconds * _speed;
                _anchor = -1;
                if (_position >= _frames.End) {
                    _position = _frames.End;
                    _playing = false;
                }
            }
            _lastNow = now;
            return true;
        }

        protected override IWidget Build(ViewFrame frame) {
            InteractiveGlyphs g = InteractiveGlyphs.For(frame.Context);
            Rows rows = new Rows();
            rows.Add(Header(g), LayoutSize.Fixed(1));
            rows.Add(new ReplayScreen(this), frame.Inline ? LayoutSize.Fixed(_frames.Height) : LayoutSize.Fraction());
            rows.Add(new Padding(new ReplayTimeline(this), 1, 0, 1, 0), LayoutSize.Fixed(1));
            rows.Add(new Padding(InfoLine(frame), 1, 0, 1, 0), LayoutSize.Fixed(1));
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            rows.Add(new KeyFooter(bindings) { Toast = CurrentToast }, LayoutSize.Fixed(1));
            return rows;
        }

        /// <summary>
        /// The one header: the run name, then what the recorded dashboard's header showed at this position (its title when
        /// no name was given or it adds something, its subtitle and its status), the play state, position and speed.
        /// </summary>
        internal HeaderBar Header(InteractiveGlyphs g) {
            ReplayChrome? chrome = CurrentChrome;
            string state = _playing ? Labels.ReplayPlaying : Labels.ReplayPaused;
            string position = ReplayPlayer.FormatTime(TimeSpan.FromMilliseconds(_position)) + " / " + ReplayPlayer.FormatTime(TimeSpan.FromMilliseconds(_frames.End)) + " " + g.Separator + " " + SpeedText(_speed);
            HeaderBar header = new HeaderBar(Markup.Escape(_title));
            if (chrome != null) {
                string recordedTitle = Markup.Strip(chrome.Title);
                if (!_explicitTitle && recordedTitle.Length > 0) {
                    header.Title = chrome.Title;
                } else if (recordedTitle.Length > 0 && _title.IndexOf(recordedTitle, StringComparison.OrdinalIgnoreCase) < 0) {
                    header.Crumbs.Add(recordedTitle);
                }
                header.Subtitle = chrome.Subtitle;
            }
            if (chrome != null && !string.IsNullOrEmpty(chrome.Status)) {
                // The recorded status keeps its place; the play state moves to the free text.
                header.Status = chrome.Status;
                header.StatusLevel = chrome.StatusLevel;
                header.Right = state + " " + g.Separator + " " + position;
            } else {
                header.Status = state;
                header.StatusLevel = _playing ? StatusKind.Running : StatusKind.Pending;
                header.Right = position;
            }
            return header;
        }

        private ReplayChrome? CurrentChrome => _frames.Count == 0 ? null : _frames.Chrome[_frames.IndexAt((long)Math.Round(_position))];

        private IWidget InfoLine(ViewFrame frame) {
            if (_searching) {
                TextInput input = new TextInput(_search, frame.Caret) { Placeholder = Labels.ReplaySearch, Focused = true };
                return new Columns(new Sized(new UiLine(context => new StyledLine(new[] { new TextSpan("/ ", context.Style(ThemeToken.Accent)) })), LayoutSize.Fixed(2)), input) { Gap = 0 };
            }
            return new UiLine(context => CurrentMarkerLine(context));
        }

        // The marker at or before the position (the one jumped to, when it was a jump), and the counts on the right.
        private StyledLine CurrentMarkerLine(RenderContext context) {
            ReplayMarker? current = _anchor >= 0 && _anchor < _markers.Count ? _markers[_anchor] : null;
            if (current == null) {
                for (int i = _markers.Count - 1; i >= 0; i--) {
                    if (_markers[i].Time.TotalMilliseconds <= _position) {
                        current = _markers[i];
                        break;
                    }
                }
            }
            List<TextSpan> spans = new List<TextSpan>();
            if (current != null) {
                spans.Add(new TextSpan(Glyph(current.Kind, context) + " ", context.Style(Token(current.Kind))));
                spans.Add(new TextSpan(ReplayPlayer.FormatTime(current.Time) + "  ", context.Style(ThemeToken.Muted)));
                spans.Add(new TextSpan(current.Text.Replace('\n', ' ').Replace('\r', ' '), context.Style(ThemeToken.Text)));
            }
            return new StyledLine(spans);
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            InteractiveGlyphs g = InteractiveGlyphs.For(frame.Context);
            if (_searching) {
                bindings.Add(new KeyBinding(Labels.KeyNext, () => FindNext(1), new KeyChord(ConsoleKey.Enter), new KeyChord(ConsoleKey.DownArrow)) { Order = 10 });
                bindings.Add(new KeyBinding(Labels.KeyPrevious, () => FindNext(-1), new KeyChord(ConsoleKey.Enter, KeyModifiers.Shift), new KeyChord(ConsoleKey.UpArrow)) { KeyText = g.KeyUpDown, Order = 11 });
                bindings.Add(new KeyBinding(Labels.KeyClose, () => _searching = false, new KeyChord(ConsoleKey.Escape)) { Order = 40 });
                return;
            }
            bindings.Add(new KeyBinding(Labels.KeyPlay, TogglePlay, new KeyChord(ConsoleKey.Spacebar), new KeyChord(' ')) { KeyText = "space", Order = 10 });
            bindings.Add(new KeyBinding(Labels.KeyFrame, () => Step(-1), new KeyChord(ConsoleKey.LeftArrow)) { KeyText = g.KeyLeftRight, Order = 11 });
            bindings.Add(new KeyBinding(string.Empty, () => Step(1), new KeyChord(ConsoleKey.RightArrow)) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyPage, () => Seek(-0.05), new KeyChord(ConsoleKey.PageUp), new KeyChord(ConsoleKey.LeftArrow, KeyModifiers.Shift)) { KeyText = "pgup/pgdn", Order = 18, InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => Seek(0.05), new KeyChord(ConsoleKey.PageDown), new KeyChord(ConsoleKey.RightArrow, KeyModifiers.Shift)) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyHome, () => MoveTo(0), new KeyChord(ConsoleKey.Home)) { KeyText = "home/end", Order = 19, InFooter = false });
            bindings.Add(new KeyBinding(string.Empty, () => MoveTo(_frames.End), new KeyChord(ConsoleKey.End)) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeySpeed, () => ChangeSpeed(1), new KeyChord('+'), new KeyChord('=')) { KeyText = "+/-", Order = 12 });
            bindings.Add(new KeyBinding(string.Empty, () => ChangeSpeed(-1), new KeyChord('-')) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyMarker, () => Jump(MarkerFilter, 1), new KeyChord('n')) { KeyText = "n/p", Order = 13 });
            bindings.Add(new KeyBinding(string.Empty, () => Jump(MarkerFilter, -1), new KeyChord('p')) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyFailure, () => Jump(m => m.Kind == ReplayMarkerKind.Failure, 1), new KeyChord('f')) { KeyText = "f/F", Order = 14 });
            bindings.Add(new KeyBinding(string.Empty, () => Jump(m => m.Kind == ReplayMarkerKind.Failure, -1), new KeyChord('F')) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyWarning, () => Jump(m => m.Kind == ReplayMarkerKind.Warning, 1), new KeyChord('w')) { KeyText = "w/W", Order = 15 });
            bindings.Add(new KeyBinding(string.Empty, () => Jump(m => m.Kind == ReplayMarkerKind.Warning, -1), new KeyChord('W')) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeyLogEntry, () => Jump(m => m.IsLog, 1), new KeyChord('l')) { KeyText = "l/L", Order = 16 });
            bindings.Add(new KeyBinding(string.Empty, () => Jump(m => m.IsLog, -1), new KeyChord('L')) { InFooter = false });
            bindings.Add(new KeyBinding(Labels.KeySearch, OpenSearch, new KeyChord('/')) { KeyText = "/", Order = 17 });
            bindings.Add(new KeyBinding(Labels.KeyQuit, Quit, new KeyChord('q'), new KeyChord(ConsoleKey.Escape)) { KeyText = "q", Order = 40 });
            bindings.Add(HelpBinding(true));
        }

        protected override bool HandleUnbound(InputEvent input) => _searching && _search.Handle(input);

        private bool MarkerFilter(ReplayMarker marker) => _hasNamedMarkers ? marker.Kind == ReplayMarkerKind.Marker : !marker.IsLog || marker.Kind != ReplayMarkerKind.Log;

        public void TogglePlay() {
            if (!_playing && _position >= _frames.End) {
                _position = 0;
            }
            _playing = !_playing && _frames.End > 0;
            _lastNow = null;
        }

        /// <summary>Moves one distinct frame back or forward (and pauses).</summary>
        public void Step(int direction) {
            _playing = false;
            int index = _frames.IndexAt((long)Math.Round(_position));
            index = Math.Max(0, Math.Min(_frames.Count - 1, index + direction));
            _position = _frames.Times[index];
            _anchor = -1;
        }

        /// <summary>Moves by a fraction of the whole recording.</summary>
        public void Seek(double fraction) => MoveTo(_position + Math.Max(1000, _frames.End) * fraction);

        public void MoveTo(double position) {
            _position = Math.Max(0, Math.Min(_frames.End, position));
            _anchor = -1;
        }

        public void ChangeSpeed(int direction) {
            int index = Array.IndexOf(Speeds, _speed);
            if (index < 0) {
                index = 2;
            }
            _speed = Speeds[Math.Max(0, Math.Min(Speeds.Length - 1, index + direction))];
        }

        /// <summary>Jumps to the next or previous marker that matches; returns whether there was one.</summary>
        public bool Jump(Func<ReplayMarker, bool> filter, int direction) {
            int found = -1;
            if (direction > 0) {
                for (int i = 0; i < _markers.Count; i++) {
                    if (filter(_markers[i]) && (_anchor >= 0 ? i > _anchor : _markers[i].Time.TotalMilliseconds > _position + 0.5)) {
                        found = i;
                        break;
                    }
                }
            } else {
                for (int i = _markers.Count - 1; i >= 0; i--) {
                    if (filter(_markers[i]) && (_anchor >= 0 ? i < _anchor : _markers[i].Time.TotalMilliseconds < _position - 0.5)) {
                        found = i;
                        break;
                    }
                }
            }
            if (found < 0) {
                ShowToast(new Toast(Labels.ReplayNoMore, StatusKind.Warn));
                return false;
            }
            Show(found);
            return true;
        }

        // Shows a marker: paused at the first frame that includes its change.
        private void Show(int index) {
            _playing = false;
            long time = (long)_markers[index].Time.TotalMilliseconds;
            int frame = _frames.IndexAt(time);
            if (_frames.Times[frame] < time && frame + 1 < _frames.Count) {
                frame++;
            }
            _position = Math.Max(time, _frames.Times[frame]);
            _position = Math.Min(_position, _frames.End);
            _anchor = index;
        }

        private void OpenSearch() {
            _searching = true;
            _playing = false;
        }

        /// <summary>Jumps to the next (or previous) log entry containing the search text, wrapping around.</summary>
        public bool FindNext(int direction) {
            string query = _search.Text.Trim();
            if (query.Length == 0) {
                return false;
            }
            Func<ReplayMarker, bool> match = m => m.IsLog && m.Text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
            int before = _anchor;
            double position = _position;
            if (Jump(match, direction)) {
                return true;
            }
            // Wrap around from the other end.
            _anchor = -1;
            _position = direction > 0 ? -1 : _frames.End + 1;
            if (Jump(match, direction)) {
                return true;
            }
            _anchor = before;
            _position = position;
            ShowToast(new Toast(PromptLabels.Format(Labels.ReplayNoMatch, query), StatusKind.Warn));
            return false;
        }

        /// <summary>Sets the search text (tests and scripted use).</summary>
        public void SetSearch(string text) {
            _searching = true;
            _search.Text = text ?? string.Empty;
        }

        private void Quit() {
            IsDone = true;
        }

        public override IReadOnlyList<StyledLine>? Summary(RenderContext context) {
            int failures = 0;
            int warnings = 0;
            foreach (ReplayMarker marker in _markers) {
                failures += marker.Kind == ReplayMarkerKind.Failure ? 1 : 0;
                warnings += marker.Kind == ReplayMarkerKind.Warning ? 1 : 0;
            }
            List<TextSpan> spans = new List<TextSpan> {
                new TextSpan(_title + "  ", context.Style(ThemeToken.Text)),
                new TextSpan(ReplayPlayer.FormatTime(TimeSpan.FromMilliseconds(_frames.End)), context.Style(ThemeToken.Muted))
            };
            if (failures > 0) {
                spans.Add(new TextSpan("  " + Glyph(ReplayMarkerKind.Failure, context) + " " + failures, context.Style(ThemeToken.Fail)));
            }
            if (warnings > 0) {
                spans.Add(new TextSpan("  " + Glyph(ReplayMarkerKind.Warning, context) + " " + warnings, context.Style(ThemeToken.Warn)));
            }
            return new[] { new StyledLine(spans) };
        }

        internal static string SpeedText(double speed) => speed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "x";

        internal static string Glyph(ReplayMarkerKind kind, RenderContext context) {
            bool ascii = context.Profile.Glyphs == GlyphTier.Ascii;
            switch (kind) {
                case ReplayMarkerKind.Failure:
                    return ascii ? "x" : "✗";
                case ReplayMarkerKind.Warning:
                    return ascii ? "!" : "▲";
                case ReplayMarkerKind.Marker:
                    return ascii ? "*" : "◆";
                default:
                    return ascii ? "." : "·";
            }
        }

        internal static ThemeToken Token(ReplayMarkerKind kind) {
            switch (kind) {
                case ReplayMarkerKind.Failure:
                    return ThemeToken.Fail;
                case ReplayMarkerKind.Warning:
                    return ThemeToken.Warn;
                case ReplayMarkerKind.Marker:
                    return ThemeToken.Accent2;
                default:
                    return ThemeToken.Muted;
            }
        }
    }

    /// <summary>The recorded screen at the player's position.</summary>
    internal sealed class ReplayScreen : IWidget {
        private readonly ReplayView _view;

        public ReplayScreen(ReplayView view) {
            _view = view;
        }

        public Size Measure(Constraints constraints, RenderContext context) => new Size(Math.Min(constraints.MaxWidth, _view.Frames.Width), Math.Min(constraints.MaxHeight, _view.Frames.Height));

        public void Render(Region region, RenderContext context) {
            ReplayFrames frames = _view.Frames;
            if (frames.Count == 0) {
                return;
            }
            Cell[][] rows = frames.Rows[frames.IndexAt((long)Math.Round(_view.Position))];
            for (int y = 0; y < rows.Length && y < region.Height; y++) {
                Cell[] row = rows[y];
                for (int x = 0; x < row.Length && x < region.Width; x++) {
                    Cell cell = row[x];
                    if (!cell.IsContinuation) {
                        region.Put(x, y, cell.Glyph ?? " ", cell.Style, Math.Max(1, (int)cell.Width));
                    }
                }
            }
        }
    }

    /// <summary>The timeline: the played part, the markers (failures above warnings above markers above log entries) and the position.</summary>
    internal sealed class ReplayTimeline : IWidget {
        private readonly ReplayView _view;

        public ReplayTimeline(ReplayView view) {
            _view = view;
        }

        public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));

        public void Render(Region region, RenderContext context) {
            int width = region.Width;
            if (width <= 0 || region.Height <= 0) {
                return;
            }
            bool ascii = context.Profile.Glyphs == GlyphTier.Ascii;
            double end = Math.Max(1, _view.Frames.End);
            int thumb = Cell(_view.Position, end, width);
            Style played = context.Style(ThemeToken.Accent);
            Style rest = context.Style(ThemeToken.Subtle);
            for (int x = 0; x < width; x++) {
                region.Put(x, 0, x <= thumb ? (ascii ? "=" : "━") : (ascii ? "-" : "─"), x <= thumb ? played : rest);
            }
            int[] rank = new int[width];
            foreach (ReplayMarker marker in _view.Markers) {
                int x = Cell(marker.Time.TotalMilliseconds, end, width);
                int priority = marker.Kind == ReplayMarkerKind.Failure ? 4 : marker.Kind == ReplayMarkerKind.Warning ? 3 : marker.Kind == ReplayMarkerKind.Marker ? 2 : 1;
                if (priority > rank[x]) {
                    rank[x] = priority;
                    region.Put(x, 0, ReplayView.Glyph(marker.Kind, context), context.Style(ReplayView.Token(marker.Kind)));
                }
            }
            region.Put(thumb, 0, ascii ? "O" : "●", played.With(TextDecoration.Bold));
        }

        private static int Cell(double time, double end, int width) => Math.Max(0, Math.Min(width - 1, (int)Math.Round(time / end * (width - 1))));
    }
}
