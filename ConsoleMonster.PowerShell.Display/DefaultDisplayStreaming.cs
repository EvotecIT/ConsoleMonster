using System;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell.Display;

/// <summary>Optional bounded aggregate for one contiguous streamed scope. Callbacks stay pure and never retain all rows.</summary>
public sealed class DefaultDisplayStreaming {
    /// <summary>Creates streaming summary callbacks over product-owned aggregate state.</summary>
    public DefaultDisplayStreaming(Func<object?> seed, Func<object?, object, object?> aggregate,
        Func<object?, long, DefaultDisplayContext, IWidget?> summary) {
        Seed = seed ?? throw new ArgumentNullException(nameof(seed));
        Aggregate = aggregate ?? throw new ArgumentNullException(nameof(aggregate));
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
    }
    /// <summary>Creates fresh state when the scope opens.</summary>
    public Func<object?> Seed { get; }
    /// <summary>Consumes one base object and returns updated bounded state.</summary>
    public Func<object?, object, object?> Aggregate { get; }
    /// <summary>Builds one summary when the scope closes normally.</summary>
    public Func<object?, long, DefaultDisplayContext, IWidget?> Summary { get; }
}
