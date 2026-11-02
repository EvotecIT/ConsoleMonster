namespace ConsoleMonster.Localization {
    public sealed partial class WidgetLabels {
        /// <summary>The live tail is following its newest entries.</summary>
        public string TailFollowing { get; set; } = "Following latest";

        /// <summary>The live tail is paused on earlier history.</summary>
        public string TailPaused { get; set; } = "Paused · viewing history";

        /// <summary>Marker before the paused state in Unicode-capable terminals.</summary>
        public string TailPauseMarker { get; set; } = "⏸";

        /// <summary>Applied log rows received since following was paused.</summary>
        public string TailNewLines { get; set; } = "{0} new lines";

        /// <summary>A paused log panel inside an app, where the full-screen wording does not fit.</summary>
        public string TailPausedShort { get; set; } = "Paused";

        /// <summary>Lines received since a log panel inside an app was paused.</summary>
        public string TailNewShort { get; set; } = "{0} new";

        /// <summary>Footer label for the key that scrolls a log panel back through its history.</summary>
        public string TailKeyHistory { get; set; } = "history";

        /// <summary>Footer label for the key that returns a paused log panel to its newest line.</summary>
        public string TailKeyFollow { get; set; } = "follow";

        /// <summary>Before the active log filter's tag, level and text.</summary>
        public string TailFiltered { get; set; } = "Filtered:";

        /// <summary>History lines evicted after being applied.</summary>
        public string TailCounts { get; set; } = "{0} history lines discarded";

        /// <summary>Keyboard navigation hints, with theme-token markup for accent keys and muted descriptions.</summary>
        public string TailKeys { get; set; } = "[accent]q[/] [muted]quit ·[/] [accent]PgUp[/] [muted]history ·[/] [accent]End[/] [muted]follow[/]";

        /// <summary>A live tail failed in its session.</summary>
        public string TailSessionFailed { get; set; } = "The log-tail live session failed.";

        /// <summary>A live tail could not update or render its view.</summary>
        public string TailViewFailed { get; set; } = "The log-tail view could not be updated or rendered.";

        /// <summary>A source reader returned a null collection.</summary>
        public string TailReaderReturnedNull { get; set; } = "The log-tail reader returned null.";

        /// <summary>A source reader returned a null entry.</summary>
        public string TailReaderReturnedNullEntry { get; set; } = "The log-tail reader returned a null entry.";
    }
}
