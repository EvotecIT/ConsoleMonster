using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell.Display;

/// <summary>A runspace-owned delegate for records outside product default views.</summary>
public interface IDefaultDisplayNativeSink : IDisposable {
    /// <summary>Begins this sink invocation.</summary>
    void Begin();
    /// <summary>Processes an unchanged original input wrapper.</summary>
    void Process(PSObject value);
    /// <summary>Flushes and normally completes this sink.</summary>
    void End();
}

internal sealed class NativeDefaultDisplaySink : IDefaultDisplayNativeSink {
    private readonly PSCmdlet _owner;
    private readonly SteppablePipeline _pipeline;
    internal NativeDefaultDisplaySink(PSCmdlet owner, bool transcript) {
        _owner = owner;
        _pipeline = ScriptBlock.Create(transcript ? "Microsoft.PowerShell.Core\\Out-Default -Transcript" : "Microsoft.PowerShell.Core\\Out-Default").GetSteppablePipeline(CommandOrigin.Internal);
    }
    public void Begin() => _pipeline.Begin(_owner);
    public void Process(PSObject value) => _pipeline.Process(value);
    public void End() => _pipeline.End();
    public void Dispose() => _pipeline.Dispose();
}
