using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Views;

namespace ConsoleMonster.Frames;

/// <summary>
/// The size and length policy for one frame render. Every number comes from the terminal size at render time, so
/// frames on a laptop and on a wide screen follow the same rules. Authors read it; they never read the console.
/// </summary>
public sealed class FrameScale {
    /// <summary>Width used when the terminal size is not known.</summary>
    public const int UnknownWidth = 120;
    /// <summary>Height used when the terminal size is not known.</summary>
    public const int UnknownHeight = 50;
    /// <summary>Environment variable that sets the default length for hosts without a profile.</summary>
    public const string LengthVariable = "CONSOLEMONSTER_VIEW_LENGTH";

    private FrameScale(int width, int height, bool known, ConsoleViewLength length) {
        Width = width; Height = height; IsSizeKnown = known; Length = length;
    }

    /// <summary>Creates the policy for a profile. A missing length uses <see cref="LengthVariable"/>, then Standard.</summary>
    /// <param name="profile">The destination profile.</param><param name="length">An explicit length.</param><returns>The policy.</returns>
    public static FrameScale For(TerminalProfile profile, ConsoleViewLength? length = null) {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        bool known = profile.Width > 0 && profile.Height > 0 && !profile.IsOutputRedirected;
        return new FrameScale(known ? profile.Width : UnknownWidth, known ? profile.Height : UnknownHeight, known, ResolveLength(length));
    }

    /// <summary>Creates the policy for an explicit size, for previews and tests.</summary>
    /// <param name="width">Terminal width.</param><param name="height">Terminal height.</param><param name="length">Length.</param><returns>The policy.</returns>
    public static FrameScale Of(int width, int height, ConsoleViewLength length = ConsoleViewLength.Standard) =>
        new FrameScale(width > 0 ? width : UnknownWidth, height > 0 ? height : UnknownHeight, width > 0 && height > 0, length);

    /// <summary>Parses a length name case-insensitively.</summary>
    /// <param name="value">The text.</param><param name="length">The parsed length.</param><returns>Whether the text named a length.</returns>
    public static bool TryParseLength(string? value, out ConsoleViewLength length) {
        length = ConsoleViewLength.Standard;
        if (string.IsNullOrWhiteSpace(value)) return false;
        foreach (ConsoleViewLength candidate in new[] { ConsoleViewLength.Summary, ConsoleViewLength.Standard, ConsoleViewLength.Full }) {
            if (string.Equals(value!.Trim(), candidate.ToString(), StringComparison.OrdinalIgnoreCase)) { length = candidate; return true; }
        }
        return false;
    }

    private static ConsoleViewLength ResolveLength(ConsoleViewLength? length) {
        if (length.HasValue) return length.Value;
        return TryParseLength(Environment.GetEnvironmentVariable(LengthVariable), out ConsoleViewLength value) ? value : ConsoleViewLength.Standard;
    }

    /// <summary>Terminal width in cells.</summary>
    public int Width { get; }
    /// <summary>Terminal height in rows.</summary>
    public int Height { get; }
    /// <summary>Whether the size came from a real terminal.</summary>
    public bool IsSizeKnown { get; }
    /// <summary>The length for this render.</summary>
    public ConsoleViewLength Length { get; }
    /// <summary>Usable width: one gutter cell on the left and one free cell on the right.</summary>
    public int ContentWidth => Math.Max(0, Width - 2);
    /// <summary>Meter tiles per line (28 cells plus a 2-cell gap each).</summary>
    public int MeterTiles => Math.Max(1, Width / 30);
    /// <summary>Whether sections stack full width instead of packing side by side.</summary>
    public bool Stacked => Width < 100;
    /// <summary>Most attention items in <see cref="ConsoleViewLength.Summary"/>; null means uncapped.</summary>
    public int? AttentionCap => Length == ConsoleViewLength.Summary ? Math.Max(5, Height - 14) : (int?)null;
    /// <summary>Most rows per detail section; null means uncapped, zero means detail is omitted.</summary>
    public int? DetailCap => Length == ConsoleViewLength.Summary ? 0 : Length == ConsoleViewLength.Standard ? 200 : (int?)null;
    /// <summary>Above this many items a detail section prints problem rows and rollups only; null never collapses.</summary>
    public int? HealthyCollapseAbove => Length == ConsoleViewLength.Full ? (int?)null : 50;
    /// <summary>Rows between repeated table headers in single-column detail.</summary>
    public int HeaderRepeat => Math.Max(20, Height - 6);

    /// <summary>How many columns a detail table of this minimum width flows into.</summary>
    /// <param name="minimumWidth">The table's minimum width.</param><returns>One to three columns.</returns>
    public int DetailColumns(int minimumWidth) => Math.Max(1, Math.Min(3, (ContentWidth + 4) / (Math.Max(1, minimumWidth) + 4)));

    /// <summary>How many matrix columns fit after a label block.</summary>
    /// <param name="labelWidth">Cells taken by row labels.</param><param name="cellWidth">Cells per column including its gap.</param><param name="width">Available width, or the content width.</param>
    /// <returns>The column capacity.</returns>
    public int MatrixCapacity(int labelWidth, int cellWidth, int? width = null) => Math.Max(0, ((width ?? ContentWidth) - labelWidth) / Math.Max(1, cellWidth));
}
