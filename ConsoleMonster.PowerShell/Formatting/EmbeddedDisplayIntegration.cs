using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections;
using System.IO;
using ConsoleMonster.PowerShell.Display;

namespace ConsoleMonster.PowerShell;

// Keeps the command module's existing formatting implementation behind the shared optional broker.
internal static class EmbeddedDisplayIntegration {
    internal static bool TryReadV2(PSCmdlet source, string path, out ConsoleViewDocument? document) {
        document = null;
        if (!path.EndsWith(".consoleviews.psd1", StringComparison.OrdinalIgnoreCase)) { return false; }
        var output = source.InvokeCommand.InvokeScript("param($path) Microsoft.PowerShell.Utility\\Import-PowerShellDataFile -LiteralPath $path -ErrorAction Stop", true, PipelineResultTypes.None, null, path);
        if (output.Count != 1 || !(output[0].BaseObject is IDictionary data) || Convert.ToInt32(data["SchemaVersion"], System.Globalization.CultureInfo.InvariantCulture) != 2) { return false; }
        document = ConsoleViewDocument.Parse(data);
        return true;
    }
    internal static bool TryImportV2(PSCmdlet source, string path, bool onlyIfMissing) {
        if (!TryReadV2(source, path, out ConsoleViewDocument? document)) { return false; }
        FormattingSessionState state = FormattingSessionState.Current;
        string key = Path.GetFullPath(path);
        EmbeddedDisplayFile file = new EmbeddedDisplayFile(Path.GetFileNameWithoutExtension(path), document!,
            onlyIfMissing ? DefaultDisplaySource.CommandModule : DefaultDisplaySource.User);
        file.SetEnabled((state.Features & ConsoleFormattingFeature.Output) != 0);
        if (state.DisplayFiles.TryGetValue(key, out EmbeddedDisplayFile? prior)) { prior.Dispose(); }
        state.DisplayFiles[key] = file;
        ConsoleFormatting.RestoreCommandsWithModule(source.MyInvocation.MyCommand.Module);
        return true;
    }
    internal static bool Unregister(string typeName) {
        bool removed = false;
        foreach (EmbeddedDisplayFile file in FormattingSessionState.Current.DisplayFiles.Values) { removed |= file.Unregister(typeName); }
        return removed;
    }
    internal static void SetOutputEnabled(bool enabled) {
        foreach (EmbeddedDisplayFile file in FormattingSessionState.Current.DisplayFiles.Values) { file.SetEnabled(enabled); }
    }
    internal static void Join() {
        FormattingSessionState state = FormattingSessionState.Current;
        if (state.DisplayLease != null) { return; }
        ModuleLease lease = DefaultDisplay.RegisterModule("ConsoleMonster", typeof(EmbeddedDisplayIntegration).Assembly.GetName().Version ?? new Version(1, 0),
            Array.Empty<string>(), source: DefaultDisplaySource.CommandModule);
        lease.JoinCommandModule((sink, topLevel, transcript) => new CommandModuleSink(sink, topLevel, transcript),
            value => (ConsoleFormatting.Features & ConsoleFormattingFeature.Output) != 0 && ConsoleFormatting.Find(value, false) != null);
        state.DisplayLease = lease;
    }
    private sealed class CommandModuleSink : IDefaultDisplayNativeSink {
        private readonly PSCmdlet _sink;
        private readonly bool _topLevel;
        private readonly bool _transcript;
        private ConsoleDefaultOutput? _output;
        internal CommandModuleSink(PSCmdlet sink, bool topLevel, bool transcript) { _sink = sink; _topLevel = topLevel; _transcript = transcript; }
        public void Begin() => _output = new ConsoleDefaultOutput(_sink, new ConsoleDefaultOutputOptions { Shim = true, TopLevel = _topLevel, Transcript = _transcript, HelpSink = _sink });
        public void Process(PSObject value) => _output!.Write(value);
        public void End() => _output?.Complete();
        public void Dispose() => _output?.Dispose();
    }
}
