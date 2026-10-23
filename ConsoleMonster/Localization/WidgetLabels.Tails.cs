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
