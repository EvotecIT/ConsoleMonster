namespace ConsoleMonster.Glyphs {
    /// <summary>Border styles. Styles a tier cannot show fall back (Rounded and Heavy become Single on Basic; all become ASCII on Ascii).</summary>
    public enum BoxStyle {
        /// <summary>Rounded corners (╭╮╰╯).</summary>
        Rounded = 0,

        /// <summary>Single lines (┌┐└┘).</summary>
        Single,

        /// <summary>Double lines (╔╗╚╝).</summary>
        Double,

        /// <summary>Heavy lines (┏┓┗┛).</summary>
        Heavy,

        /// <summary>ASCII (+-|).</summary>
        Ascii,

        /// <summary>No visible border: spaces that keep the layout.</summary>
        None
    }
}
