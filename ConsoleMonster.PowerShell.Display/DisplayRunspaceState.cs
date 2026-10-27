using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell.Display;

internal sealed class DisplayRunspaceState {
    private readonly Runspace _owner;
    private readonly List<ModuleLease> _leases = new List<ModuleLease>();
    private readonly List<DefaultDisplayRegistration> _views = new List<DefaultDisplayRegistration>();
    private readonly Dictionary<ModuleLease, Dictionary<string, Func<IReadOnlyList<object>, DefaultDisplayContext, IWidget>>> _builders = new Dictionary<ModuleLease, Dictionary<string, Func<IReadOnlyList<object>, DefaultDisplayContext, IWidget>>>();
    private readonly Dictionary<string, DefaultDisplayMode> _modes = new Dictionary<string, DefaultDisplayMode>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _diagnostics = new HashSet<string>(StringComparer.Ordinal);
    private DefaultDisplayMode? _allMode;
    private ScriptBlock? _installed;
    private ScriptBlock? _modeFunction;

    internal DisplayRunspaceState(Runspace owner) {
        _owner = owner;
        owner.StateChanged += OwnerStateChanged;
    }
    private void OwnerStateChanged(object? sender, RunspaceStateEventArgs args) {
        if (args.RunspaceStateInfo.State != RunspaceState.Closed && args.RunspaceStateInfo.State != RunspaceState.Broken) { return; }
        _views.Clear(); _leases.Clear(); _builders.Clear(); _installed = null;
        _owner.StateChanged -= OwnerStateChanged;
    }
    internal bool IsInstalled {
        get {
            if (_installed == null || Runspace.DefaultRunspace != _owner) { return false; }
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)) {
                var output = ps.AddScript("$c = $ExecutionContext.InvokeCommand.GetCommand('Out-Default', [System.Management.Automation.CommandTypes]::All); if ($c -is [System.Management.Automation.FunctionInfo]) { $c.ScriptBlock }").Invoke();
                return !ps.HadErrors && output.Count == 1 && ReferenceEquals(output[0].BaseObject, _installed);
            }
        }
    }
    internal bool OwnsBroker(CommandInfo? command) => _installed != null && command is FunctionInfo function
        && string.Equals(function.Name, "Out-Default", StringComparison.OrdinalIgnoreCase)
        && ReferenceEquals(function.ScriptBlock, _installed);
    internal void Acquire(ModuleLease lease) {
        _leases.Add(lease);
        // Bind only the actual Type in a private module. GetNewClosure also captures the invoking
        // pipeline's preference variables, which would freeze verbose/error behavior at registration.
        if (_modeFunction == null) {
            using (Stream resource = typeof(DefaultDisplay).Assembly.GetManifestResourceStream("ConsoleMonster.PowerShell.Display.DisplayMode.ps1")!)
            using (StreamReader reader = new StreamReader(resource))
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)) {
                var result = ps.AddScript("param($body, $cmDisplayType) if (-not $ExecutionContext.InvokeCommand.GetCommand('Set-ConsoleDefaultDisplay', [System.Management.Automation.CommandTypes]::All)) { Microsoft.PowerShell.Management\\Set-Item -LiteralPath function:global:Set-ConsoleDefaultDisplay -Value ((Microsoft.PowerShell.Core\\New-Module -ScriptBlock {param($cmDisplayType)} -ArgumentList $cmDisplayType).NewBoundScriptBlock([scriptblock]::Create($body))); ($ExecutionContext.InvokeCommand.GetCommand('Set-ConsoleDefaultDisplay', [System.Management.Automation.CommandTypes]::Function)).ScriptBlock }").AddArgument(reader.ReadToEnd()).AddArgument(typeof(DefaultDisplay)).Invoke();
                if (!ps.HadErrors && result.Count == 1) { _modeFunction = result[0].BaseObject as ScriptBlock; }
            }
        }
        if (!lease.IsCompatible && Diagnostic("feature:" + lease.ModuleName)) {
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)) {
                ps.AddCommand("Microsoft.PowerShell.Utility\\Write-Verbose").AddParameter("Message", "ConsoleMonster default views for '" + lease.ModuleName + "' require a newer display feature level. Update ConsoleMonster and start a new session; native views remain available.").Invoke();
            }
        }
        if (!lease.IsCompatible || Environment.GetEnvironmentVariable("CONSOLEMONSTER_DEFAULT_VIEWS") == "0" || _installed != null) { return; }
        using (Stream resource = typeof(DefaultDisplay).Assembly.GetManifestResourceStream("ConsoleMonster.PowerShell.Display.OutDefault.ps1")
            ?? throw new InvalidOperationException("The native broker resource is missing."))
        using (StreamReader reader = new StreamReader(resource))
        using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)) {
            var result = ps.AddScript("param($body, $cmDisplayType) $c = $ExecutionContext.InvokeCommand.GetCommand('Out-Default', [System.Management.Automation.CommandTypes]::All); if ($c -is [System.Management.Automation.CmdletInfo] -and $c.ModuleName -eq 'Microsoft.PowerShell.Core') { $body = (Microsoft.PowerShell.Core\\New-Module -ScriptBlock {param($cmDisplayType)} -ArgumentList $cmDisplayType).NewBoundScriptBlock([scriptblock]::Create($body)); Microsoft.PowerShell.Management\\Set-Item -LiteralPath function:global:Out-Default -Value $body; ($ExecutionContext.InvokeCommand.GetCommand('Out-Default', [System.Management.Automation.CommandTypes]::Function)).ScriptBlock }")
                .AddArgument(reader.ReadToEnd()).AddArgument(typeof(DefaultDisplay)).Invoke();
            if (!ps.HadErrors && result.Count == 1) { _installed = result[0].BaseObject as ScriptBlock; }
        }
    }
    internal void Release(ModuleLease lease) {
        _views.RemoveAll(item => ReferenceEquals(item.Lease, lease));
        _builders.Remove(lease);
        _leases.Remove(lease);
        if (Runspace.DefaultRunspace != _owner) { return; } // Never execute another runspace's cleanup in this one.
        if (_leases.Count == 0 && _modeFunction != null) {
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)) {
                ps.AddScript("param($owned) $c = $ExecutionContext.InvokeCommand.GetCommand('Set-ConsoleDefaultDisplay', [System.Management.Automation.CommandTypes]::All); if ($c -is [System.Management.Automation.FunctionInfo] -and [object]::ReferenceEquals($c.ScriptBlock, $owned)) { Microsoft.PowerShell.Management\\Remove-Item -LiteralPath function:Set-ConsoleDefaultDisplay }").AddArgument(_modeFunction).Invoke();
                if (!ps.HadErrors) { _modeFunction = null; }
            }
        }
        if (_leases.Any(item => item.IsCompatible) || _installed == null) { return; }
        using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)) {
            ps.AddScript("param($owned) $c = $ExecutionContext.InvokeCommand.GetCommand('Out-Default', [System.Management.Automation.CommandTypes]::All); if ($c -is [System.Management.Automation.FunctionInfo] -and [object]::ReferenceEquals($c.ScriptBlock, $owned)) { Microsoft.PowerShell.Management\\Remove-Item -LiteralPath function:Out-Default }").AddArgument(_installed).Invoke();
            if (!ps.HadErrors) { _installed = null; }
        }
    }
    internal void Add(DefaultDisplayRegistration registration) {
        // Retain equal/lower implementations as lease backups. They never replace an earlier equal/higher view.
        _views.Add(registration);
    }
    internal void Remove(ModuleLease lease, string typeName) => _views.RemoveAll(view => ReferenceEquals(view.Lease, lease) && view.TypeName == typeName);
    internal void AddBuilder(ModuleLease lease, string name, Func<IReadOnlyList<object>, DefaultDisplayContext, IWidget> builder) {
        if (!_builders.TryGetValue(lease, out var builders)) { builders = new Dictionary<string, Func<IReadOnlyList<object>, DefaultDisplayContext, IWidget>>(StringComparer.Ordinal); _builders.Add(lease, builders); }
        builders[name] = builder;
    }
    internal Func<IReadOnlyList<object>, DefaultDisplayContext, IWidget> Builder(ModuleLease lease, string name) {
        if (_builders.TryGetValue(lease, out var builders) && builders.TryGetValue(name, out var builder)) { return builder; }
        throw new ArgumentException("The module has not registered builder '" + name + "'.");
    }
    internal DefaultDisplayRegistration? Find(PSObject value) {
        foreach (string name in value.TypeNames) {
            if (name.StartsWith("Deserialized.", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Microsoft.PowerShell.Commands.Internal.Format.", StringComparison.Ordinal)) { return null; }
        }
        object target = value.BaseObject;
        if (target is System.Collections.IDictionary) { return null; }
        Type? runtimeType = target?.GetType();
        string? typeName = runtimeType?.FullName;
        if (typeName == null) { return null; }
        DefaultDisplayRegistration? best = null;
        foreach (DefaultDisplayRegistration view in _views) {
            if (!view.Lease.IsCompatible || view.TypeName != typeName || view.RuntimeType != null && view.RuntimeType != runtimeType
                || Mode(view.Lease) == DefaultDisplayMode.Off) { continue; }
            if (best == null || view.Lease.Source > best.Lease.Source
                || (view.Lease.Source == best.Lease.Source && view.ViewId == best.ViewId && view.Version.Major == best.Version.Major && view.Version > best.Version)) {
                best = view;
            }
        }
        if (best?.Lease.Source != DefaultDisplaySource.User && _leases.Any(lease => lease.IsCompatible && Mode(lease) != DefaultDisplayMode.Off && lease.NativeOverride?.Invoke(value) == true)) { return null; }
        return best;
    }
    internal IDefaultDisplayNativeSink CreateNativeSink(PSCmdlet owner, bool topLevel, bool transcript) {
        ModuleLease? joined = _leases.LastOrDefault(lease => lease.IsCompatible && Mode(lease) != DefaultDisplayMode.Off && lease.NativeSinkFactory != null);
        return joined?.NativeSinkFactory?.Invoke(owner, topLevel, transcript) ?? new NativeDefaultDisplaySink(owner, transcript);
    }
    internal void SetMode(DefaultDisplayMode mode, string? module) {
        if (!Enum.IsDefined(typeof(DefaultDisplayMode), mode)) { throw new ArgumentOutOfRangeException(nameof(mode)); }
        if (module == null) { _allMode = mode; _modes.Clear(); } else { _modes[module] = mode; }
    }
    internal DefaultDisplayMode Mode(ModuleLease lease) {
        if (Environment.GetEnvironmentVariable("CONSOLEMONSTER_DEFAULT_VIEWS") == "0") { return DefaultDisplayMode.Off; }
        return _modes.TryGetValue(lease.ModuleName, out DefaultDisplayMode mode) ? mode : _allMode ?? lease.Mode;
    }
    internal bool Diagnostic(string key) => _diagnostics.Add(key);
    internal static void ValidateType(string name, DefaultDisplaySource source) {
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(new[] { '*', '?', '[', ']' }) >= 0 || name.IndexOf('.') < 1
            || name.StartsWith("Deserialized.", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Microsoft.PowerShell.Commands.Internal.Format.", StringComparison.Ordinal)
            || (source == DefaultDisplaySource.Product && (name.StartsWith("System.", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.PowerShell.", StringComparison.Ordinal)
                || name.IndexOf("PSCustomObject", StringComparison.OrdinalIgnoreCase) >= 0))) {
            throw new ArgumentException("Views require an exact owned CLR type name; product views cannot register system, formatting, deserialized or wildcard types.", nameof(name));
        }
        Type? loaded = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name, false)).FirstOrDefault(type => type != null);
        if (source == DefaultDisplaySource.Product && loaded != null && (typeof(System.Collections.IDictionary).IsAssignableFrom(loaded)
            || loaded.GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)))) {
            throw new ArgumentException("Product dictionary registrations are not supported.", nameof(name));
        }
    }
}
