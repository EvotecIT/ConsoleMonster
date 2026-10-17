namespace ConsoleMonster.Widgets {
    /// <summary>What happens to text wider than its space.</summary>
    public enum TextOverflow {
        /// <summary>Wrap onto more lines.</summary>
        Wrap = 0,

        /// <summary>Cut and add an ellipsis.</summary>
        Ellipsis,

        /// <summary>Cut without an ellipsis.</summary>
        Clip
    }
}
