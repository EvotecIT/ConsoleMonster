using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>A short notice ("Preset saved") shown for a few seconds in place of the key hints.</summary>
    public sealed class Toast {
        /// <summary>Creates a toast.</summary>
        /// <param name="text">The text.</param>
        /// <param name="status">The status that picks the mark and colour (pass, warn, fail, info).</param>
        /// <param name="duration">How long it stays; null is 2.5 seconds.</param>
        public Toast(string text, StatusKind status = StatusKind.Pass, TimeSpan? duration = null) {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Status = status;
            Duration = duration ?? TimeSpan.FromSeconds(2.5);
        }

        /// <summary>The text.</summary>
        public string Text { get; }

        /// <summary>The status.</summary>
        public StatusKind Status { get; }

        /// <summary>How long it stays.</summary>
        public TimeSpan Duration { get; }

        internal TimeSpan? ShownAt { get; set; }

        internal bool IsExpired(TimeSpan now) => ShownAt.HasValue && now - ShownAt.Value >= Duration;

        internal StyledLine Line(RenderContext context, Style baseStyle) {
            Style mark = baseStyle.Combine(context.Style(Status));
            return new StyledLine(new[] {
                new TextSpan(context.Glyphs.StatusMarker(Status) + " ", mark),
                new TextSpan(Text, baseStyle.Combine(context.Style(ThemeToken.Text)).With(TextDecoration.Bold))
            });
        }
    }
}
