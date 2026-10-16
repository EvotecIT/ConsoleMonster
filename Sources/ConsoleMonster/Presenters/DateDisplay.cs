namespace ConsoleMonster.Presenters {
    /// <summary>How a date presenter shows a point in time.</summary>
    public enum DateDisplay {
        /// <summary>The date and time in the presenter's time zone (<c>2026-10-16 14:05</c>).</summary>
        Absolute = 0,

        /// <summary>Relative to now (<c>3 h ago</c>, <c>in 12 d</c>).</summary>
        Relative,

        /// <summary>Both (<c>2026-10-16 14:05 (3 h ago)</c>).</summary>
        Both
    }
}
