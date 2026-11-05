namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A control whose selection is a position in a list (a row, a point, a choice) and can be shared through app state.
    /// Controls with the same <see cref="LinkKey"/> are linked without referring to each other: when the user moves
    /// one, the app writes its <see cref="LinkIndex"/> to that state key, and every control with the key that can show
    /// that position follows it, as it does when a callback writes the key; a position a control cannot show leaves it as
    /// it is. <see cref="DataGrid"/>, <see cref="ChartView"/>, <see cref="Select"/> and <see cref="MultiSelect"/> take part;
    /// implement it on a custom <see cref="InputControl"/> to link it the same way.
    /// </summary>
    public interface ILinkedSelection {
        /// <summary>The app-state key shared with the linked controls, or null to take no part.</summary>
        string? LinkKey { get; }

        /// <summary>The position shared through the link (a source row, a point, a choice), or null when there is none.</summary>
        int? LinkIndex { get; }

        /// <summary>Follows a position another control or a callback selected; a position this control cannot show is ignored.</summary>
        /// <param name="index">The position, or null when the shared value is not a number.</param>
        void FollowLink(int? index);
    }
}
