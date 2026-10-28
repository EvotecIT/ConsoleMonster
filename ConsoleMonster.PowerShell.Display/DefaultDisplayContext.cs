using System.Globalization;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.PowerShell.Display;

/// <summary>Display choice for a module in the current runspace.</summary>
public enum DefaultDisplayMode {
    /// <summary>Use capabilities and user settings.</summary>
    Auto,
    /// <summary>Keep layout without colours or decoration.</summary>
    Plain,
    /// <summary>Use native PowerShell formatting.</summary>
    Off
}

/// <summary>How consecutive objects of one view and scope are delivered.</summary>
public enum DefaultDisplayBatch {
    /// <summary>Build once per object.</summary>
    Single,
    /// <summary>Sample within the row, time and byte limits, then deliver an append-only table; a fast completed sample can use a collected builder.</summary>
    Stream,
    /// <summary>Collect consecutive objects within the registration budget.</summary>
    Collect
}

/// <summary>Registration precedence; products never enable everyday system views.</summary>
public enum DefaultDisplaySource {
    /// <summary>Views shipped by the assembly that owns the data type.</summary>
    Product,
    /// <summary>Views explicitly enabled by the ConsoleMonster command module.</summary>
    CommandModule,
    /// <summary>Explicit runspace user override.</summary>
    User
}

/// <summary>Pure builder inputs shared with product presentation code.</summary>
public sealed class DefaultDisplayContext {
    /// <summary>Creates builder context without depending on a PowerShell host.</summary>
    public DefaultDisplayContext(TerminalProfile profile, string scope, DefaultDisplayMode mode) {
        Profile = profile;
        Scope = scope;
        Mode = mode;
    }
    /// <summary>Capability profile used for this render.</summary>
    public TerminalProfile Profile { get; }
    /// <summary>Available terminal cells.</summary>
    public int Width => Profile.Width;
    /// <summary>Session number and date culture.</summary>
    public CultureInfo Culture => Profile.Culture ?? CultureInfo.CurrentCulture;
    /// <summary>Stable scope identity for this batch.</summary>
    public string Scope { get; }
    /// <summary>Effective mode after user preferences.</summary>
    public DefaultDisplayMode Mode { get; }
}
