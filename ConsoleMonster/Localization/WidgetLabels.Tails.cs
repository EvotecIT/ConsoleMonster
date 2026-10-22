namespace ConsoleMonster.Localization {
    public sealed partial class WidgetLabels {
        /// <summary>The live tail is following its newest entries.</summary>
        public string TailFollowing { get; set; } = "Following latest";

        /// <summary>The live tail is paused on earlier history.</summary>
        public string TailPaused { get; set; } = "Paused - viewing history";

        /// <summary>History lines evicted after being applied.</summary>
        public string TailCounts { get; set; } = "{0} history lines discarded";

        /// <summary>Keyboard navigation and exit hints for the live tail.</summary>
        public string TailKeys { get; set; } = "q/Esc/Ctrl+C: quit | PgUp: history | End: follow";

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
