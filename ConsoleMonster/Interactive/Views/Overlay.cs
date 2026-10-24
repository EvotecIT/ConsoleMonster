using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Draws a dialog centred over a background (the key map, the command palette, a confirmation). The background is
    /// dimmed so the dialog reads as in front. Inline frames show the dialog instead of the background.
    /// </summary>
    internal sealed class Overlay : IWidget {
        private readonly IWidget _background;
        private readonly IWidget _dialog;
        private readonly bool _dim;

        public Overlay(IWidget background, IWidget dialog, bool dim = true) {
            _background = background ?? throw new ArgumentNullException(nameof(background));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _dim = dim;
        }

        public bool Inline { get; set; }

        /// <summary>The widest the dialog may be.</summary>
        public int MaxWidth { get; set; } = 78;

        /// <summary>Places the dialog near the top (palettes) instead of the centre.</summary>
        public bool Top { get; set; }

        public Size Measure(Constraints constraints, RenderContext context) {
            if (Inline) {
                return _dialog.Measure(constraints, context);
            }
            return _background.Measure(constraints, context);
        }

        public void Render(Region region, RenderContext context) {
            if (Inline) {
                _dialog.Render(region, context);
                return;
            }
            _background.Render(region, context);
            if (_dim) {
                Dim(region, context);
            }
            int width = Math.Min(region.Width - 2, MaxWidth);
            if (width < 10) {
                width = region.Width;
            }
            Size size = _dialog.Measure(new Constraints(width, Math.Max(1, region.Height - 2)), context);
            Rect place = DialogChrome.Place(region.Width, region.Height, Math.Min(width, Math.Max(size.Width, 10)), size.Height, Top);
            Region area = region.Sub(place.X, place.Y, place.Width, place.Height);
            area.Fill(Style.Plain);
            _dialog.Render(area, context);
        }

        internal static void Dim(Region region, RenderContext context) {
            if (!context.HasColor) {
                return;
            }
            Style dim = context.Style(ThemeToken.Subtle);
            for (int y = 0; y < region.Height; y++) {
                for (int x = 0; x < region.Width; x++) {
                    Cell cell = region.Get(x, y);
                    region.SetStyle(x, y, new Style(dim.Color, default, TextDecoration.None));
                    _ = cell;
                }
            }
        }
    }
}
