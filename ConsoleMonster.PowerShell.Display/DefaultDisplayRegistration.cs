using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell.Display;

/// <summary>Exact-type view registration with immutable identity and an optional streaming summary.</summary>
public sealed class DefaultDisplayRegistration {
    internal DefaultDisplayRegistration(ModuleLease lease, string typeName, string viewId, Version version,
        DefaultDisplayBatch batch, IEnumerable<string>? scopeKey,
        Func<IReadOnlyList<object>, DefaultDisplayContext, IWidget> builder, DefaultDisplayBudget budget, Func<object, string>? scopeSelector, Type? runtimeType) {
        Lease = lease;
        TypeName = typeName;
        ViewId = viewId;
        Version = version;
        Batch = batch;
        ScopeKey = new List<string>(scopeKey ?? Array.Empty<string>()).AsReadOnly();
        Builder = builder;
        Budget = budget;
        ScopeSelector = scopeSelector;
        RuntimeType = runtimeType;
    }
    internal ModuleLease Lease { get; }
    internal Func<IReadOnlyList<object>, DefaultDisplayContext, IWidget> Builder { get; }
    /// <summary>Exact CLR name owned by the registering module.</summary>
    public string TypeName { get; }
    /// <summary>Optional exact CLR type identity for a compiled builder. Null permits data-only name matching across assembly contexts.</summary>
    public Type? RuntimeType { get; }
    /// <summary>Stable identity shared across compatible module imports.</summary>
    public string ViewId { get; }
    /// <summary>Compatible view implementation version.</summary>
    public Version Version { get; }
    /// <summary>Batch delivery policy.</summary>
    public DefaultDisplayBatch Batch { get; }
    /// <summary>Data property names separating consecutive batches.</summary>
    public IReadOnlyList<string> ScopeKey { get; }
    /// <summary>Optional pure selector for external immutable run context. It never changes the input object.</summary>
    public Func<object, string>? ScopeSelector { get; }
    /// <summary>Per-view collection and layout limits.</summary>
    public DefaultDisplayBudget Budget { get; }
    /// <summary>Optional product aggregate for streamed groups; null prints the group row count.</summary>
    public DefaultDisplayStreaming? Streaming { get; set; }
    /// <summary>Optional Stream-only presentation for a scope that closes with one original record. Configure before the scope starts.</summary>
    public Func<object, DefaultDisplayContext, IWidget>? SingleRecordBuilder { get; set; }
    /// <summary>Optional presentation for a Stream sample that closes as a collected scope.</summary>
    public Func<IReadOnlyList<object>, DefaultDisplayContext, IWidget>? CollectedBuilder { get; set; }
    /// <summary>Optional header for an open Stream; its verdict must remain unknown until completion.</summary>
    public Func<object, DefaultDisplayContext, IWidget>? StreamingHeaderBuilder { get; set; }
    /// <summary>Builds without host I/O, for explicit preview and presentation qualification.</summary>
    public IWidget Build(IReadOnlyList<object> objects, DefaultDisplayContext context) => Builder(objects, context);
}

/// <summary>Bounded collection and declarative layout limits.</summary>
public sealed class DefaultDisplayBudget {
    /// <summary>Creates positive limits; collection limits cannot exceed the broker ceiling.</summary>
    public DefaultDisplayBudget(int records = 2000, long bytes = 16 * 1024 * 1024, int nodes = 400, int depth = 3, int children = 25) {
        if (records < 1 || records > 2000 || bytes < 1 || bytes > 16 * 1024 * 1024 || nodes < 1 || nodes > 400 || depth < 0 || depth > 64 || children < 1 || children > 25) {
            throw new ArgumentOutOfRangeException(nameof(records), "View budgets must be positive and within the broker ceilings.");
        }
        Records = records; Bytes = bytes; Nodes = nodes; Depth = depth; Children = children;
    }
    /// <summary>Maximum held records.</summary>
    public int Records { get; }
    /// <summary>Maximum estimated held bytes.</summary>
    public long Bytes { get; }
    /// <summary>Maximum rendered tree nodes.</summary>
    public int Nodes { get; }
    /// <summary>Maximum tree depth.</summary>
    public int Depth { get; }
    /// <summary>Maximum children per tree node.</summary>
    public int Children { get; }
}
