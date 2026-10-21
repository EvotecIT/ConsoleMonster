using System;
using System.Collections;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// Chooses a presenter from the value's type: statuses, booleans, numbers, durations, dates and links get their
    /// presenters; collections of numbers become sparklines; everything else is text.
    /// </summary>
    public sealed class AutoPresenter : ICellPresenter {
        private static readonly ICellPresenter Text = new TextPresenter();
        private static readonly ICellPresenter Status = new StatusPresenter();
        private static readonly ICellPresenter Flag = new BooleanPresenter();
        private static readonly ICellPresenter Number = new NumberPresenter();
        private static readonly ICellPresenter Duration = new DurationPresenter();
        private static readonly ICellPresenter Date = new DatePresenter();
        private static readonly ICellPresenter Link = new LinkPresenter();

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            return For(value).Present(value, context);
        }

        /// <summary>Returns the presenter the value's type maps to.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The presenter.</returns>
        public static ICellPresenter For(object? value) {
            switch (value) {
                case null:
                    return Text;
                case StatusKind _:
                    return Status;
                case bool _:
                    return Flag;
                case TimeSpan _:
                    return Duration;
                case DateTime _:
                case DateTimeOffset _:
                    return Date;
                case Uri _:
                case System.IO.FileSystemInfo _:
                    return Link;
                case string text:
                    return Rendering.HyperlinkTarget.From(text) != null ? Link : Text;
                case Enum _:
                    return Text;
                case IEnumerable _:
                    return Text;
            }
            return PresenterValues.TryGetNumber(value, out _) ? Number : Text;
        }
    }
}
