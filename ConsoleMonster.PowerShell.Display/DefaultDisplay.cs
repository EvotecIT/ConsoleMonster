using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;

namespace ConsoleMonster.PowerShell.Display;

/// <summary>Optional runspace-owned default display. Does not load the command module or install a dependency resolver.</summary>
public static class DefaultDisplay {
    private static readonly ConditionalWeakTable<Runspace, DisplayRunspaceState> Sessions = new ConditionalWeakTable<Runspace, DisplayRunspaceState>();
    /// <summary>Stable assembly ABI epoch, independent of package versions.</summary>
    public const int AbiEpoch = 1;
    /// <summary>Implemented adapter feature level.</summary>
    public const int FeatureLevel = 1;
    internal static DisplayRunspaceState Current => Sessions.GetValue(Runspace.DefaultRunspace
        ?? throw new InvalidOperationException("Default display registration requires an owning PowerShell runspace."), owner => new DisplayRunspaceState(owner));
    /// <summary>Acquires one module import lease. Call from the owning runspace and dispose on removal.</summary>
    public static ModuleLease RegisterModule(string moduleName, Version moduleVersion, IEnumerable<string> ownedTypeNames,
        int minimumFeatureLevel = 1, DefaultDisplaySource source = DefaultDisplaySource.Product) {
        if (string.IsNullOrWhiteSpace(moduleName)) { throw new ArgumentException("A module identity is required.", nameof(moduleName)); }
        if (moduleVersion == null) { throw new ArgumentNullException(nameof(moduleVersion)); }
        if (ownedTypeNames == null) { throw new ArgumentNullException(nameof(ownedTypeNames)); }
        if (minimumFeatureLevel < 1) { throw new ArgumentOutOfRangeException(nameof(minimumFeatureLevel)); }
        if (!Enum.IsDefined(typeof(DefaultDisplaySource), source)) { throw new ArgumentOutOfRangeException(nameof(source)); }
        ModuleLease lease = new ModuleLease(Current, moduleName, moduleVersion, ownedTypeNames, minimumFeatureLevel, source);
        Current.Acquire(lease);
        return lease;
    }
    /// <summary>Sets the explicit user preference, for one module or all modules when null.</summary>
    public static void SetMode(DefaultDisplayMode mode, string? moduleName = null) => Current.SetMode(mode, moduleName);
    /// <summary>Creates a broker sink. Automatic host provenance is supplied only by the embedded proxy.</summary>
    public static DefaultDisplaySession Begin(PSCmdlet sink, bool topLevel, bool transcript) => new DefaultDisplaySession(Current, sink, topLevel, transcript);
    /// <summary>True when the effective Out-Default is still this runspace's installed broker.</summary>
    public static bool IsBrokerInstalled => Current.IsInstalled;
    /// <summary>Checks the actual downstream command against this runspace's owned broker instance; copied definitions are not owned.</summary>
    public static bool IsOwnedBroker(CommandInfo? command) => Current.OwnsBroker(command);
    /// <summary>Inspects the effective exact-type registration without rendering or changing the object.</summary>
    public static DefaultDisplayRegistration? Resolve(PSObject value) => Current.Find(value ?? throw new ArgumentNullException(nameof(value)));
    /// <summary>Creates a native or explicitly joined command-module sink for one contiguous delegated segment.</summary>
    public static IDefaultDisplayNativeSink CreateNativeSink(PSCmdlet owner, bool topLevel, bool transcript) => Current.CreateNativeSink(owner, topLevel, transcript);
}
