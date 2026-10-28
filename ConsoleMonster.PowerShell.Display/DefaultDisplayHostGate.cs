using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.InteropServices;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.PowerShell.Display;

/// <summary>Conservative host routing shared by embedded default display consumers.</summary>
public static class DefaultDisplayHostGate {
    /// <summary>Returns false when transcript state cannot be positively read as inactive.</summary>
    public static bool IsTranscriptInactive(object? hostUserInterface) {
        if (hostUserInterface == null) { return false; }
        try {
            PropertyInfo? property = hostUserInterface.GetType().GetProperty("IsTranscribing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property != null && property.PropertyType == typeof(bool) && property.GetIndexParameters().Length == 0
                && property.GetValue(hostUserInterface, null) is bool active && !active;
        } catch (Exception) { return false; }
    }
    /// <summary>Accepts only an automatic local ConsoleHost sink with known inactive recording.</summary>
    public static bool CanRender(PSCmdlet sink, bool topLevel, bool transcript, TerminalProfile profile) {
        if (!topLevel || transcript || sink == null || sink.Host?.Name != "ConsoleHost"
            || !IsTranscriptInactive(sink.Host.UI) || Runspace.DefaultRunspace == null
            || Runspace.DefaultRunspace.ConnectionInfo != null
            || !SupportsPhysicalProfile(profile)
            || Environment.GetEnvironmentVariable("CONSOLEMONSTER_DEFAULT_VIEWS") == "0") { return false; }
        foreach (string name in new[] { "CI", "TF_BUILD", "GITHUB_ACTIONS", "GITLAB_CI", "CODEX_CI" }) {
            string? value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(value) && value != "0" && !value.Equals("false", StringComparison.OrdinalIgnoreCase)) { return false; }
        }
        try {
            return !Console.IsOutputRedirected && !Console.IsErrorRedirected && Terminal.IsConsole(Terminal.Target)
                && sink.SessionState.PSVariable.GetValue("PSSenderInfo") == null;
        } catch (Exception) { return false; }
    }
    /// <summary>Checks profile safety independently of colour mode; real runspace/host checks still apply.</summary>
    public static bool SupportsPhysicalProfile(TerminalProfile profile) => profile != null
        && !profile.IsOutputRedirected && !profile.IsContinuousIntegration && profile.Kind != TerminalKind.VisualStudioCode
        // IsConsoleHandle describes GetConsoleMode on Windows; a Unix tty never sets it.
        // Read the actual stdout route so an overridden profile cannot turn a pipe into a tty.
        && (profile.IsConsoleHandle || IsUnixTerminal());

    private static bool IsUnixTerminal() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { return false; }
        try { return !Console.IsOutputRedirected; } catch (Exception) { return false; }
    }
}
