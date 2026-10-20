namespace ConsoleMonster.Values {
    /// <summary>What a value is for display purposes, as decided by <see cref="ValueShaper.ShapeOf"/>.</summary>
    public enum ValueShape {
        /// <summary>Nothing to show: null, <see cref="System.DBNull"/> or an empty marker.</summary>
        Empty,

        /// <summary>One value shown as text: strings, numbers, dates, enums, GUIDs, versions, addresses, secure strings, byte arrays.</summary>
        Scalar,

        /// <summary>A set of named fields: an object, a dictionary, a data row, an XML element or a JSON object.</summary>
        Record,

        /// <summary>A sequence of items: arrays, lists, data tables and views, JSON arrays (never a string).</summary>
        Collection
    }

    /// <summary>How a collection inside a cell or a title is written.</summary>
    public enum CollectionDisplay {
        /// <summary>The items joined with <see cref="ShapeOptions.Separator"/>, ending with "… +N" past <see cref="ShapeOptions.ItemLimit"/> (the default).</summary>
        Join,

        /// <summary>One item per line, ending with "+N more" past <see cref="ShapeOptions.ItemLimit"/>.</summary>
        Lines,

        /// <summary>The first <see cref="ShapeOptions.ItemLimit"/> items joined, then "+N more".</summary>
        FirstN,

        /// <summary>Only the number of items ("3 items").</summary>
        Count
    }
}
