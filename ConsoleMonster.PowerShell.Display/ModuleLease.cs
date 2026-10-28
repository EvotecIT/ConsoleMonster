using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell.Display;

/// <summary>A module's registrations and defaults in one runspace. Dispose on module removal.</summary>
public sealed class ModuleLease : IDisposable {
    private readonly HashSet<string> _ownedTypes;
    internal ModuleLease(DisplayRunspaceState state, string moduleName, Version version, IEnumerable<string> ownedTypeNames,
        int minimumFeatureLevel, DefaultDisplaySource source) {
        State = state; ModuleName = moduleName; Version = version; Source = source;
        MinimumFeatureLevel = minimumFeatureLevel;
        _ownedTypes = new HashSet<string>(ownedTypeNames, StringComparer.Ordinal);
        foreach (string name in _ownedTypes) { DisplayRunspaceState.ValidateType(name, source); }
    }
    internal DisplayRunspaceState State { get; }
    internal bool Released { get; private set; }
    internal Func<PSCmdlet, bool, bool, IDefaultDisplayNativeSink>? NativeSinkFactory { get; private set; }
    internal Func<PSObject, bool>? NativeOverride { get; private set; }
    /// <summary>Owning import identity.</summary>
    public string ModuleName { get; }
    /// <summary>Owning module version.</summary>
    public Version Version { get; }
    /// <summary>Minimum display feature level required by the module.</summary>
    public int MinimumFeatureLevel { get; }
    /// <summary>Registration precedence.</summary>
    public DefaultDisplaySource Source { get; }
    /// <summary>Whether this runtime implements the module's requested feature level.</summary>
    public bool IsCompatible => MinimumFeatureLevel <= DefaultDisplay.FeatureLevel;
    /// <summary>Module default, overridden by explicit user settings.</summary>
    public DefaultDisplayMode Mode { get; set; }
    internal void ValidateOwnedType(string typeName) {
        if (Released) { throw new ObjectDisposedException(nameof(ModuleLease)); }
        if (!_ownedTypes.Contains(typeName)) { throw new ArgumentException("The type must be explicitly listed in ownedTypeNames.", nameof(typeName)); }
    }
    /// <summary>Registers a pure builder for an explicitly owned exact type name. Supply runtimeType for builders that cast to a type from a private assembly context.</summary>
    public DefaultDisplayRegistration Register(string typeName, string viewId, Version version, DefaultDisplayBatch batch,
        IEnumerable<string>? scopeKey, Func<IReadOnlyList<object>, DefaultDisplayContext, IWidget> builder,
        DefaultDisplayBudget? budget = null, Func<object, string>? scopeSelector = null, Type? runtimeType = null) {
        ValidateOwnedType(typeName);
        if (string.IsNullOrWhiteSpace(viewId)) { throw new ArgumentException("A stable view identity is required.", nameof(viewId)); }
        if (version == null) { throw new ArgumentNullException(nameof(version)); }
        if (builder == null) { throw new ArgumentNullException(nameof(builder)); }
        if (runtimeType != null && runtimeType.FullName != typeName) { throw new ArgumentException("The runtime type must have the registered exact full name.", nameof(runtimeType)); }
        if (!Enum.IsDefined(typeof(DefaultDisplayBatch), batch)) { throw new ArgumentOutOfRangeException(nameof(batch)); }
        DefaultDisplayRegistration registration = new DefaultDisplayRegistration(this, typeName, viewId, version, batch, scopeKey, builder, budget ?? new DefaultDisplayBudget(), scopeSelector, runtimeType);
        State.Add(registration);
        return registration;
    }
    /// <summary>Removes this lease's views for one exact type, preserving registrations owned by other imports.</summary>
    public void Unregister(string typeName) { ValidateOwnedType(typeName); State.Remove(this, typeName); }
    /// <summary>Registers a named compiled builder for declarative schema v2 views.</summary>
    public void RegisterBuilder(string name, Func<IReadOnlyList<object>, DefaultDisplayContext, IWidget> builder) {
        if (Released) { throw new ObjectDisposedException(nameof(ModuleLease)); }
        if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("A builder name is required.", nameof(name)); }
        State.AddBuilder(this, name, builder ?? throw new ArgumentNullException(nameof(builder)));
    }
    /// <summary>Joins the command module's existing formatting session to the shared broker. Product leases cannot replace the native sink.</summary>
    public void JoinCommandModule(Func<PSCmdlet, bool, bool, IDefaultDisplayNativeSink> factory, Func<PSObject, bool> explicitUserView) {
        if (Released) { throw new ObjectDisposedException(nameof(ModuleLease)); }
        if (Source != DefaultDisplaySource.CommandModule) { throw new InvalidOperationException("Only the explicitly enabled command module can join the native sink."); }
        NativeSinkFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        NativeOverride = explicitUserView ?? throw new ArgumentNullException(nameof(explicitUserView));
    }
    /// <summary>Releases only this import's registrations. A later custom Out-Default is preserved.</summary>
    public void Dispose() {
        if (Released) { return; }
        Released = true;
        State.Release(this);
    }
}
