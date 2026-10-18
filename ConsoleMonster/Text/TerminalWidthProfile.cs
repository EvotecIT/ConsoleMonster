using System;

namespace ConsoleMonster.Text {
    /// <summary>Describes terminal-specific Unicode cell width choices.</summary>
    public sealed class TerminalWidthProfile : IEquatable<TerminalWidthProfile> {
        /// <summary>Creates a terminal width profile.</summary>
        public TerminalWidthProfile(int emojiWidth = 2, int ambiguousWidth = 1, ZwjRendering zwjSequences = ZwjRendering.Joined, int flagWidth = 2, int textPresentationWidth = 1, string source = "default") {
            if (emojiWidth != 1 && emojiWidth != 2) throw new ArgumentOutOfRangeException(nameof(emojiWidth));
            if (ambiguousWidth != 1 && ambiguousWidth != 2) throw new ArgumentOutOfRangeException(nameof(ambiguousWidth));
            if (flagWidth < 1) throw new ArgumentOutOfRangeException(nameof(flagWidth));
            if (textPresentationWidth < 1) throw new ArgumentOutOfRangeException(nameof(textPresentationWidth));
            EmojiWidth = emojiWidth;
            AmbiguousWidth = ambiguousWidth;
            ZwjSequences = zwjSequences;
            FlagWidth = flagWidth;
            TextPresentationWidth = textPresentationWidth;
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }
        /// <summary>Emoji presentation cluster width.</summary>
        public int EmojiWidth { get; }
        /// <summary>East Asian Ambiguous code point width.</summary>
        public int AmbiguousWidth { get; }
        /// <summary>Rendering model for emoji ZWJ sequences.</summary>
        public ZwjRendering ZwjSequences { get; }
        /// <summary>Width of a regional indicator pair.</summary>
        public int FlagWidth { get; }
        /// <summary>Width of emoji base plus VS15.</summary>
        public int TextPresentationWidth { get; }
        /// <summary>How this profile was selected.</summary>
        public string Source { get; }
        /// <summary>Standard terminal width assumptions.</summary>
        public static TerminalWidthProfile Standard { get; } = new TerminalWidthProfile();
        /// <summary>Copies this profile with a different emoji width.</summary>
        public TerminalWidthProfile WithEmojiWidth(int value) => new TerminalWidthProfile(value, AmbiguousWidth, ZwjSequences, FlagWidth, TextPresentationWidth, "configured");
        /// <summary>Copies this profile with a different ambiguous width.</summary>
        public TerminalWidthProfile WithAmbiguousWidth(int value) => new TerminalWidthProfile(EmojiWidth, value, ZwjSequences, FlagWidth, TextPresentationWidth, "configured");
        /// <summary>Copies this profile with a different ZWJ rendering.</summary>
        public TerminalWidthProfile WithZwjSequences(ZwjRendering value) => new TerminalWidthProfile(EmojiWidth, AmbiguousWidth, value, FlagWidth, TextPresentationWidth, "configured");
        /// <summary>Copies this profile with a different flag width.</summary>
        public TerminalWidthProfile WithFlagWidth(int value) => new TerminalWidthProfile(EmojiWidth, AmbiguousWidth, ZwjSequences, value, TextPresentationWidth, "configured");
        /// <summary>Copies this profile with a different text presentation width.</summary>
        public TerminalWidthProfile WithTextPresentationWidth(int value) => new TerminalWidthProfile(EmojiWidth, AmbiguousWidth, ZwjSequences, FlagWidth, value, "configured");
        /// <summary>Copies this profile with a different source label.</summary>
        public TerminalWidthProfile WithSource(string value) => new TerminalWidthProfile(EmojiWidth, AmbiguousWidth, ZwjSequences, FlagWidth, TextPresentationWidth, value);
        /// <inheritdoc />
        public bool Equals(TerminalWidthProfile? other) => other != null && EmojiWidth == other.EmojiWidth && AmbiguousWidth == other.AmbiguousWidth && ZwjSequences == other.ZwjSequences && FlagWidth == other.FlagWidth && TextPresentationWidth == other.TextPresentationWidth && Source == other.Source;
        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as TerminalWidthProfile);
        /// <inheritdoc />
        public override int GetHashCode() => (((((EmojiWidth * 397) ^ AmbiguousWidth) * 397 ^ (int)ZwjSequences) * 397 ^ FlagWidth) * 397 ^ TextPresentationWidth) * 397 ^ Source.GetHashCode();
        /// <inheritdoc />
        public override string ToString() => "emoji=" + EmojiWidth + ", ambiguous=" + AmbiguousWidth + ", zwj=" + ZwjSequences + ", flag=" + FlagWidth + ", text=" + TextPresentationWidth + " (" + Source + ")";
    }
    /// <summary>Whether a terminal joins or separately renders emoji ZWJ components.</summary>
    public enum ZwjRendering {
        /// <summary>Count an emoji ZWJ sequence as one cluster.</summary>
        Joined,
        /// <summary>Sum the widths of the sequence's components.</summary>
        Split
    }
}
