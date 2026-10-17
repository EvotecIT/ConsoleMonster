namespace ConsoleMonster.Widgets {
    /// <summary>What a table column does with values wider than its negotiated width.</summary>
    public enum ColumnOverflow {
        /// <summary>Wrap onto more lines (limited by <see cref="TableColumn.MaxLines"/>; the last line ends with an ellipsis).</summary>
        Wrap = 0,

        /// <summary>Cut at the end with an ellipsis (<c>v=spf1 include:spf.prot…</c>).</summary>
        Truncate,

        /// <summary>Keep the start and the end, cut the middle (paths, DNs, URLs: <c>CN=svc…,DC=evotec,DC=xyz</c>).</summary>
        TruncateMiddle,

        /// <summary>Never shorten: the column keeps its full width, and lower-priority columns hide instead.</summary>
        Never
    }
}
